[System.Serializable]
public class Rank
{
    public int rankNumber;
    public string playerName;
    public int totalPoints;

    public Rank(int rankNumber, string playerName, int totalPoints)
    {
        this.rankNumber = rankNumber;
        this.playerName = playerName;
        this.totalPoints = totalPoints;
    }
}

