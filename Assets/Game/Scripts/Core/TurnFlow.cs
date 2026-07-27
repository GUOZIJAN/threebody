using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回合制状态机 —— 替代 async GameCircle()。
/// 每帧 Update() 根据当前 Phase 执行一步，设 Phase 值驱动状态推进。
/// TCS 仅在弹窗层（PopupBase/ResBroadcast）保留。
/// </summary>
public enum TurnPhase
{
    Idle,
    TurnStart,
    WaitingForAction,
    ChoosingGalaxy,
    WaitingBroadcastRespond,
    ChoosingResponder,
    FoldingCards,
    AIThinking,
    AICardDelay,
    TurnEnd,
    GameOver,
}

public class TurnFlow : MonoBehaviour
{
    public TurnPhase Phase { get; private set; } = TurnPhase.Idle;

    // ==================== 依赖 ====================
    private GameManager   _game;
    private PlayerManager _players;
    private GalaxyManager _galaxies;
    private ActionManager _actions;
    private CardManager   _cards;
    private UIManager     _ui;
    private SpawnManager  _spawn;
    private Player        _player;
    private List<AI>      _ais;

    // ==================== 暂存上下文 ====================
    private Card         _pendingCard;
    private int          _aiCardIndex;
    private float        _aiDelayTimer;
    private PlayerData   _broadcastInitiator;
    private BroadcastCard _broadcastCard;
    private List<GameObject> _foldedCards = new();

    // ==================== 初始化 ====================
    private void Awake()
    {
        Services.Register(this);
    }

    private void Start()
    {
        _game     = Services.Get<GameManager>();
        _players  = Services.Get<PlayerManager>();
        _galaxies = Services.Get<GalaxyManager>();
        _actions  = Services.Get<ActionManager>();
        _cards    = Services.Get<CardManager>();
        _ui       = Services.Get<UIManager>();
        _spawn    = Services.Get<SpawnManager>();
        _player   = Services.Get<Player>();
        _ais      = _game.ais;
    }

    // ==================== Update ====================
    private void Update()
    {
        switch (Phase)
        {
            case TurnPhase.AICardDelay:
                _aiDelayTimer -= Time.deltaTime;
                if (_aiDelayTimer <= 0f)
                    SetPhase(TurnPhase.AIThinking);
                break;

            case TurnPhase.TurnStart:
                ProcessTurnStart();
                break;

            case TurnPhase.AIThinking:
                ProcessAIStep();
                break;

            case TurnPhase.TurnEnd:
                ProcessTurnEnd();
                break;

            // 空闲态 —— 等玩家输入，Update 里不做任何事
            default:
                break;
        }
    }

    // ==================== Phase 工具 ====================

    private void SetPhase(TurnPhase p)
    {
        Phase = p;
        EventManager.OnPhaseChanged?.Invoke(p);
    }

    // ==================== 公开回调（UI 按钮调用） ====================

    /// <summary>点击"开始游戏"</summary>
    public void StartGame()
    {
        if (Phase != TurnPhase.Idle && Phase != TurnPhase.GameOver) return;

        _game.state = GameState.Gaming;
        _game.currentPlayerId = 0;
        _game.remainPlayers = _game.playerCount;
        _players.HandCardInit();
        EventManager.OnGameStart?.Invoke();
        SetPhase(TurnPhase.TurnStart);
    }

    /// <summary>点击"使用卡牌"</summary>
    public void OnPlayCardClicked()
    {
        if (Phase != TurnPhase.WaitingForAction) return;

        var card = _player.currentCard;
        if (card == null) { Debug.Log("没有选中卡牌！"); return; }

        var pd = _players.GetPlayer(0);
        if (pd.energy < card.cost) { Debug.Log("能量不足！"); return; }

        // 扣费、移出手中（牌还留在手牌列表，ExecuteCard 里统一处理）
        pd.energy -= card.cost;
        pd.handCards.Remove(card);
        _pendingCard = card;

        bool needsTarget = card is BroadcastCard
                        || card is StrikeCard
                        || (card is BuildCard bd && bd.effect == BuildEffect.Fly);

        if (needsTarget)
        {
            SetPhase(TurnPhase.ChoosingGalaxy);
        }
        else
        {
            ExecuteCard(null);
        }
    }

    /// <summary>点击星系（ChoosingGalaxy 状态下）</summary>
    public void OnGalaxyClicked(Galaxy galaxy)
    {
        if (Phase != TurnPhase.ChoosingGalaxy) return;
        ExecuteCard(galaxy);
    }

    /// <summary>点击"结束回合"</summary>
    public void OnEndTurnClicked()
    {
        if (Phase != TurnPhase.WaitingForAction) return;

        // 结束回合时清理选中，防止后续流程误读残留的 _game.currentCard
        if (_game.currentCard != null)
        {
            _game.currentCard.GetComponent<CardView>().MoveCardDown();
            _game.currentCard = null;
        }
        _player.currentCard = null;

        SetPhase(TurnPhase.TurnEnd);
    }

    /// <summary>点击"弃牌"</summary>
    public void OnFoldCardsClicked()
    {
        if (Phase != TurnPhase.WaitingForAction) return;

        // 取消当前选中的牌
        if (_game.currentCard != null)
        {
            _game.currentCard.GetComponent<CardView>().MoveCardDown();
            _game.currentCard = null;
            _player.currentCard = null;
        }

        _foldedCards.Clear();
        SetPhase(TurnPhase.FoldingCards);
        _ui.EnterFoldMode();
    }

    /// <summary>弃牌模式下点击确认</summary>
    public void OnFoldConfirmed()
    {
        if (Phase != TurnPhase.FoldingCards) return;

        var pd = _players.GetPlayer(0);
        foreach (var go in _foldedCards)
        {
            var cv = go.GetComponent<CardView>();
            pd.handCards.Remove(cv.card);
            _cards.discard.Add(cv.card);
            _spawn.RemoveCardFromHand(go, reposition: false);
        }
        _foldedCards.Clear();
        _spawn.RepositionHandCards();

        _ui.ExitFoldMode();
        _ui.UpdateBasePanel(0);
        SetPhase(TurnPhase.TurnEnd);
    }

    /// <summary>弃牌模式：选/取消一张牌</summary>
    public void ToggleFoldCard(GameObject cardObj)
    {
        if (Phase != TurnPhase.FoldingCards) return;

        if (_foldedCards.Contains(cardObj))
        {
            _foldedCards.Remove(cardObj);
            cardObj.GetComponent<CardView>().MoveCardDown();
        }
        else
        {
            _foldedCards.Add(cardObj);
            cardObj.GetComponent<CardView>().MoveCardUp();
        }
    }

    /// <summary>广播回应者选择</summary>
    public void OnResponderChosen(int playerId)
    {
        if (Phase != TurnPhase.ChoosingResponder) return;
        if (!_actions.BroadcastRes.ContainsKey(playerId)) return;

        var response = _actions.BroadcastRes[playerId];
        var responser = _players.GetPlayer(playerId);

        responser.energy -= response.cost;
        responser.handCards.Remove(response);

        _game.CompleteBroadcast(_broadcastCard, response, _broadcastInitiator, responser);

        // 回应者补牌
        var c = _cards.Draw();
        responser.handCards.Add(c);
        EventManager.OnDrawCard?.Invoke(c);

        _ui.UpdateBasePanel(responser.playerId);
        _ui.UpdateBasePanel(_broadcastInitiator.playerId);
        EventManager.AfterPlayerChooseBroadcast?.Invoke();
        CleanupAfterCard();
    }

    /// <summary>广播弹窗关闭回调</summary>
    public void OnBroadcastPopupClosed(bool responded)
    {
        if (Phase != TurnPhase.WaitingBroadcastRespond) return;

        if (responded)
        {
            if (_player.currentCard is not BroadcastCard humanCard) return;
            _actions.BroadcastRes[_player.data.playerId] = humanCard;
            // 保留 _game.currentCard 引用，供后续 ResolveBroadcast_AI 移除视觉手牌
        }
        else
        {
            // 拒绝回应：清理选中状态，防止 OnPlayCard 事件误删手牌
            if (_game.currentCard != null)
            {
                _game.currentCard.GetComponent<CardView>().MoveCardDown();
                _game.currentCard = null;
            }
            _player.currentCard = null;
        }

        ContinueBroadcastResolution();
    }

    // ==================== 私有：回合流程 ====================

    private void ProcessTurnStart()
    {
        var pd = _players.GetPlayer(_game.currentPlayerId);
        if (!pd.isAlive)
        {
            // 当前玩家在打击结算中被淘汰，直接结束其回合
            SetPhase(TurnPhase.TurnEnd);
            return;
        }

        ResolveProduction(pd);
        EventManager.OnTurnStart?.Invoke();

        if (_game.currentPlayerId == 0)
            SetPhase(TurnPhase.WaitingForAction);
        else
            StartAITurn();
    }

    private void ProcessTurnEnd()
    {
        var pd = _players.GetPlayer(_game.currentPlayerId);

        // 补牌到 4 张
        while (pd.handCards.Count < 4)
        {
            var c = _cards.Draw();
            pd.handCards.Add(c);
            EventManager.OnDrawCard?.Invoke(c);
        }
        _ui.UpdateBasePanel(_game.currentPlayerId);

        // 推进到下个存活玩家
        AdvanceToNextAlivePlayer();

        // 检查打击到达
        CheckArrivingStrikes();

        if (_game.state == GameState.GameOver)
        {
            SetPhase(TurnPhase.GameOver);
            return;
        }

        SetPhase(TurnPhase.TurnStart);
    }

    // ==================== 私有：AI 回合 ====================

    private void StartAITurn()
    {
        var ai = _ais[_game.currentPlayerId - 1];
        ai.BeginTurn();
        _aiCardIndex = ai.data.handCards.Count - 1;
        _aiDelayTimer = 0f;
        SetPhase(TurnPhase.AIThinking);
    }

    private void ProcessAIStep()
    {
        var ai = _ais[_game.currentPlayerId - 1];
        var pd = ai.data;

        // 如果手牌被外部修改（如广播回应），重设 _aiCardIndex 到有效范围
        if (pd.handCards.Count == 0)
        {
            SetPhase(TurnPhase.TurnEnd);
            return;
        }
        if (_aiCardIndex >= pd.handCards.Count)
            _aiCardIndex = pd.handCards.Count - 1;

        // 从上次位置继续遍历手牌
        bool played = false;
        for (int i = _aiCardIndex; i >= 0; i--)
        {
            var card = pd.handCards[i];
            if (pd.energy < card.cost) continue;

            _aiCardIndex = i - 1;
            pd.energy -= card.cost;
            pd.handCards.RemoveAt(i);

            if (card is BroadcastCard broadcast)
            {
                var target = ai.FindBestBroadcastTarget(broadcast);
                if (target != null)
                {
                    _pendingCard = broadcast;
                    ExecuteBroadcast(pd, broadcast, target);
                    return;  // 广播有子流程，不在这里设 phase
                }
                // 无有效目标，弃掉这张牌
                _cards.discard.Add(broadcast);
                played = true;
                break;
            }
            else if (card is StrikeCard strike)
            {
                var target = ai.PickRandomGalaxy();
                _actions.ExecuteStrike(pd, strike, target);
            }
            else if (card is BuildCard build)
            {
                Galaxy flyTarget = build.effect == BuildEffect.Fly ? ai.FindEmptyGalaxy() : null;
                _actions.ExecuteBuild(pd, build, flyTarget);
            }

            _cards.discard.Add(card);
            EventManager.OnPlayCard?.Invoke(pd.playerId, card);
            played = true;
            break;
        }

        if (played)
        {
            _aiDelayTimer = 1f;
            SetPhase(TurnPhase.AICardDelay);
        }
        else
        {
            SetPhase(TurnPhase.TurnEnd);
        }
    }

    // ==================== 私有：卡牌执行 ====================

    private void ExecuteCard(Galaxy galaxy)
    {
        var card = _pendingCard;
        var pd = _players.GetPlayer(_game.currentPlayerId);

        switch (card)
        {
            case BroadcastCard b:
                ExecuteBroadcast(pd, b, galaxy);
                return;  // 可能进入子 phase，不要 cleanup

            case StrikeCard s:
                _actions.ExecuteStrike(pd, s, galaxy);
                break;

            case BuildCard bd:
                _actions.ExecuteBuild(pd, bd, galaxy);
                break;
        }

        _cards.discard.Add(card);
        EventManager.OnPlayCard?.Invoke(_game.currentPlayerId, card);
        RemoveCurrentCardFromHand();
        CleanupAfterCard();
    }

    private void ExecuteBroadcast(PlayerData player, BroadcastCard card, Galaxy targetGalaxy)
    {
        _actions.BroadcastRes.Clear();
        _broadcastCard = card;
        _broadcastInitiator = player;

        // 检查目标星系上的玩家是否必须回应
        bool forceHumanRespond = false;
        if (targetGalaxy.ownerPlayerId != -1 && targetGalaxy.ownerPlayerId != player.playerId)
        {
            PlayerData targetOwner = _players.GetPlayer(targetGalaxy.ownerPlayerId);
            if (targetOwner.isAlive && CanPlayerRespond(targetOwner, targetGalaxy))
            {
                // 监听基地：所在星系被广播时可选择不回应
                bool hasNoReply = HasNoReplyBuilding(targetOwner);
                if (targetOwner.playerId == 0 && !hasNoReply)
                    forceHumanRespond = true;
                // AI 在目标星系上且没有 NoReply：ai.Respond 总会返回可用卡，自动强制回应
            }
        }

        // 收集 AI 回应（同步）
        foreach (var ai in _ais)
        {
            if (ai.data.playerId == player.playerId || !ai.data.isAlive) continue;
            var response = ai.Respond(player, targetGalaxy, card);
            if (response != null)
                _actions.BroadcastRes[ai.data.playerId] = response;
        }

        // 如果 AI 是广播发起者，且人类存活 → 需要人类弹窗回应
        if (player.playerId != 0 && _players.GetPlayer(0).isAlive)
        {
            SetPhase(TurnPhase.WaitingBroadcastRespond);
            ShowBroadcastPopup(player, targetGalaxy, card, forceHumanRespond);
            return;
        }

        ContinueBroadcastResolution();
    }

    /// <summary>检查玩家是否有能力回应广播（手牌中有可用的广播卡）</summary>
    private bool CanPlayerRespond(PlayerData pd, Galaxy targetGalaxy)
    {
        foreach (var c in pd.handCards)
        {
            if (c is BroadcastCard bc
                && pd.energy >= bc.cost
                && _galaxies.GetDistance(pd.galaxyId, targetGalaxy.id) <= bc.distance)
                return true;
        }
        return false;
    }

    /// <summary>检查玩家是否拥有监听基地（NoReply 建筑），可豁免强制广播回应</summary>
    private bool HasNoReplyBuilding(PlayerData pd)
    {
        foreach (var build in pd.buildCards)
        {
            if (build.effect == BuildEffect.NoReply)
                return true;
        }
        return false;
    }

    private async void ShowBroadcastPopup(PlayerData raiser, Galaxy galaxy, BroadcastCard card, bool forceRespond = false)
    {
        try
        {
            var response = await _player.Respond(raiser, galaxy, card, forceRespond);
            OnBroadcastPopupClosed(response != null);
        }
        catch (Exception e)
        {
            Debug.LogError($"广播弹窗异常: {e}");
            OnBroadcastPopupClosed(false);
        }
    }

    private void ContinueBroadcastResolution()
    {
        if (_actions.BroadcastRes.Count == 0)
        {
            // 无人回应
            _cards.discard.Add(_broadcastCard);
            _broadcastInitiator.energy += 1;
            _ui.UpdateBasePanel(_broadcastInitiator.playerId);
        }
        else if (_game.currentPlayerId == 0)
        {
            // 人类是广播发起者 → 选回应者
            _cards.broadcastUsed.Add(_broadcastCard);
            EventManager.OnPlayCard?.Invoke(_game.currentPlayerId, _broadcastCard);
            RemoveCurrentCardFromHand();
            EventManager.OnPlayerChooseBroadcast?.Invoke();
            SetPhase(TurnPhase.ChoosingResponder);
            return;
        }
        else
        {
            // AI 是广播发起者 → 自动选第一个回应
            _cards.broadcastUsed.Add(_broadcastCard);
            ResolveBroadcast_AI();
        }

        EventManager.OnPlayCard?.Invoke(_game.currentPlayerId, _broadcastCard);
        RemoveCurrentCardFromHand();
        CleanupAfterCard();
    }

    private void ResolveBroadcast_AI()
    {
        // 取第一个回应
        int firstKey = -1;
        BroadcastCard firstVal = null;
        foreach (var kv in _actions.BroadcastRes)
        {
            firstKey = kv.Key;
            firstVal = kv.Value;
            break;
        }

        var responser = _players.GetPlayer(firstKey);
        responser.energy -= firstVal.cost;
        responser.handCards.Remove(firstVal);

        _game.CompleteBroadcast(_broadcastCard, firstVal, _broadcastInitiator, responser);

        var c = _cards.Draw();
        responser.handCards.Add(c);
        EventManager.OnDrawCard?.Invoke(c);

        if (responser.playerId == 0)
            _spawn.RemoveCardFromHand(_game.currentCard);

        _ui.UpdateBasePanel(responser.playerId);
        _ui.UpdateBasePanel(_broadcastInitiator.playerId);
    }

    private void RemoveCurrentCardFromHand()
    {
        if (_game.currentPlayerId == 0 && _game.currentCard != null)
        {
            _spawn.RemoveCardFromHand(_game.currentCard);
        }
        _game.currentCard = null;
        _player.currentCard = null;
        _pendingCard = null;
    }

    private void CleanupAfterCard()
    {
        // 卡牌打出后检查游戏是否结束
        if (_game.state == GameState.GameOver)
        {
            SetPhase(TurnPhase.GameOver);
            return;
        }

        if (_game.currentPlayerId == 0)
            SetPhase(TurnPhase.WaitingForAction);
        else
            SetPhase(TurnPhase.AIThinking);
    }

    // ==================== 私有：回合辅助 ====================

    private void ResolveProduction(PlayerData pd)
    {
        int produceTotal = 1;
        foreach (var build in pd.buildCards)
        {
            if (build.needSun && !_galaxies.GetGalaxy(pd.galaxyId).haveSun)
                continue;
            produceTotal += build.produce;
        }
        pd.energy += produceTotal;
        Debug.Log($"玩家{pd.playerId}回合开始, 生产能量{produceTotal}, 当前能量: {pd.energy}");
    }

    private void AdvanceToNextAlivePlayer()
    {
        do
        {
            _game.currentPlayerId = (_game.currentPlayerId + 1) % _game.playerCount;
        } while (!_players.Players[_game.currentPlayerId].isAlive);
    }

    private void CheckArrivingStrikes()
    {
        var strikeList = _actions.strikeList;
        for (int i = strikeList.Count - 1; i >= 0; i--)
        {
            var strike = strikeList[i];
            if (strike.attackerId == _game.currentPlayerId)
                strike.remainSteps--;

            if (strike.remainSteps == 0)
            {
                _game.RunStrike(strike);
                strikeList.RemoveAt(i);
            }
        }

        // 所有打击结算完毕后，可能出现 strikeList 清空时只剩 1 人的情况
        _game.GameOver();
    }
}
