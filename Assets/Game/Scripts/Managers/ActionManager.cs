using System.Collections.Generic;
using UnityEngine;

public class ActionManager : MonoBehaviour
{
    public static ActionManager Instance;
    public List<StrikeInfo> strikeList = new();
    public Dictionary<int, BroadcastCard> BroadcastRes = new();

    private PlayerManager _players;
    private GalaxyManager _galaxies;

    private void Awake()
    {
        Instance = this;
        Services.Register(this);
    }

    private void Start()
    {
        _players  = Services.Get<PlayerManager>();
        _galaxies = Services.Get<GalaxyManager>();
    }

    /// <summary>发动打击：创建 StrikeInfo 加入飞行队列</summary>
    public void ExecuteStrike(PlayerData player, StrikeCard card, Galaxy galaxy)
    {
        int distance = _galaxies.GetDistance(player.galaxyId, galaxy.id);
        strikeList.Add(new StrikeInfo
        {
            cardName       = card.cardname,
            attackerId     = player.playerId,
            targetGalaxyId = galaxy.id,
            damage         = card.damage,
            effect         = card.effect,
            totalDistance  = distance,
            remainSteps    = distance,
        });
    }

    /// <summary>建造：飞行则跃迁，否则添加到建造列表</summary>
    public void ExecuteBuild(PlayerData player, BuildCard card, Galaxy galaxy)
    {
        if (card.effect == BuildEffect.Fly && galaxy != null)
            FlyTo(player, galaxy);

        player.buildCards.Add(card);
    }

    /// <summary>跃迁到目标星系</summary>
    public void FlyTo(PlayerData player, Galaxy galaxy)
    {
        if (!galaxy.isAlive || galaxy.ownerPlayerId != -1)
        {
            Debug.Log("目标星系不可飞行！");
            return;
        }

        player.galaxyId = galaxy.id;
        galaxy.ownerPlayerId = player.playerId;
        player.energy = 0;
        player.handCards.Clear();
        EventManager.OnFly?.Invoke(player, galaxy);
    }

}
