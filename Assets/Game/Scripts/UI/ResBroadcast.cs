using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ResBroadcast : PopupBase<bool>
{
    public Button YesButton;
    public Button NoButton;
    public TextMeshProUGUI BroadcastText;
    public Galaxy galaxy;

    private void Awake()
    {
        YesButton.onClick.AddListener(() => CheckClose(true));
        NoButton.onClick.AddListener(() => CheckClose(false));
    }

    protected void CheckClose(bool result)
    {
        if (result == false)
        {
            Debug.Log($"玩家{Player.Instance.data.playerId}拒绝响应广播卡");
            Close(false);
        }
        //只有玩家当前选择的是广播卡才能响应广播卡
        else
        {
            if(Player.Instance.currentCard.type == CardType.Broadcast)
            {
                // 响应广播卡时还需要检查距离是否满足条件
                if(GalaxyManager.Instance.GetDistance(Player.Instance.data.galaxyId, galaxy.id) > Player.Instance.currentCard.distance)
                {
                    Debug.LogWarning("玩家当前选择的广播卡无法响应该广播卡，因为距离超过了广播卡的作用范围");
                }
                else
                {
                    Debug.Log($"玩家{Player.Instance.data.playerId}选择了响应广播卡");
                    Close(true);
                }
            }
            else
            {
                Debug.LogWarning("玩家当前选择的不是广播卡，无法响应广播卡");
            }
        }
    }
}