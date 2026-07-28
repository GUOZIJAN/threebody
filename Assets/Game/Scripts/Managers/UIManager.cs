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
    public GamePopup GamePopup;

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

    // ==================== 存储 lambda 委托，确保 OnDestroy 能正确取消订阅 ====================
    private System.Action _onTurnStartUpdateBasePanel;
    private System.Action _onTurnStartUpdateStrikePanel;
    private System.Action<int, Card> _onPlayCardUpdateBasePanel;
    private System.Action<int, Card> _onPlayCardHideFoldButton;
    private System.Action<Card> _onDrawCardUpdateCount;
    private System.Action _onPlayerChooseBroadcastHandler;
    private System.Action _onAfterPlayerChooseBroadcastHandler;
    private System.Action _onGameStartUpdateAllPanels;
    private System.Action<int> _onGameOverHandler;

    private void Awake()
    {
        Instance = this;
        Services.Register(this);

        // 创建并存储所有委托（避免匿名 lambda 无法在 OnDestroy 中取消订阅）
        _onTurnStartUpdateBasePanel       = () => UpdateBasePanel(_game.currentPlayerId);
        _onTurnStartUpdateStrikePanel     = () => UpdateStrikePanel(_game.currentPlayerId);
        _onPlayCardUpdateBasePanel        = (id, _) => UpdateBasePanel(id);
        _onPlayCardHideFoldButton         = (_, _) => FoldCardButton.SetActive(false);
        _onDrawCardUpdateCount            = _ => UpdateCardCount();
        _onPlayerChooseBroadcastHandler   = () => ChangePlayerPanelColor(PanelAvailableColor);
        _onAfterPlayerChooseBroadcastHandler = () => ChangePlayerPanelColor(PanelUnavailableColor);
        _onGameStartUpdateAllPanels       = () => UpdateAllPanels();
        _onGameOverHandler                = OnGameOver;

        EventManager.OnTurnStart += ShowUseCardButton;
        EventManager.OnTurnStart += ShowEndTurnButton;
        EventManager.OnTurnStart += ShowFoldCardButton;
        EventManager.OnTurnStart += _onTurnStartUpdateBasePanel;
        EventManager.OnTurnStart += _onTurnStartUpdateStrikePanel;
        EventManager.OnPlayCard += _onPlayCardUpdateBasePanel;
        EventManager.OnPlayCard += UpdateItemPanel;
        EventManager.OnPlayCard += _onPlayCardHideFoldButton;
        EventManager.OnPlayerEliminate += ChangePanelColor;
        EventManager.OnDrawCard += _onDrawCardUpdateCount;
        EventManager.OnFly += UpdateAfterFly;
        EventManager.OnPlayerChooseBroadcast += _onPlayerChooseBroadcastHandler;
        EventManager.AfterPlayerChooseBroadcast += _onAfterPlayerChooseBroadcastHandler;
        EventManager.OnGameStart += _onGameStartUpdateAllPanels;
        EventManager.OnGameOver += _onGameOverHandler;
    }

    private void OnDestroy()
    {
        EventManager.OnTurnStart -= ShowUseCardButton;
        EventManager.OnTurnStart -= ShowEndTurnButton;
        EventManager.OnTurnStart -= ShowFoldCardButton;
        EventManager.OnTurnStart -= _onTurnStartUpdateBasePanel;
        EventManager.OnTurnStart -= _onTurnStartUpdateStrikePanel;
        EventManager.OnPlayCard -= _onPlayCardUpdateBasePanel;
        EventManager.OnPlayCard -= UpdateItemPanel;
        EventManager.OnPlayCard -= _onPlayCardHideFoldButton;
        EventManager.OnPlayerEliminate -= ChangePanelColor;
        EventManager.OnDrawCard -= _onDrawCardUpdateCount;
        EventManager.OnFly -= UpdateAfterFly;
        EventManager.OnPlayerChooseBroadcast -= _onPlayerChooseBroadcastHandler;
        EventManager.AfterPlayerChooseBroadcast -= _onAfterPlayerChooseBroadcastHandler;
        EventManager.OnGameStart -= _onGameStartUpdateAllPanels;
        EventManager.OnGameOver -= _onGameOverHandler;
    }

    private void Update()
    {
        if (_game == null || GamePopup == null) return;

        if (Input.GetKeyDown(KeyCode.Escape) && _game.state == GameState.Gaming)
        {
            if (GamePopup.gameObject.activeSelf)
                GamePopup.Hide();         // 再按 ESC 关闭弹窗
            else
                GamePopup.Show("是否退出游戏");
        }
    }

    private void OnGameOver(int winnerId)
    {
        if (GamePopup == null) return;

        if (winnerId == -1)
            GamePopup.Show("无人生还");
        else if (winnerId == 0)
            GamePopup.Show("你取得胜利");
        else
            GamePopup.Show($"玩家{winnerId}取得胜利");
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
        if (playerId < 0 || playerId >= PlayerPanels.Count) return;

        GameObject targetPanel = PlayerPanels[playerId];
        if (targetPanel == null) return;

        Transform baseInfo = targetPanel.transform.Find("Base");
        if (baseInfo == null) return;

        PlayerData targetPlayer = _players.GetPlayer(playerId);
        if (targetPlayer == null) return;

        var energyText = baseInfo.Find("energy");
        if (energyText != null) energyText.GetComponent<TextMeshProUGUI>().text = $"{targetPlayer.energy}";
        var cardText = baseInfo.Find("card");
        if (cardText != null) cardText.GetComponent<TextMeshProUGUI>().text = $"{targetPlayer.handCards.Count}";
    }

    public void UpdateItemPanel(int playerId, Card card)
    {
        if (playerId < 0 || playerId >= PlayerPanels.Count) return;

        GameObject targetPanel = PlayerPanels[playerId];
        if (targetPanel == null) return;

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
        if (itemPanel == null) return;

        ScrollRect scrollRect = itemPanel.GetComponent<ScrollRect>();
        if (scrollRect == null || scrollRect.content == null) return;

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
        if (_actions == null) return;
        if (playerId < 0 || playerId >= PlayerPanels.Count) return;

        GameObject targetPanel = PlayerPanels[playerId];
        if (targetPanel == null) return;

        Transform strikePanel = targetPanel.transform.Find("Strike_list");
        if (strikePanel == null) return;

        ScrollRect scrollRect = strikePanel.GetComponent<ScrollRect>();
        if (scrollRect == null || scrollRect.content == null) return;

        foreach (Transform child in scrollRect.content)
            Destroy(child.gameObject);

        foreach (var strike in _actions.strikeList)
        {
            if (strike.attackerId == playerId)
            {
                GameObject item = Instantiate(ItemPrefab, scrollRect.content);
                item.GetComponent<TextMeshProUGUI>().text = strike.cardName;
                var targetText = item.transform.Find("target");
                if (targetText != null) targetText.GetComponent<TextMeshProUGUI>().text = strike.targetGalaxyId.ToString();
                var remainText = item.transform.Find("remain");
                if (remainText != null) remainText.GetComponent<TextMeshProUGUI>().text = (strike.totalDistance - strike.remainSteps).ToString();
            }
        }
    }

    public void ChangePanelColor(int playerId)
    {
        if (playerId < 0 || playerId >= PlayerPanels.Count) return;
        PlayerPanels[playerId].GetComponent<Image>().color = Color.red;
    }

    public void UpdateCardCount()
    {
        if (_cards != null)
            CardCountText.text = $"{_cards.deck.Count}";
    }

    public void UpdateAfterFly(PlayerData player, Galaxy targetGalaxy)
    {
        if (player == null) return;
        Transform basePanel = PlayerPanels[player.playerId].transform.Find("Base");
        if (basePanel == null) return;
        var energyText = basePanel.Find("energy");
        if (energyText != null) energyText.GetComponent<TextMeshProUGUI>().text = "0";
        var cardText = basePanel.Find("card");
        if (cardText != null) cardText.GetComponent<TextMeshProUGUI>().text = "0";
        if (player.playerId == 0)
            PlayerGalaxyText.text = $"所在星系: {targetGalaxy.id}";
    }

    public void ChangePlayerPanelColor(Color color)
    {
        if (_actions == null) return;
        foreach (var playerId in _actions.BroadcastRes.Keys)
        {
            if (playerId >= 0 && playerId < PlayerPanels.Count)
                PlayerPanels[playerId].GetComponent<Image>().color = color;
        }
    }

    public void UpdateAllPanels()
    {
        for (int i = 0; i < PlayerPanels.Count; i++)
            UpdateBasePanel(i);
    }
}
