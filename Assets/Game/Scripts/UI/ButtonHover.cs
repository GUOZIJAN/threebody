using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 按钮悬停效果：鼠标进入/离开/按下时缩放动画。
/// 挂载到任意带 RectTransform 的 UI 按钮上即可。
/// </summary>
public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float _hoverScale  = 1.08f;
    [SerializeField] private float _pressScale  = 0.94f;
    [SerializeField] private float _duration    = 0.12f;
    [SerializeField] private Ease   _ease       = Ease.OutBack;

    private RectTransform _rect;
    private Vector3       _baseScale;
    private bool          _isHovering;

    private void Awake()
    {
        _rect      = GetComponent<RectTransform>();
        _baseScale = _rect.localScale;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        _isHovering = true;
        _rect.DOKill();
        _rect.DOScale(_baseScale * _hoverScale, _duration).SetEase(_ease);
    }

    public void OnPointerExit(PointerEventData e)
    {
        _isHovering = false;
        _rect.DOKill();
        _rect.DOScale(_baseScale, _duration).SetEase(_ease);
    }

    public void OnPointerDown(PointerEventData e)
    {
        _rect.DOKill();
        _rect.DOScale(_baseScale * _pressScale, _duration * 0.6f).SetEase(_ease);
    }

    public void OnPointerUp(PointerEventData e)
    {
        _rect.DOKill();
        float target = _isHovering ? _hoverScale : 1f;
        _rect.DOScale(_baseScale * target, _duration).SetEase(_ease);
    }
}
