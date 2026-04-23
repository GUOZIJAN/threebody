using System.Collections.Generic;

public class PlayerData
{
    public int playerId;
    public int galaxyId;
    public int energy;
    public List<Card> handCards = new List<Card>();
    public List<Card> buildCards = new List<Card>();
    public bool isAlive = true;
    public int lastBroadcastGalaxy = -1;
}