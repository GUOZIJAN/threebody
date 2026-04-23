using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameState state;
    public int currentPlayerId;
    public int playerCount = PlayerManager.Instance.playerCount;
    private void Awake()
    {
        Instance = this;
    }

    public void GameStart()
    {
        state = GameState.Gaming;
        currentPlayerId = 0;
        EventManager.OnTurnStart?.Invoke();
    }

    public void NextTurn()
    {
        EventManager.OnTurnEnd?.Invoke();

        //找到下一个存活玩家
        while (!PlayerManager.Instance.Players[(currentPlayerId + 1) % playerCount].isAlive)
        {
            currentPlayerId = (currentPlayerId + 1) % playerCount;
        }
        currentPlayerId = (currentPlayerId + 1) % playerCount;
        EventManager.OnTurnStart?.Invoke();
    }

    public void CheckStrike()
    {
        foreach(var strike in ActionManager.Instance.strikeList){
            strike.remainSteps--;
            if (strike.remainSteps == 0)
            {
                RunStrike(strike);
                ActionManager.Instance.strikeList.Remove(strike);
            }
        }
    }

    public void RunStrike(StrikeInfo strike)
    {
        
    }
}