using System.Threading.Tasks;
using UnityEngine;

public class ChoiceManager : MonoBehaviour
{
    public static ChoiceManager Instance;
    private TaskCompletionSource<Galaxy> galaxyTcs;

    private void Awake()
    {
        Instance = this;
    }

    public Task<Galaxy> ChooseGalaxy()
    {
        galaxyTcs = new TaskCompletionSource<Galaxy>();
        return galaxyTcs.Task;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Galaxy galaxy = hit.collider.GetComponent<Galaxy>();
                if (galaxy != null)
                {
                    OnGalaxySelected(galaxy);
                }
            }
        }
    }

    public void OnGalaxySelected(Galaxy galaxy)
    {
        galaxyTcs?.SetResult(galaxy);
    }
}
