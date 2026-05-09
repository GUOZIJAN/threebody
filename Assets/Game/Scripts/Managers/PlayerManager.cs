using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using System.Linq;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    public int playerCount = 4;
    public List<PlayerData> Players;

    private void Awake()
    {
        Instance = this;
    }

    public PlayerData GetPlayer(int id)
    {
        Debug.Log(id);
        return Players[id];
    }

    public void Init()
    {
        Players = new List<PlayerData>();
        //随机星系序列
        List<int> list = new List<int> { 1,2,3,4,5,6,7,8,9 };
        System.Random rnd = new System.Random();
        list = list.OrderBy(x => rnd.Next()).ToList();

        for(int i = 0; i < playerCount; i++)
        {
            PlayerData newPlayer = new PlayerData()
            {
                playerId = i,
                galaxyId = list[i],
                energy = 3,
            };
            //初始四张手牌
            for(int j = 0; j < 4; j++)
            {
                newPlayer.handCards.Add(CardManager.Instance.Draw());
            }
            Players.Add(newPlayer);
            GalaxyManager.Instance.GetGalaxy(newPlayer.galaxyId).ownerPlayerId = i;
        }
        
        Debug.Log("玩家数据初始化完成");
    }
}