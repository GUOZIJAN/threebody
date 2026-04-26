using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class GalaxyManager : MonoBehaviour
{
    public static GalaxyManager Instance;
    public List<Galaxy> galaxyList = new List<Galaxy>(5);
    public List<List<int>> distance = new List<List<int>>();

    private void Awake()
    {
        Instance = this;
    }

    public Galaxy GetGalaxy(int id)
    {
        return galaxyList[id-1];
    }
    
    public int GetDistance(int id_1,int id_2)
    {
        return distance[id_1-1][id_2-1];
    }
    public void Init()
    {
        Galaxy temp;
        for(int i = 1; i <= 9; i++)
        {
            temp = new Galaxy(){id = i};
            galaxyList.Add(temp);
            distance.Add(new List<int>());
        }
        galaxyList[0].neighborIds.AddRange(new List<int>{2,3,4});
        galaxyList[1].neighborIds.AddRange(new List<int>{1,4,6});
        galaxyList[2].neighborIds.AddRange(new List<int>{1,4,7});
        galaxyList[3].neighborIds.AddRange(new List<int>{1,2,3});
        galaxyList[4].neighborIds.AddRange(new List<int>{4,6,7});
        galaxyList[5].neighborIds.AddRange(new List<int>{2,5,8});
        galaxyList[6].neighborIds.AddRange(new List<int>{3,5,9});
        galaxyList[7].neighborIds.AddRange(new List<int>{5,6,9});
        galaxyList[8].neighborIds.AddRange(new List<int>{5,7,8});

        distance[0].AddRange(new List<int>{0,1,1,1,2,2,2,3,3});
        distance[1].AddRange(new List<int>{1,0,2,1,2,1,3,2,3});
        distance[2].AddRange(new List<int>{1,2,0,1,2,3,1,3,2});
        distance[3].AddRange(new List<int>{1,1,1,0,1,2,2,2,2});
        distance[4].AddRange(new List<int>{2,2,2,1,0,1,1,1,1});
        distance[5].AddRange(new List<int>{2,1,3,2,1,0,2,1,2});
        distance[6].AddRange(new List<int>{2,3,1,2,1,2,0,2,1});
        distance[7].AddRange(new List<int>{3,2,3,2,1,1,2,0,1});
        distance[8].AddRange(new List<int>{3,3,2,3,2,2,1,1,0});
    }
}