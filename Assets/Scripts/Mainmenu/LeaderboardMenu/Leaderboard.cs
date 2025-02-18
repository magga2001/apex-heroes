using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class Leaderboard : MonoBehaviour
{
    [SerializeField] private Transform content; // Content object in the Scroll View
    [SerializeField] private GameObject rankPrefab; // Prefab for each leaderboard entry

    private List<Rank> leaderboardData;

    private void Start()
    {

        // TODO: Get it from database in the future

        // Create static leaderboard data
        leaderboardData = new List<Rank>
        {
            new Rank(1, "PlayerOne", 1500),
            new Rank(2, "PlayerTwo", 1400),
            new Rank(3, "PlayerThree", 1350),
            new Rank(4, "PlayerFour", 1300),
            new Rank(5, "PlayerFive", 1250),
            new Rank(6, "PlayerSix", 1200),
            new Rank(7, "PlayerSeven", 1150),
            new Rank(8, "PlayerEight", 1100),
            new Rank(9, "PlayerNine", 1050),
            new Rank(10, "PlayerTen", 1000),
        };

        PopulateLeaderboard();
    }

    private void PopulateLeaderboard()
    {
        foreach (Rank rank in leaderboardData)
        {
            // Instantiate the rank prefab
            GameObject newEntry = Instantiate(rankPrefab, content);

            // Assign values to the prefab's UI components
            TextMeshProUGUI[] textFields = newEntry.GetComponentsInChildren<TextMeshProUGUI>();
            textFields[0].text = rank.rankNumber.ToString(); // Rank number
            textFields[1].text = rank.playerName;           // Player name
            textFields[2].text = rank.totalPoints.ToString(); // Total points
        }
    }
}

