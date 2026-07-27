using UnityEngine;

public class AI : MonoBehaviour
{
    public PlayerData data;
    public int id;

    private PlayerManager _players;
    private GalaxyManager _galaxies;
    private ActionManager _actions;

    private void Start()
    {
        _players  = Services.Get<PlayerManager>();
        _galaxies = Services.Get<GalaxyManager>();
        _actions  = Services.Get<ActionManager>();
    }

    public void Init()
    {
        data = _players.GetPlayer(id);
        Debug.Log($"ai{id}初始化完成");
    }

    /// <summary>回合开始，重置（当前无状态，保留供未来扩展）</summary>
    public void BeginTurn() { }

    // ==================== 目标选择 ====================

    /// <summary>广播：选距离内最远的星系</summary>
    public Galaxy FindBestBroadcastTarget(BroadcastCard card)
    {
        Galaxy best = null;
        int maxDist = -1;
        foreach (var galaxy in _galaxies.galaxyList)
        {
            int d = _galaxies.GetDistance(data.galaxyId, galaxy.id);
            if (d <= card.distance && d > maxDist)
            {
                maxDist = d;
                best = galaxy;
            }
        }
        return best;
    }

    /// <summary>打击：随机选一个星系</summary>
    public Galaxy PickRandomGalaxy()
    {
        return _galaxies.galaxyList[Random.Range(0, _galaxies.galaxyList.Count)];
    }

    /// <summary>跃迁：选一个空的可居住星系</summary>
    public Galaxy FindEmptyGalaxy()
    {
        foreach (var g in _galaxies.galaxyList)
        {
            if (g.isAlive && g.ownerPlayerId == -1)
                return g;
        }
        return null;
    }

    // ==================== 广播回应 ====================

    /// <summary>决定是否回应他人的广播（同步）</summary>
    public BroadcastCard Respond(PlayerData raiser, Galaxy galaxy, BroadcastCard card)
    {
        // 监听基地：所在星系被广播时可选择不回应
        if (galaxy.ownerPlayerId == data.playerId && HasNoReplyBuilding())
        {
            Debug.Log($"AI玩家{data.playerId}拥有监听基地，拒绝回应玩家{raiser.playerId}的广播");
            return null;
        }

        foreach (var handCard in data.handCards)
        {
            if (handCard is BroadcastCard handBroadcast
                && _galaxies.GetDistance(data.galaxyId, galaxy.id) <= handBroadcast.distance
                && data.energy >= handBroadcast.cost)
            {
                Debug.Log($"AI玩家{data.playerId}响应了玩家{raiser.playerId}的广播卡{card.cardname}");
                return handBroadcast;
            }
        }
        Debug.Log($"AI玩家{data.playerId}没有响应玩家{raiser.playerId}的广播卡{card.cardname}");
        return null;
    }

    private bool HasNoReplyBuilding()
    {
        foreach (var build in data.buildCards)
        {
            if (build.effect == BuildEffect.NoReply)
                return true;
        }
        return false;
    }
}
