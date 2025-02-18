using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;

public class ObjectPoolingManager : NetworkBehaviour
{
    public static ObjectPoolingManager Instance { get; private set; }

    [Header("Prefab References")]
    [SerializeField] private NetworkPrefabRef bulletPrefab;
    [SerializeField] private NetworkPrefabRef poisonBulletPrefab;
    [SerializeField] private NetworkPrefabRef freezeBulletPrefab;
    [SerializeField] private NetworkPrefabRef rocketPrefab;

    public NetworkPrefabRef BulletPrefab { get { return bulletPrefab; } }
    public NetworkPrefabRef PoisonBulletPrefab { get { return poisonBulletPrefab; } }
    public NetworkPrefabRef FreezeBulletPrefab { get { return freezeBulletPrefab; } }
    public NetworkPrefabRef RocketPrefab { get { return rocketPrefab; } }

    [Header("Pool Sizes")]
    [SerializeField] private int bulletPoolSize = 10;
    [SerializeField] private int poisonBulletPoolSize = 5;
    [SerializeField] private int freezeBulletPoolSize = 5;
    [SerializeField] private int rocketPoolSize = 5;

    private Dictionary<NetworkPrefabRef, Queue<NetworkObject>> pools;

    public override void Spawned()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("ObjectPoolingManager: Instance already exists! Aborting initialization.");
            return;
        }

        ObjectPoolingEvents.OnObjectInitialized += HandleObjectInitialization;

        if (Object.HasStateAuthority)
        {
            Debug.Log("ObjectPoolingManager: Host initializing pools.");
            InitializePools();
        }
        else
        {
            Debug.Log("ObjectPoolingManager: Client ready to request objects.");
        }
    }

    // Event handler for object initialization
    private void HandleObjectInitialization(NetworkObject obj)
    {
        obj.transform.SetParent(transform);
        obj.gameObject.SetActive(false);
        Debug.Log($"[ObjectPoolingManager] Initialized object {obj.name}");
    }

    private void OnDisable()
    {
        ObjectPoolingEvents.OnObjectInitialized -= HandleObjectInitialization;
    }

    private void InitializePools()
    {
        pools = new Dictionary<NetworkPrefabRef, Queue<NetworkObject>>();

        CreatePool(bulletPrefab, bulletPoolSize);
        CreatePool(poisonBulletPrefab, poisonBulletPoolSize);
        CreatePool(freezeBulletPrefab, freezeBulletPoolSize);
        CreatePool(rocketPrefab, rocketPoolSize);

        Debug.Log("ObjectPoolingManager successfully initialized.");
    }

    private void CreatePool(NetworkPrefabRef prefabRef, int poolSize)
    {
        if (!Object.HasStateAuthority) return;

        if (!prefabRef.IsValid)
        {
            Debug.LogError($"Invalid prefab reference for {prefabRef}. Skipping pool creation.");
            return;
        }

        var pool = new Queue<NetworkObject>();

        for (int i = 0; i < poolSize; i++)
        {
            var obj = Runner.Spawn(prefabRef, Vector3.zero, Quaternion.identity, Object.InputAuthority);

            if (obj != null)
            {
                RPC_SetParent(obj, GetComponent<NetworkObject>());
                RPC_DeactivateObject(obj);
                pool.Enqueue(obj);
            }
            else
            {
                Debug.LogError($"Failed to spawn object for prefab: {prefabRef}");
            }
        }

        pools[prefabRef] = pool;
        Debug.Log($"Pool created for {prefabRef} with {pool.Count} objects.");
    }

    private NetworkObject GetObjectFromPool(NetworkPrefabRef prefabRef)
    {
        if (!pools.ContainsKey(prefabRef))
        {
            Debug.LogError($"No pool found for prefab: {prefabRef}. Creating a new pool dynamically.");
            CreatePool(prefabRef, 1);
        }

        var pool = pools[prefabRef];

        foreach (var obj in pool)
        {
            if (!obj.gameObject.activeInHierarchy)
            {
                RPC_SetObjectActive(obj, true);
                return obj;
            }
        }

        if (Object.HasStateAuthority)
        {
            var newObj = Runner.Spawn(prefabRef, Vector3.zero, Quaternion.identity, Object.InputAuthority);

            if (newObj != null)
            {
                RPC_SetParent(newObj, GetComponent<NetworkObject>());
                RPC_DeactivateObject(newObj);  // First deactivate
                pool.Enqueue(newObj);
                RPC_SetObjectActive(newObj, true);
                return newObj;
            }
            else
            {
                Debug.LogError($"Failed to spawn new object for prefab: {prefabRef}");
            }
        }

        return null;
    }

    public NetworkObject GetBullet(string shotBy, bool shotByPlayer, int damage, Player player = null)
    {
        if (Object.HasStateAuthority)
        {
            var bullet = GetObjectFromPool(bulletPrefab);
            var bulletComponent = bullet.GetComponent<Bullet>();

            bulletComponent.ShotBy = shotBy;
            bulletComponent.ShotByPlayer = shotByPlayer;
            bulletComponent.DamageSetUp(damage, bulletPrefab);

            //bulletComponent.RPC_PlayParticle();

            if (player != null)
                bulletComponent.Player = player;

            return bullet;
        }

        RequestBulletFromHost(shotBy, shotByPlayer, damage, player);
        return null;
    }

    public NetworkObject GetPoisonBullet()
    {
        return Object.HasStateAuthority ? GetObjectFromPool(poisonBulletPrefab) : RequestObjectFromHost(poisonBulletPrefab);
    }

    public NetworkObject GetFreezeBullet()
    {
        return Object.HasStateAuthority ? GetObjectFromPool(freezeBulletPrefab) : RequestObjectFromHost(freezeBulletPrefab);
    }

    public NetworkObject GetRocket()
    {
        return Object.HasStateAuthority ? GetObjectFromPool(rocketPrefab) : RequestObjectFromHost(rocketPrefab);
    }

    public void ReturnToPool(NetworkPrefabRef prefabRef, NetworkObject obj)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("Client cannot directly manage the pool. Notify the host instead.");
            RPC_ReturnObjectToHost(prefabRef, obj);
            return;
        }

        if (pools == null || !pools.ContainsKey(prefabRef))
        {
            Debug.LogError($"No pool exists for prefab: {prefabRef}. Destroying returned object.");
            Runner.Despawn(obj);
            return;
        }

        RPC_SetObjectActive(obj, false);
        pools[prefabRef].Enqueue(obj);
    }

    private NetworkObject RequestObjectFromHost(NetworkPrefabRef prefabRef)
    {
        Debug.Log($"Client requesting object for {prefabRef} from host.");
        RPC_RequestObject(prefabRef);
        return null;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestObject(NetworkPrefabRef prefabRef)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogError("ObjectPoolingManager: RPC called by non-authoritative object.");
            return;
        }

        var obj = GetObjectFromPool(prefabRef);
        if (obj != null)
        {
            Debug.Log($"ObjectPoolingManager: Object spawned for prefab {prefabRef}");
        }
    }

    private void RequestBulletFromHost(string shotBy, bool shotByPlayer, int damage, Player player)
    {
        Debug.Log("Client requesting bullet from host.");
        RPC_RequestBullet(shotBy, shotByPlayer, damage, player);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetParent(NetworkObject obj, NetworkObject parent)
    {
        if (obj != null && parent != null)
        {
            obj.transform.SetParent(parent.transform);
            Debug.Log($"Parent of {obj.name} set to {parent.name}");
        }
        else
        {
            Debug.LogError("RPC_SetParent: Either obj or parent is null.");
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetObjectActive(NetworkObject obj, bool active)
    {
        if (obj != null)
        {
            obj.gameObject.SetActive(active);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestBullet(string shotBy, bool shotByPlayer, int damage, Player player)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogError("ObjectPoolingManager: RPC called by non-authoritative object.");
            return;
        }

        var bullet = GetObjectFromPool(bulletPrefab);
        var bulletComponent = bullet.GetComponent<Bullet>();

        bulletComponent.ShotBy = shotBy;
        bulletComponent.ShotByPlayer = shotByPlayer;
        bulletComponent.DamageSetUp(damage, bulletPrefab);

        //bulletComponent.RPC_PlayParticle();

        if (player != null)
            bulletComponent.Player = player;
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SyncObjectTransform(NetworkObject obj, Vector3 position, Quaternion rotation)
    {
        if (obj != null)
        {
            obj.transform.position = position;
            obj.transform.rotation = rotation;
        }
        else
        {
            Debug.LogError("RPC_SyncObjectTransform: Attempted to sync null object.");
        }
    }
}


//using System.Collections.Generic;
//using UnityEngine;
//using Fusion;

//public class ObjectPoolingManager : NetworkBehaviour
//{
//    public static ObjectPoolingManager Instance { get; private set; }

//    [Header("Prefab References")]
//    [SerializeField] private NetworkPrefabRef bulletPrefab;
//    [SerializeField] private NetworkPrefabRef poisonBulletPrefab;
//    [SerializeField] private NetworkPrefabRef freezeBulletPrefab;
//    [SerializeField] private NetworkPrefabRef rocketPrefab;

//    public NetworkPrefabRef BulletPrefab { get { return bulletPrefab; } }
//    public NetworkPrefabRef PoisonBulletPrefab { get { return poisonBulletPrefab; } }
//    public NetworkPrefabRef FreezeBulletPrefab { get { return freezeBulletPrefab; } }
//    public NetworkPrefabRef RocketPrefab { get { return rocketPrefab; } }

//    [Header("Pool Sizes")]
//    [SerializeField] private int bulletPoolSize = 10;
//    [SerializeField] private int poisonBulletPoolSize = 5;
//    [SerializeField] private int freezeBulletPoolSize = 5;
//    [SerializeField] private int rocketPoolSize = 5;

//    private Dictionary<NetworkPrefabRef, Queue<NetworkObject>> pools;

//    public override void Spawned()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//        }
//        else
//        {
//            Debug.LogError("ObjectPoolingManager: Instance already exists! Aborting initialization.");
//            return;
//        }

//        if (Object.HasStateAuthority)
//        {
//            Debug.Log("ObjectPoolingManager: Host initializing pools.");
//            InitializePools();
//        }
//        else
//        {
//            Debug.Log("ObjectPoolingManager: Client ready to request objects.");
//            //Runner.Despawn(Object);
//        }
//    }

//    private void InitializePools()
//    {
//        pools = new Dictionary<NetworkPrefabRef, Queue<NetworkObject>>();

//        CreatePool(bulletPrefab, bulletPoolSize);
//        CreatePool(poisonBulletPrefab, poisonBulletPoolSize);
//        CreatePool(freezeBulletPrefab, freezeBulletPoolSize);
//        CreatePool(rocketPrefab, rocketPoolSize);

//        Debug.Log("ObjectPoolingManager successfully initialized.");
//    }

//    private void CreatePool(NetworkPrefabRef prefabRef, int poolSize)
//    {
//        if (!Object.HasStateAuthority) return;

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
//                RPC_DeactivateObject(obj);
//                pool.Enqueue(obj);
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
//        if (!pools.ContainsKey(prefabRef))
//        {
//            Debug.LogError($"No pool found for prefab: {prefabRef}. Creating a new pool dynamically.");
//            CreatePool(prefabRef, 1);
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

//    public NetworkObject GetBullet(string shotBy, bool shotByPlayer, int damage, Player player = null)
//    {
//        if (Object.HasStateAuthority)
//        {
//            var bullet = GetObjectFromPool(bulletPrefab);
//            var bulletComponent = bullet.GetComponent<Bullet>();

//            bulletComponent.ShotBy = shotBy;
//            bulletComponent.ShotByPlayer = shotByPlayer;
//            bulletComponent.DamageSetUp(damage, bulletPrefab);

//            if (player != null)
//                bulletComponent.Player = player;

//            return bullet;
//        }

//        RequestBulletFromHost(shotBy, shotByPlayer, damage, player);
//        return null;
//    }

//    public NetworkObject GetPoisonBullet()
//    {
//        return Object.HasStateAuthority ? GetObjectFromPool(poisonBulletPrefab) : RequestObjectFromHost(poisonBulletPrefab);
//    }

//    public NetworkObject GetFreezeBullet()
//    {
//        return Object.HasStateAuthority ? GetObjectFromPool(freezeBulletPrefab) : RequestObjectFromHost(freezeBulletPrefab);
//    }

//    public NetworkObject GetRocket()
//    {
//        return Object.HasStateAuthority ? GetObjectFromPool(rocketPrefab) : RequestObjectFromHost(rocketPrefab);
//    }

//    private void RequestBulletFromHost(string shotBy, bool shotByPlayer, int damage, Player player)
//    {
//        Debug.Log("Client requesting bullet from host.");
//        RPC_RequestBullet(shotBy, shotByPlayer, damage, player);
//    }

//    private NetworkObject RequestObjectFromHost(NetworkPrefabRef prefabRef)
//    {
//        Debug.Log($"Client requesting object for {prefabRef} from host.");
//        RPC_RequestObject(prefabRef);
//        return null;
//    }

//    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
//    private void RPC_RequestObject(NetworkPrefabRef prefabRef)
//    {
//        if (!Object.HasStateAuthority)
//        {
//            Debug.LogError("ObjectPoolingManager: RPC called by non-authoritative object.");
//            return;
//        }

//        var obj = GetObjectFromPool(prefabRef);
//        if (obj != null)
//        {
//            Debug.Log($"ObjectPoolingManager: Object spawned for prefab {prefabRef}");
//        }
//    }

//    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
//    private void RPC_RequestBullet(string shotBy, bool shotByPlayer, int damage, Player player)
//    {
//        if (!Object.HasStateAuthority)
//        {
//            Debug.LogError("ObjectPoolingManager: RPC called by non-authoritative object.");
//            return;
//        }

//        var bullet = GetObjectFromPool(bulletPrefab);
//        var bulletComponent = bullet.GetComponent<Bullet>();

//        bulletComponent.ShotBy = shotBy;
//        bulletComponent.ShotByPlayer = shotByPlayer;
//        bulletComponent.DamageSetUp(damage, bulletPrefab);

//        if (player != null)
//            bulletComponent.Player = player;
//    }

//    public void ReturnToPool(NetworkPrefabRef prefabRef, NetworkObject obj)
//    {
//        if (!Object.HasStateAuthority)
//        {
//            Debug.LogWarning("Client cannot directly manage the pool. Notify the host instead.");
//            RPC_ReturnObjectToHost(prefabRef, obj);
//            return;
//        }

//        if (pools == null)
//        {
//            Debug.LogError("Pools dictionary is null! Ensure InitializePools is called before using ObjectPoolingManager.");
//            return;
//        }

//        if (!pools.ContainsKey(prefabRef))
//        {
//            Debug.LogError($"No pool exists for prefab: {prefabRef}. Destroying returned object.");
//            Runner.Despawn(obj);
//            return;
//        }

//        // Deactivate the object on all clients
//        RPC_DeactivateObject(obj);

//        ////obj.gameObject.SetActive(false);
//        //Runner.Despawn(obj);
//        pools[prefabRef].Enqueue(obj);
//    }

//    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
//    private void RPC_DeactivateObject(NetworkObject obj)
//    {
//        obj.gameObject.SetActive(false);
//    }

//    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
//    private void RPC_ReturnObjectToHost(NetworkPrefabRef prefabRef, NetworkObject obj)
//    {
//        if (!Object.HasStateAuthority)
//        {
//            Debug.LogError("ObjectPoolingManager: RPC called by non-authoritative object.");
//            return;
//        }

//        ReturnToPool(prefabRef, obj);
//    }

//}

// old shit


//using System.Collections.Generic;
//using UnityEngine;
//using Fusion;
//public class ObjectPoolingManager : NetworkBehaviour
//{
//    public static ObjectPoolingManager Instance { get; private set; }

//    [Header("Prefab References")]
//    [SerializeField] private NetworkPrefabRef bulletPrefab;
//    [SerializeField] private NetworkPrefabRef poisonBulletPrefab;
//    [SerializeField] private NetworkPrefabRef freezeBulletPrefab;
//    [SerializeField] private NetworkPrefabRef rocketPrefab;

//    public NetworkPrefabRef BulletPrefab { get { return rocketPrefab; } }
//    public NetworkPrefabRef PoisonBulletPrefab { get { return rocketPrefab; } }
//    public NetworkPrefabRef FreezeBulletPrefab { get { return rocketPrefab; } }
//    public NetworkPrefabRef RocketPrefab { get { return rocketPrefab; } }

//    [Header("Pool Sizes")]
//    [SerializeField] private int bulletPoolSize = 10;
//    [SerializeField] private int poisonBulletPoolSize = 5;
//    [SerializeField] private int freezeBulletPoolSize = 5;
//    [SerializeField] private int rocketPoolSize = 5;

//    private Dictionary<NetworkPrefabRef, Queue<NetworkObject>> pools;

//    //public override void Spawned()
//    //{
//    //    //if (!Object.HasStateAuthority)
//    //    //{
//    //    //    // Only the host initializes the pool
//    //    //    return;
//    //    //}

//    //    //Debug.Log("Object Pooling Manager spawned");
//    //    //Instance = this;
//    //    //InitializePools();

//    //    // Ensure singleton instance is only set once
//    //    if (Instance == null)
//    //    {
//    //        Instance = this;
//    //    }
//    //    else
//    //    {
//    //        Debug.LogError("Object Pooling Manager: Instance already exists! Aborting initialization.");
//    //        return; // Prevent duplicate initialization
//    //    }

//    //    if (Object.HasStateAuthority)
//    //    {
//    //        // Host-specific initialization
//    //        Debug.Log("Object Pooling Manager: Host initializing pools.");
//    //        InitializePools(); // Host initializes shared pools
//    //    }
//    //    else
//    //    {
//    //        // Client-specific initialization (optional)
//    //        Debug.Log("Object Pooling Manager: Client skipping pool initialization.");
//    //    }
//    //}

//    public void Initialise()
//    {
//        //if (!Object.HasStateAuthority)
//        //{
//        //    // Only the host initializes the pool
//        //    return;
//        //}

//        //Debug.Log("Object Pooling Manager spawned");
//        //Instance = this;
//        //InitializePools();

//        if (Object.HasStateAuthority)
//        {
//            Debug.Log("Object Pooling Manager: Host initializing pools.");
//            Instance = this;
//            InitializePools(); // Host initializes pool state
//        }
//        else
//        {
//            Debug.Log("Object Pooling Manager: Client initializing local pools.");
//            Instance = this;
//            InitializePools(); // Clients initialize their local pools
//        }
//    }

//    private void InitializePools()
//    {
//        pools = new Dictionary<NetworkPrefabRef, Queue<NetworkObject>>();

//        CreatePool(bulletPrefab, bulletPoolSize);
//        CreatePool(poisonBulletPrefab, poisonBulletPoolSize);
//        CreatePool(freezeBulletPrefab, freezeBulletPoolSize);
//        CreatePool(rocketPrefab, rocketPoolSize);

//        Debug.Log("ObjectPoolingManager successfully initialized.");
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
//        if (!pools.ContainsKey(prefabRef))
//        {
//            Debug.LogError($"No pool found for prefab: {prefabRef}. Creating a new pool dynamically.");
//            CreatePool(prefabRef, 1);
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

//        //// No inactive objects found, dynamically spawn a new one
//        //var newObj = Runner.Spawn(prefabRef, Vector3.zero, Quaternion.identity, Object.InputAuthority, (runner, spawnedObj) =>
//        //{
//        //    spawnedObj.transform.SetParent(transform);
//        //    spawnedObj.gameObject.SetActive(true);
//        //});

//        //if (newObj != null)
//        //{
//        //    pool.Enqueue(newObj);
//        //}
//        //else
//        //{
//        //    Debug.LogError($"Failed to spawn new object for prefab: {prefabRef}");
//        //}

//        //return newObj;
//    }

//    public NetworkObject GetBullet(string shotBy, bool shotByPlayer, int damage, Player player = null)
//    {
//        var bullet = GetObjectFromPool(bulletPrefab);
//        var bulletComponent = bullet.GetComponent<Bullet>();

//        bulletComponent.ShotBy = shotBy;
//        bulletComponent.ShotByPlayer = shotByPlayer;
//        bulletComponent.DamageSetUp(damage, bulletPrefab);

//        if (player != null)
//            bulletComponent.Player = player;

//        return bullet;
//    }

//    public NetworkObject GetPoisonBullet()
//    {
//        return GetObjectFromPool(poisonBulletPrefab);
//    }

//    public NetworkObject GetFreezeBullet()
//    {
//        return GetObjectFromPool(freezeBulletPrefab);
//    }

//    public NetworkObject GetRocket()
//    {
//        return GetObjectFromPool(rocketPrefab);
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

////public class ObjectPoolingManager : MonoBehaviour
////{

////    private static ObjectPoolingManager instance;
////    public static ObjectPoolingManager Instance { get { return instance; } }

////    [SerializeField] private GameObject bulletPrefab;
////    [SerializeField] private GameObject poisonBulletPrefab;
////    [SerializeField] private GameObject freezeBulletPrefab;
////    [SerializeField] private GameObject rocketPrefab;

////    private List<GameObject> bullets;
////    private List<GameObject> poisonBullets;
////    private List<GameObject> freezeBullets;
////    private List<GameObject> rockets;

////    [SerializeField] private int bulletAmount = 10;
////    [SerializeField] private int poisonBulletAmount = 5;
////    [SerializeField] private int freezeBulletAmount = 5;
////    [SerializeField] private int rocketAmount = 5;

////    void Awake()
////    {
////        instance = this;

////        //Preload bullets
////        bullets = new List<GameObject>(bulletAmount);
////        for (int i = 0; i < bulletAmount; i++)
////        {
////            GameObject prefabInstance = Instantiate(bulletPrefab);
////            //So the prefabInstance will be under this ObjectPooling Manager for organisation
////            prefabInstance.transform.SetParent(transform);
////            prefabInstance.SetActive(false);

////            bullets.Add(prefabInstance);
////        }

////        //Preload Poison Bullet
////        poisonBullets = new List<GameObject>(poisonBulletAmount);
////        for (int i = 0; i < poisonBulletAmount; i++)
////        {
////            GameObject prefabInstance = Instantiate(poisonBulletPrefab);
////            //So the prefabInstance will be under this ObjectPooling Manager for organisation
////            prefabInstance.transform.SetParent(transform);
////            prefabInstance.SetActive(false);

////            poisonBullets.Add(prefabInstance);
////        }

////        //Preload Freeze Bullet
////        freezeBullets = new List<GameObject>(freezeBulletAmount);
////        for (int i = 0; i < freezeBulletAmount; i++)
////        {
////            GameObject prefabInstance = Instantiate(freezeBulletPrefab);
////            //So the prefabInstance will be under this ObjectPooling Manager for organisation
////            prefabInstance.transform.SetParent(transform);
////            prefabInstance.SetActive(false);

////            freezeBullets.Add(prefabInstance);
////        }

////        //Preload Rocket Bullet
////        rockets = new List<GameObject>(rocketAmount);
////        for (int i = 0; i < rocketAmount; i++)
////        {
////            GameObject prefabInstance = Instantiate(rocketPrefab);
////            //So the prefabInstance will be under this ObjectPooling Manager for organisation
////            prefabInstance.transform.SetParent(transform);
////            prefabInstance.SetActive(false);

////            rockets.Add(prefabInstance);
////        }
////    }

////    public GameObject GetBullet(string shotBy, bool shotByPlayer, int damage, Player player = null)
////    {
////        foreach (GameObject bullet in bullets)
////        {
////            if (!bullet.activeInHierarchy)
////            {
////                bullet.SetActive(true);
////                bullet.GetComponent<Bullet>().ShotBy = shotBy;
////                bullet.GetComponent<Bullet>().ShotByPlayer = shotByPlayer;
////                bullet.GetComponent<Bullet>().DamageSetUp(damage);

////                if(player != null)
////                {
////                    bullet.GetComponent<Bullet>().Player = player;
////                }

////                return bullet;
////            }
////        }
////        GameObject prefabInstance = Instantiate(bulletPrefab);
////        //so the prefabInstance will be under this ObjectPooling Manager for organisation
////        prefabInstance.transform.SetParent(transform);
////        prefabInstance.GetComponent<Bullet>().ShotBy = shotBy;
////        prefabInstance.GetComponent<Bullet>().ShotByPlayer = shotByPlayer;
////        prefabInstance.GetComponent<Bullet>().DamageSetUp(damage);

////        if (player != null)
////        {
////            prefabInstance.GetComponent<Bullet>().Player = player;
////        }

////        bullets.Add(prefabInstance);
////        return prefabInstance;
////    }

////    public GameObject GetPoisonBullet()
////    {
////        foreach (GameObject poisonBullet in poisonBullets)
////        {
////            if (!poisonBullet.activeInHierarchy)
////            {
////                poisonBullet.SetActive(true);
////                return poisonBullet;
////            }
////        }
////        GameObject prefabInstance = Instantiate(poisonBulletPrefab);
////        //so the prefabInstance will be under this ObjectPooling Manager for organisation
////        prefabInstance.transform.SetParent(transform);
////        poisonBullets.Add(prefabInstance);
////        return prefabInstance;
////    }

////    public GameObject GetFreezeBullet()
////    {
////        foreach (GameObject freezeBullet in freezeBullets)
////        {
////            if (!freezeBullet.activeInHierarchy)
////            {
////                freezeBullet.SetActive(true);
////                return freezeBullet;
////            }
////        }
////        GameObject prefabInstance = Instantiate(freezeBulletPrefab);
////        //so the prefabInstance will be under this ObjectPooling Manager for organisation
////        prefabInstance.transform.SetParent(transform);
////        freezeBullets.Add(prefabInstance);
////        return prefabInstance;
////    }

////    public GameObject GetRocket()
////    {
////        foreach (GameObject rocket in rockets)
////        {
////            if (!rocket.activeInHierarchy)
////            {
////                rocket.SetActive(true);
////                return rocket;
////            }
////        }
////        GameObject prefabInstance = Instantiate(rocketPrefab);
////        //so the prefabInstance will be under this ObjectPooling Manager for organisation
////        prefabInstance.transform.SetParent(transform);
////        rockets.Add(prefabInstance);
////        return prefabInstance;
////    }
////}
