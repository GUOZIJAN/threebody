using System.Collections.Generic;
using UnityEngine;

public class ActionManager : MonoBehaviour
{
    public static ActionManager Instance;
    public List<StrikeInfo> strikeList = new List<StrikeInfo>();

    private void Awake()
    {
        Instance = this;
    }
}