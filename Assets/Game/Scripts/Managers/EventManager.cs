using System;

public static class EventManager
{
    public static Action OnTurnStart;
    public static Action OnTurnEnd;
    public static Action<int> OnPlayerEliminate;
    public static Action OnGameWin;
    public static Action<Card> OnDrawCard;
    public static Action<int,Card> OnPlayCard;
    public static Action<PlayerData,Galaxy> OnFly;
    public static Action OnPlayerChooseBroadcast;
    public static Action AfterPlayerChooseBroadcast;
    public static Action OnGameStart;
}