using System.Threading.Tasks;
using UnityEngine;

public class AI : MonoBehaviour
{
    public PlayerData data;
    public int id;

    public void Init()
    {
        data = PlayerManager.Instance.GetPlayer(id); // 默认玩家ID为0 

        Debug.Log($"ai{id}初始化完成");
    }

    public BroadcastCard Respond(PlayerData raiser, Galaxy galaxy, BroadcastCard card)
    {
        // 简单的AI逻辑：遍历所有手牌，如果有可用的广播卡，则响应
        foreach (var handCard in data.handCards)
        {
            if (handCard.type == CardType.Broadcast && GalaxyManager.Instance.GetDistance(data.galaxyId, galaxy.id) <= ((BroadcastCard)handCard).distance)
            {
                if(data.energy >= handCard.cost)
                {
                    data.energy -= handCard.cost;
                    data.handCards.Remove(handCard);
                    Debug.Log($"AI玩家{data.playerId}响应了玩家{raiser.playerId}的广播卡{card.cardname}");
                    return (BroadcastCard)handCard;
                }
            }
        }
        Debug.Log($"AI玩家{data.playerId}没有响应玩家{raiser.playerId}的广播卡{card.cardname}");
        return null;
    }

    public async Task TurnStart()
    {
        // AI的回合开始时可以执行一些自动操作
        //遍历手牌，碰到可用牌就使用，星系选择随机，但尽可能远离当前所在星系
        for(int i = data.handCards.Count - 1; i >= 0; i--)
        {
            var handCard = data.handCards[i];
            if(data.energy >= handCard.cost)
            {
                if(handCard.type == CardType.Broadcast)
                {
                    BroadcastCard card = (BroadcastCard)handCard;
                    Galaxy targetGalaxy = null;
                    int maxDistance = -1;
                    foreach(var galaxy in GalaxyManager.Instance.galaxyList)
                    {
                        int distance = GalaxyManager.Instance.GetDistance(data.galaxyId, galaxy.id);
                        if(distance <= card.distance && distance > maxDistance)
                        {
                            maxDistance = distance;
                            targetGalaxy = galaxy;
                        }
                    }
                    ChoiceManager.Instance.AISelectedGalaxy = targetGalaxy;
                    if(targetGalaxy != null)
                    {
                        await ActionManager.Instance.UseCard(data.playerId, card);
                    }
                }

                else if (handCard.type == CardType.Strike)
                {
                    StrikeCard card = (StrikeCard)handCard;
                    int GalaxyId = Random.Range(1,10); // 随机选择一个星系作为打击目标
                    Galaxy targetGalaxy = GalaxyManager.Instance.GetGalaxy(GalaxyId);
                    ChoiceManager.Instance.AISelectedGalaxy = targetGalaxy;

                    await ActionManager.Instance.UseCard(data.playerId, card);
                }

                else if (handCard.type == CardType.Build)
                {
                    BuildCard card = (BuildCard)handCard;
                    await ActionManager.Instance.UseCard(data.playerId, card);
                }
                
                await Task.Delay(1000); // 每使用一张牌后等待1秒，模拟思考时间
            }
        }
    }
}