using System.Threading.Tasks;
using UnityEngine;

public class ChoiceManager : MonoBehaviour
{
    public static ChoiceManager Instance;
    public Galaxy AISelectedGalaxy;
    private TaskCompletionSource<Galaxy> galaxyTcs;

    private void Awake()
    {
        Instance = this;
    }

    public Task<Galaxy> ChooseGalaxy()
    {
        galaxyTcs = new TaskCompletionSource<Galaxy>();
        CheckAI();
        return galaxyTcs.Task;
    }


    public void OnGalaxySelected(Galaxy galaxy)
    {
        galaxyTcs?.SetResult(galaxy);
    }

    public void CheckAI()
    {
        if(GameManager.Instance.currentPlayerId != 0)
        {
            OnGalaxySelected(AISelectedGalaxy);
             Debug.Log($"AI选择了星系{AISelectedGalaxy.id}");
        }
    }
}
