using UnityEngine;

public class Player
{
    public static Player Instance;
    public PlayerData data;
    public ResBroadcast resBroadcast;
    public BroadcastCard currentBroadcastCard;

    private void Awake()
    {  
        Instance = this;
    }

    public void Init()
    {
        data = PlayerManager.Instance.GetPlayer(0); // 默认玩家ID为0 
    }

    async public void Respond(PlayerData raiser, BroadcastCard card)
    {
        resBroadcast.BroadcastText.text = $"玩家{raiser.playerId}使用了广播卡{card.cardname}，是否响应？";
        bool response = await resBroadcast.ShowAsync();
        if (response)
        {
            // 先默认玩家选择广播卡，后续再修改
            ActionManager.Instance.BroadcastRes[raiser.playerId] = currentBroadcastCard;
            Debug.Log($"玩家{data.playerId}响应了玩家{raiser.playerId}的广播卡{card.cardname}");
        }
    }
}

