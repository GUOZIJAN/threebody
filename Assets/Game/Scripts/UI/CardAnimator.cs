using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

/// <summary>
/// 卡牌动画：使用 / 弃置 / 摧毁。
/// - 玩家：上移 + 淡出
/// - AI：在面板位置生成临时卡牌 → 左移 + 淡出
/// - 弃置：同上但更快，批量时顺序执行
/// - 建筑摧毁：列表项上移 + 淡出
/// </summary>
public class CardAnimator : MonoBehaviour
{
    public static CardAnimator Instance;

    [Header("动画参数")]
    [SerializeField] private float _useDuration     = 0.5f;
    [SerializeField] private float _discardDuration  = 0.22f;
    [SerializeField] private float _discardInterval  = 0.12f;  // 批量弃置间隔
    [SerializeField] private float _moveDistanceUp   = 100f;
    [SerializeField] private float _moveDistanceLeft = 150f;

    [Header("AI 临时卡牌")]
    [SerializeField] private GameObject _cardPrefab;

    private UIManager    _ui;
    private SpawnManager _spawn;
    private RectTransform _canvasRect;

    // ==================== 生命周期 ====================

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _ui         = Services.Get<UIManager>();
        _spawn      = Services.Get<SpawnManager>();
        _canvasRect = GetComponent<RectTransform>();
    }

    // ==================== 卡牌使用 ====================

    /// <summary>玩家使用手牌：上移 + 淡出，完成后销毁</summary>
    public void AnimatePlayerUse(GameObject cardObj, Action onComplete = null)
    {
        if (cardObj == null) { onComplete?.Invoke(); return; }

        RectTransform rect = cardObj.GetComponent<RectTransform>();
        CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        Sequence seq = DOTween.Sequence();
        seq.Join(rect.DOAnchorPosY(rect.anchoredPosition.y + _moveDistanceUp, _useDuration)
            .SetEase(Ease.OutCubic));
        seq.Join(cg.DOFade(0f, _useDuration).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            if (cardObj != null) Destroy(cardObj);
            onComplete?.Invoke();
        });
    }

    /// <summary>AI 使用卡牌：在面板处生成临时卡牌 → 左移 + 淡出</summary>
    public void AnimateAIUse(Card card, int playerId, Action onComplete = null)
    {
        if (_cardPrefab == null || _canvasRect == null)
        {
            Debug.LogWarning("CardAnimator: cardPrefab 或 canvasRect 未设置");
            onComplete?.Invoke();
            return;
        }

        // 获取 AI 面板在 Canvas 空间的位置
        Vector3 worldPos = _ui.GetPlayerPanelPosition(playerId);
        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, worldPos, null, out anchoredPos);

        GameObject temp = Instantiate(_cardPrefab, _canvasRect);
        RectTransform rect = temp.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos + new Vector2(60f, 0f); // 面板右侧偏移
        rect.localScale = Vector3.one * 0.85f;

        // --- 设置完整卡面 ---
        // Cost / Name
        temp.transform.Find("CostText").GetComponent<TextMeshProUGUI>().text = card.cost.ToString();
        temp.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = card.cardname;

        // Type
        temp.transform.Find("TypeText").GetComponent<TextMeshProUGUI>().text = card.type.ToString();

        // Power（广播=距离，打击=伤害，建设=空）
        TextMeshProUGUI powerText = temp.transform.Find("PowerText").GetComponent<TextMeshProUGUI>();
        switch (card.type)
        {
            case CardType.Broadcast:
                powerText.text = (card as BroadcastCard)?.distance.ToString() ?? "";
                break;
            case CardType.Strike:
                powerText.text = (card as StrikeCard)?.damage.ToString() ?? "";
                break;
            default:
                powerText.text = "";
                break;
        }

        // Description
        string desc = _spawn.GetCardDescription(card);
        if(card.type != CardType.Broadcast)
            temp.transform.Find("DescText").GetComponent<TextMeshProUGUI>().text = desc;
        else 
            temp.transform.Find("DescText").GetComponent<TextMeshProUGUI>().text = "";

        // Background sprite
        Sprite bgSprite = Resources.Load<Sprite>("pic/" + card.cardname);
        if (bgSprite != null)
            temp.transform.Find("Background").GetComponent<UnityEngine.UI.Image>().sprite = bgSprite;

        // 移除 CardView 避免误触
        CardView cv = temp.GetComponent<CardView>();
        if (cv != null) Destroy(cv);

        // 动画：左移 + 淡出
        CanvasGroup cg = temp.AddComponent<CanvasGroup>();

        Sequence seq = DOTween.Sequence();
        seq.Join(rect.DOAnchorPosX(rect.anchoredPosition.x - _moveDistanceLeft, _useDuration)
            .SetEase(Ease.OutCubic));
        seq.Join(cg.DOFade(0f, _useDuration).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            if (temp != null) Destroy(temp);
            onComplete?.Invoke();
        });
    }

    // ==================== 卡牌弃置 ====================

    /// <summary>单张弃置动画（速度更快）</summary>
    public void AnimateDiscard(GameObject cardObj, bool isPlayer, Action onComplete = null)
    {
        if (cardObj == null) { onComplete?.Invoke(); return; }

        RectTransform rect = cardObj.GetComponent<RectTransform>();
        CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        float targetY = rect.anchoredPosition.y + _moveDistanceUp;
        float targetX = isPlayer
            ? rect.anchoredPosition.x
            : rect.anchoredPosition.x - _moveDistanceLeft;

        Sequence seq = DOTween.Sequence();
        seq.Join(rect.DOAnchorPos(new Vector2(targetX, targetY), _discardDuration)
            .SetEase(Ease.OutCubic));
        seq.Join(cg.DOFade(0f, _discardDuration * 0.9f).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            if (cardObj != null) Destroy(cardObj);
            onComplete?.Invoke();
        });
    }

    /// <summary>批量弃置：顺序执行，间隔 _discardInterval</summary>
    public void AnimateDiscardSequence(List<GameObject> cards, bool isPlayer, Action onComplete = null)
    {
        StartCoroutine(DiscardSequenceRoutine(cards, isPlayer, onComplete));
    }

    private IEnumerator DiscardSequenceRoutine(List<GameObject> cards, bool isPlayer, Action onComplete)
    {
        foreach (var obj in cards)
        {
            if (obj != null)
            {
                // 先从手牌列表移除（数据层）
                AnimateDiscard(obj, isPlayer);
            }
            yield return new WaitForSeconds(_discardInterval);
        }
        onComplete?.Invoke();
    }

    // ==================== 建筑摧毁 ====================

    /// <summary>建筑列表项被摧毁：上移 + 淡出</summary>
    public void AnimateBuildDestroyed(GameObject buildItem, Action onComplete = null)
    {
        if (buildItem == null) { onComplete?.Invoke(); return; }

        RectTransform rect = buildItem.GetComponent<RectTransform>();
        CanvasGroup cg = buildItem.GetComponent<CanvasGroup>();
        if (cg == null) cg = buildItem.AddComponent<CanvasGroup>();

        Sequence seq = DOTween.Sequence();
        seq.Join(rect.DOAnchorPosY(rect.anchoredPosition.y + _moveDistanceUp, _discardDuration)
            .SetEase(Ease.OutCubic));
        seq.Join(cg.DOFade(0f, _discardDuration).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            if (buildItem != null) Destroy(buildItem);
            onComplete?.Invoke();
        });
    }

    /// <summary>批量摧毁建筑列表项（顺序执行）</summary>
    public void AnimateBuildsDestroyed(List<GameObject> items, Action onComplete = null)
    {
        StartCoroutine(BuildDestroySequenceRoutine(items, onComplete));
    }

    private IEnumerator BuildDestroySequenceRoutine(List<GameObject> items, Action onComplete)
    {
        foreach (var obj in items)
        {
            if (obj != null)
                AnimateBuildDestroyed(obj);
            yield return new WaitForSeconds(_discardInterval);
        }
        onComplete?.Invoke();
    }

    // ==================== 工具 ====================

    /// <summary>获取手牌中指定类型的所有 GameObject</summary>
    public List<GameObject> FindHandCardsOfType<T>() where T : Card
    {
        var result = new List<GameObject>();
        foreach (var cv in _spawn.handCards)
        {
            if (cv != null && cv.card is T)
                result.Add(cv.gameObject);
        }
        return result;
    }

    /// <summary>从 SpawnManager 手牌列表中移除指定 CardView（不销毁）</summary>
    public void DetachFromHand(GameObject cardObj)
    {
        if (cardObj == null) return;
        CardView cv = cardObj.GetComponent<CardView>();
        if (cv != null && _spawn.handCards.Contains(cv))
            _spawn.handCards.Remove(cv);
    }

}
