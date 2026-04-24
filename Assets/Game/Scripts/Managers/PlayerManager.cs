using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    public int playerCount = 3;
    public List<PlayerData> Players = new List<PlayerData>();

    private void Awake()
    {
        Instance = this;
    }

    public PlayerData GetPlayer(int id)
    {
        return Players[id];
    }
}