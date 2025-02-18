using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Smooth teleport

public class Teleport : Ability
{
    [SerializeField] private float teleportDistance = 10f;
    [SerializeField] private float teleportSpeed = 20f;  // Speed of the smooth teleport

    public override void Activate(PlayerRef playerref, Player player)
    {
        // Calculate the target position
        Vector3 teleportPosition = player.transform.position + player.transform.forward * teleportDistance;

        // Start the smooth teleportation
        player.StartCoroutine(SmoothTeleport(player, teleportPosition));
    }

    private IEnumerator SmoothTeleport(Player player, Vector3 targetPosition)
    {
        // Smoothly move the player from the current position to the target position
        while (Vector3.Distance(player.transform.position, targetPosition) > 0.1f)
        {
            player.transform.position = Vector3.MoveTowards(player.transform.position, targetPosition, teleportSpeed * Time.deltaTime);
            yield return null;  // Wait until the next frame
        }

        // Ensure the player ends up exactly at the target position
        player.transform.position = targetPosition;
    }
}

// Instant teleport

//public class Teleport : Ability
//{
//[SerializeField] private float teleportDistance = 10f;

//public override void Activate(Player player)
//{
//Vector3 teleportPosition = player.transform.position + player.transform.forward * teleportDistance;
// player.transform.position = teleportPosition;
//}
//}
