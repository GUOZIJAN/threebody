using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance;
    public List<Card> deck = new List<Card>();
    public List<Card> discard = new List<Card>();
    public List<Card> broadcastUsed = new List<Card>();

    [Header("ScriptableObject 配置（可选）")]
    [Tooltip("拖入 DeckConfigSO 后，可调用 InitDeckFromConfig() 从配置构建牌堆")]
    [SerializeField] private DeckConfigSO deckConfig;

    private void Awake()
    {
        Instance = this;
    }
    
    public void InitDeck()
    {
        deck.Clear();
        for(int i = 0; i < 4; i++)
        {
            deck.Add(new BroadcastCard(CardType.Broadcast,1,"宇宙广播",BroadcastChoice.Fake,2));
            deck.Add(new StrikeCard(CardType.Strike,4,"热核打击",1,StrikeEffect.None));
            deck.Add(new StrikeCard(CardType.Strike,6,"光粒打击",2,StrikeEffect.DestroySun));
            deck.Add(new BuildCard(CardType.Build,3,"聚变反应堆",0,1,false,BuildEffect.None));
        } 
        for(int i = 0; i < 3; i++)
        {
            deck.Add(new StrikeCard(CardType.Strike,8,"湮灭打击",3,StrikeEffect.DestroySunAndBuild));
            deck.Add(new StrikeCard(CardType.Strike,10,"降维打击",10,StrikeEffect.DestroyAll));
            deck.Add(new BuildCard(CardType.Build,8,"量子幽灵",3,0,false,BuildEffect.None));
            deck.Add(new BuildCard(CardType.Build,6,"反物质引擎",0,2,false,BuildEffect.None));
            deck.Add(new BuildCard(CardType.Build,6,"戴森球",0,3,true,BuildEffect.None));
            deck.Add(new StrikeCard(CardType.Strike,4,"科技锁死",0,StrikeEffect.DestroyHand));
        }
        for(int i = 0; i < 9; i++)
        {
            deck.Add(new BroadcastCard(CardType.Broadcast,0,"恒星广播",BroadcastChoice.Cooperate,1));
        }
        for(int i = 0; i < 5; i++)
        {
            deck.Add(new BroadcastCard(CardType.Broadcast,0,"恒星广播",BroadcastChoice.Fake,1));
            deck.Add(new BuildCard(CardType.Build,6,"掩体星环",2,0,false,BuildEffect.None));
            deck.Add(new BuildCard(CardType.Build,2,"太阳能阵列",0,1,true,BuildEffect.None));
            
        }
        for(int i = 0; i < 6; i++)
        {
            deck.Add(new BroadcastCard(CardType.Broadcast,1,"宇宙广播",BroadcastChoice.Cooperate,2));
        }
        for(int i = 0; i < 2; i++)
        {
            deck.Add(new BroadcastCard(CardType.Broadcast,2,"超距广播",BroadcastChoice.Cooperate,10));
            deck.Add(new BroadcastCard(CardType.Broadcast,2,"超距广播",BroadcastChoice.Fake,10));
            deck.Add(new BuildCard(CardType.Build,10,"光速飞船",0,0,false,BuildEffect.Fly));
            deck.Add(new BuildCard(CardType.Build,2,"监听基地",0,0,false,BuildEffect.NoReply));
        } 

        Shuffle(deck);
        Debug.Log("牌堆初始化完成");
    }

    /// <summary>
    /// 从 DeckConfigSO (ScriptableObject) 构建牌堆。
    /// 需要在 Inspector 中拖入 DeckConfigSO 资产。
    /// 如果未配置，回退到硬编码的 InitDeck()。
    /// </summary>
    public void InitDeckFromConfig()
    {
        deck.Clear();

        if (deckConfig == null)
        {
            Debug.LogWarning("CardManager: deckConfig 未配置，回退到默认硬编码牌堆。");
            InitDeck();
            return;
        }

        var configDeck = deckConfig.BuildDeck();
        if (configDeck == null || configDeck.Count == 0)
        {
            Debug.LogWarning("CardManager: DeckConfigSO 构建牌堆为空，回退到默认硬编码牌堆。");
            InitDeck();
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
        InitDeckFromConfig();
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
