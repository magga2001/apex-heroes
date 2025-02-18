using Fusion;
using UnityEngine;

public class CrateSpawner : MonoBehaviour
{
    public Transform[] spawnPoints;

    public void Initialise()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Spawn points are not assigned.");
            return;
        }

        // Subscribe to the initialization event
        MatchObjectPoolingManager.OnInitialized += SpawnCrates;

        // If already initialized, spawn immediately
        if (MatchObjectPoolingManager.Instance != null && MatchObjectPoolingManager.Instance.IsInitialized)
        {
            SpawnCrates();
        }
    }

    //private void Start()
    //{
    //    if (spawnPoints == null || spawnPoints.Length == 0)
    //    {
    //        Debug.LogError("Spawn points are not assigned.");
    //        return;
    //    }

    //    // Subscribe to the initialization event
    //    MatchObjectPoolingManager.OnInitialized += SpawnCrates;

    //    // If already initialized, spawn immediately
    //    if (MatchObjectPoolingManager.Instance != null && MatchObjectPoolingManager.Instance.IsInitialized)
    //    {
    //        SpawnCrates();
    //    }
    //}

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        MatchObjectPoolingManager.OnInitialized -= SpawnCrates;
    }

    public void SpawnCrates()
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (MatchObjectPoolingManager.Instance != null)
            {
                MatchObjectPoolingManager.Instance.SpawnCrateAt(spawnPoint.position, Quaternion.Euler(90, 0, 0));
            }
            else
            {
                Debug.LogError("[CrateSpawner] MatchObjectPoolingManager is not initialized.");
            }
        }
    }

    //public void SpawnCrates()
    //{
    //    foreach (Transform spawnPoint in spawnPoints)
    //    {
    //        NetworkObject crate = MatchObjectPoolingManager.Instance.GetCrate();

    //        if (crate != null)
    //        {
    //            //crate.transform.position = spawnPoint.position;
    //            //Change this if have to, this is to override network rotation
    //            crate.transform.rotation = Quaternion.Euler(90, 0, 0);
    //            crate.gameObject.SetActive(true);
    //        }
    //        else
    //        {
    //            Debug.LogWarning("Failed to retrieve a crate from the pool.");
    //        }
    //    }
    //}
}




//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class CrateSpawner : MonoBehaviour
//{
//    // Assign the prefab in the Inspector
//    public GameObject crate;

//    // Array of Transform objects where prefabs will be spawned
//    public Transform[] spawnPoints;

//    // Method to spawn prefabs at each transform
//    public void SpawnCrates()
//    {
//        foreach (Transform spawnPoint in spawnPoints)
//        {
//            GameObject crate = MatchObjectPoolingManager.Instance.GetCrate();
//            crate.transform.position = spawnPoint.position;
//            crate.transform.rotation = spawnPoint.rotation;
//        }
//    }

//    // Example usage for testing: call the spawn on Start
//    private void Start()
//    {
//        if (crate != null && spawnPoints.Length > 0)
//        {
//            SpawnCrates();
//        }
//        else
//        {
//            Debug.LogError("Prefab or spawn points are not assigned.");
//        }
//    }
//}
