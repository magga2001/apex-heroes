using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : BaseGun
{
    // Changed to a list to handle multiple fire points
    [SerializeField] private List<Transform> firePoints;

    [SerializeField] private Transform muzzleEffectPos;

    [SerializeField] private int damage;

    [SerializeField] private float boostDuration = 5f;  // How long the boost lasts
    [SerializeField] private int damageMultiplier = 2;  // Amount to multiply the damage

    public override void Fire(PlayerRef playeref, string shotBy, Player player = null)
    {
        if (!Object.HasStateAuthority)
            return;

        Debug.Log("Calling Bullet");

        // Loop through each fire point and shoot a bullet from each one
        foreach (Transform firePoint in firePoints)
        {
            Debug.Log("Getting Bullet");
            // Get a bullet from object pooling manager and shoot toward the direction the weapon is facing
            NetworkObject bullet = ObjectPoolingManager.Instance.GetBullet(shotBy, true, damage, player);
           // bullet.GetComponent<Bullet>().RPC_StopParticle();
            bullet.GetComponent<Bullet>()._playerref = playeref;
            if (bullet != null)
            {
                // Use Teleport to set the bullet's position and rotation
                var networkTransform = bullet.GetComponent<NetworkTransform>();

                if (networkTransform != null)
                {

                    networkTransform.Teleport(firePoint.position, firePoint.rotation);
                    Debug.Log($"Bullet teleported to position: {firePoint.position}, rotation: {firePoint.rotation}");
                }
                else
                {
                    Debug.LogWarning("Bullet is missing a NetworkTransform component!");
                    bullet.transform.position = firePoint.position;
                    bullet.transform.rotation = firePoint.rotation;
                }
            }
            else
            {
                Debug.LogError("Failed to spawn bullet from pool!");
            }

           // bullet.GetComponent<Bullet>().RPC_PlayParticle();
            //bullet.transform.position = firePoint.transform.position;
            //bullet.transform.rotation = firePoint.transform.rotation;

            // Force the NetworkTransform to accept this as the new baseline
            //var netTransform = bullet.GetComponent<NetworkTransform>();
            //if (netTransform != null)
            //{
            //    netTransform.Teleport(firePoint.position, firePoint.rotation);
            //}

            //// Now activate the bullet
            //bullet.gameObject.SetActive(true);
        }

        //NetworkObject muzzleEffect = EffectPoolingManager.Instance.GetBaseMuzzleEffect();
        //muzzleEffect.transform.position = muzzleEffectPos.transform.position;
        AudioManager.Instance.PlaySound("Normal Shooting");
    }
    public void IncreaseDamage(int damageMultiplier)
    {
        if (!Object.HasStateAuthority)
        {
            return; // Only the server processes healing
        }
       

       
        NetworkObject damageIncreaseEffect = EffectPoolingManager.Instance.GetDamageIncreaseEffect();
        // Player position
        RPC_IncreaseDamage(damageIncreaseEffect.Id, transform.position, damageMultiplier);
        StartCoroutine(DecreaseDamageCoroutine());
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_IncreaseDamage(NetworkId effectId,Vector3 positionobj, int damageMultiplier)
    {
        if(Object.HasInputAuthority)
        {
            damage *= damageMultiplier;
        }
        if (Runner.TryFindObject(effectId, out NetworkObject netObj))  //Used Runner.TryFindObject() to find the NetworkObject on all clients using the NetworkId.
        {
            netObj.transform.position = positionobj;
            netObj.gameObject.SetActive(true);
            return;
        }
    }
    IEnumerator DecreaseDamageCoroutine()
    {
        yield return new WaitForSeconds(boostDuration);
        DecreaseDamage(damageMultiplier);
    }
    public void DecreaseDamage(int damageMultipler)
    {
        if (!Object.HasStateAuthority)
        {
            return; // Only the server processes healing
        }
        RPC_Decrease(damageMultipler);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Decrease(int damageMultiplier)
    {
        if (Object.HasInputAuthority)
        {
            damage /= damageMultiplier;
        }
    }
}
