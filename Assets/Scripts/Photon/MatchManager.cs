using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Fusion.Sockets;
using System.Collections;
using TMPro;
using System.Linq;
using System.Threading.Tasks;

public class MatchManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static MatchManager Instance { get; private set; }
    string playername;

    [Header("Player Settings")]
    [SerializeField] private GameObject playerPrefab;               // Prefab to spawn when a player joins
    [SerializeField] private int maxPlayers = 2;              // Maximum number of players allowed in the match
    [SerializeField] private List<Transform> spawnPositions;  // Spawn points for players

    [Header("UI Settings")]
    [SerializeField] private GameObject mainUI;               // Main game UI
    [SerializeField] private GameObject loadingScreen;        // Loading screen UI
    [SerializeField] private CanvasGroup loadingScreenCanvas; // For fade effects
    [SerializeField] private CanvasGroup matchEndingCanvas;
    [SerializeField] private TextMeshProUGUI statusText;      // Status text UI

    [Header("Pooling Manager Prefabs")]
    [SerializeField] private NetworkPrefabRef ObjectPoolingManagerPrefab;
    [SerializeField] private NetworkPrefabRef MatchObjectPoolingManagerPrefab;
    [SerializeField] private NetworkPrefabRef EffectPoolingManagerPrefab;
    //[SerializeField] private NetworkPrefabRef RankingManagerPrefab;
    int countPlayerno;
    private int currentPlayerCount = 0; // Tracks the current number of players in the room
    private HashSet<PlayerRef> readyPlayers = new HashSet<PlayerRef>();
    private NetworkRunner networkRunner;
    bool isgameover;
    private PlayerRef playerRef;
    int playerlistcount;

    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this; // Set the singleton instance
         //   DontDestroyOnLoad(gameObject); // Ensure it persists across scene loads
        }
        //else
        //{
        //    Destroy(gameObject); // Prevent duplicate instances
        //}

        if (networkRunner == null)
        {
            networkRunner = FindObjectOfType<NetworkRunner>();
            if (networkRunner != null)
            {
                networkRunner.AddCallbacks(this);
            }
            else
            {
                Debug.LogError("No NetworkRunner found!");
            }
        }
        else
        {
            Debug.Log("NetworkRunner already assigned.");
        }
        isgameover = false;
        playerlistcount = maxPlayers;
        playername = "Player";
        countPlayerno = 0;
    }

    private void Start()
    {
        ShowLoadingScreen(true); // Display the loading screen initially
        statusText.text = "Waiting for players...";
        HandleExistingPlayers();
    }

    private void HandleExistingPlayers()
    {
        Debug.Log("Handling existing players...");

        foreach (var player in networkRunner.ActivePlayers)
        {
            Debug.Log($"Existing player found: {player.PlayerId}");
            OnPlayerJoined(networkRunner, player);
        }
    }


    public async void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerJoined called for Player: {player.PlayerId}");

        // Update the current player count
        currentPlayerCount = runner.ActivePlayers.Count();

        statusText.text = $"Players joined: {currentPlayerCount}/{maxPlayers}";

        if (currentPlayerCount <= maxPlayers)
        {
            if (runner.IsServer)
            {

                await SpawnPlayer(runner, player);
            }
        }

        if (currentPlayerCount == maxPlayers)
        {
            Debug.Log("All players joined. Starting match!");
            StartCoroutine(StartMatch());
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player left: {player.PlayerId}");
        currentPlayerCount = runner.SessionInfo.PlayerCount;
        if (statusText != null)
        {
            statusText.text = $"Players joined: {currentPlayerCount}/{maxPlayers}";
        }
        if (runner.IsServer)
        {
            NetworkObject playerObject = runner.GetPlayerObject(player);
            if (playerObject != null)
            {
                Player plyer = playerObject.GetComponent<Player>();
                if (plyer != null &&!isgameover)
                {
                    isgameover = true;
                    Debug.Log("Working on server");
                    plyer.RPC_UpdatePlayerRankingOnLeft();
                    plyer.RPC_RemoveFromPlayerList();
                    StartCoroutine(DespawnAfterDelay(playerObject));
                }
                return;
            }
           
        }
        //if (player == runner.LocalPlayer)
        //{
        //    DisconnectPlayer();
        //}
    }
    private IEnumerator DespawnAfterDelay(NetworkObject playerobj)
    {
        yield return new WaitForSeconds(0.5f);

        // Despawn the object on the server
        if (networkRunner != null)
        {
            networkRunner.Despawn(playerobj);
        }
    }
    public void RemovefromPlayerlist(Player player)
    {
        playersList.Remove(player);
    }
    public async Task SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {

        playerRef = player;
        // Check if the player is already spawned
        if (runner.TryGetPlayerObject(player, out _))
        {
            Debug.LogWarning($"Player {player.PlayerId} is already spawned. Skipping...");
            return;
        }

        Debug.Log($"Spawning player: {player.PlayerId}");

        if (runner.IsServer)
        {
            // Randomly select a spawn point from the list
            int randomIndex = Random.Range(0, spawnPositions.Count);
            Transform randomSpawnPoint = spawnPositions[randomIndex];
            spawnPositions.RemoveAt(randomIndex);

            // Use the selected spawn point's position for spawning
            Vector3 spawnPosition = randomSpawnPoint.position;

            if (playerPrefab != null)
            {
                // Spawn the player prefab
                NetworkObject spawnedPlayer = await runner.SpawnAsync(playerPrefab, spawnPosition, Quaternion.identity, player);
                runner.SetPlayerObject(player, spawnedPlayer);
                countPlayerno++;
                spawnedPlayer.GetComponent<Player>().SetNickname(playername+countPlayerno);
               
                // Check if this is the local player
                if (player == runner.LocalPlayer)
                {
                    Debug.Log("Assigning camera to the local player.");
                    CameraController cameraController = FindObjectOfType<CameraController>();
                    cameraController?.SetTargetPlayer(spawnedPlayer.transform);
                    
                }
            }
            else
            {
                Debug.LogError("Player prefab is null. Cannot spawn.");
            }
        }
        

    }

    //private IEnumerator SignalReady()
    //{
    //    // Simulate any loading tasks here (e.g., loading player assets)
    //    yield return new WaitForSeconds(1f); // Simulated delay for player setup

    //    Debug.Log("Local player is ready. Signaling readiness...");
    //    networkRunner.SendUserSimulationMessage("PlayerReady", null); // Broadcast readiness
    //}

    //public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    //{
    //    if (message.Tag == "PlayerReady")
    //    {
    //        PlayerRef player = message.Sender;
    //        Debug.Log($"Player {player.PlayerId} is ready.");
    //        readyPlayers.Add(player);

    //        CheckAllPlayersReady();
    //    }
    //}

    //private void CheckAllPlayersReady()
    //{
    //    if (readyPlayers.Count == currentPlayerCount && currentPlayerCount == maxPlayers)
    //    {
    //        Debug.Log("All players are ready. Starting the match!");
    //        StartCoroutine(StartMatch());
    //    }
    //}

    private IEnumerator StartMatch()
    {
        Debug.Log("Starting the match coroutine...");
        statusText.text = "Preparing Match...";
        
        yield return new WaitForSeconds(2f); // Optional delay for final preparations
        if (statusText != null)
        {
            statusText.text = "Starting Match...";
        }
        Debug.Log("Activating main UI...");
        mainUI.SetActive(true); // Activate the main game UI
        Debug.Log("Total :" + playerlistcount);
        yield return StartCoroutine(FadeLoadingScreen(0, 1f)); // Fade out the loading screen

        Debug.Log("Match started!");
    }

    private IEnumerator FadeLoadingScreen(float targetAlpha, float duration)
    {
        Debug.Log("Fading out for main UI...");
        float startAlpha = loadingScreenCanvas.alpha;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            loadingScreenCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
            yield return null;
        }
        loadingScreenCanvas.alpha = targetAlpha;

        if (targetAlpha < 0)
        {
            Debug.Log("disable the loading screen");
            loadingScreen.SetActive(false);
        }
       
    }

    public void ShowLoadingScreen(bool show)
    {
        loadingScreen.SetActive(show);
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("Scene loading completed!");

        // Crate Spawner
        var crateSpawner = FindObjectOfType<CrateSpawner>();
        if (crateSpawner != null)
        {
            Debug.Log("CrateSpawner found in the GameArena scene.");
            crateSpawner.Initialise();
        }
        else
        {
            Debug.LogError("CrateSpawner is missing in the scene. Ensure it exists in the GameArena.");
        }

        // Ensure ObjectPoolingManager is spawned by the server
        if (runner.IsServer)
        {
            if (ObjectPoolingManager.Instance == null)
            {
                Debug.Log("Server is spawning ObjectPoolingManager.");
                NetworkObject poolingObject = runner.Spawn(
                    ObjectPoolingManagerPrefab,
                    Vector3.zero,
                    Quaternion.identity
                );
            }

            if (MatchObjectPoolingManager.Instance == null)
            {
                Debug.Log("Server is spawning MatchObjectPoolingManager.");
                NetworkObject poolingObject = runner.Spawn(
                    MatchObjectPoolingManagerPrefab,
                    Vector3.zero,
                    Quaternion.identity
                );
            }

            if (EffectPoolingManager.Instance == null)
            {
                Debug.Log("Server is spawning EffectPoolingManager.");
                NetworkObject poolingObject = runner.Spawn(
                    EffectPoolingManagerPrefab,
                    Vector3.zero,
                    Quaternion.identity
                );
            }

            //if (RankingManager.Instance == null)
            //{
            //    Debug.Log("Server is spawning RankingManager.");
            //    NetworkObject rankingManager = runner.Spawn(
            //        RankingManagerPrefab,
            //        Vector3.zero,
            //        Quaternion.identity
            //    );
            //}
        }
        else
        {
            Debug.Log("Client waiting for server to spawn managers.");
        }
    }

    [SerializeField]
    private List<Player> playersList = new List<Player>();
    [SerializeField] private List<TextMeshProUGUI> rankList = new List<TextMeshProUGUI>();

    [SerializeField]
    public int deadPlayersCount;

    public void UpdateDeadPlayerCount(Player player)
    {
        deadPlayersCount++;
        Debug.Log("Working 111");
        rankList[playerlistcount - deadPlayersCount].text = $"{playerlistcount - (deadPlayersCount - 1)} : {player.Nickname}...";
        if (player.HasInputAuthority)
        {
            rankList[playerlistcount - deadPlayersCount].gameObject.transform.GetChild(0).gameObject.SetActive(true);
        }
        if (deadPlayersCount == playerlistcount-1)
        {
            Player winner = null;

            for (int i = 0; i < playerlistcount; i++)
            {
                if (playersList[i].PlayerMesh.activeInHierarchy) {
                    winner = playersList[i];
                    break;
                }
            }

            rankList[0].text = $"{1} : {winner.Nickname}";
            Debug.Log("Input Autority :" + winner.GetComponent<Player>().HasInputAuthority);
            if (winner.GetComponent<Player>().HasInputAuthority)
            {
               rankList[0].gameObject.transform.GetChild(0).gameObject.SetActive(true);
            }
            if (!isgameover)
            {
                GameOver();
            }
            winner.gameObject.SetActive(false);
            return;
        }
        
    }

    public void RegisterPlayer(Player player)
    {
        playersList.Add(player);
        if (rankList[playersList.Count-1] != null)
        {
            rankList[playersList.Count - 1].text = $"........playing";
        }
    }


    //public void OnSceneLoadDone(NetworkRunner runner)
    //{
    //    Debug.Log("Scene loading completed!");

    //    //Awake();
    //    //Start();

    //    var crateSpawner = FindObjectOfType<CrateSpawner>();
    //    if (crateSpawner != null)
    //    {
    //        Debug.Log("CrateSpawner found in the GameArena scene.");
    //        crateSpawner.Initialise(); // Initialize or reinitialize the pools
    //    }
    //    else
    //    {
    //        Debug.LogError("CrateSpawner is missing in the scene. Ensure it exists in the GameArena.");
    //    }

    //    // Find the existing ObjectPoolingManager in the new scene
    //    var objectPoolingManager = FindObjectOfType<ObjectPoolingManager>();
    //    if (objectPoolingManager != null)
    //    {
    //        Debug.Log("ObjectPoolingManager found in the GameArena scene.");
    //        objectPoolingManager.Initialise(); // Initialize or reinitialize the pools
    //    }
    //    else
    //    {
    //        Debug.LogError("ObjectPoolingManager is missing in the scene. Ensure it exists in the GameArena.");
    //    }

    //    // Find the existing ObjectPoolingManager in the new scene
    //    var matchobjectPoolingManager = FindObjectOfType<MatchObjectPoolingManager>();
    //    if (matchobjectPoolingManager != null)
    //    {
    //        Debug.Log("MatchObjectPoolingManager found in the GameArena scene.");
    //        matchobjectPoolingManager.Initialise(); // Initialize or reinitialize the pools
    //    }
    //    else
    //    {
    //        Debug.LogError("MatchObjectPoolingManager is missing in the scene. Ensure it exists in the GameArena.");
    //    }

    //    // Find the existing ObjectPoolingManager in the new scene
    //    var effectPoolingManager = FindObjectOfType<EffectPoolingManager>();
    //    if (effectPoolingManager != null)
    //    {
    //        Debug.Log("EffectPoolingManager found in the GameArena scene.");
    //        effectPoolingManager.Initialise(); // Initialize or reinitialize the pools
    //    }
    //    else
    //    {
    //        Debug.LogError("EffectPoolingManager is missing in the scene. Ensure it exists in the GameArena.");
    //    }
    //}

    public void GameOver()
    {
        isgameover = true;
        StartCoroutine(FadeInMatchEndingScreen(5f));
    }

    private IEnumerator FadeInMatchEndingScreen(float duration)
    {
        Debug.Log("Fading in the match-ending UI...");

        // Ensure the canvas is active and fully transparent at the start
        matchEndingCanvas.alpha = 0;
        matchEndingCanvas.gameObject.SetActive(true);
        matchEndingCanvas.interactable = false;
        matchEndingCanvas.blocksRaycasts = false;

        float startAlpha = 0;
        float targetAlpha = 1; // Fully visible

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            matchEndingCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
            yield return null;
        }

        // Ensure it's fully visible at the end
        matchEndingCanvas.alpha = targetAlpha;
        matchEndingCanvas.interactable = true;
        matchEndingCanvas.blocksRaycasts = true;

        Debug.Log("Match-ending UI fully displayed.");
    }

    public void DisconnectPlayer()
    {
        if (networkRunner != null)
        {
            foreach (var netObj in FindObjectsOfType<NetworkObject>())
            {
                Debug.Log("Works on Client 0 "+netObj);
                if (netObj.HasInputAuthority)
                {
                    Player plyer = netObj.GetComponent<Player>();
                    Debug.Log("Works on Client 1");
                    if (plyer != null)
                    {
                        plyer.RPC_DisconnectClient();
                    }
                }
            }
            Debug.Log("Works on Clients");
            StartCoroutine(nameof(shutdownfunfornetworkrunner));
           
        }
        else
        {
            Debug.LogError("Cannot disconnect player: NetworkRunner is null.");
        }

       
    }
    IEnumerator shutdownfunfornetworkrunner()
    {
        yield return new WaitForSeconds(1f);
        networkRunner.Shutdown();
        int mainMenuSceneIndex = 0; // Replace 0 with the build index of your main menu scene
        SceneManager.LoadScene(mainMenuSceneIndex);
    }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        PlayerInputData data = new PlayerInputData
        {
            Horizontal = Input.GetAxis("Horizontal"),
            Vertical = Input.GetAxis("Vertical")
        };

        input.Set(data);
    }



    // Implement the rest of INetworkRunnerCallbacks (can be left empty if not needed)
    //public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    //public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) {}
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject networkObject, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject networkObject, PlayerRef player) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    //public void OnSceneLoadDone(NetworkRunner runner) { }

    //private void OnDestroy()
    //{
    //    if (networkRunner != null)
    //    {
    //        // Unregister the callbacks to avoid memory leaks
    //        networkRunner.RemoveCallbacks(this);
    //    }
    //}
}
