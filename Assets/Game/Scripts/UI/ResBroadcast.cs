using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ResBroadcast : PopupBase<bool>
{
    public Button YesButton;
    public Button NoButton;
    public TextMeshProUGUI BroadcastText;

    private void Awake()
    {
        YesButton.onClick.AddListener(() => Close(true));
        NoButton.onClick.AddListener(() => Close(false));
    }
}