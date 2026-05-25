using UnityEngine;
using UnityEngine.EventSystems;

class PlayerPannelOnClick : MonoBehaviour, IPointerClickHandler
{
    public int id;
    public void OnPointerClick(PointerEventData eventData)
    {
        ChoiceManager.Instance.OnPlayerChoose(id);
    }
}