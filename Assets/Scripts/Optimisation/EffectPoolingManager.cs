using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class EffectPoolingManager : NetworkBehaviour
{
    public static EffectPoolingManager Instance { get; private set; }

    [Header("Prefab Explosion References")]
    [SerializeField] private GameObject poisonExplosionPrefabObject;
    [SerializeField] private GameObject freezeExplosionPrefabObject;
    [SerializeField] private GameObject rocketExplosionPrefabObject;
    [SerializeField] private GameObject baseMuzzlePrefabObject;
    [SerializeField] private GameObject rocketMuzzlePrefabObject;
    [SerializeField] private GameObject damageIncreaseEffectPrefabObject;
    [SerializeField] private GameObject healingEffectPrefabObject;
    [SerializeField] private GameObject openBuffEffectPrefabObject;
    [SerializeField] private GameObject gotShotPrefabObject;
    [SerializeField] private GameObject deadPrefabPrefabObject;

    [SerializeField] private NetworkPrefabRef poisonExplosionPrefab;
    [SerializeField] private NetworkPrefabRef freezeExplosionPrefab;
    [SerializeField] private NetworkPrefabRef rocketExplosionPrefab;
    [SerializeField] private NetworkPrefabRef baseMuzzlePrefab;
    [SerializeField] private NetworkPrefabRef rocketMuzzlePrefab;
    [SerializeField] private NetworkPrefabRef damageIncreaseEffectPrefab;
    [SerializeField] private NetworkPrefabRef healingEffectPrefab;
    [SerializeField] private NetworkPrefabRef openBuffEffectPrefab;
    [SerializeField] private NetworkPrefabRef gotShotPrefab;
    [SerializeField] private NetworkPrefabRef deadPrefabPrefab;

    [Header("Pool Sizes")]
    [SerializeField] private int poisonExplosionPoolSize = 3;
    [SerializeField] private int freezeExplosionPoolSize = 3;
    [SerializeField] private int rocketExplosionPoolSize = 3;
    [SerializeField] private int baseMuzzlePoolSize = 3;
    [SerializeField] private int rocketMuzzlePoolSize = 3;
    [SerializeField] private int damageIncreaseEffectPoolSize = 3;
    [SerializeField] private int healingEffectPoolSize = 3;
    [SerializeField] private int openBuffEffectPoolSize = 3;
    [SerializeField] private int gotShotPoolSize = 3;
    [SerializeField] private int deadPrefabPoolSize = 3;

    private Dictionary<GameObject, NetworkPrefabRef> prefabMap;
    private Dictionary<NetworkPrefabRef, Queue<NetworkObject>> pools;

    public override void Spawned()
    {
        // Singleton logic
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("EffectPoolingManager: Instance already exists! Aborting initialization.");
            return; // Prevent duplicate initialization
        }

        EffectPoolingEvents.OnObjectInitialized += HandleObjectInitialization;

        if (Object.HasStateAuthority)
        {
            Debug.Log("EffectPoolingManager: Host initializing prefab map and pools.");
            InitializePrefabMap();
            InitializePools();
        }
        else
        {
            Debug.Log("EffectPoolingManager: Client ready to request effects.");
            Runner.Despawn(Object);
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
        EffectPoolingEvents.OnObjectInitialized -= HandleObjectInitialization;
    }

    private void InitializePrefabMap()
    {
        prefabMap = new Dictionary<GameObject, NetworkPrefabRef>
        {
            { poisonExplosionPrefabObject, poisonExplosionPrefab },
            { freezeExplosionPrefabObject, freezeExplosionPrefab },
            { rocketExplosionPrefabObject, rocketExplosionPrefab },
            { baseMuzzlePrefabObject, baseMuzzlePrefab },
            { rocketMuzzlePrefabObject, rocketMuzzlePrefab },
            { damageIncreaseEffectPrefabObject, damageIncreaseEffectPrefab },
            { healingEffectPrefabObject, healingEffectPrefab },
            { openBuffEffectPrefabObject, openBuffEffectPrefab },
            { gotShotPrefabObject, gotShotPrefab },
            { deadPrefabPrefabObject, deadPrefabPrefab }
        };
    }

    private void InitializePools()
    {
        pools = new Dictionary<NetworkPrefabRef, Queue<NetworkObject>>();

        CreatePool(poisonExplosionPrefab, poisonExplosionPoolSize);
        CreatePool(freezeExplosionPrefab, freezeExplosionPoolSize);
        CreatePool(rocketExplosionPrefab, rocketExplosionPoolSize);
        CreatePool(baseMuzzlePrefab, baseMuzzlePoolSize);
        CreatePool(rocketMuzzlePrefab, rocketMuzzlePoolSize);
        CreatePool(damageIncreaseEffectPrefab, damageIncreaseEffectPoolSize);
        CreatePool(healingEffectPrefab, healingEffectPoolSize);
        CreatePool(openBuffEffectPrefab, openBuffEffectPoolSize);
        CreatePool(gotShotPrefab, gotShotPoolSize);
        CreatePool(deadPrefabPrefab, deadPrefabPoolSize);

        Debug.Log("EffectPoolingManager: Pools initialized by host.");
    }

    private void CreatePool(NetworkPrefabRef prefabRef, int poolSize)
    {
        if (!Object.HasStateAuthority) return;

        if (!prefabRef.IsValid)
        {
            Debug.LogError($"EffectPoolingManager: Invalid prefab reference for {prefabRef}. Skipping.");
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
                Debug.LogError($"EffectPoolingManager: Failed to spawn object for prefab {prefabRef}");
            }
        }

        pools[prefabRef] = pool;
        Debug.Log($"EffectPoolingManager: Pool created for {prefabRef} with {pool.Count} objects.");
    }

    private NetworkObject GetObjectFromPool(NetworkPrefabRef prefabRef)
    {
        if (!pools.ContainsKey(prefabRef))
        {
            Debug.LogWarning($"EffectPoolingManager: No pool found for prefab {prefabRef}. Clients cannot create pools.");
            return null; // Prevent client-side dynamic creation
        }

        var pool = pools[prefabRef];

        foreach (var obj in pool)
        {
            if (!obj.gameObject.activeInHierarchy)
            {
                obj.gameObject.SetActive(true);
                return obj;
            }
        }

        if (Object.HasStateAuthority)
        {
            var newObj = Runner.Spawn(prefabRef, Vector3.zero, Quaternion.identity, Object.InputAuthority, (runner, spawnedObj) =>
            {
                spawnedObj.transform.SetParent(transform);
                spawnedObj.gameObject.SetActive(true);
            });

            if (newObj != null)
            {
                pool.Enqueue(newObj);
                return newObj;
            }
        }

        return null;
    }

    // Public methods for requesting effects
    public NetworkObject GetImpactEffect(GameObject impactPrefab)
    {
        if (prefabMap.TryGetValue(impactPrefab, out var prefabRef))
        {
            return Object.HasStateAuthority ? GetObjectFromPool(prefabRef) : RequestEffectFromHost(prefabRef);
        }

        Debug.LogWarning("Unrecognized impact effect prefab.");
        return null;
    }

    public NetworkObject GetBaseMuzzleEffect()
    {
        return Object.HasStateAuthority ? GetObjectFromPool(baseMuzzlePrefab) : RequestEffectFromHost(baseMuzzlePrefab);
    }

    public NetworkObject GetRocketMuzzleEffect()
    {
        return Object.HasStateAuthority ? GetObjectFromPool(rocketMuzzlePrefab) : RequestEffectFromHost(rocketMuzzlePrefab);
    }

    public NetworkObject GetDamageIncreaseEffect()
    {
        return Object.HasStateAuthority ? GetObjectFromPool(damageIncreaseEffectPrefab) : RequestEffectFromHost(damageIncreaseEffectPrefab);
    }

    public NetworkObject GetHealingEffect()
    {
        return Object.HasStateAuthority ? GetObjectFromPool(healingEffectPrefab) : RequestEffectFromHost(healingEffectPrefab);
    }

    public NetworkObject GetOpenBuffEffect()
    {
        return Object.HasStateAuthority ? GetObjectFromPool(openBuffEffectPrefab) : RequestEffectFromHost(openBuffEffectPrefab);
    }

    public NetworkObject GetGotShotEffect()
    {
        return Object.HasStateAuthority ? GetObjectFromPool(gotShotPrefab) : RequestEffectFromHost(gotShotPrefab);
    }

    public NetworkObject GetDeadEffect()
    {
        return Object.HasStateAuthority ? GetObjectFromPool(deadPrefabPrefab): RequestEffectFromHost(deadPrefabPrefab);
    }

    private NetworkObject RequestEffectFromHost(NetworkPrefabRef prefabRef)
    {
        Debug.Log($"Client requesting effect {prefabRef} from host.");
        RPC_RequestEffect(prefabRef);
        return null; // Return null immediately, host handles spawning
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestEffect(NetworkPrefabRef prefabRef)
    {
        //if (!Object.HasStateAuthority)
        //{
        //    Debug.LogError("EffectPoolingManager: RPC called by non-authoritative object.");
        //    return;
        //}

        var obj = GetObjectFromPool(prefabRef);
        if (obj != null)
        {
            Debug.Log($"EffectPoolingManager: Effect spawned for prefab {prefabRef}");
        }
        else
        {
            Debug.LogError($"EffectPoolingManager: Failed to provide effect from pool for {prefabRef}");
        }
    }

    public void ReturnToPool(NetworkPrefabRef prefabRef, NetworkObject obj)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("Client cannot directly manage the pool. Notify the host instead.");
            RPC_ReturnObjectToHost(prefabRef, obj);
            return;
        }

        if (pools == null)
        {
            Debug.LogError("Pools dictionary is null! Ensure InitializePools is called before using ObjectPoolingManager.");
            return;
        }

        if (!pools.ContainsKey(prefabRef))
        {
            Debug.LogError($"EffectPoolingManager: No pool exists for prefab {prefabRef}. Destroying object.");
            Runner.Despawn(obj);
            return;
        }

        obj.gameObject.SetActive(false);
        pools[prefabRef].Enqueue(obj);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ReturnObjectToHost(NetworkPrefabRef prefabRef, NetworkObject obj)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogError("ObjectPoolingManager: RPC called by non-authoritative object.");
            return;
        }

        ReturnToPool(prefabRef, obj);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DeactivateObject(NetworkObject obj)
    {
        obj.gameObject.SetActive(false);
    }

}


//using System.Collections.Generic;
//using UnityEngine;
//using Fusion;

//public class EffectPoolingManager : NetworkBehaviour
//{
//    public static EffectPoolingManager Instance { get; private set; }

//    [Header("Prefab Explosion References")]
//    [SerializeField] private GameObject poisonExplosionPrefabObject;
//    [SerializeField] private GameObject freezeExplosionPrefabObject;
//    [SerializeField] private GameObject rocketExplosionPrefabObject;
//    [SerializeField] private GameObject baseMuzzlePrefabObject;
//    [SerializeField] private GameObject rocketMuzzlePrefabObject;
//    [SerializeField] private GameObject damageIncreaseEffectPrefabObject;
//    [SerializeField] private GameObject healingEffectPrefabObject;
//    [SerializeField] private GameObject openBuffEffectPrefabObject;
//    [SerializeField] private GameObject gotShotPrefabObject;
//    [SerializeField] private GameObject deadPrefabPrefabObject;

//    [SerializeField] private NetworkPrefabRef poisonExplosionPrefab;
//    [SerializeField] private NetworkPrefabRef freezeExplosionPrefab;
//    [SerializeField] private NetworkPrefabRef rocketExplosionPrefab;
//    [SerializeField] private NetworkPrefabRef baseMuzzlePrefab;
//    [SerializeField] private NetworkPrefabRef rocketMuzzlePrefab;
//    [SerializeField] private NetworkPrefabRef damageIncreaseEffectPrefab;
//    [SerializeField] private NetworkPrefabRef healingEffectPrefab;
//    [SerializeField] private NetworkPrefabRef openBuffEffectPrefab;
//    [SerializeField] private NetworkPrefabRef gotShotPrefab;
//    [SerializeField] private NetworkPrefabRef deadPrefabPrefab;

//    [Header("Pool Sizes")]
//    [SerializeField] private int poisonExplosionPoolSize = 3;
//    [SerializeField] private int freezeExplosionPoolSize = 3;
//    [SerializeField] private int rocketExplosionPoolSize = 3;
//    [SerializeField] private int baseMuzzlePoolSize = 3;
//    [SerializeField] private int rocketMuzzlePoolSize = 3;
//    [SerializeField] private int damageIncreaseEffectPoolSize = 3;
//    [SerializeField] private int openBuffEffectPoolSize = 3;
//    [SerializeField] private int healingEffectPoolSize = 3;
//    [SerializeField] private int gotShotPoolSize = 3;
//    [SerializeField] private int deadPrefabPoolSize = 3;

//    private Dictionary<GameObject, NetworkPrefabRef> prefabMap;
//    private Dictionary<NetworkPrefabRef, Queue<NetworkObject>> pools;

//    //public override void Spawned()
//    //{
//    //    //if (!Object.HasStateAuthority)
//    //    //{
//    //    //    // Only the host initializes the pool
//    //    //    return;
//    //    //}

//    //    //Instance = this;
//    //    //InitializePrefabMap();
//    //    //InitializePools();

//    //    if (Object.HasStateAuthority)
//    //    {
//    //        // Only the host sets the singleton instance
//    //        Instance = this;

//    //        Debug.Log("PoolingManager: Host initializing prefab map and pools.");
//    //        InitializePrefabMap(); // Host initializes prefab map
//    //        InitializePools();     // Host initializes pools
//    //    }
//    //    else
//    //    {
//    //        // Clients should not overwrite the instance
//    //        if (Instance == null)
//    //        {
//    //            Instance = this;
//    //        }

//    //        Debug.Log("PoolingManager: Client initializing local pools.");
//    //        InitializePrefabMap(); // Clients initialize their local prefab map
//    //        InitializePools();     // Clients initialize their local pools
//    //    }
//    //}

//    public void Initialise()
//    {
//        //if (!Object.HasStateAuthority)
//        //{
//        //    // Only the host initializes the pool
//        //    return;
//        //}

//        //Instance = this;
//        //InitializePrefabMap();
//        //InitializePools();

//        Instance = this; // Set the singleton instance for all clients and the host

//        if (Object.HasStateAuthority)
//        {
//            // Host-specific initialization
//            Debug.Log("PoolingManager: Host initializing prefab map and pools.");
//            InitializePrefabMap(); // Host initializes prefab map
//            InitializePools();     // Host initializes pools
//        }
//        else
//        {
//            // Client-specific initialization
//            Debug.Log("PoolingManager: Client initializing local pools.");
//            InitializePools(); // Clients initialize local pools
//        }
//    }

//    private void InitializePrefabMap()
//    {
//        prefabMap = new Dictionary<GameObject, NetworkPrefabRef>
//        {
//            { poisonExplosionPrefabObject, poisonExplosionPrefab },
//            { freezeExplosionPrefabObject, freezeExplosionPrefab },
//            { rocketExplosionPrefabObject, rocketExplosionPrefab },
//            { baseMuzzlePrefabObject, baseMuzzlePrefab },
//            { rocketMuzzlePrefabObject, rocketMuzzlePrefab },
//            { damageIncreaseEffectPrefabObject, damageIncreaseEffectPrefab },
//            { healingEffectPrefabObject, healingEffectPrefab },
//            { openBuffEffectPrefabObject, openBuffEffectPrefab },
//            { gotShotPrefabObject, gotShotPrefab },
//            { deadPrefabPrefabObject, deadPrefabPrefab }
//        };
//    }

//    private void InitializePools()
//    {
//        pools = new Dictionary<NetworkPrefabRef, Queue<NetworkObject>>();

//        CreatePool(poisonExplosionPrefab, poisonExplosionPoolSize);
//        CreatePool(freezeExplosionPrefab, freezeExplosionPoolSize);
//        CreatePool(rocketExplosionPrefab, rocketExplosionPoolSize);
//        CreatePool(baseMuzzlePrefab, baseMuzzlePoolSize);
//        CreatePool(rocketMuzzlePrefab, rocketMuzzlePoolSize);
//        CreatePool(damageIncreaseEffectPrefab, damageIncreaseEffectPoolSize);
//        CreatePool(healingEffectPrefab, healingEffectPoolSize);
//        CreatePool(openBuffEffectPrefab, openBuffEffectPoolSize);
//        CreatePool(gotShotPrefab, gotShotPoolSize);
//        CreatePool(deadPrefabPrefab, deadPrefabPoolSize);

//        Debug.Log("EffectPoolingManager successfully initialized.");
//    }

//    private void CreatePool(NetworkPrefabRef prefabRef, int poolSize)
//    {
//        if (!Object.HasStateAuthority) // Ensure only the server runs this
//            return;

//        if (!prefabRef.IsValid)
//        {
//            Debug.LogError($"Invalid prefab reference for {prefabRef}. Skipping pool creation.");
//            return;
//        }

//        var pool = new Queue<NetworkObject>();

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
//        Debug.Log($"Pool created for {prefabRef} with {pool.Count} objects.");
//    }

//    private NetworkObject GetObjectFromPool(NetworkPrefabRef prefabRef)
//    {
//        //if (!pools.ContainsKey(prefabRef))
//        //{
//        //    Debug.LogError($"No pool found for prefab: {prefabRef}. Creating a new pool dynamically.");
//        //    CreatePool(prefabRef, 1);
//        //}

//        if (!pools.ContainsKey(prefabRef))
//        {
//            Debug.LogWarning($"No pool found for prefab: {prefabRef}. Clients cannot create pools. Waiting for server.");

//            if (Object.HasStateAuthority)
//            {
//                // If the server doesn't have the pool, create it dynamically
//                Debug.LogError($"Server missing pool for prefab: {prefabRef}. Creating dynamically.");
//                CreatePool(prefabRef, 1);
//            }
//            else
//            {
//                // If the client tries to access the pool, wait for the server to synchronize
//                return null; // Prevents the client from creating the pool
//            }
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
//    }

//    public NetworkObject GetImpactEffect(GameObject impactPrefab)
//    {
//        if (prefabMap.TryGetValue(impactPrefab, out var prefabRef))
//        {
//            return GetObjectFromPool(prefabRef);
//        }

//        Debug.LogWarning("Unrecognized impact effect prefab.");
//        return null;
//    }

//    public NetworkObject GetBaseMuzzleEffect()
//    {
//        return GetObjectFromPool(baseMuzzlePrefab);
//    }

//    public NetworkObject GetRocketMuzzleEffect()
//    {
//        return GetObjectFromPool(rocketMuzzlePrefab);
//    }

//    public NetworkObject GetDamageIncreaseEffect()
//    {
//        return GetObjectFromPool(damageIncreaseEffectPrefab);
//    }

//    public NetworkObject GetHealingEffect()
//    {
//        return GetObjectFromPool(healingEffectPrefab);
//    }

//    public NetworkObject GetOpenBuffEffect()
//    {
//        return GetObjectFromPool(openBuffEffectPrefab);
//    }

//    public NetworkObject GetGotShotEffect()
//    {
//        return GetObjectFromPool(gotShotPrefab);
//    }

//    public NetworkObject GetDeadEffect()
//    {
//        return GetObjectFromPool(deadPrefabPrefab);
//    }

//    public void ReturnToPool(NetworkPrefabRef prefabRef, NetworkObject obj)
//    {
//        if (!pools.ContainsKey(prefabRef))
//        {
//            Debug.LogError($"No pool exists for prefab: {prefabRef}. Destroying returned object.");
//            Runner.Despawn(obj);
//            return;
//        }

//        obj.gameObject.SetActive(false);
//        pools[prefabRef].Enqueue(obj);
//    }
//}


////using System.Collections;
////using System.Collections.Generic;
////using UnityEngine;

////public class EffectPoolingManager : MonoBehaviour
////{

////    private static EffectPoolingManager instance;
////    public static EffectPoolingManager Instance { get { return instance; } }

////    [SerializeField] private GameObject poisonExplosionPrefab;
////    [SerializeField] private GameObject freezeExplosionPrefab;
////    [SerializeField] private GameObject rocketExplosionPrefab;

////    private List<GameObject> poisonExplosions;
////    private List<GameObject> freezeExplosions;
////    private List<GameObject> rocketExplosions;

////    [SerializeField] private int poisonExplosionAmount = 3;
////    [SerializeField] private int freezeExplosionAmount = 3;
////    [SerializeField] private int rocketExplosionAmount = 3;

////    void Awake()
////    {
////        instance = this;

////        //Preload poison explosion effect
////        poisonExplosions = new List<GameObject>(poisonExplosionAmount);
////        for (int i = 0; i < poisonExplosionAmount; i++)
////        {
////            GameObject prefabInstance = Instantiate(poisonExplosionPrefab);
////            //So the prefabInstance will be under this ObjectPooling Manager for organisation
////            prefabInstance.transform.SetParent(transform);
////            prefabInstance.SetActive(false);

////            poisonExplosions.Add(prefabInstance);
////        }

////        //Preload freeze explosion effect
////        freezeExplosions = new List<GameObject>(freezeExplosionAmount);
////        for (int i = 0; i < freezeExplosionAmount; i++)
////        {
////            GameObject prefabInstance = Instantiate(freezeExplosionPrefab);
////            //So the prefabInstance will be under this ObjectPooling Manager for organisation
////            prefabInstance.transform.SetParent(transform);
////            prefabInstance.SetActive(false);

////            freezeExplosions.Add(prefabInstance);
////        }

////        //Preload rocket explosion effect
////        rocketExplosions = new List<GameObject>(rocketExplosionAmount);
////        for (int i = 0; i < rocketExplosionAmount; i++)
////        {
////            GameObject prefabInstance = Instantiate(rocketExplosionPrefab);
////            //So the prefabInstance will be under this ObjectPooling Manager for organisation
////            prefabInstance.transform.SetParent(transform);
////            prefabInstance.SetActive(false);

////            rocketExplosions.Add(prefabInstance);
////        }
////    }

////    public GameObject GetImpactEffect(GameObject impactPrefab)
////    {
////        if (impactPrefab == poisonExplosionPrefab)
////        {
////            return GetExplosionFromPool(poisonExplosions, poisonExplosionPrefab);
////        }
////        else if (impactPrefab == freezeExplosionPrefab)
////        {
////            return GetExplosionFromPool(freezeExplosions, freezeExplosionPrefab);
////        }
////        else if (impactPrefab == rocketExplosionPrefab)
////        {
////            return GetExplosionFromPool(rocketExplosions, rocketExplosionPrefab);
////        }

////        // Fallback in case the prefab is unrecognized
////        Debug.LogWarning("Unrecognized explosion prefab");
////        return null;
////    }

////    private GameObject GetExplosionFromPool(List<GameObject> explosionPool, GameObject explosionPrefab)
////    {
////        foreach (GameObject explosion in explosionPool)
////        {
////            if (!explosion.activeInHierarchy)
////            {
////                explosion.SetActive(true);
////                return explosion;
////            }
////        }

////        // If no inactive explosion is available, instantiate a new one
////        return CreateNewExplosion(explosionPool, explosionPrefab);
////    }

////    private GameObject CreateNewExplosion(List<GameObject> explosionPool, GameObject explosionPrefab)
////    {
////        GameObject newExplosion = Instantiate(explosionPrefab);
////        newExplosion.transform.SetParent(transform);
////        newExplosion.SetActive(true);
////        explosionPool.Add(newExplosion);
////        return newExplosion;
////    }

////}
