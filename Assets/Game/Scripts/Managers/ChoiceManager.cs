using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChoiceManager : MonoBehaviour
{
    public static ChoiceManager Instance;
    public Galaxy AISelectedGalaxy;
    public List<GameObject> foldedCards = new List<GameObject>();
    public bool isFoldingCards = false;
    private TaskCompletionSource<Galaxy> galaxyTcs;
    private TaskCompletionSource<bool> PlayerTurnTcs;
    private TaskCompletionSource<int> playerChooseTcs;
    private TaskCompletionSource<List<GameObject>> foldCardTcs;

    private GameManager _game;

    private void Awake()
    {
        Instance = this;
        Services.Register(this);
    }

    private void Start()
    {
        _game = Services.Get<GameManager>();
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

    public Task<List<GameObject>> FoldCards()
    {
        foldCardTcs = new TaskCompletionSource<List<GameObject>>();
        isFoldingCards = true;
        return foldCardTcs.Task;
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

    public void OnCardsFolded()
    {
        isFoldingCards = false;
        foldCardTcs?.SetResult(foldedCards);
        foldedCards.Clear();
        foldCardTcs = null;
    }

    public void CheckAI()
    {
        if(_game.currentPlayerId != 0)
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
