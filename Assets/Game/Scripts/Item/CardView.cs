using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine.EventSystems;

public class CardView : MonoBehaviour, IPointerClickHandler
{
    public Card card;

    private RectTransform _rect;

    [Header("动画参数")]
    public float moveDuration = 0.6f;     // 移动时长
    public float arcHeight = 150f;        // 弧线高度
    public float scaleStart = 0.7f;       // 出生初始缩放
    public float scaleEnd = 0.7f;           // 最终缩放
    public float selectUpOffset = 20f;    // 选中时向上移动的距离

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }


    // 从牌堆起点 弧线飞到手牌终点
    public void FlyToHand(Vector2 startPos, Vector2 endPos)
    {
        _rect.anchoredPosition = startPos;
        _rect.localScale = Vector3.one * scaleStart;

        Vector3 midPos = new Vector3(
            (startPos.x + endPos.x) / 2f,
            Mathf.Max(startPos.y, endPos.y) + arcHeight,
            0
        );

        // 路径必须是 Vector3[]
        Vector3[] path = { startPos, midPos, endPos };

        _rect.DOPath(path, moveDuration, PathType.CatmullRom)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                _rect.DOScale(1.05f, 0.1f).SetLoops(2, LoopType.Yoyo);
            });

        _rect.DOScale(scaleEnd, moveDuration).SetEase(Ease.OutBack);
    }

    //卡牌点击会向上移动并变为选中状态
    public void OnPointerClick(PointerEventData eventData)
    {
        GameObject other = GameManager.Instance.currentCard;
        if(other != gameObject)
        {
            if(other != null)
            {
                other.GetComponent<CardView>().MoveCardDown();
            }
            _rect.DOAnchorPosY(_rect.anchoredPosition.y + selectUpOffset, 0.2f);
            GameManager.Instance.currentCard = gameObject;
        }
    }

    public void MoveCardDown()
    {
        _rect.DOAnchorPosY(_rect.anchoredPosition.y - selectUpOffset, 0.2f);
    }
}