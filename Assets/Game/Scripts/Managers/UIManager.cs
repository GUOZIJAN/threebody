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
    public GameObject FoldCardButton;
    public GameObject ConfirmFoldButton;
    public GameObject ItemPrefab;
    public TextMeshProUGUI CardCountText;
    public TextMeshProUGUI PlayerGalaxyText;   // 显示玩家所在星系的文本组件
    public List<GameObject> PlayerPanels;   // 玩家面板列表，包含玩家信息和手牌展示等UI元素

    private GameManager gameManager;
    private Color PanelAvailableColor = new Color32(95,255,0,100);
    private Color PanelUnavailableColor = new Color32(255,255,255,100);

    private void Awake()
    {
        Instance = this;
        EventManager.OnTurnStart += ShowUseCardButton;
        EventManager.OnTurnStart += ShowEndTurnButton;
        EventManager.OnTurnStart += ShowFoldCardButton;
        EventManager.OnTurnStart += () => UpdateBasePanel(gameManager.currentPlayerId);
        EventManager.OnTurnStart += () => UpdateStrikePanel(gameManager.currentPlayerId);
        EventManager.OnPlayCard += UpdateBasePanel;
        EventManager.OnPlayCard += UpdateItemPanel;
        EventManager.OnPlayCard += (id,card) => FoldCardButton.SetActive(false);
        EventManager.OnPlayerEliminate += ChangePanelColor;
        EventManager.OnDrawCard += (card) => UpdateCardCount();
        EventManager.OnFly += UpdateAfterFly;
        EventManager.OnPlayerChooseBroadcast += () => ChangePlayerPanelColor(PanelAvailableColor);
        EventManager.AfterPlayerChooseBroadcast += () => ChangePlayerPanelColor(PanelUnavailableColor);
        EventManager.OnGameStart += () => UpdateAllPanels();
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

    public void ShowFoldCardButton()
    {
        if(gameManager.currentPlayerId == 0)
        {
            FoldCardButton.SetActive(true);
            Debug.Log("显示弃牌按钮");
        }
        else
        {
            FoldCardButton.SetActive(false);
            Debug.Log("隐藏弃牌按钮");
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

    public async void OnFoldCardButtonClicked()
    {
        UseCardButton.SetActive(false);
        EndTurnButton.SetActive(false);
        FoldCardButton.SetActive(false);
        ConfirmFoldButton.SetActive(true);

        if(gameManager.currentCard != null)
        {
            gameManager.currentCard.GetComponent<CardView>().MoveCardDown();
            gameManager.currentCard = null;     //弃牌时,清空'当前卡牌'
            Player.Instance.currentCard = null;    //同样清空Player的currentCard
        }

        List<GameObject> foldedCards = await ChoiceManager.Instance.FoldCards();
        
        foreach (GameObject card in foldedCards)
        {
            // 处理弃掉的卡牌
            PlayerManager.Instance.GetPlayer(gameManager.currentPlayerId).handCards.Remove(card.GetComponent<CardView>().card);
            SpawnManager.Instance.RemoveCardFromHand(card);
            CardManager.Instance.discard.Add(card.GetComponent<CardView>().card);
        }

        UpdateBasePanel(gameManager.currentPlayerId); // 更新玩家面板显示
        ConfirmFoldButton.SetActive(false);
        ChoiceManager.Instance.OnPlayerTurnEnd(); // 弃牌后直接结束回合
    }

    public void OnConfirmFoldButtonClicked()
    {
        ChoiceManager.Instance.OnCardsFolded();
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
                UpdateStrikePanel(playerId);
                return;
        }
        ScrollRect scrollRect = itemPanel.GetComponent<ScrollRect>();
        GameObject item = Instantiate(ItemPrefab, scrollRect.content);
        item.GetComponent<TextMeshProUGUI>().text = $"{card.cost}  {card.cardname}";
    }

    public void UpdateStrikePanel(int playerId)
    {
        //先清空面板，在根据strikelist重新生成
        GameObject targetPanel = PlayerPanels[playerId];
        Transform strikePanel = targetPanel.transform.Find("Strike_list");
        ScrollRect scrollRect = strikePanel.GetComponent<ScrollRect>();

        foreach (Transform child in scrollRect.content)
        {
            Destroy(child.gameObject);
        }
        
        foreach (var strike in ActionManager.Instance.strikeList)
        {
            if (strike.attackerId == playerId)
            {
                GameObject item = Instantiate(ItemPrefab, scrollRect.content);
                item.GetComponent<TextMeshProUGUI>().text = strike.cardName;
                item.transform.Find("target").GetComponent<TextMeshProUGUI>().text = strike.targetGalaxyId.ToString();
                item.transform.Find("remain").GetComponent<TextMeshProUGUI>().text = (strike.totalDistance - strike.remainSteps).ToString();
            }
        }
    }

    public void ChangePanelColor(int playerId)
    {
        PlayerPanels[playerId].GetComponent<Image>().color = Color.red; // 设置为红色
    }

    public void UpdateCardCount()
    {
        CardCountText.text = $"{CardManager.Instance.deck.Count}"; // 更新牌堆剩余卡牌数量
    }

    public void UpdateAfterFly(PlayerData player,Galaxy targetGalaxy)
    {
        Transform playerBasePanel = PlayerPanels[player.playerId].transform.Find("Base");
        playerBasePanel.Find("energy").GetComponent<TextMeshProUGUI>().text = "0";
        playerBasePanel.Find("card").GetComponent<TextMeshProUGUI>().text = "0";
        if(player.playerId == 0)
        {
            PlayerGalaxyText.text = $"所在星系: {targetGalaxy.id}";   //玩家需额外更新星系
        }
    }

    public void ChangePlayerPanelColor(Color color)
    {
        Dictionary<int, BroadcastCard> BroadcastRes = ActionManager.Instance.BroadcastRes;
        foreach(var playerId in BroadcastRes.Keys)
        {
            PlayerPanels[playerId].GetComponent<Image>().color = color;
        }
    }

    public void UpdateAllPanels()
    {
        for(int i = 0; i < PlayerPanels.Count; i++)
        {
            UpdateBasePanel(i);
        }
    }
}