using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 牌组配置（ScriptableObject），聚合所有 CardConfigSO。
/// 在 Unity Editor 中右键 → Create → DarkForest → Deck Config 即可创建。
/// 策划在 Inspector 中将所有 CardConfigSO 拖入列表，调整每张牌的 countInDeck 即可控制配比。
/// </summary>
[CreateAssetMenu(menuName = "DarkForest/Deck Config", fileName = "Deck_Standard")]
public class DeckConfigSO : ScriptableObject
{
    [Header("牌组")]
    [Tooltip("构成牌堆的所有卡牌配置")]
    public List<CardConfigSO> cards;

    /// <summary>
    /// 根据所有 CardConfigSO 的 countInDeck 生成实际牌堆列表。
    /// 每张 CardConfigSO 会被复制 countInDeck 份。
    /// </summary>
    public List<Card> BuildDeck()
    {
        var deck = new List<Card>();

        if (cards == null || cards.Count == 0)
        {
            Debug.LogWarning("DeckConfigSO: cards 列表为空，无法构建牌堆。");
            return deck;
        }

        foreach (var config in cards)
        {
            if (config == null)
            {
                Debug.LogWarning("DeckConfigSO: cards 列表中存在空引用，已跳过。");
                continue;
            }

            if (config.countInDeck <= 0)
                continue;

            for (int i = 0; i < config.countInDeck; i++)
            {
                var card = config.CreateCard();
                if (card != null)
                {
                    deck.Add(card);
                }
            }
        }

        Debug.Log($"DeckConfigSO: 牌堆构建完成，共 {deck.Count} 张卡牌。");
        return deck;
    }

    /// <summary>
    /// 验证配置完整性，Editor 中可手动调用检查。
    /// </summary>
    [ContextMenu("验证配置")]
    private void ValidateConfig()
    {
        if (cards == null || cards.Count == 0)
        {
            Debug.LogError("DeckConfigSO: cards 列表为空！");
            return;
        }

        int totalCount = 0;
        foreach (var config in cards)
        {
            if (config == null)
            {
                Debug.LogError("DeckConfigSO: 列表中存在空引用！");
                continue;
            }
            if (string.IsNullOrEmpty(config.cardId))
            {
                Debug.LogWarning($"DeckConfigSO: 卡牌 {config.name} 的 cardId 为空。");
            }
            totalCount += config.countInDeck;
        }

        Debug.Log($"DeckConfigSO 验证完成: {cards.Count} 种卡牌，共 {totalCount} 张。");
    }
}
