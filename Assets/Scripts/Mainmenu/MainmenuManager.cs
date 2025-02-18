using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using System.Collections;
using System.Threading.Tasks;
using Fusion.Sockets;

public class MainMenuManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Transition Settings")]
    [SerializeField] private CanvasGroup matchSearchingCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;      // Duration of the fade effect
    [SerializeField] private string gameSceneName = "GameScene"; // Name of the game scene
    [SerializeField] private string sessionName = "QuickMatch";  // Shared session name for matchmaking
    [SerializeField] private int maxPlayers = 1;           // Maximum number of players in a room
    [SerializeField] private GameObject playerPrefab;

    private NetworkRunner networkRunner; // Reference to the NetworkRunner
    [SerializeField]private bool isTransitioning = false; // Prevent duplicate transitions

    private void Start()
    {
        // Ensure a NetworkRunner exists
        if (networkRunner == null)
        {
            networkRunner = gameObject.AddComponent<NetworkRunner>();
            DontDestroyOnLoad(networkRunner.gameObject); // Ensure it persists between scenes
            networkRunner.AddCallbacks(this); // Add MainMenuManager as a callback listener
        }
    }

    public void OnPlayButtonClicked()
    {
        // Start the process of joining or creating a room
        JoinOrCreateRoomAsync();
    }

    private async void JoinOrCreateRoomAsync()
    {
        ShowMatchSearchingScreen();

        var joinResult = await networkRunner.JoinSessionLobby(SessionLobby.ClientServer);

        if (joinResult.Ok)
        {
            Debug.Log("Successfully joined a Lobby.");
          
        }
        else
        {
            //Destroy(networkRunner);

            //networkRunner = gameObject.AddComponent<NetworkRunner>();
            //networkRunner.AddCallbacks(this); // Add MainMenuManager as a callback listener

            //// Start the room creation or joining process
            //var startArgs = new StartGameArgs
            //{
            //    GameMode = GameMode.Server,
            //    SessionName = $"{sessionName}_{System.DateTime.Now}",
            //    PlayerCount = 5,
            //    SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            //};

            //var startResult = await networkRunner.StartGame(startArgs);

            //if (startResult.Ok)
            //{
            //    Debug.Log("Successfully created a room.");
            //}
            //else
            //{
            //    Debug.LogError("Failed to join or create room: " + startResult.ShutdownReason);
            //}
        }
    }

    private void ShowMatchSearchingScreen()
    {
        if (matchSearchingCanvasGroup != null)
        {
            matchSearchingCanvasGroup.alpha = 0;
            matchSearchingCanvasGroup.gameObject.SetActive(true);
            StartCoroutine(FadeCanvasGroup(matchSearchingCanvasGroup, 1f, fadeDuration));
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration, System.Action onComplete = null)
    {
        float startAlpha = canvasGroup.alpha;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    // Callback when a player joins
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        //if (player == runner.LocalPlayer)
        //{
        //    Debug.LogWarning("Player is spawned");
        //    runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity);
        //}

        int playerCount = runner.SessionInfo.PlayerCount;
        Debug.Log($"Player joined. Current player count: {playerCount}/{maxPlayers}/{isTransitioning}");

        if (playerCount == maxPlayers + 1 && !isTransitioning)
        {
            Debug.Log($"True and goes to if");
            isTransitioning = true; // Prevent duplicate transitions
            TransitionToGameScene(runner);
        }
    }

    private void TransitionToGameScene(NetworkRunner runner)
    {
        if (runner.IsSceneAuthority)
        {
            //// Get the build index of the target scene
            //int gameSceneIndex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/GameArena.unity");
            //if (gameSceneIndex < 0)
            //{
            //    Debug.LogError("Game scene not found in build settings. Ensure the path is correct and added to Build Settings.");
            //    return;
            //}

            // Create a SceneRef from the scene index
            SceneRef sceneRef = SceneRef.FromIndex(1);

            // Load the scene using Fusion's NetworkRunner.LoadScene
            Debug.Log($"Loading scene: GameArena (index: {1})");
            //runner.LoadScene(sceneRef, new LoadSceneParameters(LoadSceneMode.Single));
            runner.UnloadScene(SceneRef.FromIndex(0));
            runner.LoadScene(sceneRef);
        }
        else
        {
            Debug.LogWarning("Only the server/host can trigger the scene transition.");
        }
    }


    // Callback when a player leaves
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player left. Current player count: {runner.SessionInfo.PlayerCount}/{maxPlayers}");
    }

    // Unused INetworkRunnerCallbacks (required for interface)

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) 
    {
        if (sessionList.Count > 0)
        {
            Debug.Log($"Session count is {sessionList.Count}");
            foreach (var session in sessionList)
            {
                if(session.IsOpen)
                {
                    runner.StartGame(new StartGameArgs()
                    {
                        GameMode = GameMode.Client, // act as a Client
                        SessionName = session.Name  
                    });
                    return;
                }
            }
            // Join
            Debug.Log("No Sessions available to join");
        }
        else
        {
            Debug.Log("No Sessions Available");
            StartNewSession(runner); // Act as a server
          
        }
    }
    public void StartNewSession(NetworkRunner runner)
    {
        var startArgs = new StartGameArgs
        {
            GameMode = GameMode.Server,
            SessionName = $"{sessionName}_{System.DateTime.Now}",
            PlayerCount = maxPlayers,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };
        runner.StartGame(startArgs);
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject networkObject, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject networkObject, PlayerRef player) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) 
    {
    
    }
}