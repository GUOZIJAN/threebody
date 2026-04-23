public enum CardType
{
    Broadcast,
    Strike,
    Build
}

public enum BroadcastChoice
{
    Cooperate,
    Fake
}

public enum GameState
{
    Prepare,
    Gaming,
    GameOver
}

public enum PlayerAction
{
    None,
    DiscardAndDraw,
    PlayCard
}

public enum BuildEffect
{
    None,
    Fly,
    NoReply,
}

public enum StrikeEffect
{
    None,
    DestroySun,
    DestroySunAndBuild,
    DestroyAll,
    DestroyHand,
}
