using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class GalaxyManager : MonoBehaviour
{
    public static GalaxyManager Instance;
    public List<Galaxy> galaxyList = new List<Galaxy>(5);

    private void Awake()
    {
        Instance = this;
    }

    public Galaxy GetGalaxy(int id)
    {
        return galaxyList[id-1];
    }
    
    public void Init()
    {
        Galaxy temp;
        for(int i = 1; i <= 9; i++)
        {
            temp = new Galaxy(){id = i};
            galaxyList.Add(temp);
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
    }
}