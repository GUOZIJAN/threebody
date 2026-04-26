using UnityEngine;

public class ChioceManager
{
    public static ChioceManager Instance;
    public Galaxy targetGalaxy;
    public bool isWaitingChoose = false;

    private void Awake()
    {
        Instance = this;
    }

    public Galaxy chooseGalaxy()
    {
        while (isWaitingChoose)
        {
            //空循环，等待mousedown给targetGalaxy赋值
        }
        return targetGalaxy;
    }
}