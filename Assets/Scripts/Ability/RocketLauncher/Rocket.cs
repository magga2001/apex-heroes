using Fusion;
using System.Collections;
using UnityEngine;

public class Rocket : NetworkBehaviour
{
    [SerializeField] private float explosionRadius = 20f;
    [SerializeField] private float explosionForce = 700f;
    [SerializeField] private int damage = 100;
    public PlayerRef _playerref;
    [SerializeField] private float knockbackDuration = 0.5f;
    [SerializeField] private GameObject explosionEffectPrefab;
    private string shotBy; // Tracks who fired the bullet
    public string ShotBy { get { return shotBy; } set { shotBy = value; } }
    public override void Spawned()
    {
        ObjectPoolingEvents.OnObjectInitialized?.Invoke(Object);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!Runner.IsServer)
            return;
        Debug.Log("Collider with player");
        // Apply gameplay logic (damage, knockback)
        HandleExplosion();

        // Trigger explosion effect across all clients
        TriggerExplosionEffect(transform.position);
       
            // Return the rocket to the pool
        ObjectPoolingManager.Instance.ReturnToPool(ObjectPoolingManager.Instance.RocketPrefab, GetComponent<NetworkObject>());
        return;
    }

    private void HandleExplosion()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider nearbyObject in colliders)
        {
            
            Player targetPlayer = nearbyObject.GetComponent<Player>();
            if (targetPlayer != null)
            {
                Debug.Log("Triggered 333");
                targetPlayer.TakeDamage(_playerref, damage, shotBy);
                Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                    StartCoroutine(ApplyKnockbackForLimitedTime(rb));

                }
            }

            Enemy enemy = nearbyObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Apply damage
                enemy.TakeDamage(_playerref, damage, shotBy);
                // Apply knockback
                Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                    StartCoroutine(ApplyKnockbackForLimitedTime(rb));
                  
                }
            }
        }
    }
    public void TriggerExplosionEffect(Vector3 position)
    {
        Debug.Log("Collider with player 11");
        // Show explosion effect locally
        NetworkObject explosionEffect = EffectPoolingManager.Instance.GetImpactEffect(explosionEffectPrefab);
        RPC_ExplosionEffect(position, explosionEffect.Id);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ExplosionEffect(Vector3 position, NetworkId effectId)
    {
        if (Runner.TryFindObject(effectId, out NetworkObject netObj))  //Used Runner.TryFindObject() to find the NetworkObject on all clients using the NetworkId.
        {
            netObj.transform.position = position;
            netObj.transform.rotation = Quaternion.identity;
            netObj.gameObject.SetActive(true);
            return;
        }
    }
    private IEnumerator ApplyKnockbackForLimitedTime(Rigidbody rb)
    {
        float timer = 0f;

        while (timer < knockbackDuration)
        {
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Stop the enemy's movement after knockback duration
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}


//using Fusion;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class Rocket : NetworkBehaviour
//{
//    [SerializeField] private float explosionRadius = 20f;  // Radius of explosion
//    [SerializeField] private float explosionForce = 700f;  // Force applied to enemies
//    [SerializeField] private int damage = 100;  // Damage dealt by the rocket
//    [SerializeField] private GameObject explosionEffectPrefab;
//    [SerializeField] private float knockbackDuration = 0.5f; // Duration to apply force

//    private string shotBy;
//    public string ShotBy { get { return shotBy; } set { shotBy = value; } }

//    private void OnCollisionEnter(Collision collision)
//    {
//        Debug.Log("Rocket collided with: " + collision.gameObject.name);

//        // Create an explosion force that affects enemies in the area
//        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
//        foreach (Collider nearbyObject in colliders)
//        {
//            // Check if the object is an enemy
//            Enemy enemy = nearbyObject.GetComponent<Enemy>();
//            if (enemy != null)
//            {
//                // Apply damage to the enemy
//                enemy.TakeDamage(damage, shotBy);

//                // Apply explosion force to the enemy's Rigidbody
//                Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
//                if (rb != null && !rb.isKinematic)
//                {
//                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);

//                    // Apply knockback force for a limited time
//                    StartCoroutine(ApplyKnockbackForLimitedTime(rb));
//                }
//            }
//        }

//        // Instantiate a visual explosion effect
//        if (explosionEffectPrefab != null)
//        {
//            //Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

//            // Get the pooled impact effect from the EffectPoolingManager
//            NetworkObject explosionEffect = EffectPoolingManager.Instance.GetImpactEffect(explosionEffectPrefab);

//            // Move the pooled explosion effect to the position of the explosion
//            if (explosionEffect != null)
//            {
//                explosionEffect.transform.position = transform.position;
//                explosionEffect.transform.rotation = Quaternion.identity; // Optional, reset rotation
//            }
//        }

//        // Destroy the rocket after impact
//        gameObject.SetActive(false);
//    }

//    // Coroutine to apply knockback for a limited time, then stop the movement
//    private IEnumerator ApplyKnockbackForLimitedTime(Rigidbody rb)
//    {
//        float timer = 0f;

//        while (timer < knockbackDuration)
//        {
//            timer += Time.deltaTime;
//            yield return null;  // Wait for the next frame
//        }

//        // After knockback time, stop the enemy's movement
//        rb.velocity = Vector3.zero;
//        rb.angularVelocity = Vector3.zero;
//    }
//}
