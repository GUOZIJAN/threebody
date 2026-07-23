using UnityEngine;
using UnityEngine.EventSystems;

class PlayerPannelOnClick : MonoBehaviour, IPointerClickHandler
{
    public int id;

    private ChoiceManager _choice;

    private void Start()
    {
        _choice = Services.Get<ChoiceManager>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _choice.OnPlayerChoose(id);
    }
}