using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class MatchObjectPoolingManager : NetworkBehaviour
{
    public static event Action OnInitialized;
    public static MatchObjectPoolingManager Instance { get; private set; }

    [Header("Prefab References")]
    [SerializeField] private NetworkPrefabRef cratePrefabRef;
    [SerializeField] private NetworkPrefabRef damageBuffBoxPrefabRef;
    [SerializeField] private NetworkPrefabRef healingBoxPrefabRef;

    [Header("Pool Sizes")]
    [SerializeField] private int cratePoolSize = 3;
    [SerializeField] private int damageBuffBoxPoolSize = 3;
    [SerializeField] private int healingBoxPoolSize = 3;

    private Dictionary<NetworkPrefabRef, Queue<NetworkObject>> pools;

    public bool IsInitialized { get; private set; } = false;

    public override void Spawned()
    {
        // Singleton logic
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            return;
        }

        MatchObjectPoolingEvents.OnObjectInitialized += HandleObjectInitialization;

        if (Object.HasStateAuthority)
        {
            // Host-specific initialization
            Debug.Log("MatchObjectPoolingManager: Host initializing pools.");
            InitializePools();
            OnInitialized?.Invoke();
        }
        else
        {
            // Client-specific setup
            Debug.Log("MatchObjectPoolingManager: Client ready to request objects.");
        }
    }

    private void HandleObjectInitialization(NetworkObject obj)
    {
        obj.transform.SetParent(transform);
        obj.gameObject.SetActive(false);
        Debug.Log($"[ObjectPoolingManager] Initialized object {obj.name}");
    }

    private void OnDisable()
    {
        MatchObjectPoolingEvents.OnObjectInitialized -= HandleObjectInitialization;
    }

    private void InitializePools()
    {
        pools = new Dictionary<NetworkPrefabRef, Queue<NetworkObject>>();

        // Dynamically initialize pools for each prefab
        CreatePool(cratePrefabRef, cratePoolSize);
        CreatePool(damageBuffBoxPrefabRef, damageBuffBoxPoolSize);
        CreatePool(healingBoxPrefabRef, healingBoxPoolSize);

        IsInitialized = true;
        Debug.Log("MatchObjectPoolingManager: Host pools initialized.");
    }

    private void CreatePool(NetworkPrefabRef prefabRef, int poolSize)
    {
        if (!Object.HasStateAuthority) return; // Ensure only the host initializes

        if (!prefabRef.IsValid)
        {
            Debug.LogError($"MatchObjectPoolingManager: Invalid prefab reference for {prefabRef}. Skipping.");
            return;
        }

        var pool = new Queue<NetworkObject>();

        for (int i = 0; i < poolSize; i++)
        {
            var obj = Runner.Spawn(prefabRef, Vector3.zero, Quaternion.identity, Object.InputAuthority, (runner, spawnedObj) =>
            {
                spawnedObj.transform.SetParent(transform);
                //spawnedObj.gameObject.SetActive(false); // Deactivate object
            });

            if (obj != null)
            {
                RPC_DeactivateObject(obj);
                pool.Enqueue(obj);
            }
            else
            {
                Debug.LogError($"MatchObjectPoolingManager: Failed to spawn object for prefab {prefabRef}");
            }
        }

        pools[prefabRef] = pool;
        Debug.Log($"MatchObjectPoolingManager: Pool created for prefab {prefabRef} with {pool.Count} objects.");
    }

    private NetworkObject GetObjectFromPool(NetworkPrefabRef prefabRef)
    {
        if (!pools.ContainsKey(prefabRef))
        {
            Debug.LogError($"MatchObjectPoolingManager: No pool found for prefab {prefabRef}");
            return null;
        }

        var pool = pools[prefabRef];

        foreach (var obj in pool)
        {
            if (!obj.gameObject.activeInHierarchy)
            {
                //obj.gameObject.SetActive(true);
                //return obj;
                if (Object.HasStateAuthority)
                {
                    RPC_ActivateObject(obj);
                }
                return obj;
            }
        }

        // If no inactive object is found, spawn a new one (host only)
        if (Object.HasStateAuthority)
        {
            var newObj = Runner.Spawn(prefabRef, Vector3.zero, Quaternion.identity, Object.InputAuthority, (runner, spawnedObj) =>
            {
                spawnedObj.transform.SetParent(transform);
                spawnedObj.gameObject.SetActive(true); // Activate new object
            });

            if (newObj != null)
            {
                pool.Enqueue(newObj);
                RPC_ActivateObject(newObj);
                return newObj;
            }
            else
            {
                Debug.LogError($"MatchObjectPoolingManager: Failed to spawn new object for prefab {prefabRef}");
            }
        }

        return null;
    }

    // Public methods for specific prefabs
    public NetworkObject GetCrate()
    {
        return Object.HasStateAuthority ? GetObjectFromPool(cratePrefabRef) : RequestObjectFromHost(cratePrefabRef);
    }

    public NetworkObject GetDamageBuffBox()
    {
        return Object.HasStateAuthority ? GetObjectFromPool(damageBuffBoxPrefabRef) : RequestObjectFromHost(damageBuffBoxPrefabRef);
    }

    public NetworkObject GetHealingBox()
    {
        return Object.HasStateAuthority ? GetObjectFromPool(healingBoxPrefabRef) : RequestObjectFromHost(healingBoxPrefabRef);
    }

    public void SpawnCrateAt(Vector3 position, Quaternion rotation)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogError("[MatchObjectPoolingManager] Only the host can spawn and synchronize crates.");
            return;
        }

        NetworkObject crate = GetCrate();

        if (crate != null)
        {
            // Set the crate's transform and synchronize it'
            RPC_SynchronizeCrateTransform(crate.Id, position, rotation);
        }
        else
        {
            Debug.LogWarning("[MatchObjectPoolingManager] Failed to retrieve a crate from the pool.");
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SynchronizeCrateTransform(NetworkId effectId, Vector3 position, Quaternion rotation)
    {
        if (Runner.TryFindObject(effectId, out NetworkObject netObj))  //Used Runner.TryFindObject() to find the NetworkObject on all clients using the NetworkId.
        {
            netObj.transform.position = position;
            netObj.transform.rotation = rotation;
            netObj.gameObject.SetActive(true);
            return;
        }
        //if (crate != null)
        //{
        //    crate.transform.position = position;
        //    crate.transform.rotation = rotation;
        //    crate.gameObject.SetActive(true);
        //    Debug.Log($"[MatchObjectPoolingManager] Synchronized crate {crate.name} to position {position}, rotation {rotation}");
        //}
    }

    private NetworkObject RequestObjectFromHost(NetworkPrefabRef prefabRef)
    {
        Debug.Log($"Client requesting object of prefab {prefabRef} from host.");
        RPC_RequestObject(prefabRef); // Send RPC to host
        return null; // Return null immediately since the host handles the spawn
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestObject(NetworkPrefabRef prefabRef)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogError("MatchObjectPoolingManager: RPC called by non-authoritative object.");
            return;
        }

        var obj = GetObjectFromPool(prefabRef);
        if (obj != null)
        {
            Debug.Log($"MatchObjectPoolingManager: Object spawned for prefab {prefabRef}");
        }
        else
        {
            Debug.LogError($"MatchObjectPoolingManager: Failed to provide object from pool for prefab {prefabRef}");
        }
    }

    public void ReturnToPool(NetworkPrefabRef prefabRef, NetworkObject obj)
    {
        if (Object.HasStateAuthority)
        {
            obj.gameObject.SetActive(false);
            pools[prefabRef].Enqueue(obj);
            Debug.Log($"MatchObjectPoolingManager: Object returned to pool for prefab {prefabRef}");
        }
        else
        {
            Debug.LogError("MatchObjectPoolingManager: Only the host can return objects to the pool.");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ActivateObject(NetworkObject obj)
    {
        obj.gameObject.SetActive(true);
        Debug.Log($"[MatchObjectPoolingManager] Activated object {obj.name}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DeactivateObject(NetworkObject obj)
    {
        obj.gameObject.SetActive(false);
    }
}


//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using Fusion;

//public class MatchObjectPoolingManager : NetworkBehaviour
//{
//    public static MatchObjectPoolingManager Instance { get; private set; }

//    public static event Action OnInitialized;

//    [Header("Prefab References")]
//    [SerializeField] private NetworkPrefabRef cratePrefabRef;
//    [SerializeField] private NetworkPrefabRef damageBuffBoxPrefabRef;
//    [SerializeField] private NetworkPrefabRef healingBoxPrefabRef;

//    [Header("Pool Sizes")]
//    [SerializeField] private int cratePoolSize = 3;
//    [SerializeField] private int damageBuffBoxPoolSize = 3;
//    [SerializeField] private int healingBoxPoolSize = 3;

//    private Dictionary<NetworkPrefabRef, Queue<NetworkObject>> pools;

//    public bool IsInitialized { get; private set; } = false;

//    //public override void Spawned()
//    //{
//    //    //Instance = this;

//    //    //if (!Object.HasStateAuthority)
//    //    //{
//    //    //    // Only the host should initialize the pools
//    //    //    return;
//    //    //}

//    //    //// Validate prefab references before initializing pools
//    //    //if (!ValidatePrefabRefs())
//    //    //{
//    //    //    Debug.LogError("Invalid prefab references detected. Initialization aborted.");
//    //    //    return;
//    //    //}

//    //    //// Initialize pools
//    //    //InitializePools();

//    //    //// Notify listeners that initialization is complete
//    //    //OnInitialized?.Invoke();

//    //    // Ensure singleton instance is only set once
//    //    if (Instance == null)
//    //    {
//    //        Instance = this;
//    //    }
//    //    else
//    //    {
//    //        Debug.LogError("MatchObjectPoolingManager: Instance already exists! Aborting initialization.");
//    //        return; // Prevent duplicate initialization
//    //    }

//    //    if (Object.HasStateAuthority)
//    //    {
//    //        // Host-specific initialization
//    //        Debug.Log("MatchObjectPoolingManager: Host initializing pools.");

//    //        // Validate prefab references before proceeding
//    //        if (!ValidatePrefabRefs())
//    //        {
//    //            Debug.LogError("MatchObjectPoolingManager: Invalid prefab references detected. Initialization aborted.");
//    //            return;
//    //        }

//    //        // Initialize pools for host
//    //        InitializePools();

//    //        // Notify listeners that initialization is complete
//    //        OnInitialized?.Invoke();
//    //    }
//    //    else
//    //    {
//    //        // Client-specific initialization
//    //        Debug.Log("MatchObjectPoolingManager: Client initializing local pools.");

//    //        // Clients initialize their local pools without validation
//    //        InitializePools();
//    //    }
//    //}

//    public void Initialise()
//    {
//        //Instance = this;

//        //if (!Object.HasStateAuthority)
//        //{
//        //    // Only the host should initialize the pools
//        //    return;
//        //}

//        //// Validate prefab references before initializing pools
//        //if (!ValidatePrefabRefs())
//        //{
//        //    Debug.LogError("Invalid prefab references detected. Initialization aborted.");
//        //    return;
//        //}

//        //// Initialize pools
//        //InitializePools();

//        //// Notify listeners that initialization is complete
//        //OnInitialized?.Invoke();

//        Instance = this; // Set the instance for singleton access

//        if (Object.HasStateAuthority)
//        {
//            // Host-specific initialization
//            Debug.Log("MatchObjectPoolingManager: Host initializing pools.");
//            if (!ValidatePrefabRefs())
//            {
//                Debug.LogError("Invalid prefab references detected. Initialization aborted on host.");
//                return;
//            }

//            InitializePools();
//            OnInitialized?.Invoke();
//        }
//        else
//        {
//            // Client-specific initialization
//            Debug.Log("MatchObjectPoolingManager: Client initializing local pools.");
//            InitializePools(); // Clients can safely initialize their local pools without validation
//        }
//    }

//    private void InitializePools()
//    {
//        pools = new Dictionary<NetworkPrefabRef, Queue<NetworkObject>>();

//        // Dynamically initialize pools based on the references
//        CreatePool(cratePrefabRef, cratePoolSize);
//        CreatePool(damageBuffBoxPrefabRef, damageBuffBoxPoolSize);
//        CreatePool(healingBoxPrefabRef, healingBoxPoolSize);

//        IsInitialized = true;
//        Debug.Log("MatchObjectPoolingManager successfully initialized.");
//    }

//    private void CreatePool(NetworkPrefabRef prefabRef, int poolSize)
//    {
//        if (!Object.HasStateAuthority) // Ensure only the server runs this
//            return;

//        if (!prefabRef.IsValid) return;

//        var pool = new Queue<NetworkObject>(poolSize);

//        for (int i = 0; i < poolSize; i++)
//        {
//            var obj = Runner.Spawn(prefabRef, Vector3.zero, Quaternion.identity, Object.InputAuthority, (runner, spawnedObj) =>
//            {
//                spawnedObj.transform.SetParent(transform);
//            });

//            if (obj != null)
//            {
//                pool.Enqueue(obj);
//                obj.gameObject.SetActive(false);
//            }
//            else
//            {
//                Debug.LogError($"Failed to spawn object for prefab: {prefabRef}");
//            }
//        }

//        pools[prefabRef] = pool;
//        Debug.Log($"Pool initialized for prefab {prefabRef} with {pool.Count} objects.");
//    }

//    private bool ValidatePrefabRefs()
//    {
//        bool isValid = true;

//        if (!cratePrefabRef.IsValid)
//        {
//            Debug.LogError("Crate PrefabRef is invalid. Ensure it is assigned and registered in NetworkProjectConfig.");
//            isValid = false;
//        }

//        if (!damageBuffBoxPrefabRef.IsValid)
//        {
//            Debug.LogError("Damage Buff Box PrefabRef is invalid. Ensure it is assigned and registered in NetworkProjectConfig.");
//            isValid = false;
//        }

//        if (!healingBoxPrefabRef.IsValid)
//        {
//            Debug.LogError("Healing Box PrefabRef is invalid. Ensure it is assigned and registered in NetworkProjectConfig.");
//            isValid = false;
//        }

//        return isValid;
//    }

//    private NetworkObject GetObjectFromPool(NetworkPrefabRef prefabRef)
//    {
//        if (!pools.ContainsKey(prefabRef))
//        {
//            Debug.LogError($"No pool found for prefab: {prefabRef}");
//            return null;
//        }

//        var pool = pools[prefabRef];

//        foreach (var obj in pool)
//        {
//            if (!obj.gameObject.activeInHierarchy)
//            {
//                obj.gameObject.SetActive(true);
//                return obj;
//            }
//        }

//        // If no inactive objects are available, spawn a new one (only the server does this)
//        if (Object.HasStateAuthority)
//        {
//            var newObj = Runner.Spawn(prefabRef, Vector3.zero, Quaternion.identity, Object.InputAuthority, (runner, spawnedObj) =>
//            {
//                spawnedObj.transform.SetParent(transform);
//                spawnedObj.gameObject.SetActive(true);
//            });

//            if (newObj != null)
//            {
//                pool.Enqueue(newObj);
//                return newObj;
//            }
//            else
//            {
//                Debug.LogError($"Failed to spawn new object for prefab: {prefabRef}");
//            }
//        }

//        return null;

//        //// If no inactive object is found, spawn a new one
//        //var newObj = Runner.Spawn(prefabRef, Vector3.zero, Quaternion.identity, Object.InputAuthority, (runner, spawnedObj) => {
//        //    spawnedObj.transform.SetParent(transform);
//        //    spawnedObj.gameObject.SetActive(true);
//        //});

//        //if (newObj == null)
//        //{
//        //    Debug.LogError($"Failed to spawn new object for prefab: {prefabRef}");
//        //    return null;
//        //}

//        //pool.Enqueue(newObj);
//        //return newObj;
//    }

//    // Explicit methods for getting specific objects
//    public NetworkObject GetCrate()
//    {
//        return GetObjectFromPool(cratePrefabRef);
//    }

//    public NetworkObject GetDamageBuffBox()
//    {
//        return GetObjectFromPool(damageBuffBoxPrefabRef);
//    }

//    public NetworkObject GetHealingBox()
//    {
//        return GetObjectFromPool(healingBoxPrefabRef);
//    }
//}

////using System.Collections;
////using System.Collections.Generic;
////using UnityEngine;

////public class MatchObjectPoolingManager : MonoBehaviour
////{

////    private static MatchObjectPoolingManager instance;
////    public static MatchObjectPoolingManager Instance { get { return instance; } }

////    [SerializeField] private GameObject cratePrefab;
////    [SerializeField] private GameObject damageBuffBoxPrefab;
////    [SerializeField] private GameObject healingBoxPrefab;


////    private List<GameObject> crates;
////    private List<GameObject> damageBuffBoxes;
////    private List<GameObject> healingBoxes;

////    [SerializeField] private int crateAmount = 3;
////    [SerializeField] private int damageBuffBoxAmount = 3;
////    [SerializeField] private int healingBoxAmount = 3;

////    void Awake()
////    {
////        instance = this;

////        //Preload crates
////        crates = new List<GameObject>(crateAmount);
////        for (int i = 0; i < crateAmount; i++)
////        {
////            GameObject prefabInstance = Instantiate(cratePrefab);
////            //So the prefabInstance will be under this ObjectPooling Manager for organisation
////            prefabInstance.transform.SetParent(transform);
////            prefabInstance.SetActive(false);

////            crates.Add(prefabInstance);
////        }

////        //Preload damage buff boxes
////        damageBuffBoxes = new List<GameObject>(damageBuffBoxAmount);
////        for (int i = 0; i < damageBuffBoxAmount; i++)
////        {
////            GameObject prefabInstance = Instantiate(damageBuffBoxPrefab);
////            //So the prefabInstance will be under this ObjectPooling Manager for organisation
////            prefabInstance.transform.SetParent(transform);
////            prefabInstance.SetActive(false);

////            damageBuffBoxes.Add(prefabInstance);
////        }

////        //Preload healing box
////        healingBoxes = new List<GameObject>(healingBoxAmount);
////        for (int i = 0; i < healingBoxAmount; i++)
////        {
////            GameObject prefabInstance = Instantiate(healingBoxPrefab);
////            //So the prefabInstance will be under this ObjectPooling Manager for organisation
////            prefabInstance.transform.SetParent(transform);
////            prefabInstance.SetActive(false);

////            healingBoxes.Add(prefabInstance);
////        }
////    }

////    public GameObject GetCrate()
////    {
////        foreach (GameObject crate in crates)
////        {
////            if (!crate.activeInHierarchy)
////            {
////                crate.SetActive(true);
////                return crate;
////            }
////        }
////        GameObject prefabInstance = Instantiate(cratePrefab);
////        //so the prefabInstance will be under this ObjectPooling Manager for organisation
////        prefabInstance.transform.SetParent(transform);
////        crates.Add(prefabInstance);
////        return prefabInstance;
////    }

////    public GameObject GetDamageBuffBox()
////    {
////        foreach (GameObject damageBuffBox in damageBuffBoxes)
////        {
////            if (!damageBuffBox.activeInHierarchy)
////            {
////                damageBuffBox.SetActive(true);
////                return damageBuffBox;
////            }
////        }
////        GameObject prefabInstance = Instantiate(damageBuffBoxPrefab);
////        //so the prefabInstance will be under this ObjectPooling Manager for organisation
////        prefabInstance.transform.SetParent(transform);
////        damageBuffBoxes.Add(prefabInstance);
////        return prefabInstance;
////    }

////    public GameObject GetHealingBox()
////    {
////        foreach (GameObject healingBox in healingBoxes)
////        {
////            if (!healingBox.activeInHierarchy)
////            {
////                healingBox.SetActive(true);
////                return healingBox;
////            }
////        }
////        GameObject prefabInstance = Instantiate(healingBoxPrefab);
////        //so the prefabInstance will be under this ObjectPooling Manager for organisation
////        prefabInstance.transform.SetParent(transform);
////        healingBoxes.Add(prefabInstance);
////        return prefabInstance;
////    }
////}
