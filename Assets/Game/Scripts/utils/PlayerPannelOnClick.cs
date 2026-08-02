using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerPannelOnClick : MonoBehaviour, IPointerClickHandler
{
    public int id;

    private TurnFlow _turnFlow;

    private void Start()
    {
        _turnFlow = Services.Get<TurnFlow>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _turnFlow.OnResponderChosen(id);
    }
}
