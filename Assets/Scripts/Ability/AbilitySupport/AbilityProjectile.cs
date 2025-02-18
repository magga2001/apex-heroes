using System.Collections;
using UnityEngine;
using Fusion;

public class AbilityProjectile : NetworkBehaviour
{
    private float effectRadius;
    private float effectDuration;
    private LayerMask affectedLayer;
    private System.Action<Collider> effectAction;  // Action to apply the specific effect
    private NetworkPrefabRef associatedPrefabRef;  // Track the prefab reference
    public PlayerRef _playerref;
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float explosionDelay = 2f;  // Time before the projectile explodes automatically

    private string shotBy;
    public override void Spawned()
    {
        ObjectPoolingEvents.OnObjectInitialized?.Invoke(Object);
    }

    // Initialize method for setting up the projectile with custom values
    public void Initialize(float radius, float duration, LayerMask layerMask, System.Action<Collider> action, NetworkPrefabRef prefabRef,string shotby)
    {
        effectRadius = radius;
        effectDuration = duration;
        affectedLayer = layerMask;
        effectAction = action;
        associatedPrefabRef = prefabRef; // Assign the prefab reference
        shotBy = shotby;
        if (Object.HasStateAuthority)
        {
            // Start the timer to explode after a delay
            Runner.Invoke("Explode", explosionDelay);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!Object.HasStateAuthority)
        {
         // Only the server processes collisions
         return;
        }

        // Cancel the delayed explosion
        Runner.CancelInvoke("Explode" + _playerref);
        // Trigger the explosion immediately

        Player targetPlayer = collision.gameObject.GetComponent<Player>();
        if (targetPlayer != null)
        {
            targetPlayer.TakeDamage(_playerref,50, shotBy);
        }
        Explode();
    }
    
    private void Explode()
    {
        // Apply the effect in the area of impact
        RPC_ApplyEffect();

        // Trigger explosion visuals on all clients
        TriggerExplosionEffect(transform.position);

        // Return the projectile to the pool
        NetworkObject projectile = GetComponent<NetworkObject>();
        if (projectile != null)
        {
           
            ObjectPoolingManager.Instance.ReturnToPool(associatedPrefabRef, projectile);
        }
        else
        {
            Debug.LogWarning("NetworkObject not found on projectile; cannot return to pool.");
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ApplyEffect()
    {
        Collider[] affectedObjects = Physics.OverlapSphere(transform.position, effectRadius, affectedLayer);
        foreach (Collider affectedObject in affectedObjects)
        {
            effectAction?.Invoke(affectedObject);  // Apply the custom effect to each affected object
        }
    }
    private void TriggerExplosionEffect(Vector3 position)
    {
        // Handle explosion visuals locally on each client
        if (impactEffectPrefab != null)
        {
            NetworkObject explosionEffect = EffectPoolingManager.Instance.GetImpactEffect(impactEffectPrefab);
            RPC_ExplosionEffect(position, explosionEffect);
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ExplosionEffect(Vector3 position,NetworkId effectId)
    {
        if (Runner.TryFindObject(effectId, out NetworkObject netObj))  //Used Runner.TryFindObject() to find the NetworkObject on all clients using the NetworkId.
        {
            netObj.transform.position = position;
            netObj.transform.rotation = Quaternion.identity;
            netObj.gameObject.SetActive(true);
            return;
        }
    }

}


//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Fusion;

//public class AbilityProjectile : MonoBehaviour
//{
//    private float effectRadius;
//    private float effectDuration;
//    private LayerMask affectedLayer;
//    private System.Action<Collider> effectAction;  // Action to apply the specific effect
//    [SerializeField] private GameObject impactEffectPrefab;
//    [SerializeField] private float explosionDelay = 2f;  // Time before the projectile explodes automatically

//    // Initialize method for setting up the projectile with custom values
//    public void Initialize(float radius, float duration, LayerMask layerMask, System.Action<Collider> action)
//    {
//        effectRadius = radius;
//        effectDuration = duration;
//        affectedLayer = layerMask;
//        effectAction = action;

//        // Start the timer to explode after a delay
//        Invoke("Explode", explosionDelay);
//    }

//    private void OnCollisionEnter(Collision collision)
//    {
//        // Cancel the delayed explosion if the projectile hits something first
//        CancelInvoke("Explode");

//        // Trigger the explosion immediately
//        Explode();
//    }

//    private void Explode()
//    {
//        // Apply the effect in the area of impact
//        ApplyEffect();

//        // Trigger explosion effect (visual or particle effect)
//        if (impactEffectPrefab != null)
//        {
//            // Get the pooled impact effect from the EffectPoolingManager
//            NetworkObject explosionEffect = EffectPoolingManager.Instance.GetImpactEffect(impactEffectPrefab);

//            // Move the pooled explosion effect to the position of the explosion
//            if (explosionEffect != null)
//            {
//                explosionEffect.transform.position = transform.position;
//                explosionEffect.transform.rotation = Quaternion.identity; // Optional, reset rotation
//            }
//        }

//        // Destroy the projectile after explosion
//        gameObject.SetActive(false);
//    }

//    void ApplyEffect()
//    {
//        // Find all affected objects within the explosion radius
//        Collider[] affectedObjects = Physics.OverlapSphere(transform.position, effectRadius, affectedLayer);
//        foreach (Collider affectedObject in affectedObjects)
//        {
//            effectAction?.Invoke(affectedObject);  // Apply the custom effect to each affected object
//        }
//    }
//}
