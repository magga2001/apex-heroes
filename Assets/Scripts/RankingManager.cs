using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class RankingManager : NetworkBehaviour
{
    public static RankingManager Instance { get; private set; }

    private List<Player> playersList = new List<Player>();

    [SerializeField]
    public int deadPlayersCount;

    public void UpdateDeadPlayerCount(Player _player)
    {
        deadPlayersCount++;

        foreach (Player player in playersList)
        {
            if (player == _player)
            {

                break;
            }
        }
    }

    public void RegisterPlayer(Player player)
    {
        playersList.Add(player);
    }

    //public override void FixedUpdateNetwork()
    //{
    //    foreach (var change in _changeDetector.DetectChanges(this))
    //    {
    //        switch (change)
    //        {
    //            case nameof(deadPlayersCount):
    //                Debug.Log($"Changes being done: {change}");
    //                break;
    //        }
    //    }
    //}

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; // Set the singleton instance
            DontDestroyOnLoad(gameObject); // Ensure it persists across scene loads
        }
        else
        {
            Destroy(gameObject); // Prevent duplicate instances
        }
    }
}
