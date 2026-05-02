using UnityEngine;
using UnityEngine.EventSystems;

class GalaxyOnClick : MonoBehaviour, IPointerClickHandler
{
    public int id;
    public void OnPointerClick(PointerEventData eventData)
    {
        ChoiceManager.Instance.OnGalaxySelected(GalaxyManager.Instance.GetGalaxy(id));
    }
}