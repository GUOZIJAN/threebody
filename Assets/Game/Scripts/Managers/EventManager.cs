using System;

public static class EventManager
{
    public static Action OnTurnStart;
    public static Action OnTurnEnd;
    public static Action<int> OnPlayerEliminate;
    public static Action OnGameWin;
    public static Action<Card> OnDrawCard;
}