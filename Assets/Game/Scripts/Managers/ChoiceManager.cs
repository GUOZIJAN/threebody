using System.Threading.Tasks;
using UnityEngine;

public class ChoiceManager : MonoBehaviour
{
    public static ChoiceManager Instance;
    public Galaxy AISelectedGalaxy;
    private TaskCompletionSource<Galaxy> galaxyTcs;
    private TaskCompletionSource<bool> PlayerTurnTcs;

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

    //创建玩家回合开始任务
    public Task<bool> PlayerTurnStart()
    {
        PlayerTurnTcs = new TaskCompletionSource<bool>();
        return PlayerTurnTcs.Task;
    }

    //玩家回合结束时调用，完成任务
    public void OnPlayerTurnEnd()
    {
        PlayerTurnTcs?.SetResult(true);
    }
}
