using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using System;
using Unity.Mathematics;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameState state;
    public int currentPlayerId;
    public GameObject currentCard;   //plyer还有一个currentCard，表示当前选中的卡的Card
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
        UIManager.Instance.Init();
        ais.ForEach(ai => ai.Init());
    }

    public void GameStart()
    {
        state = GameState.Gaming;
        currentPlayerId = 0;
        players.HandCardInit();
        EventManager.OnTurnStart?.Invoke();
    }

    public async Task GameCircle()
    {
        while (true)
        {
            if(currentPlayerId == 0)
            {
                //玩家回合
                await ChoiceManager.Instance.PlayerTurnStart();
            }
            else
            {
                //AI回合
                await ais[currentPlayerId-1].TurnStart();
            }
            NextTurn();
        }
        
    }

    //玩家结束回合时，补牌并触发事件
    private void EndCurrentTurn(PlayerData player)
    {
        EventManager.OnTurnEnd?.Invoke();
        Debug.Log($"回合结束！当前玩家：{currentPlayerId}");
    }

    //回合结束，补牌到四张
    private void DrawHandCardsForCurrentPlayer(PlayerData player)
    {
        while(player.handCards.Count < 4)
        {
            player.handCards.Add(cards.Draw());
        }
    }

    //找到下一个存活的玩家，更新currentPlayerId
    private void AdvanceToNextAlivePlayer()
    {
        do
        {
            currentPlayerId = (currentPlayerId + 1) % playerCount;
        } while (!players.Players[currentPlayerId].isAlive);
    }

    //主要是生产能量和触发回合开始事件
    private void ResolveTurnStartEffects(PlayerData player)
    {
        int produceTotal = 1;  //每回合基础产能1点
        foreach (var build in player.buildCards)
        {
            // 戴森球/太阳能阵列需要恒星才能产能量
            if (build.needSun && !galaxys.GetGalaxy(player.galaxyId).haveSun)
                continue;
            produceTotal += build.produce;
        }
        player.energy += produceTotal;
        EventManager.OnTurnStart?.Invoke();
        Debug.Log($"玩家{currentPlayerId}回合开始,生产能量{produceTotal},当前能量：{player.energy}");
    }

    private void CheckStrike(int nowPlayer)
    {
        //中途会发生修改
        for (int i = ActionManager.Instance.strikeList.Count-1; i >= 0; i--)
        {
            StrikeInfo strike = ActionManager.Instance.strikeList[i];
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

    public void NextTurn()
    {
        PlayerData currentPlayer = players.GetPlayer(currentPlayerId);
        EndCurrentTurn(currentPlayer);
        DrawHandCardsForCurrentPlayer(currentPlayer);
        AdvanceToNextAlivePlayer();
        currentPlayer = players.GetPlayer(currentPlayerId);
        CheckStrike(currentPlayerId);
        ResolveTurnStartEffects(currentPlayer);
    }

    private void ApplyStrikeToGalaxy(StrikeInfo strike, Galaxy target)
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

    //打击作用于玩家，返回值为是否被消灭
    private bool ApplyStrikeToPlayer(StrikeInfo strike, PlayerData targetPlayer)
    {
        switch (strike.effect)
        {
            case StrikeEffect.DestroySunAndBuild :
                targetPlayer.buildCards.Clear();
                break;

            case StrikeEffect.DestroyHand :
                targetPlayer.handCards.Clear();
                break;

            case StrikeEffect.DestroyAll :
                targetPlayer.buildCards.Clear();
                targetPlayer.handCards.Clear();
                return true;    
        }

        int maxDenfense = 0;
        //检查是否会被消灭
        foreach(var build in targetPlayer.buildCards)
        {
            maxDenfense = math.max(maxDenfense,build.defense);
        }

        return maxDenfense < strike.damage;
    }

    private void HandleStrikeElimination(StrikeInfo strike, PlayerData targetPlayer)
    {
        if (ApplyStrikeToPlayer(strike, targetPlayer))
        {
            targetPlayer.isAlive = false;
            //打击者获得能量
            remainPlayers--;
            players.GetPlayer(strike.attackerId).energy += remainPlayers * 3;
            galaxys.GetGalaxy(targetPlayer.galaxyId).ownerPlayerId = -1;
            EventManager.OnPlayerEliminate?.Invoke(targetPlayer.playerId);
            Debug.Log($"玩家{targetPlayer.playerId}被打击淘汰！");
        }
    }

    public void RunStrike(StrikeInfo strike)
    {
        Galaxy target = galaxys.GetGalaxy(strike.targetGalaxyId);
        ApplyStrikeToGalaxy(strike, target);
        //打击星系有玩家
        if (target.ownerPlayerId != -1)
        {
            PlayerData targetPlayer = players.GetPlayer(target.ownerPlayerId);
            bool isEliminated = ApplyStrikeToPlayer(strike, targetPlayer);
            if (isEliminated)
            {
                HandleStrikeElimination(strike, targetPlayer);
                GameOver();
            }
        }
    }

    private bool IsLastPlayerWin()
    {
        return remainPlayers == 1 && actions.strikeList.Count == 0;
    }

    private bool IsNoPlayerWin()
    {
        return remainPlayers == 0;
    }

    public void GameOver()
    {
        if (IsNoPlayerWin())
        {
            //无人生还
            Debug.Log("无人生还");
        }
        if(IsLastPlayerWin())
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
            Debug.Log($"玩家{player1.playerId}和玩家{player2.playerId} 双方获得3点能量");
        }
        else if (card1.choice == BroadcastChoice.Fake && card2.choice == BroadcastChoice.Fake)
        {
            //都不响应，无效果
            Debug.Log($"玩家{player1.playerId}和玩家{player2.playerId} 双方都选择欺骗，无效果");
        }
        else if (card1.choice == BroadcastChoice.Cooperate && card2.choice == BroadcastChoice.Fake)
        {
            player2.energy += 5;
            Debug.Log($"玩家{player2.playerId}欺骗成功获得5点能量");
        }
        else if (card1.choice == BroadcastChoice.Fake && card2.choice == BroadcastChoice.Cooperate)
        {
            player1.energy += 5;
            Debug.Log($"玩家{player1.playerId}欺骗成功获得5点能量");
        }
    }
}

    