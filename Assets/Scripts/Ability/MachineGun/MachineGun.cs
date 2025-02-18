using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineGun : Ability
{
    public float fireRate = 0.1f;  // Time between shots (high fire rate for machine gun)
    public float abilityDuration = 5f;  // Duration of the machine gun effect

    public override void Activate(PlayerRef playerref,Player player)
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }
        player.StartCoroutine(AutoShoot(playerref,player));
    }

    private IEnumerator AutoShoot(PlayerRef playerref, Player player)
    {
        float endTime = Time.time + abilityDuration;

        // Automatically shoot while the ability is active
        while (Time.time < endTime)
        {
            player.ManualShooting(playerref);  // Call the player's shooting method
            yield return new WaitForSeconds(fireRate);  // Wait for the fire rate before shooting again
        }
    }
}
