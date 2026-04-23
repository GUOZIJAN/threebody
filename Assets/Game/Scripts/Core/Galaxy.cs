using System.Collections.Generic;

public class Galaxy
{
    public int id;
    public List<int> neighborIds = new List<int>();
    public int ownerPlayerId = -1;
}