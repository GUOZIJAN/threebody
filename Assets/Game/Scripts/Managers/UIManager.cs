using System.Collections.Generic;
using UnityEngine;
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
    private TurnFlow _turnFlow;
    private GameManager _game;
    private PlayerManager _players;
    private ActionManager _actions;
    private Player _player;
    private SpawnManager _spawn;
    private CardManager _cards;

    private Color PanelAvailableColor   = new Color32(95, 255, 0, 100);
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
        EventManager.OnPlayCard += (id, _) => UpdateBasePanel(id);
        EventManager.OnPlayCard += UpdateItemPanel;
        EventManager.OnPlayCard += (_, _) => FoldCardButton.SetActive(false);
        EventManager.OnPlayerEliminate += ChangePanelColor;
        EventManager.OnDrawCard += _ => UpdateCardCount();
        EventManager.OnFly += UpdateAfterFly;
        EventManager.OnPlayerChooseBroadcast += () => ChangePlayerPanelColor(PanelAvailableColor);
        EventManager.AfterPlayerChooseBroadcast += () => ChangePlayerPanelColor(PanelUnavailableColor);
        EventManager.OnGameStart += () => UpdateAllPanels();
    }

    private void OnDestroy()
    {
        EventManager.OnTurnStart -= ShowUseCardButton;
        EventManager.OnTurnStart -= ShowEndTurnButton;
        EventManager.OnTurnStart -= ShowFoldCardButton;
        EventManager.OnPlayCard -= UpdateItemPanel;
        EventManager.OnPlayerEliminate -= ChangePanelColor;
        EventManager.OnFly -= UpdateAfterFly;
    }

    public void Init()
    {
        _turnFlow = Services.Get<TurnFlow>();
        _game     = Services.Get<GameManager>();
        _players  = Services.Get<PlayerManager>();
        _actions  = Services.Get<ActionManager>();
        _player   = Services.Get<Player>();
        _spawn    = Services.Get<SpawnManager>();
        _cards    = Services.Get<CardManager>();

        PlayerGalaxyText.text = $"所在星系: {_players.GetPlayer(0).galaxyId}";
    }

    // ==================== 按钮回调 → TurnFlow ====================

    public void OnUseCardButtonClicked()
    {
        _turnFlow.OnPlayCardClicked();
    }

    public void OnEndTurnButtonClicked()
    {
        _turnFlow.OnEndTurnClicked();
    }

    public void OnGameStartButtonClicked()
    {
        GameStartButton.SetActive(false);
        _game.GameStart();
    }

    public void OnFoldCardButtonClicked()
    {
        _turnFlow.OnFoldCardsClicked();
    }

    public void OnConfirmFoldButtonClicked()
    {
        _turnFlow.OnFoldConfirmed();
    }

    // ==================== 弃牌 UI 状态 ====================

    public void EnterFoldMode()
    {
        UseCardButton.SetActive(false);
        EndTurnButton.SetActive(false);
        FoldCardButton.SetActive(false);
        ConfirmFoldButton.SetActive(true);
    }

    public void ExitFoldMode()
    {
        ConfirmFoldButton.SetActive(false);
    }

    // ==================== 按钮显隐 ====================

    public void ShowUseCardButton()
    {
        UseCardButton.SetActive(_game.currentPlayerId == 0);
    }

    public void ShowEndTurnButton()
    {
        EndTurnButton.SetActive(_game.currentPlayerId == 0);
    }

    public void ShowFoldCardButton()
    {
        FoldCardButton.SetActive(_game.currentPlayerId == 0);
    }

    // ==================== 面板更新 ====================

    public void UpdateBasePanel(int playerId)
    {
        GameObject targetPanel = PlayerPanels[playerId];
        PlayerData targetPlayer = _players.GetPlayer(playerId);
        Transform baseInfo = targetPanel.transform.Find("Base");
        baseInfo.Find("energy").GetComponent<TextMeshProUGUI>().text = $"{targetPlayer.energy}";
        baseInfo.Find("card").GetComponent<TextMeshProUGUI>().text = $"{targetPlayer.handCards.Count}";
    }

    public void UpdateItemPanel(int playerId, Card card)
    {
        GameObject targetPanel = PlayerPanels[playerId];
        Transform itemPanel = null;
        switch (card.type)
        {
            case CardType.Broadcast:
                itemPanel = targetPanel.transform.Find("Broadcast_list");
                break;
            case CardType.Build:
                itemPanel = targetPanel.transform.Find("Build_list");
                break;
            case CardType.Strike:
                UpdateStrikePanel(playerId);
                return;
        }
        ScrollRect scrollRect = itemPanel.GetComponent<ScrollRect>();
        GameObject item = Instantiate(ItemPrefab, scrollRect.content);
        item.GetComponent<TextMeshProUGUI>().text = $"{card.cost}  {card.cardname}";
    }

    /// <summary>获取玩家面板的世界坐标（供 CardAnimator 使用）</summary>
    public Vector3 GetPlayerPanelPosition(int playerId)
    {
        if (playerId < 0 || playerId >= PlayerPanels.Count) return Vector3.zero;
        return PlayerPanels[playerId].transform.position;
    }

    /// <summary>获取玩家建筑列表中的所有 GameObject</summary>
    public List<GameObject> GetBuildPanelItems(int playerId)
    {
        var items = new List<GameObject>();
        Transform buildPanel = PlayerPanels[playerId].transform.Find("Build_list");
        if (buildPanel == null) return items;

        ScrollRect scrollRect = buildPanel.GetComponent<ScrollRect>();
        foreach (Transform child in scrollRect.content)
            items.Add(child.gameObject);
        return items;
    }

    /// <summary>清除建筑面板（不带动画）</summary>
    public void ClearBuildPanel(int playerId)
    {
        Transform buildPanel = PlayerPanels[playerId].transform.Find("Build_list");
        if (buildPanel == null) return;
        ScrollRect scrollRect = buildPanel.GetComponent<ScrollRect>();
        foreach (Transform child in scrollRect.content)
            Destroy(child.gameObject);
    }

    public void UpdateStrikePanel(int playerId)
    {
        GameObject targetPanel = PlayerPanels[playerId];
        Transform strikePanel = targetPanel.transform.Find("Strike_list");
        ScrollRect scrollRect = strikePanel.GetComponent<ScrollRect>();

        foreach (Transform child in scrollRect.content)
            Destroy(child.gameObject);

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
        PlayerPanels[playerId].GetComponent<Image>().color = Color.red;
    }

    public void UpdateCardCount()
    {
        CardCountText.text = $"{_cards.deck.Count}";
    }

    public void UpdateAfterFly(PlayerData player, Galaxy targetGalaxy)
    {
        Transform basePanel = PlayerPanels[player.playerId].transform.Find("Base");
        basePanel.Find("energy").GetComponent<TextMeshProUGUI>().text = "0";
        basePanel.Find("card").GetComponent<TextMeshProUGUI>().text = "0";
        if (player.playerId == 0)
            PlayerGalaxyText.text = $"所在星系: {targetGalaxy.id}";
    }

    public void ChangePlayerPanelColor(Color color)
    {
        foreach (var playerId in _actions.BroadcastRes.Keys)
            PlayerPanels[playerId].GetComponent<Image>().color = color;
    }

    public void UpdateAllPanels()
    {
        for (int i = 0; i < PlayerPanels.Count; i++)
            UpdateBasePanel(i);
    }
}
