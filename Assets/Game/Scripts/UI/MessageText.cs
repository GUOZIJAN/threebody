using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 消息文本框 —— 显示游戏提示和事件消息。
/// 挂载到场景中带 TextMeshProUGUI 的 GameObject 上。
/// </summary>
public class MessageText : MonoBehaviour
{
    public static MessageText Instance;

    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private float _defaultDuration = 2.5f;
    [SerializeField] private float _fadeDuration = 0.25f;

    private readonly Queue<QueuedMessage> _queue = new();
    private Coroutine _runner;

    // ==================== 生命周期 ====================

    private void Awake()
    {
        Instance = this;
        _text ??= GetComponent<TextMeshProUGUI>();
        _text.alpha = 0f;
    }

    private void Start()
    {
        // 游戏事件
        EventManager.OnGameStart += OnGameStart;
        EventManager.OnPlayCard += OnPlayCard;
        EventManager.OnPlayerEliminate += OnPlayerEliminate;
        EventManager.OnFly += OnFly;
        EventManager.OnPlayerChooseBroadcast += OnPlayerChooseBroadcast;
        EventManager.AfterPlayerChooseBroadcast += OnAfterPlayerChooseBroadcast;

        // 阶段变化
        EventManager.OnPhaseChanged += OnPhaseChanged;
    }

    private void OnDestroy()
    {
        EventManager.OnGameStart -= OnGameStart;
        EventManager.OnPlayCard -= OnPlayCard;
        EventManager.OnPlayerEliminate -= OnPlayerEliminate;
        EventManager.OnFly -= OnFly;
        EventManager.OnPlayerChooseBroadcast -= OnPlayerChooseBroadcast;
        EventManager.AfterPlayerChooseBroadcast -= OnAfterPlayerChooseBroadcast;
        EventManager.OnPhaseChanged -= OnPhaseChanged;
    }

    // ==================== 公开接口 ====================

    /// <summary>显示一条消息。duration ≤ 0 且 persistent 为 true 时持久显示，直到 Hide() 或下一条消息。</summary>
    public void Show(string msg, float duration = -1f, bool persistent = false)
    {
        float d = persistent ? -1f : (duration > 0f ? duration : _defaultDuration);
        _queue.Enqueue(new QueuedMessage { text = msg, duration = d });
        _runner ??= StartCoroutine(RunQueue());
    }

    /// <summary>立即隐藏并清空队列</summary>
    public void Hide()
    {
        _queue.Clear();
        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }
        _text.DOKill();
        _text.DOFade(0f, _fadeDuration);
    }

    // ==================== 游戏事件处理 ====================

    private void OnGameStart()
    {
        Show("游戏开始！");
    }

    private void OnPlayCard(int playerId, Card card)
    {
        string who = playerId == 0 ? "你" : $"玩家{playerId}";
        Show($"{who}使用了 {card.cardname}");
    }

    private void OnPlayerEliminate(int playerId)
    {
        Show($"玩家{playerId}被淘汰", 3f);
    }

    private void OnFly(PlayerData player, Galaxy galaxy)
    {
        string who = player.playerId == 0 ? "你" : $"玩家{player.playerId}";
        Show($"{who}跃迁至星系{galaxy.id}");
    }

    private void OnPlayerChooseBroadcast()
    {
        Show("请选择是否回应广播", persistent: true);
    }

    private void OnAfterPlayerChooseBroadcast()
    {
        Hide();
    }

    // ==================== 阶段变化处理 ====================

    private void OnPhaseChanged(TurnPhase phase)
    {
        switch (phase)
        {
            case TurnPhase.ChoosingGalaxy:
                Show("请选择目标星系", persistent: true);
                break;
            case TurnPhase.FoldingCards:
                Show("请选择要弃置的手牌", persistent: true);
                break;
            case TurnPhase.ChoosingResponder:
                Show("请选择广播回应者", persistent: true);
                break;
            case TurnPhase.WaitingForAction:
                Show("你的回合：选择手牌使用或弃置");
                break;
            case TurnPhase.WaitingBroadcastRespond:
                Show("等待其他玩家回应广播...", persistent: true);
                break;
            case TurnPhase.AIThinking:
            case TurnPhase.AICardDelay:
                Show("其他玩家思考中...", persistent: true);
                break;
            case TurnPhase.GameOver:
                Show("游戏结束", persistent: true);
                break;
            case TurnPhase.TurnStart:
                Hide();
                break;
        }
    }

    // ==================== 队列消费 ====================

    private IEnumerator RunQueue()
    {
        while (_queue.Count > 0)
        {
            var msg = _queue.Dequeue();
            _text.text = msg.text;
            _text.DOFade(1f, _fadeDuration);

            yield return new WaitForSeconds(_fadeDuration);

            if (msg.duration < 0f)
            {
                // 持久消息：不自动消失，等待外部 Hide() 或新消息覆盖
                _runner = null;
                yield break;
            }

            yield return new WaitForSeconds(msg.duration);
            _text.DOFade(0f, _fadeDuration);
            yield return new WaitForSeconds(_fadeDuration + 0.1f);
        }

        _runner = null;
    }

    // ==================== 内部类型 ====================

    private struct QueuedMessage
    {
        public string text;
        public float duration; // < 0 = 持久显示
    }
}
