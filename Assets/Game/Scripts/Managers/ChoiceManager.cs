using UnityEngine;

/// <summary>
/// ChoiceManager 在状态机重构后不再持有 TCS。
/// 保留为占位，供未来多人热座/网络选择逻辑使用。
/// </summary>
public class ChoiceManager : MonoBehaviour
{
    public static ChoiceManager Instance;

    private void Awake()
    {
        Instance = this;
        Services.Register(this);
    }
}
