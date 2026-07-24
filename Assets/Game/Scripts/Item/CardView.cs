using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

public class CardView : MonoBehaviour, IPointerClickHandler
{
    public Card card;

    private RectTransform _rect;

    private GameManager _game;
    private Player _player;
    private TurnFlow _turnFlow;

    [Header("动画参数")]
    public float moveDuration = 0.6f;
    public float arcHeight = 150f;
    public float scaleStart = 0.7f;
    public float scaleEnd = 0.7f;
    public float selectUpOffset = 20f;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void Start()
    {
        _game     = Services.Get<GameManager>();
        _turnFlow = Services.Get<TurnFlow>();
        _player   = Services.Get<Player>();
    }

    /// <summary>弧线飞到手牌位置</summary>
    public void FlyToHand(Vector2 startPos, Vector2 endPos)
    {
        _rect.anchoredPosition = startPos;
        _rect.localScale = Vector3.one * scaleStart;

        Vector3 midPos = new Vector3(
            (startPos.x + endPos.x) / 2f,
            Mathf.Max(startPos.y, endPos.y) + arcHeight,
            0
        );

        Vector3[] path = { startPos, midPos, endPos };

        _rect.DOPath(path, moveDuration, PathType.CatmullRom)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                _rect.DOScale(1.05f, 0.1f).SetLoops(2, LoopType.Yoyo);
            });

        _rect.DOScale(scaleEnd, moveDuration).SetEase(Ease.OutBack);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 弃牌模式：切换选中状态
        if (_turnFlow.Phase == TurnPhase.FoldingCards)
        {
            _turnFlow.ToggleFoldCard(gameObject);
            return;
        }

        // 普通选中/取消选中
        GameObject other = _game.currentCard;
        if (other != gameObject)
        {
            if (other != null)
                other.GetComponent<CardView>().MoveCardDown();

            MoveCardUp();
            _game.currentCard = gameObject;
            _player.currentCard = card;
        }
        else
        {
            MoveCardDown();
            _game.currentCard = null;
            _player.currentCard = null;
        }
    }

    public void MoveCardUp()
    {
        _rect.DOAnchorPosY(_rect.anchoredPosition.y + selectUpOffset, 0.2f);
    }

    public void MoveCardDown()
    {
        _rect.DOAnchorPosY(_rect.anchoredPosition.y - selectUpOffset, 0.2f);
    }
}
