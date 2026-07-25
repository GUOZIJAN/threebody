using System;
using System.Linq;
using UnityEngine;
using TMPro;

/// <summary>
/// 调试控制台：F1 开关，输入指令执行调试功能。
/// 场景配置：在 Canvas 下创建一个 Panel，内含 TMP_InputField 和可选 TMP_Text（输出）。
///          把此脚本挂到 Panel 或父节点均可（使用 CanvasGroup 控制显隐，不依赖 SetActive）。
/// </summary>
public class DebugConsole : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private TextMeshProUGUI _outputText;

    private CanvasGroup _canvasGroup;
    private bool _visible;

    private GameManager   _game;
    private PlayerManager _players;
    private GalaxyManager _galaxies;
    private CardManager   _cards;
    private SpawnManager  _spawn;
    private UIManager     _ui;

    // ==================== 生命周期 ====================

    private void Start()
    {
        _game     = Services.Get<GameManager>();
        _players  = Services.Get<PlayerManager>();
        _galaxies = Services.Get<GalaxyManager>();
        _cards    = Services.Get<CardManager>();
        _spawn    = Services.Get<SpawnManager>();
        _ui       = Services.Get<UIManager>();

        // 用 CanvasGroup 控制显隐，避免 SetActive(false) 导致 Update 停止
        if (_panel != null)
        {
            _canvasGroup = _panel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = _panel.AddComponent<CanvasGroup>();
        }
        SetVisible(false);

        if (_inputField != null)
            _inputField.onSubmit.AddListener(OnSubmit);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1) && _panel != null)
        {
            SetVisible(!_visible);
            if (_visible && _inputField != null)
            {
                _inputField.text = "";
                _inputField.ActivateInputField();
            }
        }

        if (_visible && Input.GetKeyDown(KeyCode.Escape))
            SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        _visible = visible;
        _canvasGroup.alpha          = visible ? 1f : 0f;
        _canvasGroup.interactable   = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    // ==================== 指令解析 ====================

    private void OnSubmit(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        Execute(input.Trim());
        _inputField.text = "";
        _inputField.ActivateInputField();
    }

    private void Execute(string input)
    {
        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;
        string cmd = parts[0].ToLower();

        try
        {
            switch (cmd)
            {
                case "give":   CmdGive(parts);   break;
                case "kill":   CmdKill(parts);   break;
                case "move":   CmdMove(parts);   break;
                case "energy": CmdEnergy(parts); break;
                case "des":    CmdDes(parts);    break;
                case "debug":  CmdDebug();       break;
                default: Log($"未知指令: {cmd}"); break;
            }
        }
        catch (Exception e)
        {
            Log($"指令异常: {e.Message}");
        }
    }

    // ==================== 指令实现 ====================

    /// <summary>give &lt;playerId&gt; &lt;cardName&gt; — 替换玩家最右侧手牌</summary>
    private void CmdGive(string[] parts)
    {
        if (parts.Length < 3) { Log("用法: give <playerId> <cardName>"); return; }
        if (!int.TryParse(parts[1], out int playerId)) { Log($"无效玩家ID: {parts[1]}"); return; }

        var pd = _players.GetPlayer(playerId);
        if (pd == null) { Log($"玩家{playerId}不存在"); return; }

        string cardName = parts[2];
        Card card = FindCard(cardName);
        if (card == null) { Log($"未找到卡牌: {cardName}"); return; }

        // 移除最右侧牌（数据）
        if (pd.handCards.Count > 0)
        {
            _cards.discard.Add(pd.handCards[pd.handCards.Count - 1]);
            pd.handCards.RemoveAt(pd.handCards.Count - 1);
        }

        // 移除最右侧视觉牌
        if (playerId == 0 && _spawn.handCards.Count > 0)
            _spawn.RemoveCardFromHand(_spawn.handCards[_spawn.handCards.Count - 1].gameObject, reposition: false);

        // 添加新牌
        pd.handCards.Add(card);
        if (playerId == 0)
            EventManager.OnDrawCard?.Invoke(card);

        _ui.UpdateBasePanel(playerId);
        Log($"玩家{playerId}获得 {cardName}");
    }

    /// <summary>kill &lt;playerId&gt; — 淘汰玩家</summary>
    private void CmdKill(string[] parts)
    {
        if (parts.Length < 2) { Log("用法: kill <playerId>"); return; }
        if (!int.TryParse(parts[1], out int playerId)) { Log($"无效玩家ID: {parts[1]}"); return; }

        var pd = _players.GetPlayer(playerId);
        if (pd == null) { Log($"玩家{playerId}不存在"); return; }
        if (!pd.isAlive) { Log($"玩家{playerId}已被淘汰"); return; }

        pd.isAlive = false;
        _game.remainPlayers--;
        _galaxies.GetGalaxy(pd.galaxyId).ownerPlayerId = -1;
        EventManager.OnPlayerEliminate?.Invoke(playerId);
        _game.GameOver();
        Log($"玩家{playerId}已被淘汰");
    }

    /// <summary>move &lt;playerId&gt; &lt;galaxyId&gt; — 移动玩家到星系</summary>
    private void CmdMove(string[] parts)
    {
        if (parts.Length < 3) { Log("用法: move <playerId> <galaxyId>"); return; }
        if (!int.TryParse(parts[1], out int playerId)) { Log($"无效玩家ID: {parts[1]}"); return; }
        if (!int.TryParse(parts[2], out int galaxyId)) { Log($"无效星系ID: {parts[2]}"); return; }

        var pd = _players.GetPlayer(playerId);
        if (pd == null) { Log($"玩家{playerId}不存在"); return; }

        Galaxy target = _galaxies.GetGalaxy(galaxyId);
        if (!target.isAlive) { Log($"星系{galaxyId}已被摧毁"); return; }

        _galaxies.GetGalaxy(pd.galaxyId).ownerPlayerId = -1;
        pd.galaxyId = galaxyId;
        target.ownerPlayerId = playerId;

        if (playerId == 0)
            _ui.UpdateAfterFly(pd, target);

        Log($"玩家{playerId} → 星系{galaxyId}");
    }

    /// <summary>energy &lt;playerId&gt; &lt;value&gt; — 设置玩家能量</summary>
    private void CmdEnergy(string[] parts)
    {
        if (parts.Length < 3) { Log("用法: energy <playerId> <value>"); return; }
        if (!int.TryParse(parts[1], out int playerId)) { Log($"无效玩家ID: {parts[1]}"); return; }
        if (!int.TryParse(parts[2], out int value)) { Log($"无效数值: {parts[2]}"); return; }

        var pd = _players.GetPlayer(playerId);
        if (pd == null) { Log($"玩家{playerId}不存在"); return; }

        pd.energy = value;
        _ui.UpdateBasePanel(playerId);
        Log($"玩家{playerId}能量 → {value}");
    }

    /// <summary>des &lt;playerId&gt; — 摧毁玩家所有建筑</summary>
    private void CmdDes(string[] parts)
    {
        if (parts.Length < 2) { Log("用法: des <playerId>"); return; }
        if (!int.TryParse(parts[1], out int playerId)) { Log($"无效玩家ID: {parts[1]}"); return; }

        var pd = _players.GetPlayer(playerId);
        if (pd == null) { Log($"玩家{playerId}不存在"); return; }

        int count = pd.buildCards.Count;
        pd.buildCards.Clear();
        _ui.UpdateBasePanel(playerId);
        Log($"摧毁玩家{playerId}的{count}个建筑");
    }

    /// <summary>debug — 输出所有玩家信息到控制台</summary>
    private void CmdDebug()
    {
        foreach (var pd in _players.Players)
        {
            Galaxy g = _galaxies.GetGalaxy(pd.galaxyId);
            string info = $"[Debug] 玩家{pd.playerId}: ";
            info += pd.isAlive ? "存活" : "已淘汰";
            info += $", 能量={pd.energy}, 手牌={pd.handCards.Count}, 建筑={pd.buildCards.Count}";
            info += $", 星系{pd.galaxyId}";
            if (!g.isAlive) info += "(已摧毁)";
            if (!g.haveSun) info += "(无阳光)";
            Debug.Log(info);
        }
        Log("已输出所有玩家信息到 Console");
    }

    // ==================== 辅助 ====================

    /// <summary>在牌堆、弃牌堆、已用广播中搜索卡牌（不偷其他玩家的手牌）</summary>
    private Card FindCard(string cardName)
    {
        Card match = _cards.deck.FirstOrDefault(c => c.cardname == cardName);
        if (match != null) { _cards.deck.Remove(match); return match; }

        match = _cards.discard.FirstOrDefault(c => c.cardname == cardName);
        if (match != null) { _cards.discard.Remove(match); return match; }

        match = _cards.broadcastUsed.FirstOrDefault(c => c.cardname == cardName);
        if (match != null) { _cards.broadcastUsed.Remove(match); return match; }

        return null;
    }

    private void Log(string msg)
    {
        Debug.Log($"[Console] {msg}");
        if (_outputText != null)
            _outputText.text = msg;
    }
}
