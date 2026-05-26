using System.Threading.Tasks;
using UnityEngine;

public class ChoiceManager : MonoBehaviour
{
    public static ChoiceManager Instance;
    public Galaxy AISelectedGalaxy;
    private TaskCompletionSource<Galaxy> galaxyTcs;
    private TaskCompletionSource<bool> PlayerTurnTcs;
    private TaskCompletionSource<int> playerChooseTcs;

    private void Awake()
    {
        Instance = this;
    }

    public void ClearGalaxyTcs()
    {
        galaxyTcs = null;
    }

    public Task<Galaxy> ChooseGalaxy()
    {
        galaxyTcs = new TaskCompletionSource<Galaxy>();
        CheckAI();
        return galaxyTcs.Task;
    }

    public Task<int> PlayerChoose()
    {
        playerChooseTcs = new TaskCompletionSource<int>();
        // 显示选项UI，等待玩家选择
        // 这里需要实现一个UI界面来显示options，并在玩家选择后调用OnPlayerChoose(index)
        return playerChooseTcs.Task;
    }

    public void OnPlayerChoose(int index)
    {
        playerChooseTcs?.SetResult(index);
        playerChooseTcs = null;
    }

    public void OnGalaxySelected(Galaxy galaxy)
    {
        galaxyTcs?.SetResult(galaxy);  //这里不能直接赋值null
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
        PlayerTurnTcs = null;
    }
}
