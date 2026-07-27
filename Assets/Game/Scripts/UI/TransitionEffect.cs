using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 阶段过渡效果：全屏遮罩闪黑/闪白，配合 TurnFlow 阶段切换。
/// 场景配置：在 Canvas 最上层创建一个满屏 Image（黑色），挂载此脚本，拖入 _overlay。
/// </summary>
public class TransitionEffect : MonoBehaviour
{
    public static TransitionEffect Instance;

    [SerializeField] private Image _overlay;

    private void Awake()
    {
        Instance = this;
        if (_overlay != null)
        {
            _overlay.raycastTarget = false;
            _overlay.color = Color.clear;
        }
    }

    /// <summary>轻闪黑 —— 回合切换、进入选择阶段</summary>
    public void Pulse(float duration = 0.3f)
    {
        if (_overlay == null) return;
        _overlay.DOKill();
        _overlay.color = new Color(0, 0, 0, 0.35f);
        _overlay.DOFade(0f, duration).SetEase(Ease.OutSine);
    }

    /// <summary>强闪黑 —— 游戏结束</summary>
    public void FadeToBlack(float duration = 0.8f)
    {
        if (_overlay == null) return;
        _overlay.DOKill();
        _overlay.DOFade(1f, duration).SetEase(Ease.InOutQuad);
    }

    /// <summary>从黑幕淡入</summary>
    public void FadeFromBlack(float duration = 0.5f)
    {
        if (_overlay == null) return;
        _overlay.DOKill();
        _overlay.color = Color.black;
        _overlay.DOFade(0f, duration).SetEase(Ease.OutQuad);
    }

    /// <summary>闪白 —— 广播、淘汰等事件</summary>
    public void FlashWhite(float duration = 0.2f)
    {
        if (_overlay == null) return;
        _overlay.DOKill();
        _overlay.color = new Color(1, 1, 1, 0.4f);
        _overlay.DOFade(0f, duration).SetEase(Ease.OutSine);
    }
}
