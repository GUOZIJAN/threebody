using JetBrains.Annotations;
using UnityEngine;

class GalaxyOnClick : MonoBehaviour
{
    public int id;
    private void OnMouseDown()
    {
        ChoiceManager.Instance.OnGalaxySelected(GalaxyManager.Instance.GetGalaxy(id));
    }
}