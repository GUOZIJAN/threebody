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
    public int playerCount = 4;
    public int remainPlayers;
    public PlayerManager players;
    public GalaxyManager galaxys;
    public ActionManager actions;
    public CardManager cards;
    public List<AI> ais;
    public Player player;
    private void Awake()
    {
        Instance = this;
        state = GameState.Prepare;
    }

    private void Start()
    {
        players = PlayerManager.Instance;
        galaxys = GalaxyManager.Instance;
        actions = ActionManager.Instance;
        cards = CardManager.Instance;
        playerCount = players.playerCount;
        remainPlayers = playerCount;
        galaxys.Init();
        cards.InitDeck();
        players.Init();
        player.Init();
        foreach(var ai in ais)
        {
            ai.Init();
        }
    }

    public void GameStart()
    {
        state = GameState.Gaming;
        currentPlayerId = 0;
        Debug.Log($"游戏开始！当前玩家：{currentPlayerId}");
        EventManager.OnTurnStart?.Invoke();
    }

    public void NextTurn()
    {
        PlayerData currentPlayer = players.GetPlayer(currentPlayerId);
        while(currentPlayer.handCards.Count < 4)
        {
            currentPlayer.handCards.Add(cards.Draw());
        }
        EventManager.OnTurnEnd?.Invoke();
        Debug.Log($"回合结束！当前玩家：{currentPlayerId}");
        //找到下一个存活玩家
        while (!players.Players[(currentPlayerId + 1) % playerCount].isAlive)
        {
            currentPlayerId = (currentPlayerId + 1) % playerCount;
        }
        currentPlayerId = (currentPlayerId + 1) % playerCount;
        currentPlayer = players.GetPlayer(currentPlayerId);
        CheckStrike(currentPlayerId);
        int produceTotal = 0;
        foreach (var build in currentPlayer.buildCards)
        {
            // 戴森球/太阳能阵列需要恒星才能产能量
            if (build.needSun && !galaxys.GetGalaxy(currentPlayer.galaxyId).haveSun)
                continue;
            produceTotal += build.produce;
        }
        currentPlayer.energy += produceTotal;
        Debug.Log($"玩家{currentPlayerId}回合开始 抽卡1张 生产能量{produceTotal}，当前能量：{currentPlayer.energy}");
        EventManager.OnTurnStart?.Invoke();
        Debug.Log($"新回合开始！当前玩家：{currentPlayerId}");
    }

    public void CheckStrike(int nowPlayer)
    {
        foreach(var strike in ActionManager.Instance.strikeList){
            if (strike.attackerId == nowPlayer)
            {
                strike.remainSteps--;
            }
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
            //可以防御
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
            //不能防御
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

                targetPlayer.isAlive = false;
                //打击者获得能量
                remainPlayers--;
                players.GetPlayer(strike.attackerId).energy += remainPlayers * 3;
                EventManager.OnPlayerEliminate?.Invoke(targetPlayer.playerId);

                GameOver();
            }
        }
    }

    public void GameOver()
    {
        if (remainPlayers == 0)
        {
            //无人生还
            Debug.Log("无人生还");
        }
        if(actions.strikeList.Count == 0 && remainPlayers == 1)
        {
            //单人胜利
            Debug.Log("单人胜利");
        }
    }


    public void CompleteBroadcast(BroadcastCard card1, BroadcastCard card2,PlayerData player1, PlayerData player2)
    {
        //根据玩家选择的响应结果，处理广播效果
        if (card1.choice == BroadcastChoice.Cooperate && card2.choice == BroadcastChoice.Cooperate)
        {
            player1.energy += 3;
            player2.energy += 3;
        }
        else if (card1.choice == BroadcastChoice.Fake && card2.choice == BroadcastChoice.Fake)
        {
            //都不响应，无效果
        }
        else if (card1.choice == BroadcastChoice.Cooperate && card2.choice == BroadcastChoice.Fake)
        {
            player2.energy += 5;
        }
        else if (card1.choice == BroadcastChoice.Fake && card2.choice == BroadcastChoice.Cooperate)
        {
            player1.energy += 5;
        }
    }
}

    