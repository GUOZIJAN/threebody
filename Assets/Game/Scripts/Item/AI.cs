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
}