using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using System;
using Unity.Mathematics;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameState state;
    public int currentPlayerId;
    public int playerCount;
    public int remainPlayers;
    public PlayerManager players;
    public GalaxyManager galaxys;
    public ActionManager actions;
    private void Awake()
    {
        Instance = this;
        players = PlayerManager.Instance;
        galaxys = GalaxyManager.Instance;
        actions = ActionManager.Instance;
        playerCount = players.playerCount;
        remainPlayers = playerCount;
    }

    public void GameStart()
    {
        state = GameState.Gaming;
        currentPlayerId = 0;
        EventManager.OnTurnStart?.Invoke();
    }

    public void NextTurn()
    {
        EventManager.OnTurnEnd?.Invoke();

        //找到下一个存活玩家
        while (!players.Players[(currentPlayerId + 1) % playerCount].isAlive)
        {
            currentPlayerId = (currentPlayerId + 1) % playerCount;
        }
        currentPlayerId = (currentPlayerId + 1) % playerCount;
        EventManager.OnTurnStart?.Invoke();
    }

    public void CheckStrike()
    {
        foreach(var strike in ActionManager.Instance.strikeList){
            strike.remainSteps--;
            if (strike.remainSteps == 0)
            {
                RunStrike(strike);
                ActionManager.Instance.strikeList.Remove(strike);
            }
        }
    }

    public void RunStrike(StrikeInfo strike)
    {
        Galaxy target = galaxys.GetGalaxy(strike.targetGalaxyId);
        //打击星系没有玩家
        if (target.ownerPlayerId == -1)
        {
            //检查是否摧毁恒星或星系
            if(strike.effect==StrikeEffect.DestroySun || strike.effect == StrikeEffect.DestroySunAndBuild)
            {
                target.haveSun = false;
            }
            else if (strike.effect == StrikeEffect.DestroyAll)
            {
                target.haveSun = false;                
                target.isAlive = false;
            }
        }
        //有玩家
        else
        {
            PlayerData targetPlayer = players.GetPlayer(target.ownerPlayerId);
            int maxDenfense = 0;
            //检查是否能防御住
            foreach(var build in targetPlayer.buildCards)
            {
                maxDenfense = math.max(maxDenfense,build.defense);
            }
            if (maxDenfense >= strike.damage)
            {
                switch (strike.effect)
                {
                    case StrikeEffect.DestroySun :
                        target.haveSun = false;
                        break;

                    case StrikeEffect.DestroySunAndBuild :
                        target.haveSun = false;
                        targetPlayer.buildCards.Clear();
                        break;

                    case StrikeEffect.DestroyHand :
                        targetPlayer.handCards.Clear();
                        break;
                }
            }
            else
            {
                if(strike.effect==StrikeEffect.DestroySun || strike.effect == StrikeEffect.DestroySunAndBuild)
                {
                    target.haveSun = false;
                }
                else if (strike.effect == StrikeEffect.DestroyAll)
                {
                    target.haveSun = false;                
                    target.isAlive = false;
                }

                //打击者获得能量
                remainPlayers--;
                players.GetPlayer(strike.attackerId).energy += remainPlayers * 3;
                EventManager.OnPlayerEliminate?.Invoke(targetPlayer.playerId);
            }
        }
    }

    public void GameOver()
    {
        if(actions.strikeList.Count == 0)
        {
            
            if (remainPlayers == 1)
            {
                //单人胜利
            }
            else if (remainPlayers == 0)
            {
                //无人生还
            }
        }
    }
}

    