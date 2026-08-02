using UnityEngine;

/// <summary>
/// 单张卡牌的配置模板（ScriptableObject）。
/// 在 Unity Editor 中右键 → Create → DarkForest → Card Config 即可创建。
/// 策划无需改代码，直接在 Inspector 中填写卡牌参数。
/// </summary>
[CreateAssetMenu(menuName = "DarkForest/Card Config", fileName = "Card_")]
public class CardConfigSO : ScriptableObject
{
    [Header("基础信息")]
    [Tooltip("唯一标识，如 strike_light_bolt")]
    public string cardId;

    [Tooltip("卡牌名称，如'光粒打击'")]
    public string cardName;

    [TextArea(2, 4)]
    [Tooltip("卡牌描述文本，显示在卡面上")]
    public string description;

    [Tooltip("卡面图片")]
    public Sprite cardSprite;

    [Tooltip("卡牌类型")]
    public CardType cardType;

    [Tooltip("打出消耗的能量")]
    public int cost;

    [Header("牌组配比")]
    [Tooltip("标准牌堆中放入几张此牌")]
    [Min(0)]
    public int countInDeck;

    // ==================== 广播牌专属 ====================
    [Header("广播牌专属")]
    [Tooltip("合作 / 伪装")]
    public BroadcastChoice broadcastChoice;

    [Tooltip("广播可覆盖的距离范围")]
    public int broadcastDistance;

    // ==================== 打击牌专属 ====================
    [Header("打击牌专属")]
    [Tooltip("打击伤害值")]
    public int damage;

    [Tooltip("打击附加效果")]
    public StrikeEffect strikeEffect;

    // ==================== 建设牌专属 ====================
    [Header("建设牌专属")]
    [Tooltip("防御值")]
    public int defense;

    [Tooltip("每回合产能")]
    public int produce;

    [Tooltip("产能是否需要星系有恒星")]
    public bool needSun;

    [Tooltip("建设牌附加效果")]
    public BuildEffect buildEffect;

    /// <summary>
    /// 根据此配置创建一张运行时 Card 对象。
    /// </summary>
    public Card CreateCard()
    {
        switch (cardType)
        {
            case CardType.Broadcast:
                return new BroadcastCard(
                    cardType,
                    cost,
                    cardName,
                    broadcastChoice,
                    broadcastDistance
                );

            case CardType.Strike:
                return new StrikeCard(
                    cardType,
                    cost,
                    cardName,
                    damage,
                    strikeEffect
                );

            case CardType.Build:
                return new BuildCard(
                    cardType,
                    cost,
                    cardName,
                    defense,
                    produce,
                    needSun,
                    buildEffect
                );

            default:
                Debug.LogError($"CardConfigSO: 未知的卡牌类型 {cardType}，cardId={cardId}");
                return null;
        }
    }
}
