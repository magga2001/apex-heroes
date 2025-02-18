using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class FusionSessionManager : MonoBehaviour, IPlayerJoined, IPlayerLeft
{
    public NetworkPrefabRef playerPrefab; // Assign the player prefab in the inspector

    private NetworkRunner runner;

    private void Start()
    {

    }

    public void StartGameSession()
    {
        // Start as Server
        StartSession(GameMode.Server);
    }

    //async void FindAvailableSessions()
    //{
    //    var runner = GetComponent<NetworkRunner>();
    //    var result = await runner.FindGame(new FindGameArgs
    //    {
    //        Lobby = new Photon.Realtime.TypedLobby("DefaultLobby", Photon.Realtime.LobbyType.Default),
    //        MaxResults = 10 // Optional: Limit the number of sessions returned
    //    });

    //    if (result.Ok && result.GameList != null)
    //    {
    //        foreach (var session in result.GameList)
    //        {
    //            Debug.Log($"Session Name: {session.SessionName}, Player Count: {session.PlayerCount}/{session.MaxPlayers}");
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogError("No sessions found or an error occurred.");
    //    }
    //}


    private async void StartSession(GameMode mode)
    {
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true; // Enable input handling

        // Start the network runner
        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            SessionName = "MySession", // Unique session name
            Scene = new NetworkSceneInfo(),
            PlayerCount = 5,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>() // Manages scene loading
        });

        if (result.Ok)
        {
            Debug.Log($"{mode} started successfully.");
        }
        else
        {
            Debug.LogError($"Failed to start {mode}: {result.ShutdownReason}");
        }
    }

    public void JoinSession()
    {
        // Start as Client
        StartSession(GameMode.Client);
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (runner.IsServer)
        {
            // Spawn the player object on the server
            Vector3 spawnPosition = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
            runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        // Handle player disconnection
        Debug.Log($"Player {player} left.");
    }
}