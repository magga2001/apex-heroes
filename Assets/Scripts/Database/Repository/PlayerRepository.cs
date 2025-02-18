using System;
using System.Collections;
using UnityEngine;

public class PlayerRepository : MonoBehaviour
{
    public static PlayerData GetPlayerData(int playerId)
    {
        string query = $"SELECT * FROM players WHERE player_id = {playerId};";
        var table = DatabaseManager.FetchData(query);

        if (table.Rows.Count > 0)
        {
            var row = table.Rows[0];
            return new PlayerData
            {
                PlayerId = Convert.ToInt32(row["player_id"]),
                Username = row["username"].ToString(),
                Email = row["email"].ToString(),
                WalletAddress = row["wallet_address"].ToString(),
                XP = Convert.ToInt32(row["xp"]),
                Coins = Convert.ToInt32(row["coins"])
            };
        }

        return null; // Player not found
    }

    public static void UpdatePlayerStats(int playerId, int xp, int coins)
    {
        string query = $"UPDATE player_stats SET xp = {xp}, coins = {coins} WHERE player_id = {playerId};";
        DatabaseManager.ExecuteQuery(query);
    }
}
