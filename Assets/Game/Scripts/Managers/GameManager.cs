using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameState state;
    public int playerCount = 3;
    public int currentPlayerId;
    public List<PlayerData> players = new List<PlayerData>();

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
        while (!players[(currentPlayerId + 1) % playerCount].isAlive)
        {
            currentPlayerId = (currentPlayerId + 1) % playerCount;
        }
        currentPlayerId = (currentPlayerId + 1) % playerCount;
        EventManager.OnTurnStart?.Invoke();
    }

    public void CheckStrike()
    {
        
    }

    public void RunStrike()
    {
        
    }
}