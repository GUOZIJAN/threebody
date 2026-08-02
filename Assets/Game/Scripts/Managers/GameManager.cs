using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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
    private RectTransform _canvasRect;

    private void Awake()
    {
        Instance = this;
        Services.Register(this);
        state = GameState.Prepare;
    }

    private void Start()
    {
        players   = Services.Get<PlayerManager>();
        galaxys   = Services.Get<GalaxyManager>();
        actions   = Services.Get<ActionManager>();
        cards     = Services.Get<CardManager>();
        _turnFlow  = Services.Get<TurnFlow>();
        var shakeRoot = GameObject.Find("ShakeContainer");
        if (shakeRoot != null)
            _canvasRect = shakeRoot.GetComponent<RectTransform>();
        else
            _canvasRect = FindObjectOfType<Canvas>()?.GetComponent<RectTransform>();

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

        bool eliminated = false;
        if (target.ownerPlayerId != -1)
        {
            PlayerData targetPlayer = players.GetPlayer(target.ownerPlayerId);

            // 动画：收集将被摧毁的建筑列表项
            bool clearsBuilds = strike.effect == StrikeEffect.DestroySunAndBuild
                             || strike.effect == StrikeEffect.DestroyAll;
            var buildItems = clearsBuilds
                ? Services.Get<UIManager>().GetBuildPanelItems(targetPlayer.playerId)
                : null;

            // 动画：收集将被弃置的手牌
            bool discardsBuilds = strike.effect == StrikeEffect.DestroyHand
                               || strike.effect == StrikeEffect.DestroyAll;
            List<GameObject> discardCards = null;
            if (discardsBuilds && targetPlayer.playerId == 0)
            {
                discardCards = strike.effect == StrikeEffect.DestroyAll
                    ? Services.Get<SpawnManager>().handCards.ConvertAll(cv => cv != null ? cv.gameObject : null)
                    : CardAnimator.Instance.FindHandCardsOfType<BuildCard>();
                // 过滤掉 null 引用
                if (strike.effect == StrikeEffect.DestroyAll)
                    discardCards.RemoveAll(go => go == null);
            }

            if (ApplyStrikeToPlayer(strike, targetPlayer))
            {
                HandleStrikeElimination(strike, targetPlayer);
                GameOver();
                eliminated = true;
            }

            // 播放建筑摧毁动画
            if (buildItems != null && buildItems.Count > 0)
                CardAnimator.Instance.AnimateBuildsDestroyed(buildItems);

            // 播放手牌弃置动画（科技锁死），完成后重排手牌
            if (discardCards != null && discardCards.Count > 0)
            {
                foreach (var obj in discardCards)
                    CardAnimator.Instance.DetachFromHand(obj);
                CardAnimator.Instance.AnimateDiscardSequence(discardCards, isPlayer: true, onComplete: () =>
                {
                    Services.Get<SpawnManager>().RepositionHandCards();
                });
            }
        }

        // 屏幕震动：摇 Canvas 的 localPosition（Overlay 模式下 anchoredPosition 会被覆盖）
        if (_canvasRect != null)
        {
            float strength = eliminated ? 12f : 6f;
            _canvasRect.DOShakePosition(0.35f, strength, 20, 90f).SetEase(Ease.OutQuad);
        }
    }

    private void ApplyStrikeToGalaxy(StrikeInfo strike, Galaxy target)
    {
        bool changed = false;

        if (strike.effect == StrikeEffect.DestroySun
         || strike.effect == StrikeEffect.DestroySunAndBuild)
        {
            target.haveSun = false;
            changed = true;
        }
        else if (strike.effect == StrikeEffect.DestroyAll)
        {
            target.haveSun = false;
            target.isAlive = false;
            changed = true;
        }

        if (changed)
            EventManager.OnGalaxyStateChanged?.Invoke(target.id);
    }

    private bool ApplyStrikeToPlayer(StrikeInfo strike, PlayerData targetPlayer)
    {
        switch (strike.effect)
        {
            case StrikeEffect.DestroySunAndBuild:
                targetPlayer.buildCards.Clear();
                break;
            case StrikeEffect.DestroyHand:
                targetPlayer.handCards.RemoveAll(c => c is BuildCard);
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
        if (remainPlayers != 1) return false;

        // 找到最后存活的玩家
        PlayerData lastPlayer = null;
        foreach (var p in players.Players)
        {
            if (p.isAlive) { lastPlayer = p; break; }
        }
        if (lastPlayer == null) return false;

        // 只有不再有打击指向该玩家时，才算胜利
        // （该玩家自己发出的打击可以忽略，因为已经没有其他对手了）
        foreach (var strike in actions.strikeList)
        {
            Galaxy targetGalaxy = galaxys.GetGalaxy(strike.targetGalaxyId);
            if (targetGalaxy.ownerPlayerId == lastPlayer.playerId)
                return false;
        }

        return true;
    }

    private bool IsNoPlayerWin()
    {
        return remainPlayers == 0;
    }

    public void GameOver()
    {
        int winnerId = -1;

        if (IsNoPlayerWin())
        {
            state = GameState.GameOver;
            Debug.Log("无人生还");
        }
        else if (IsLastPlayerWin())
        {
            state = GameState.GameOver;
            // 找到最后存活的玩家
            for (int i = 0; i < players.Players.Count; i++)
            {
                if (players.Players[i].isAlive)
                {
                    winnerId = i;
                    break;
                }
            }
            Debug.Log($"玩家{winnerId}取得胜利");
        }

        if (state == GameState.GameOver)
            EventManager.OnGameOver?.Invoke(winnerId);
    }

    // ==================== 广播结算 ====================

    public void CompleteBroadcast(BroadcastCard card1, BroadcastCard card2, PlayerData player1, PlayerData player2)
    {
        string n1 = player1.playerId == 0 ? "你" : $"玩家{player1.playerId}";
        string n2 = player2.playerId == 0 ? "你" : $"玩家{player2.playerId}";
        string c1 = card1.choice == BroadcastChoice.Cooperate ? "合作" : "欺骗";
        string c2 = card2.choice == BroadcastChoice.Cooperate ? "合作" : "欺骗";

        if (card1.choice == BroadcastChoice.Cooperate && card2.choice == BroadcastChoice.Cooperate)
        {
            player1.energy += 3;
            player2.energy += 3;
            Debug.Log($"{n1}和{n2} 双方合作，各获得3点能量");
            if (MessageText.Instance != null)
                MessageText.Instance.Show($"{n1}合作，{n2}合作，双方各获得3点能量", 3f);
        }
        else if (card1.choice == BroadcastChoice.Fake && card2.choice == BroadcastChoice.Fake)
        {
            Debug.Log($"{n1}和{n2} 双方都选择欺骗，无效果");
            if (MessageText.Instance != null)
                MessageText.Instance.Show($"{n1}欺骗，{n2}欺骗，双方均无收益", 3f);
        }
        else if (card1.choice == BroadcastChoice.Cooperate && card2.choice == BroadcastChoice.Fake)
        {
            player2.energy += 5;
            Debug.Log($"{n2}欺骗成功获得5点能量");
            if (MessageText.Instance != null)
                MessageText.Instance.Show($"{n1}{c1}，{n2}{c2}，{n2}获得5点能量", 3f);
        }
        else if (card1.choice == BroadcastChoice.Fake && card2.choice == BroadcastChoice.Cooperate)
        {
            player1.energy += 5;
            Debug.Log($"{n1}欺骗成功获得5点能量");
            if (MessageText.Instance != null)
                MessageText.Instance.Show($"{n1}{c1}，{n2}{c2}，{n1}获得5点能量", 3f);
        }
    }
}
