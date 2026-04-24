using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ActionManager : MonoBehaviour
{
    public static ActionManager Instance;
    public List<StrikeInfo> strikeList = new List<StrikeInfo>();

    private void Awake()
    {
        Instance = this;
    }

    public void UseCard(int playId, Card card)
    {
        PlayerData player = PlayerManager.Instance.GetPlayer(playId);
        if (player.energy < card.cost)  return;

        switch (card.type)
        {
            case CardType.Broadcast :
                DoBroadcast(player, (BroadcastCard)card);
                break;
            
            case CardType.Build :
                DoBuild(player, (BuildCard)card);
                break;

            case CardType.Strike :
                DoStrike(player, (StrikeCard)card, ChooseTarget());
                break;
        }

        player.handCards.Remove(card);
    }

    public void DoBroadcast(PlayerData player,BroadcastCard card)
    {
        
    }

    public void DoBuild(PlayerData player,BuildCard card)
    {
        player.buildCards.Add(card);
    }

    public void DoStrike(PlayerData player,StrikeCard card,int galaxyId)
    {
        int distance = GalaxyManager.Instance.GetDistance(player.galaxyId,galaxyId);
        StrikeInfo newStrike = new StrikeInfo()
        {
            attackerId = player.playerId,
            targetGalaxyId = galaxyId,
            damage = card.damage,
            effect = card.effect,
            totalDistance = distance,
            remainSteps = distance
        };

        strikeList.Add(newStrike);
    }

    public int ChooseTarget()
    {
        //待开发
        return 1;
    }
}