using UnityEngine;

public class ChoiceManager
{
    public static ChoiceManager Instance;
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
