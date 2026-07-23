using UnityEngine;
using UnityEngine.EventSystems;

class GalaxyOnClick : MonoBehaviour, IPointerClickHandler
{
    public int id;

    private ChoiceManager _choice;
    private GalaxyManager _galaxies;

    private void Start()
    {
        _choice   = Services.Get<ChoiceManager>();
        _galaxies = Services.Get<GalaxyManager>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _choice.OnGalaxySelected(_galaxies.GetGalaxy(id));
    }
}