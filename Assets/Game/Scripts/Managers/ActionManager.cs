using System.Collections.Generic;
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

    public void UseCard(int playId, Card card)
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
                DoBuild(player, (BuildCard)card);
                break;

            case CardType.Strike :
                DoStrike(player, (StrikeCard)card, choiceManager.chooseGalaxy());
                break;
        }

        player.handCards.Remove(card);
        // 广播卡已经在 DoBroadcast 中处理，不需要再添加到 discard 列表
        if (card.type != CardType.Broadcast)
        {
            CardManager.Instance.discard.Add(card);
        }
    }

    public void DoBroadcast(PlayerData player,BroadcastCard card)
    {
        // 记录广播的星系
        player.lastBroadcastGalaxy = player.galaxyId;
        // 处理广播效果
        foreach (var otherPlayer in PlayerManager.Instance.Players)
        {
            if (otherPlayer.playerId != player.playerId && otherPlayer.isAlive)
            {
                int distance = GalaxyManager.Instance.GetDistance(player.galaxyId, otherPlayer.galaxyId);
                if (distance <= card.distance)
                {
                    // 广播影响范围内的玩家
                    // 这里可以添加具体的广播效果逻辑
                    Debug.Log($"玩家{player.playerId}使用{card.cardname}广播，影响到玩家{otherPlayer.playerId}，距离：{distance}");
                }
            }
        }
        // 将广播卡移到已使用的广播卡列表
        CardManager.Instance.broadcastUsed.Add(card);
    }

    public void DoBuild(PlayerData player,BuildCard card)
    {
        if (card.effect == BuildEffect.Fly)
        {
            FlyTo(player,choiceManager.chooseGalaxy());
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