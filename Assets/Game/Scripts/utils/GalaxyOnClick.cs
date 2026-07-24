using UnityEngine;
using UnityEngine.EventSystems;

public class GalaxyOnClick : MonoBehaviour, IPointerClickHandler
{
    public int id;

    private TurnFlow _turnFlow;
    private GalaxyManager _galaxies;

    private void Start()
    {
        _turnFlow = Services.Get<TurnFlow>();
        _galaxies = Services.Get<GalaxyManager>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _turnFlow.OnGalaxyClicked(_galaxies.GetGalaxy(id));
    }
}
