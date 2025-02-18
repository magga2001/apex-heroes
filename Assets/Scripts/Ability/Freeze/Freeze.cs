using Fusion;
using UnityEngine;

public class Freeze : Ability
{
    [SerializeField] private GameObject freezePrefab;  // The projectile prefab for freezer ability
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private float upwardForce = 0.5f;
    [SerializeField] private float freezeRadius = 5f;
    [SerializeField] private float freezeDuration = 5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float slowMultiplier = 0.5f;  // Multiplier to reduce movement speed (50% slower)

    [SerializeField] private float throwingOffset = 1.5f;

    public override void Activate(PlayerRef playeref, Player player)
    {
        // Instantiate and throw the freezer projectile
        //GameObject freezer = Instantiate(freezePrefab, player.transform.position + player.transform.forward * throwingOffset + player.transform.up * 1.0f, player.transform.rotation);

        if (!Object.HasStateAuthority)
            return;

        NetworkObject freezer = ObjectPoolingManager.Instance.GetFreezeBullet();
        freezer.transform.position = player.transform.position + player.transform.forward * throwingOffset + player.transform.up * 1.0f;
        freezer.transform.rotation = player.transform.rotation;

        Rigidbody rb = freezer.GetComponent<Rigidbody>();
        rb.AddForce(player.transform.forward * throwForce + player.transform.up * throwForce * upwardForce, ForceMode.VelocityChange);

        // Assign freeze effect to the freezer projectile
        AbilityProjectile projectileScript = freezer.GetComponent<AbilityProjectile>();
        projectileScript.Initialize(freezeRadius, freezeDuration, enemyLayer, ApplyFreezeEffect, ObjectPoolingManager.Instance.FreezeBulletPrefab,GetComponent<Player>().Nickname.ToString());
    }

    // Freeze effect logic
    private void ApplyFreezeEffect(Collider enemyCollider)
    {
        Enemy enemy = enemyCollider.GetComponent<Enemy>();
        if (enemy != null)
        {
            FreezeEffect freezeEffect = new FreezeEffect(enemy, freezeDuration, slowMultiplier);
            enemy.AddStatusEffect(freezeEffect);
        }
        Player targetPlayer = enemyCollider.GetComponent<Player>();
        if (targetPlayer != null)
        {
            FreezeEffect freezeEffect = new FreezeEffect(targetPlayer, freezeDuration, slowMultiplier);
            enemy.AddStatusEffect(freezeEffect);
        }
    }
}
