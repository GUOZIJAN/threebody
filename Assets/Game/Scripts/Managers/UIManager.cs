using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public GameObject UseCardButton;
    public GameObject GameStartButton;
    public GameObject EndTurnButton;
    public GameObject ItemPrefab;
    public TextMeshProUGUI PlayerGalaxyText;   // 显示玩家所在星系的文本组件
    public List<GameObject> PlayerPanels;   // 玩家面板列表，包含玩家信息和手牌展示等UI元素

    private GameManager gameManager;

    private void Awake()
    {
        Instance = this;
        EventManager.OnTurnStart += ShowUseCardButton;
        EventManager.OnTurnStart += ShowEndTurnButton;
        EventManager.OnTurnStart += () => UpdateBasePanel(gameManager.currentPlayerId);
        EventManager.OnPlayCard += UpdateBasePanel;
        EventManager.OnPlayCard += UpdateItemPanel;
    }

    public void Init()
    {
        gameManager = GameManager.Instance;
        PlayerGalaxyText.text = $"所在星系: {PlayerManager.Instance.GetPlayer(0).galaxyId}";
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

    public void ShowEndTurnButton()
    {
        if(gameManager.currentPlayerId == 0)
        {
            EndTurnButton.SetActive(true);
            Debug.Log("显示结束回合按钮");
        }
        else
        {
            EndTurnButton.SetActive(false);
            Debug.Log("隐藏结束回合按钮");
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
        gameManager.currentCard = null;     //使用完卡牌后,清空'当前卡牌'
        Player.Instance.currentCard = null;    //同样清空Player的currentCard
        Debug.Log("使用卡牌按钮被点击了！");  //不放在gamemanager，只有玩家使用这个变量
    }

    public void OnEndTurnButtonClicked()
    {
        Debug.Log("结束回合按钮被点击了！");
        ChoiceManager.Instance.OnPlayerTurnEnd();
    }

    public async void OnGameStartButtonClicked()
    {
        GameStartButton.SetActive(false);
        GameManager.Instance.GameStart();
        await GameManager.Instance.GameCircle();
    }

    public void UpdateBasePanel(int playerId)
    {
        // 根据playerId更新对应的玩家面板UI
        // 例如，更新玩家的能量、手牌等信息
        GameObject targetPanel = PlayerPanels[playerId];
        PlayerData targetPlayer = PlayerManager.Instance.GetPlayer(playerId);
        Transform baseInfo = targetPanel.transform.Find("Base");
        // 更新能量显示
        baseInfo.Find("energy").GetComponent<TextMeshProUGUI>().text = $"{targetPlayer.energy}";
        // 更新手牌数量显示
        baseInfo.Find("card").GetComponent<TextMeshProUGUI>().text = $"{targetPlayer.handCards.Count}";
    }

    // Overload to match delegates with Card parameter (e.g., Action<int, Card>)
    public void UpdateBasePanel(int playerId, Card card)
    {
        UpdateBasePanel(playerId);
    }

    public void UpdateItemPanel(int playerId,Card card)
    {
        GameObject targetPanel = PlayerPanels[playerId];
        PlayerData targetPlayer = PlayerManager.Instance.GetPlayer(playerId);
        Transform itemPanel = null;
        //不同类型卡牌改动不同信息
        switch (card.type)
        {
            case CardType.Broadcast :
                itemPanel = targetPanel.transform.Find("Broadcast_list");
                break;

            case CardType.Build :
                itemPanel = targetPanel.transform.Find("Build_list");
                break;

            case CardType.Strike :
                itemPanel = targetPanel.transform.Find("Strike_list");
                break;
        }
        ScrollRect scrollRect = itemPanel.GetComponent<ScrollRect>();
        GameObject item = Instantiate(ItemPrefab, scrollRect.content);
        item.GetComponent<TextMeshProUGUI>().text = $"{card.cost}  {card.cardname}";
    }
}