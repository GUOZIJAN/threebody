using System;
using Unity.VisualScripting;

public class Card
{
    public CardType type;
    public int cost;
    public string cardname;

    public Card(CardType type,int cost,string cardname)
    {
        this.type = type;
        this.cost = cost;
        this.cardname = cardname;
    }
}

public class BroadcastCard : Card
{
    public BroadcastChoice choice;
    public int distance;

    public BroadcastCard(CardType type,int cost,string cardname,BroadcastChoice choice,int distance) : base(type, cost, cardname)
    {
        this.choice = choice;
        this.distance = distance;
    }
}

public class StrikeCard : Card
{
    public int damage;
    public StrikeEffect effect;

    public StrikeCard(CardType type,int cost,string cardname,int damage,StrikeEffect effect):base(type, cost, cardname)
    {
        this.damage = damage;
        this.effect = effect;
    }
}

public class BuildCard : Card
{
    public int defense;
    public int produce;
    public bool needSun;
    public BuildEffect effect;

    public BuildCard(CardType type,int cost,string cardname,int defense,int produce,bool needSun,BuildEffect effect):base(type, cost, cardname)
    {
        this.defense = defense;
        this.produce = produce;
        this.needSun = needSun;
        this.effect = effect;
    }
}