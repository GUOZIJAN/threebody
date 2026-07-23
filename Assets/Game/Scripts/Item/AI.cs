using System.Threading.Tasks;
using UnityEngine;

public class AI : MonoBehaviour
{
    public PlayerData data;
    public int id;

    private PlayerManager _players;
    private GalaxyManager _galaxies;
    private ChoiceManager _choice;
    private ActionManager _actions;

    private void Start()
    {
        _players  = Services.Get<PlayerManager>();
        _galaxies = Services.Get<GalaxyManager>();
        _choice   = Services.Get<ChoiceManager>();
        _actions  = Services.Get<ActionManager>();
    }

    public void Init()
    {
        data = _players.GetPlayer(id);

        Debug.Log($"ai{id}初始化完成");
    }

    public BroadcastCard Respond(PlayerData raiser, Galaxy galaxy, BroadcastCard card)
    {
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

    public async Task TurnStart()
    {
        for (int i = data.handCards.Count - 1; i >= 0; i--)
        {
            var handCard = data.handCards[i];
            if (data.energy >= handCard.cost)
            {
                if (handCard is BroadcastCard broadcastCard)
                {
                    Galaxy targetGalaxy = null;
                    int maxDistance = -1;
                    foreach (var galaxy in _galaxies.galaxyList)
                    {
                        int distance = _galaxies.GetDistance(data.galaxyId, galaxy.id);
                        if (distance <= broadcastCard.distance && distance > maxDistance)
                        {
                            maxDistance = distance;
                            targetGalaxy = galaxy;
                        }
                    }
                    _choice.AISelectedGalaxy = targetGalaxy;
                    if (targetGalaxy != null)
                    {
                        await _actions.UseCard(data.playerId, broadcastCard);
                    }
                }
                else if (handCard.type == CardType.Strike && handCard is StrikeCard strikeCard)
                {
                    int randomIndex = Random.Range(0, _galaxies.galaxyList.Count);
                    Galaxy targetGalaxy = _galaxies.galaxyList[randomIndex];
                    _choice.AISelectedGalaxy = targetGalaxy;
                    await _actions.UseCard(data.playerId, strikeCard);
                }
                else if (handCard.type == CardType.Build && handCard is BuildCard buildCard)
                {
                    await _actions.UseCard(data.playerId, buildCard);
                    if (buildCard.effect == BuildEffect.Fly)
                    {
                        break;
                    }
                }

                await Task.Delay(1000);
            }
        }
    }
}