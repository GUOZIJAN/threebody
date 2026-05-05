using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;
    public Transform[] cardSpawnPoints;

    private void Awake()
    {
        Instance = this;
    }

}

