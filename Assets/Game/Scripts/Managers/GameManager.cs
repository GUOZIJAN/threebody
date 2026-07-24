using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameState state;
    public int currentPlayerId;
    public GameObject currentCard;
    public int playerCount = 4;
    public int remainPlayers;

    // 依赖（通过 Services 获取，部分字段被其他类直接访问）
    public PlayerManager players;
    public GalaxyManager galaxys;
    public ActionManager actions;
    public CardManager cards;
    public List<AI> ais;
    public Player player;

    private TurnFlow _turnFlow;

    private void Awake()
    {
        Instance = this;
        Services.Register(this);
        state = GameState.Prepare;
    }

    private void Start()
    {
        players  = Services.Get<PlayerManager>();
        galaxys  = Services.Get<GalaxyManager>();
        actions  = Services.Get<ActionManager>();
        cards    = Services.Get<CardManager>();
        _turnFlow = Services.Get<TurnFlow>();

        playerCount = players.playerCount;
        remainPlayers = playerCount;
        galaxys.Init();
        cards.InitDeck();
        players.Init();
        player.Init();
        Services.Get<UIManager>().Init();
        ais.ForEach(ai => ai.Init());
    }

    /// <summary>玩家点击"开始游戏"，委托给 TurnFlow</summary>
    public void GameStart()
    {
        _turnFlow.StartGame();
    }

    // ==================== 打击结算 ====================

    public void RunStrike(StrikeInfo strike)
    {
        Galaxy target = galaxys.GetGalaxy(strike.targetGalaxyId);
        ApplyStrikeToGalaxy(strike, target);

        if (target.ownerPlayerId != -1)
        {
            PlayerData targetPlayer = players.GetPlayer(target.ownerPlayerId);
            if (ApplyStrikeToPlayer(strike, targetPlayer))
            {
                HandleStrikeElimination(strike, targetPlayer);
                GameOver();
            }
        }
    }

    private void ApplyStrikeToGalaxy(StrikeInfo strike, Galaxy target)
    {
        if (strike.effect == StrikeEffect.DestroySun
         || strike.effect == StrikeEffect.DestroySunAndBuild)
        {
            target.haveSun = false;
        }
        else if (strike.effect == StrikeEffect.DestroyAll)
        {
            target.haveSun = false;
            target.isAlive = false;
        }
    }

    private bool ApplyStrikeToPlayer(StrikeInfo strike, PlayerData targetPlayer)
    {
        switch (strike.effect)
        {
            case StrikeEffect.DestroySunAndBuild:
                targetPlayer.buildCards.Clear();
                break;
            case StrikeEffect.DestroyHand:
                targetPlayer.handCards.Clear();
                break;
            case StrikeEffect.DestroyAll:
                targetPlayer.buildCards.Clear();
                targetPlayer.handCards.Clear();
                return true;
        }

        int maxDefense = 0;
        foreach (var build in targetPlayer.buildCards)
            maxDefense = Mathf.Max(maxDefense, build.defense);

        return maxDefense < strike.damage;
    }

    private void HandleStrikeElimination(StrikeInfo strike, PlayerData targetPlayer)
    {
        targetPlayer.isAlive = false;
        remainPlayers--;
        players.GetPlayer(strike.attackerId).energy += remainPlayers * 3;
        galaxys.GetGalaxy(targetPlayer.galaxyId).ownerPlayerId = -1;
        EventManager.OnPlayerEliminate?.Invoke(targetPlayer.playerId);
        Debug.Log($"玩家{targetPlayer.playerId}被打击淘汰！");
    }

    // ==================== 胜负判定 ====================

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
            state = GameState.GameOver;
            Debug.Log("无人生还");
        }
        else if (IsLastPlayerWin())
        {
            state = GameState.GameOver;
            Debug.Log("单人胜利");
        }
    }

    // ==================== 广播结算 ====================

    public void CompleteBroadcast(BroadcastCard card1, BroadcastCard card2, PlayerData player1, PlayerData player2)
    {
        if (card1.choice == BroadcastChoice.Cooperate && card2.choice == BroadcastChoice.Cooperate)
        {
            player1.energy += 3;
            player2.energy += 3;
            Debug.Log($"玩家{player1.playerId}和玩家{player2.playerId} 双方获得3点能量");
        }
        else if (card1.choice == BroadcastChoice.Fake && card2.choice == BroadcastChoice.Fake)
        {
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
