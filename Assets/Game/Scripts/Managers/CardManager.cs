using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance;
    public List<Card> deck = new List<Card>();
    public List<Card> discard = new List<Card>();
    public List<Card> broadcastUsed = new List<Card>();

    [Header("牌堆配置")]
    [Tooltip("拖入 DeckConfigSO，或放在 Resources/ 下自动加载")]
    [SerializeField] private DeckConfigSO deckConfig;

    private void Awake()
    {
        Instance = this;
        Services.Register(this);
    }

    public void InitDeck()
    {
        deck.Clear();

        if (deckConfig == null)
            deckConfig = Resources.Load<DeckConfigSO>("Deck_Standard");

        if (deckConfig == null)
        {
            Debug.LogError("CardManager: deckConfig 未配置且 Resources/Deck_Standard 不存在，无法初始化牌堆！");
            return;
        }

        var configDeck = deckConfig.BuildDeck();
        if (configDeck == null || configDeck.Count == 0)
        {
            Debug.LogError("CardManager: DeckConfigSO 构建牌堆为空！");
            return;
        }

        deck = configDeck;
        Shuffle(deck);
        Debug.Log($"牌堆初始化完成（来自 DeckConfigSO），共 {deck.Count} 张");
    }

    /// <summary>
    /// 重新加载配置并重建牌堆（Editor 热重载用）。
    /// </summary>
    [ContextMenu("从配置重建牌堆")]
    private void ReloadDeckFromConfig()
    {
        InitDeck();
    }

    public void Shuffle(List<Card> list)
    {
        for(int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i,list.Count);
            (list[i],list[r]) = (list[r],list[i]);
        }
    }

    public Card Draw()
    {
        if (deck.Count == 0)
        {
            deck.AddRange(discard);
            deck.AddRange(broadcastUsed);
            discard.Clear();
            broadcastUsed.Clear();
            Shuffle(deck);
        }
        Card c = deck[0];
        deck.RemoveAt(0);
        return c;
    }
}
