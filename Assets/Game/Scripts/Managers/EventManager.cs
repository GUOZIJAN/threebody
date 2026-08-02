using System;

public static class EventManager
{
    public static Action OnTurnStart;
    public static Action<int> OnPlayerEliminate;
    public static Action<Card> OnDrawCard;
    public static Action<int,Card> OnPlayCard;
    public static Action<PlayerData,Galaxy> OnFly;
    public static Action OnPlayerChooseBroadcast;
    public static Action AfterPlayerChooseBroadcast;
    public static Action OnGameStart;
    public static Action<TurnPhase> OnPhaseChanged;
    public static Action<int> OnGalaxyStateChanged;  // int = galaxyId
    public static Action<int> OnGameOver;             // int = winnerId (-1 表示无人生还)
}