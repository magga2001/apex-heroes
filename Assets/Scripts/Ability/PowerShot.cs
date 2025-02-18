using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerShot : Ability
{
    [SerializeField] private int shotsToFire = 3;
    [SerializeField] private float fireRate = 0.2f;

    public override void Activate(PlayerRef playerref, Player player)
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }
        StartCoroutine(FirePowerShot(playerref,player));
    }

    private IEnumerator FirePowerShot(PlayerRef playerref, Player player)
    {
        for (int i = 0; i < shotsToFire; i++)
        {
            player.ManualShooting(playerref);
            yield return new WaitForSeconds(fireRate);
        }
    }
}
