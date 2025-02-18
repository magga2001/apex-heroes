using Fusion;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour, IPlayerJoined
{
    [SerializeField] private GameObject playerPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        //if (HasStateAuthority)
        //{
        //    print("Has state authority");
        //}

        if (player == Runner.LocalPlayer)
        {
            //print($"Inside the local player condition");
            Runner.Spawn(playerPrefab, Vector3.one, Quaternion.identity, player);
        }
    }
}
