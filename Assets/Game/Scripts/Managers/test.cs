using UnityEngine;

public class GameEntry : MonoBehaviour
{
    private void Start()
    {
        GameManager.Instance.GameStart();
    }
}