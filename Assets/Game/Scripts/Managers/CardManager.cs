using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance;
    public List<Card> deck = new List<Card>();
    public List<Card> discard = new List<Card>();
    public List<Card> broadcastUsed = new List<Card>();

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

        Debug.Log("牌堆初始化完成");
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
        EventManager.OnDrawCard?.Invoke(c);
        return c;
    }
}
