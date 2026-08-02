using System.Threading.Tasks;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance;
    public PlayerData data;
    public ResBroadcast resBroadcast;
    public Card currentCard;

    private PlayerManager _players;

    private void Awake()
    {
        Instance = this;
        Services.Register(this);
    }

    private void Start()
    {
        _players = Services.Get<PlayerManager>();
    }

    public void Init()
    {
        data = _players.GetPlayer(0);
        Debug.Log($"玩家初始化完成");
    }

    async public Task<BroadcastCard> Respond(PlayerData raiser, Galaxy galaxy, BroadcastCard card, bool forceRespond = false)
    {
        resBroadcast.BroadcastText.text = forceRespond
            ? $"玩家{raiser.playerId}在你的星系{galaxy.id}使用了广播卡{card.cardname}，你必须回应！"
            : $"玩家{raiser.playerId}在星系{galaxy.id}使用了广播卡{card.cardname}，是否响应？";
        resBroadcast.galaxy = galaxy;
        resBroadcast.ForceRespond = forceRespond;
        bool response = await resBroadcast.ShowAsync();
        if (response)
        {
            Debug.Log($"玩家{data.playerId}响应了玩家{raiser.playerId}的广播卡{card.cardname}");
            return currentCard as BroadcastCard;
        }
        return null;
    }
}

