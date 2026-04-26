using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ActionManager : MonoBehaviour
{
    public static ActionManager Instance;
    public ChioceManager chioceManager;
    public List<StrikeInfo> strikeList = new List<StrikeInfo>();

    private void Awake()
    {
        Instance = this;
        chioceManager = ChioceManager.Instance;
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
                DoStrike(player, (StrikeCard)card, chioceManager.chooseGalaxy());
                break;
        }

        player.handCards.Remove(card);
        CardManager.Instance.discard.Add(card);
    }

    public void DoBroadcast(PlayerData player,BroadcastCard card)
    {
        
    }

    public void DoBuild(PlayerData player,BuildCard card)
    {
        if (card.effect == BuildEffect.Fly)
        {
            FlyTo(player,chioceManager.chooseGalaxy());
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
        player.galaxyId = galaxy.id;
        galaxy.ownerPlayerId = player.playerId;
        player.energy = 0;
        player.handCards.Clear();
    }
}