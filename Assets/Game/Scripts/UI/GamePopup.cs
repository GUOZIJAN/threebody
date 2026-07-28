using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 通用游戏弹窗：显示消息 + 重新开始 / 退出游戏 两个按钮。
/// 用于 ESC 暂停弹窗和游戏结束弹窗。
/// </summary>
public class GamePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _quitButton;

    private void Awake()
    {
        _restartButton.onClick.AddListener(OnRestart);
        _quitButton.onClick.AddListener(OnQuit);
    }

    /// <summary>显示弹窗</summary>
    public void Show(string message)
    {
        _messageText.text = message;
        gameObject.SetActive(true);
    }

    /// <summary>隐藏弹窗</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnRestart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
