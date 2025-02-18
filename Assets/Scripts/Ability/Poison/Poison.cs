using Fusion;
using UnityEngine;

public class Poison : Ability
{
    [SerializeField] private GameObject poisonPrefab;
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private float upwardForce = 0.5f;
    [SerializeField] private float poisonRadius = 5f;
    [SerializeField] private float poisonDuration = 5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float damageMultiplier = 2f;

    [SerializeField] private float throwingOffset = 1.5f;

    public override void Activate(PlayerRef playeref, Player player)
    {
        // Instantiate the projectile and throw it
        //GameObject projectile = Instantiate(poisonPrefab, player.transform.position + player.transform.forward * throwingOffset + player.transform.up * 1.0f, player.transform.rotation);

        if (!Object.HasStateAuthority)
            return;

        NetworkObject poison = ObjectPoolingManager.Instance.GetPoisonBullet();

        if (poison != null)
        {
            // Use Teleport to set the bullet's position and rotation
            var networkTransform = poison.GetComponent<NetworkTransform>();
            Debug.Log("Reference of  " + playeref);
            poison.GetComponent<AbilityProjectile>()._playerref = playeref;
            if (networkTransform != null)
            {
                Debug.LogWarning("FirstIf!");
                networkTransform.DisableSharedModeInterpolation = true;
                networkTransform.Teleport(player.transform.position + player.transform.forward * throwingOffset + player.transform.up * 1.0f, player.transform.rotation);
                //Debug.Log($"Bullet teleported to position: {firePoint.position}, rotation: {firePoint.rotation}");
                networkTransform.DisableSharedModeInterpolation = false;
            }
            else
            {
                Debug.LogWarning("Poison is missing a NetworkTransform component!");
                poison.transform.position = player.transform.position + player.transform.forward * throwingOffset + player.transform.up * 1.0f;
                poison.transform.rotation = player.transform.rotation;
            }
        }
        else
        {
            Debug.LogError("Failed to spawn bullet from pool!");
        }
        //poison.transform.position = player.transform.position + player.transform.forward * throwingOffset + player.transform.up * 1.0f;
        //poison.transform.rotation = player.transform.rotation;

        Rigidbody rb = poison.GetComponent<Rigidbody>();
        // rb.AddForce(player.transform.forward * throwForce, ForceMode.VelocityChange);
        rb.AddForce(player.transform.forward * throwForce + player.transform.up * throwForce * upwardForce, ForceMode.VelocityChange);

        // Assign poison effect to the projectile
        AbilityProjectile projectileScript = poison.GetComponent<AbilityProjectile>();
        projectileScript.Initialize(poisonRadius, poisonDuration, enemyLayer, ApplyPoisonEffect, ObjectPoolingManager.Instance.PoisonBulletPrefab,GetComponent<Player>().Nickname.ToString());
    }
    private void ApplyPoisonEffect(Collider enemyCollider)
    {
        Enemy enemy = enemyCollider.GetComponent<Enemy>();
        if (enemy != null)
        {
            // Add the poison effect to the enemy
            PoisonEffect poisonEffect = new PoisonEffect(enemy, poisonDuration, damageMultiplier);
            enemy.AddStatusEffect(poisonEffect);
        }
        Player ply = enemyCollider.GetComponent<Player>();
        if (enemy != null)
        {
            // Add the poison effect to the enemy
            PoisonEffect poisonEffect = new PoisonEffect(enemy, poisonDuration, damageMultiplier);
            enemy.AddStatusEffect(poisonEffect);
        }
    }
}
