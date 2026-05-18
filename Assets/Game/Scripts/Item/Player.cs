using System.Threading.Tasks;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance;
    public PlayerData data;
    public ResBroadcast resBroadcast;
    public Card currentCard;

    private void Awake()
    {  
        Instance = this;
    }

    public void Init()
    {
        data = PlayerManager.Instance.GetPlayer(0); // 默认玩家ID为0 
        Debug.Log($"玩家初始化完成");
    }

    async public Task<BroadcastCard> Respond(PlayerData raiser,Galaxy galaxy,BroadcastCard card)
    {
        resBroadcast.BroadcastText.text = $"玩家{raiser.playerId}在星系{galaxy.id}使用了广播卡{card.cardname}，是否响应？";
        resBroadcast.galaxy = galaxy;
        bool response = await resBroadcast.ShowAsync();
        if (response)
        {
            data.energy -= currentCard.cost;
            data.handCards.Remove(currentCard);
            SpawnManager.Instance.RemoveCardFromHand_Broadcast();
            Debug.Log($"玩家{data.playerId}响应了玩家{raiser.playerId}的广播卡{card.cardname}");
            return (BroadcastCard)currentCard;
        }
        return null;
    }
}

