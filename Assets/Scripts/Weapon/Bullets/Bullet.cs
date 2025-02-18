using System.Collections;
using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float speed = 20f; // Bullet speed
    [SerializeField] private float lifeDuration = 3f; // Bullet lifetime before auto-return to pool
    private float lifeTimer;

    private int damage; // Bullet damage amount

    private bool shotByPlayer;
    public bool ShotByPlayer { get { return shotByPlayer; } set { shotByPlayer = value; } }

    private string shotBy; // Tracks who fired the bullet
    public string ShotBy { get { return shotBy; } set { shotBy = value; } }

    public PlayerRef _playerref;

    private Player player; // Reference to the player who fired the bullet
    public Player Player { get { return player; } set { player = value; } }

    private NetworkPrefabRef associatedPrefabRef; // Tracks the prefab reference for pooling

    public override void Spawned()
    {
        ObjectPoolingEvents.OnObjectInitialized?.Invoke(Object);
    }

    private void OnEnable()
    {
        // Reset life timer when bullet is activated
        lifeTimer = lifeDuration;
    }
    public override void FixedUpdateNetwork()
    {
        // Only host moves the bullet
        if (Object.HasStateAuthority)
        {
            // Move forward based on speed and Runner.DeltaTime
            transform.position += transform.forward * speed * Runner.DeltaTime;
            Debug.Log("Bullet Moving");
            // Decrease lifetime
            DecreaseLifeTime();
        }
    }

    private void DecreaseLifeTime()
    {
        lifeTimer -= Runner.DeltaTime;
        if (lifeTimer <= 0f)
        { 
            Debug.Log("Bullet return to pool");
            ReturnToPool(); // Only host calls this
        }
    }

    public void DamageSetUp(int damage, NetworkPrefabRef prefabRef)
    {
        this.damage = damage;
        this.associatedPrefabRef = prefabRef; // Assign prefab reference for pooling
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only the host handles collisions and object pooling
        //if (!Object.HasInputAuthority)
        //    return;
        Debug.LogWarning($"{name} is server {Runner.IsServer}");
        Debug.Log($"Bullet Collide 1 {Runner.IsServer}");

        if (!Runner.IsServer)
            return;

        Debug.LogWarning($"{name} collided with {other.name}  and {shotByPlayer}");
        Debug.Log($"Bullet Collide 2 {Runner.IsServer}");
        // Handle collision with a Player
        Player targetPlayer = other.GetComponent<Player>();
        Debug.Log($"Bullet Collide 3 {targetPlayer} {ShotByPlayer}");
        if (targetPlayer != null && ShotByPlayer&& targetPlayer.GetComponent<NetworkObject>().Runner.LocalPlayer!= _playerref)
        {
            Debug.Log($"Bullet Collide with local player :: {targetPlayer.GetComponent<NetworkObject>().Runner.LocalPlayer}");
            targetPlayer.TakeDamage(_playerref,damage, shotBy);
            if (player != null)
            {
                
                player.CallIncreaseAbilityXP();
            }
            ReturnToPool();
            return;
        }
        
        // Handle collision with an Enemy
        Enemy enemy = other.GetComponent<Enemy>();
        Debug.Log($"Bullet Collide 4 {enemy} {ShotByPlayer}");
        if (enemy != null && ShotByPlayer)
        {
            enemy.TakeDamage(_playerref, damage, shotBy);

            // Award XP to the firing player
            if (player != null)
            {
               player.CallIncreaseAbilityXP();
            }

            ReturnToPool();
            return;
        }

        // Handle collision with a Crate
        Crate crate = other.GetComponent<Crate>();
        Debug.Log($"Bullet Collide 5 {crate} {ShotByPlayer}");
        if (crate != null && ShotByPlayer)
        {
            crate.TakeDamage(damage);
            ReturnToPool();
            return;
        }

        // If it hits something else or nothing relevant, you can decide what to do
        // For now, if it doesn't match anything above and is a host action, just return to pool
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (associatedPrefabRef != null && ObjectPoolingManager.Instance != null)
        {
            // Only host manages the pool, this call is safe since we are in HasStateAuthority context
            ObjectPoolingManager.Instance.ReturnToPool(associatedPrefabRef, Object);
        }
        else
        {
            Debug.LogWarning("No associated prefab reference or ObjectPoolingManager is null; unable to return to pool.");
            gameObject.SetActive(false); // Fallback: deactivate the bullet if pooling fails
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_PlayParticle()
    {
        GetComponent<ParticleSystem>().Play();
        Debug.Log("ParticleStarted");
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_StopParticle()
    {
        GetComponent<ParticleSystem>().Stop();
    }
}


//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Fusion;

//public class Bullet : NetworkBehaviour
//{
//    [SerializeField] private float speed = 20f;
//    [SerializeField] private float lifeDuration;

//    private int damage;

//    private float lifeTimer;

//    private bool shotByPlayer;
//    public bool ShotByPlayer { get { return shotByPlayer; } set { shotByPlayer = value; } }

//    private string shotBy;
//    public string ShotBy { get { return shotBy; } set { shotBy = value; } }

//    private Player player;
//    public Player Player { get { return player; } set { player = value; } }

//    private void OnEnable()
//    {
//        lifeTimer = lifeDuration;
//        shotBy = "";
//        player = null;
//    }

//    void Update()
//    {
//        transform.position += transform.forward * speed * Time.deltaTime;
//        DecreaseLifeTime();
//    }

//    private void DecreaseLifeTime()
//    {
//        lifeTimer -= Time.deltaTime;
//        if (lifeTimer <= 0f)
//        {
//            gameObject.SetActive(false);
//        }
//    }

//    public void DamageSetUp(int damage)
//    {
//        this.damage = damage;
//    }

//    private void OnTriggerEnter(Collider other)
//    {
//        Player player = other.GetComponent<Player>();
//        if(player != null && ShotByPlayer == false)
//        {
//            player.TakeDamage(damage, shotBy);
//            gameObject.SetActive(false);    
//        }

//        Enemy enemy = other.GetComponent<Enemy>();
//        if (enemy != null && ShotByPlayer == true)
//        {
//            enemy.TakeDamage(damage, shotBy);
//            if(this.player != null)
//            {
//                this.player.IncreaseAbilityXP(2);
//            }
//            gameObject.SetActive(false);
//        }

//        Crate crate = other.GetComponent<Crate>();
//        if(crate != null && ShotByPlayer == true)
//        {
//            crate.TakeDamage(damage);
//            gameObject.SetActive(false);
//        }
//    }
//}
