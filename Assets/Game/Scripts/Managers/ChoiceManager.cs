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


    public void OnGalaxySelected(Galaxy galaxy)
    {
        galaxyTcs?.SetResult(galaxy);
    }
}
