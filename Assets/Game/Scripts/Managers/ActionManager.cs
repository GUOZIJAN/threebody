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
        choiceManager = ChoiceManager.Instance;
    }

    public async void UseCard(int playId, Card card)
    {
        PlayerData player = PlayerManager.Instance.GetPlayer(playId);
        if (player.energy < card.cost)  return;
        player.energy -= card.cost;

        switch (card.type)
        {
            case CardType.Broadcast :
                DoBroadcast(player, (BroadcastCard)card);
                break;
            
            case CardType.Build :
                await DoBuild(player, (BuildCard)card);
                break;

            case CardType.Strike :
                Galaxy targetGalaxy = await ChoiceManager.Instance.ChooseGalaxy();
                DoStrike(player, (StrikeCard)card, targetGalaxy);
                break;
        }

        player.handCards.Remove(card);
        // 广播卡已经在 DoBroadcast 中处理，不需要再添加到 discard 列表
        if (card.type != CardType.Broadcast)
        {
            CardManager.Instance.discard.Add(card);
        }
    }

    async public void DoBroadcast(PlayerData player,BroadcastCard card)
    {
        // 记录广播的星系
        player.lastBroadcastGalaxy = player.galaxyId;
        BroadcastCard response;
        // 处理ai广播效果
        foreach (var ai in GameManager.Instance.ais)
        {
            if(ai.data.playerId != player.playerId && ai.data.isAlive)
            {
                response = ai.Respond(player, GalaxyManager.Instance.GetGalaxy(player.galaxyId), card);
                if (response != null)
                {
                    BroadcastRes[ai.data.playerId] = response;
                }
            }
        }
        // 处理玩家广播效果,需要异步
        response = await GameManager.Instance.player.Respond(player, GalaxyManager.Instance.GetGalaxy(player.galaxyId), card);
        if (response != null)
        {
            BroadcastRes[GameManager.Instance.player.data.playerId] = response;
        }
        // 将广播卡移到已使用的广播卡列表
        CardManager.Instance.broadcastUsed.Add(card);

        if(BroadcastRes.Count == 0)
        {
            Debug.Log("没有玩家响应广播卡");
        }
        else response = BroadcastRes.Values.First(); // 这里简单地取第一个响应的广播卡来处理后续效果，实际可以根据需求设计更复杂的逻辑
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
    }

    public void DiscardBuildCard(PlayerData player, BuildCard card)
    {
        player.buildCards.Remove(card);
        CardManager.Instance.discard.Add(card);
        player.energy += card.cost / 2; //丢弃建筑卡返还一半能量
    }
}