using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GalaxyOnClick : MonoBehaviour, IPointerClickHandler
{
    public int id;

    private TurnFlow      _turnFlow;
    private GalaxyManager _galaxies;
    private Image         _image;

    // 颜色定义
    private const float Alpha180 = 180f / 255f;

    private static readonly Color DefaultColor = Color.white;
    private static readonly Color NoSunColor   = new Color(0f, 0f, 0f, Alpha180);  // 恒星被毁 → 黑色
    private static readonly Color DeadColor    = new Color(1f, 0f, 0f, Alpha180);  // 星系被毁 → 红色

    private void Start()
    {
        _turnFlow = Services.Get<TurnFlow>();
        _galaxies = Services.Get<GalaxyManager>();
        _image    = GetComponent<Image>();

        EventManager.OnGalaxyStateChanged += OnGalaxyStateChanged;
    }

    private void OnDestroy()
    {
        EventManager.OnGalaxyStateChanged -= OnGalaxyStateChanged;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _turnFlow.OnGalaxyClicked(_galaxies.GetGalaxy(id));
    }

    private void OnGalaxyStateChanged(int galaxyId)
    {
        if (galaxyId != id || _image == null) return;

        Galaxy g = _galaxies.GetGalaxy(id);
        if (!g.isAlive)
            _image.color = DeadColor;
        else if (!g.haveSun)
            _image.color = NoSunColor;
        else
            _image.color = DefaultColor;
    }
}
