using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Crate : NetworkBehaviour
{
    [Header("Crate Settings")]
    [SerializeField] private float rotationSpeed = 180f;  // Speed of rotation
    [SerializeField] private Vector3 rotationAxis = Vector3.up;  // Axis of rotation
    [SerializeField] private int maxHealth = 100;  // Max HP of the crate
    [SerializeField] private HealthBar healthBar;

    // Networked property for health
    [Networked] public int CurrentHealth { get; set; }

    [Header("Power-Up Settings")]
    public List<NetworkPrefabRef> powerUpPrefabs;  // List of power-up prefabs for Fusion

    // Local variable to track health changes
    private int previousHealth;

    // Called when the crate is spawned or reset

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            // Set the health to max on spawn
            CurrentHealth = maxHealth;
        }

        // Initialize previousHealth
        previousHealth = CurrentHealth;

        // Update the health bar on all clients
        UpdateHealthBar(CurrentHealth);
    }

    private void Update()
    {
        // Rotate the crate around the selected axis in 3D space
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
    }

    // Function to take damage, called by other players or events
    public void TakeDamage(int damageAmount)
    {
        if (Object.HasStateAuthority)
        {
            CurrentHealth -= damageAmount;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
            NetworkObject openBuffEffect = EffectPoolingManager.Instance.GetOpenBuffEffect();
            RPC_UpdateHealthBar(CurrentHealth);
            if (CurrentHealth <= 0)
            {
                RPC_CrackOpen(openBuffEffect.Id);
                if (powerUpPrefabs.Count == 0) return;
                int randomIndex = Random.Range(0, powerUpPrefabs.Count);
                NetworkObject powerUpObject;
                if (randomIndex == 0)
                {
                    powerUpObject = MatchObjectPoolingManager.Instance.GetDamageBuffBox();
                }
                else
                {
                    powerUpObject = MatchObjectPoolingManager.Instance.GetHealingBox();
                }
                if (powerUpObject != null)
                {
                    RPC_DropPowerUp(powerUpObject.Id, transform.position);
                }
            }
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateHealthBar(int CurrentHealth)
    {
        UpdateHealthBar(CurrentHealth);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_CrackOpen(NetworkId effectId)
    {
        GetComponent<Collider>().enabled = false;
        if (Runner.TryFindObject(effectId, out NetworkObject netObj))  //Used Runner.TryFindObject() to find the NetworkObject on all clients using the NetworkId.
        {
            netObj.transform.position = transform.position;
            netObj.gameObject.SetActive(true);
            StartCoroutine(nameof(disablecrate));
            return;
        }
      
    }
    IEnumerator disablecrate()
    {
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
    }
    // This function randomly selects and spawns a power-up
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DropPowerUp(NetworkId effectId,Vector3 SpawnPos)
    {
        Debug.Log("calling.............");
        if (Runner.TryFindObject(effectId, out NetworkObject netObj))  //Used Runner.TryFindObject() to find the NetworkObject on all clients using the NetworkId.
        {
            netObj.transform.position = SpawnPos;
            netObj.transform.rotation = Quaternion.Euler(90, 0, 0);
            netObj.gameObject.SetActive(true);
            return;
        }
    }

    // Updates the health bar UI
    private void UpdateHealthBar(int CurrentHealth)
    {
        if (healthBar != null)
        {
            healthBar.SetHealth(CurrentHealth);
        }
    }

    // Manually check for health changes in FixedUpdateNetwork
    public override void FixedUpdateNetwork()
    {
        // Check if health has changed
        //if (previousHealth != CurrentHealth)
        //{
        //    // Update the health bar
        //    UpdateHealthBar();

        //    // Update previousHealth to current
        //    previousHealth = CurrentHealth;
        //}
    }
}


//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class Crate : MonoBehaviour
//{
//    [Header("Crate Settings")]
///    [SerializeField] private float rotationSp//d = 180f;  // Speed of rotation
//    [SerializeFi//d] private Vector3 rotationAxis = //ctor3.up;  // Axis of rotation
//    [SerializeF//ld] private int maxHealth = 100;  // Max HP o//the crate
//    [SerializeField] private HealthBar healthBar;
//    private int currentHealth;

//    [Header("Power-Up Settings")]
//    public List<GameObject> powerUpPrefabs;  // List of power-up prefabs

//    void OnEnable()
//    {
//        currentHealth = maxHealth;  // Set the crate's health to max at the start
//        healthBar.SetMaxHealth(currentHealth);
//    }

//    void Update()
//    {
//        // Rotate the crate around the selected axis in 3D space
//        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
//    }

//    // Function that gets called when the crate takes damage
//    public void TakeDamage(int damageAmount)
//    {
//        currentHealth -= damageAmount;
//        healthBar.SetHealth(currentHealth);

//        // Check if the crate's health has reached zero
//        if (currentHealth <= 0)
//        {
//            CrackOpen();
//        }
//    }

//    // Called when the crate's health reaches zero
//    void CrackOpen()
//    {
//        // Optionally add cracking animations or sound effects here
//        DropPowerUp();
//        gameObject.SetActive(false);  // Destroy the crate after dropping the power-up
//    }

//    // This function randomly selects a power-up to drop
//    void DropPowerUp()
//    {
//        if (powerUpPrefabs.Count == 0) return;  // Ensure there are power-ups in the list

//        // Choose a random power-up from the list
//        int randomIndex = Random.Range(0, powerUpPrefabs.Count);

//        GameObject powerup;

//        if (randomIndex == 0)
//        {
//            powerup = MatchObjectPoolingManager.Instance.GetDamageBuffBox();
//        }
//        else
//        {
//            powerup = MatchObjectPoolingManager.Instance.GetHealingBox();
//        }

//        powerup.transform.position = transform.position;
//        powerup.transform.rotation = Quaternion.identity;

//        //GameObject powerUpToSpawn = powerUpPrefabs[randomIndex];

//        // Instantiate the chosen power-up at the crate's position
//        //Instantiate(powerUpToSpawn, transform.position, Quaternion.identity);
//    }
//}
