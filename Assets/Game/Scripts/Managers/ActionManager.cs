using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class ActionManager : MonoBehaviour
{
    public static ActionManager Instance;
    public ChoiceManager choiceManager;
    public List<StrikeInfo> strikeList = new List<StrikeInfo>();
    public Dictionary<int, BroadcastCard> BroadcastRes = new Dictionary<int, BroadcastCard>();

    private void Awake()
    {
        Instance = this;
        
    }
    private void Start()
    {
        choiceManager = ChoiceManager.Instance;
    }

    public async Task UseCard(int playerId, Card card)
    {
        PlayerData player = PlayerManager.Instance.GetPlayer(playerId);

        if (player.energy < card.cost)
        {
            Debug.Log("能量不足，无法使用卡牌！");
            return;
        }  
        player.energy -= card.cost;

        switch (card.type)
        {
            case CardType.Broadcast :
                await DoBroadcast(player, (BroadcastCard)card);
                break;
            
            case CardType.Build :
                await DoBuild(player, (BuildCard)card);
                break;

            case CardType.Strike :
                Galaxy targetGalaxy = await ChoiceManager.Instance.ChooseGalaxy();
                //先暂时放在这里处理，后续重构
                ChoiceManager.Instance.ClearGalaxyTcs(); // 使用完后清空TaskCompletionSource，防止重复使用
                DoStrike(player, (StrikeCard)card, targetGalaxy);
                break;
        }

        player.handCards.Remove(card);
        // 广播卡已经在 DoBroadcast 中处理，不需要再添加到 discard 列表
        if (card.type != CardType.Broadcast)
        {
            CardManager.Instance.discard.Add(card);
        }
        EventManager.OnPlayCard?.Invoke(playerId, card);  //更新UI
    }

    async public Task DoBroadcast(PlayerData player,BroadcastCard card)
    {
        // 记录广播的星系
        player.lastBroadcastGalaxy = player.galaxyId;
        BroadcastCard response;
        PlayerData responser;
        Galaxy targetGalaxy;
        BroadcastRes.Clear(); // 清空上一次广播的响应记录
        //循环，直到玩家选择一个合法的星系作为广播目标

        Debug.Log($"玩家{player.playerId}使用了广播卡{card.cardname}，正在选择广播目标星系...");
        while(true)
        {
            targetGalaxy = await ChoiceManager.Instance.ChooseGalaxy();
            if(GalaxyManager.Instance.GetDistance(player.galaxyId, targetGalaxy.id) <= card.distance)
            {
                Debug.Log($"玩家{player.playerId}选择了星系{targetGalaxy.id}作为广播目标");
                break;
            }
            else
            {
                Debug.Log("选择的星系超出广播范围，请重新选择！");
            }
        }
        // 处理ai广播效果
        foreach (var ai in GameManager.Instance.ais)
        {
            if(ai.data.playerId != player.playerId && ai.data.isAlive)
            {
                response = ai.Respond(player, targetGalaxy, card);
                if (response != null)
                {
                    BroadcastRes[ai.data.playerId] = response;
                }
            }
        }
        // 处理玩家广播效果,需要异步
        if(player.playerId != 0)
        {
            response = await GameManager.Instance.player.Respond(player, targetGalaxy, card);
            if (response != null)
            {
                BroadcastRes[GameManager.Instance.player.data.playerId] = response;
            }
        }
        
        // 将广播卡移到已使用的广播卡列表
        Debug.Log($"响应数量：{BroadcastRes.Count}");
        if(BroadcastRes.Count == 0)
        {
            CardManager.Instance.discard.Add(card);
            player.energy += 1; //没有玩家响应广播卡返还1点能量
            Debug.Log("没有玩家响应广播卡");
            UIManager.Instance.UpdateBasePanel(player.playerId);// 只需要发布方更新UI
            return;
        }
        else
        {
            CardManager.Instance.broadcastUsed.Add(card);
            //玩家可以自主选择，并有UI提示
            if(GameManager.Instance.currentPlayerId == 0)
            {
                EventManager.OnPlayerChooseBroadcast?.Invoke();
                int index = await ChoiceManager.Instance.PlayerChoose();
                Debug.Log($"玩家选择了响应{index}");
                EventManager.AfterPlayerChooseBroadcast?.Invoke();
                response = BroadcastRes[index];
            }
            //AI默认响应第一个
            else
            {
                response = BroadcastRes.Values.First(); 
            }
            responser = PlayerManager.Instance.GetPlayer(BroadcastRes.Keys.First());

            responser.energy -= response.cost; // 响应玩家需要支付响应卡的能量
            responser.handCards.Remove(response); // 响应玩家需要移除响应卡
            if(GameManager.Instance.currentPlayerId == 0)
            {
                SpawnManager.Instance.RemoveCardFromHand_Broadcast(); // 如果当前玩家是响应玩家，需要更新UI移除手牌
            }
            GameManager.Instance.CompleteBroadcast(card, response, player, responser);
            responser.handCards.Add(CardManager.Instance.Draw()); // 响应广播卡的玩家抽一张牌作为奖励
            UIManager.Instance.UpdateBasePanel(responser.playerId); // 更新UI
            UIManager.Instance.UpdateBasePanel(player.playerId);// 双方都要更新UI
        }
        
        
    }

    public async Task DoBuild(PlayerData player,BuildCard card)
    {
        if (card.effect == BuildEffect.Fly)
        {
            FlyTo(player,await ChoiceManager.Instance.ChooseGalaxy());
        }
        player.buildCards.Add(card);
    }

    public void DoStrike(PlayerData player,StrikeCard card,Galaxy galaxy)
    {
        //计算距离
        int distance = GalaxyManager.Instance.GetDistance(player.galaxyId,galaxy.id);
        StrikeInfo newStrike = new StrikeInfo()  //构造strike
        {
            cardName = card.cardname,
            attackerId = player.playerId,
            targetGalaxyId = galaxy.id,
            damage = card.damage,
            effect = card.effect,
            totalDistance = distance,
            remainSteps = distance
        };

        strikeList.Add(newStrike);
    }

    public void FlyTo(PlayerData player,Galaxy galaxy)
    {
        if(!galaxy.isAlive || galaxy.ownerPlayerId != -1)
        {
            Debug.Log("目标星系不可飞行！");
            return;
        } 
        player.galaxyId = galaxy.id;
        galaxy.ownerPlayerId = player.playerId;
        player.energy = 0;
        player.handCards.Clear();
        EventManager.OnFly?.Invoke(player, galaxy); //通知UI更新
    }

    public void DiscardBuildCard(PlayerData player, BuildCard card)
    {
        player.buildCards.Remove(card);
        CardManager.Instance.discard.Add(card);
        player.energy += card.cost / 2; //丢弃建筑卡返还一半能量
    }
}