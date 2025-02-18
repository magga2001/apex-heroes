using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageBuffBox : BuffBox
{
    [SerializeField] private int damageMultiplier = 2;  // Amount to multiply the damage
    //[SerializeField] private float boostDuration = 5f;  // How long the boost lasts

    // Comment for fix lmao

    private void OnTriggerEnter(Collider other)
    {
       
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                StartCoroutine(ApplyDamageBoost(player));  // Start the damage boost coroutine
                gameObject.SetActive(false);
            }
    }

    private IEnumerator ApplyDamageBoost(Player player)
    {
        Gun gun = player.GetComponentInChildren<Gun>();  // Get the player's gun component
        if (gun != null)
        {
            gun.IncreaseDamage(damageMultiplier);  // Increase the gun's damage
            yield return new WaitForSeconds(0f);  // Wait for the boost duration
            //gun.DecreaseDamage(damageMultiplier);  // Revert the damage boost
        }
    }
}