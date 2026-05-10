using System.Threading.Tasks;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public GameObject UseCardButton;
    private GameManager gameManager;

    private void Awake()
    {
        Instance = this;
        EventManager.OnTurnStart += ShowUseCardButton;
    }

    public void Init()
    {
        gameManager = GameManager.Instance;
    }

    public void ShowUseCardButton()
    {
        if(gameManager.currentPlayerId == 0)
        {
            UseCardButton.SetActive(true);
            Debug.Log("显示使用卡牌按钮");
        }
        else
        {
            UseCardButton.SetActive(false);
            Debug.Log("隐藏使用卡牌按钮");
        }
    }

    public async void OnUseCardButtonClicked()
    {
        if(gameManager.currentCard == null)
        {
            Debug.Log("没有选中卡牌！");
            return;
        }
        await ActionManager.Instance.UseCard(gameManager.currentPlayerId, gameManager.currentCard.GetComponent<CardView>().card);
        Debug.Log("使用卡牌按钮被点击了！");
    }

    public async void OnEndTurnButtonClicked()
    {
        Debug.Log("结束回合按钮被点击了！");
        GameManager.Instance.CircleStart();
    }
}