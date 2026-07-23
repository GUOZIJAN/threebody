using System;
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
    public TextMeshProUGUI PlayerGalaxyText;
    public List<GameObject> PlayerPanels;

    // 缓存的依赖
    private GameManager _game;
    private PlayerManager _players;
    private ActionManager _actions;
    private ChoiceManager _choice;
    private Player _player;
    private SpawnManager _spawn;
    private CardManager _cards;

    private Color PanelAvailableColor = new Color32(95, 255, 0, 100);
    private Color PanelUnavailableColor = new Color32(255, 255, 255, 100);

    private void Awake()
    {
        Instance = this;
        Services.Register(this);
        EventManager.OnTurnStart += ShowUseCardButton;
        EventManager.OnTurnStart += ShowEndTurnButton;
        EventManager.OnTurnStart += ShowFoldCardButton;
        EventManager.OnTurnStart += () => UpdateBasePanel(_game.currentPlayerId);
        EventManager.OnTurnStart += () => UpdateStrikePanel(_game.currentPlayerId);
        EventManager.OnPlayCard += UpdateBasePanel;
        EventManager.OnPlayCard += UpdateItemPanel;
        EventManager.OnPlayCard += (id, card) => FoldCardButton.SetActive(false);
        EventManager.OnPlayerEliminate += ChangePanelColor;
        EventManager.OnDrawCard += (card) => UpdateCardCount();
        EventManager.OnFly += UpdateAfterFly;
        EventManager.OnPlayerChooseBroadcast += () => ChangePlayerPanelColor(PanelAvailableColor);
        EventManager.AfterPlayerChooseBroadcast += () => ChangePlayerPanelColor(PanelUnavailableColor);
        EventManager.OnGameStart += () => UpdateAllPanels();
    }

    public void Init()
    {
        _game    = Services.Get<GameManager>();
        _players = Services.Get<PlayerManager>();
        _actions = Services.Get<ActionManager>();
        _choice  = Services.Get<ChoiceManager>();
        _player  = Player.Instance;
        _spawn   = Services.Get<SpawnManager>();
        _cards   = Services.Get<CardManager>();

        PlayerGalaxyText.text = $"所在星系: {_players.GetPlayer(0).galaxyId}";
    }

    public void ShowUseCardButton()
    {
        if(_game.currentPlayerId == 0)
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
        if(_game.currentPlayerId == 0)
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
        if(_game.currentPlayerId == 0)
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
        try { await UseCardAsync(); }
        catch (Exception e) { Debug.LogError($"使用卡牌失败: {e}"); }
    }

    private async Task UseCardAsync()
    {
        if (_game.currentCard == null)
        {
            Debug.Log("没有选中卡牌！");
            return;
        }
        await _actions.UseCard(_game.currentPlayerId, _game.currentCard.GetComponent<CardView>().card);
        _game.currentCard = null;
        _player.currentCard = null;
    }

    public void OnEndTurnButtonClicked()
    {
        Debug.Log("结束回合按钮被点击了！");
        _choice.OnPlayerTurnEnd();
    }

    public async void OnGameStartButtonClicked()
    {
        try { await GameStartAsync(); }
        catch (Exception e) { Debug.LogError($"游戏启动失败: {e}"); }
    }

    private async Task GameStartAsync()
    {
        GameStartButton.SetActive(false);
        _game.GameStart();
        await _game.GameCircle();
    }

    public async void OnFoldCardButtonClicked()
    {
        try { await FoldCardsAsync(); }
        catch (Exception e) { Debug.LogError($"弃牌失败: {e}"); }
    }

    private async Task FoldCardsAsync()
    {
        UseCardButton.SetActive(false);
        EndTurnButton.SetActive(false);
        FoldCardButton.SetActive(false);
        ConfirmFoldButton.SetActive(true);

        if (_game.currentCard != null)
        {
            _game.currentCard.GetComponent<CardView>().MoveCardDown();
            _game.currentCard = null;
            _player.currentCard = null;
        }

        List<GameObject> foldedCards = await _choice.FoldCards();

        foreach (GameObject card in foldedCards)
        {
            _players.GetPlayer(_game.currentPlayerId).handCards.Remove(card.GetComponent<CardView>().card);
            _spawn.RemoveCardFromHand(card);
            _cards.discard.Add(card.GetComponent<CardView>().card);
            Debug.Log($"玩家{_game.currentPlayerId}弃掉了一张牌，当前手牌数量：{_players.GetPlayer(_game.currentPlayerId).handCards.Count}");
            foreach (Card c in _players.GetPlayer(_game.currentPlayerId).handCards)
            {
                Debug.Log($"玩家{_game.currentPlayerId}的手牌中还有：{c.cardname}");
            }
        }

        UpdateBasePanel(_game.currentPlayerId);
        ConfirmFoldButton.SetActive(false);
        _choice.OnPlayerTurnEnd();
    }

    public void OnConfirmFoldButtonClicked()
    {
        _choice.OnCardsFolded();
    }

    public void UpdateBasePanel(int playerId)
    {
        // 根据playerId更新对应的玩家面板UI
        // 例如，更新玩家的能量、手牌等信息
        GameObject targetPanel = PlayerPanels[playerId];
        PlayerData targetPlayer = _players.GetPlayer(playerId);
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
        PlayerData targetPlayer = _players.GetPlayer(playerId);
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
        
        foreach (var strike in _actions.strikeList)
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
        CardCountText.text = $"{_cards.deck.Count}"; // 更新牌堆剩余卡牌数量
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
        Dictionary<int, BroadcastCard> BroadcastRes = _actions.BroadcastRes;
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