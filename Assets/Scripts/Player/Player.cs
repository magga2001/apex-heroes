using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    [Networked] public NetworkString<_16> Nickname { get; set; }

    //[SerializeField] private string player_name;

    [SerializeField] private int maxHealth;
    [SerializeField] private HealthBar healthBar; // World-space Health Bar
    [SerializeField] private XPBar xpBar;         // Local XP Bar
    [SerializeField] private XPBar abilityXpBar;  // Local Ability XP Bar
    [SerializeField] private int maxXP;
    [SerializeField] private int maxAbilityXP;
    [SerializeField] private int numOfSubBar;
    [SerializeField] private int numOfAbilitySubBar;

    private int currentHealth; // Synced health
    private float currentXP;       // Local XP
    private float currentAbilityXP; // Local Ability XP

    [SerializeField] private Ability currentAbility;
    [SerializeField] private PowerShot powershot;
    [SerializeField] private BaseGun gun;

    private int subBarXP;
    private int subBarAbilityXP;

    [SerializeField] private float xpPerTick = 0.5f;  // XP added per tick
    [SerializeField] private float xpIncreaseInterval = 1f;  // Time interval (seconds) between XP increases

    [SerializeField] private Vector3 effectOffset;
    //public string PlayerName => player_name;
    public GameObject PlayerMesh;
    public GameObject Playercanvas;
    public GameObject[] Playerbars;


    public int CurrentHealth
    {
        get
        {
            return currentHealth;
        }

        set
        {
            Debug.LogWarning($"Setting currentHealth to: {value}");
            currentHealth = value;
        }
    }
    public int MaxHealth => maxHealth;
    public float CurrentXP => currentXP;
    public int MaxXP => maxXP;
    public float CurrentAbilityXp => currentAbilityXP;
    public int MaxAbilityXP => maxAbilityXP;

    public override void Spawned()
    {
        // This runs after the player object is fully recognized by Fusion.
        // Check if this player is the local player (input authority)

        //EffectPoolingEvents.OnObjectInitialized?.Invoke(Object);

        if (Object.HasInputAuthority)
        {
            Debug.Log("Local player spawned. Assigning camera.");

            CameraController cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null)
            {
                cameraController.SetTargetPlayer(transform);
            }
            else
            {
                Debug.LogError("No CameraController found in the scene. Ensure it exists.");
            }
        }

        if (Runner.IsServer)
        {
            RPC_InitializeValues();
        }
    }
    //[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void SetNickname(string newName)
    {
        Nickname = newName; // Only State Authority can change it
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_InitializeValues()
    {
        MatchManager.Instance.RegisterPlayer(this);
        CurrentHealth = maxHealth; // Initialize health on the server

        Debug.LogWarning($"Current Health: {CurrentHealth}");

        currentXP = maxXP;
        subBarXP = maxXP / numOfSubBar;
        currentAbilityXP = maxAbilityXP;
        subBarAbilityXP = maxAbilityXP / numOfAbilitySubBar;

        healthBar.SetMaxHealth(maxHealth);
        xpBar.SetMaxXP(maxXP, numOfSubBar);
        abilityXpBar.SetMaxXP(maxAbilityXP, numOfAbilitySubBar);

        StartCoroutine(IncreaseXPOverTime());
    }

    void Start()
    {
     
    }

    void Update()
    {
        if (PlayerMesh.activeInHierarchy)
        {
            if (Input.GetMouseButtonDown(0))
            {
                AutoShooting();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                UseCurrentAbility();
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                UsePowerShot();
            }
        }

    }
    public void AutoShooting()
    {
        if (!Object.HasInputAuthority)
            return;

        Debug.Log("Name is " + Nickname.ToString());
        RPC_AutoShooting(Runner.LocalPlayer, Nickname.ToString());
        Debug.Log("get the reference" + Runner.LocalPlayer);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_AutoShooting(PlayerRef playerref, string playerName)
    {
        gun.Shooting(playerref, playerName, this);
        RPC_DecreaseXP();
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DecreaseXP()
    {
        DecreaseXP(subBarXP);
    }
    public void ManualShooting(PlayerRef playerref)
    {
        Manual_Shooting(playerref,Nickname.ToString());
    }
    private void Manual_Shooting(PlayerRef playerref, string player_name)
    {
        gun.Fire(playerref, player_name);
    }

    public void TakeDamage(PlayerRef _playerref, int damage, string shotBy)
    {
        Debug.LogWarning($"{shotBy} shot {name} with {damage} damage");
        CurrentHealth -= damage;

        if (CurrentHealth <= 0)
        {
            Die(_playerref, shotBy);
        }
        else
        {
            RPC_UpdateHealthBar(CurrentHealth);
            //RPC_IncreaseAbilityXP(subBarAbilityXP/2);
        }
    }

    public void IncreaseHealth(int health)
    {
        if (!Object.HasStateAuthority)
        {
            return; // Only the server processes healing
        }

        CurrentHealth += health;
        if (CurrentHealth > maxHealth)
        {
            CurrentHealth = maxHealth;
        }

        RPC_UpdateHealthBar(CurrentHealth);
        NetworkObject healingEffect = EffectPoolingManager.Instance.GetHealingEffect();
        RPC_HealingEffect(healingEffect.Id,transform.position,transform.rotation);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_HealingEffect(NetworkId effectId, Vector3 position, Quaternion rotation)
    {
        if (Runner.TryFindObject(effectId, out NetworkObject netObj))  //Used Runner.TryFindObject() to find the NetworkObject on all clients using the NetworkId.
        {
            netObj.transform.position = position;
            netObj.transform.rotation = rotation;
            netObj.gameObject.SetActive(true);
            return;
        }
    }
    public void Die(PlayerRef _playerref, string killBy)
    {
        if (!Object.HasStateAuthority)
        {return;}                      // Only the server processes death
        CurrentHealth = 0;
        NetworkObject deadEffect = EffectPoolingManager.Instance.GetDeadEffect();
        RPC_CalltheDeadEffect(deadEffect.Id);
        AddKillfeedEntry(_playerref, killBy, Nickname.ToString());
        RPC_UpdateHealthBar(CurrentHealth);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_CalltheDeadEffect(NetworkId effectId)
    {
        if (Runner.TryFindObject(effectId, out NetworkObject netObj))  //Used Runner.TryFindObject() to find the NetworkObject on all clients using the NetworkId.
        {
            netObj.transform.position = transform.position + effectOffset;
            netObj.gameObject.SetActive(true);
            return;
        }
    }
    public void IncreaseXP(float xp)
    {
        if (Playercanvas.activeInHierarchy)
        {
            currentXP += xp;
            if (currentXP > maxXP)
            {
                currentXP = maxXP;
            }

            xpBar.SetXP(currentXP); // Local update
        }
    }
    public void DecreaseXP(float xp)
    {
        if (Playercanvas.activeInHierarchy)
        {
            currentXP -= xp;
            if (currentXP < 0)
            {
                currentXP = 0;
            }

            xpBar.SetXP(currentXP); // Local update
        }
    }
    public void CallIncreaseAbilityXP()
    {
        RPC_IncreaseAbilityXP(subBarAbilityXP / 2);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_IncreaseAbilityXP(float xp)
    {
        if (Playercanvas.activeInHierarchy)
        {
            currentAbilityXP += xp;
            if (currentAbilityXP > maxAbilityXP)
            {
                currentAbilityXP = maxAbilityXP;
            }
            abilityXpBar.SetXP(currentAbilityXP); // Local update
        }
    }
    public void DecreaseAbilityXP(float xp)
    {
        if (Playercanvas.activeInHierarchy)
        {
            currentAbilityXP -= xp;
            if (currentAbilityXP < 0)
            {
                currentAbilityXP = 0;
            }

            abilityXpBar.SetXP(currentAbilityXP); // Local update
        }
    }

    public bool CanShoot()
    {
        return currentXP >= subBarXP;
    }

    public bool CanUsePowerShot()
    {
        return currentAbilityXP >= subBarAbilityXP;
    }

    public bool CanUseAbility()
    {
        return currentAbilityXP >= maxAbilityXP;
    }

    public void UsePowerShot()
    {
        if (!Object.HasInputAuthority)
            return;
        RPC_UsePowerShot(Runner.LocalPlayer);
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UsePowerShot(PlayerRef playeref)
    {
        if (CanUsePowerShot())
        {
            powershot.Activate(playeref, this);
            RPC_AfterPowerShotDecreaseAbility();
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AfterPowerShotDecreaseAbility()
    {
        DecreaseAbilityXP(subBarAbilityXP);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_UseAbility()
    {
        if (CanUseAbility())
        {
            currentAbilityXP = 0;
            abilityXpBar.SetXP(0);
        }
    }
    public void UseCurrentAbility()
    {
        // Only allow the player with InputAuthority
        if (!Object.HasInputAuthority)
            return;
        Debug.Log("Reference ++" + Runner.LocalPlayer);
        RPC_UseCurrentAbility(Runner.LocalPlayer);


    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_UseCurrentAbility(PlayerRef playeref)
    {
        if (CanUseAbility())
        {
            currentAbility.Activate(playeref, this);
            RPC_UseAbility();
        }
    }
  
    private IEnumerator IncreaseXPOverTime()
    {
        while (true)
        {
            float elapsedTime = 0f;
            while (elapsedTime < xpIncreaseInterval)
            {
                float xpIncreaseThisFrame = xpPerTick * (Time.deltaTime / xpIncreaseInterval);
                //IncreaseXP(xpIncreaseThisFrame);
                IncreaseXP(xpIncreaseThisFrame);
                elapsedTime += Time.deltaTime;

                yield return null;
            }
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_UpdatePlayerRankingOnLeft()
    {
        gameObject.SetActive(false);
        MatchManager.Instance.UpdateDeadPlayerCount(this);// Update Player Ranking
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_UpdateHealthBar(int health)
    {
        healthBar.SetHealth(health);

        if (health <= 0)
        {
            StopCoroutine(xpBar.SmoothUpdateXPBars(CurrentAbilityXp)); // stop Coroutine updatesmooth bars
            PlayerMesh.SetActive(false); // disable player mesh
            gameObject.GetComponent<CapsuleCollider>().enabled = false;
            gameObject.GetComponent<Rigidbody>().isKinematic = true;
            StartCoroutine(waitsomesecond());
            MatchManager.Instance.UpdateDeadPlayerCount(this);  // Update the dead player count
            if (Object.HasInputAuthority)
            {
                MatchManager.Instance.GameOver();
            }
        }
    }
    IEnumerator waitsomesecond()
    {
        yield return new WaitForSeconds(1.5f);
        foreach (var item in Playerbars)
        {
            item.SetActive(false);
        }
        Playercanvas.SetActive(false);
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_DisconnectClient()
    {
       if(Object.HasStateAuthority) // if this is server
        {
            RPC_RemoveFromPlayerList(); // Remove from player list
            StartCoroutine(nameof(DespawnAfterDelay)); // Despawn after delay
        }
    }
    private IEnumerator DespawnAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        if (Runner != null)
        {
            Runner.Despawn(GetComponent<NetworkObject>());
        }
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_RemoveFromPlayerList()
    {
       
            MatchManager.Instance.RemovefromPlayerlist(this);
      
    }

    public void AddKillfeedEntry(PlayerRef playeref, string killer, string victim)
    {
        if (Runner.IsServer)
        {
            // Broadcast the killfeed entry to all clients.
            RPC_AddKillfeedEntryRpc(playeref,killer, victim);
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_AddKillfeedEntryRpc(PlayerRef playeref, string killer, string victim)
    {
        // Instantiate the prefab in the container
        GameObject newEntry = Instantiate(KillfeedManager.Instance.killfeedEntryPrefab, KillfeedManager.Instance.killfeedContainer); 

        // Ensure correct scale and position
        newEntry.transform.localScale = Vector3.one;  // Set scale to (1, 1, 1)
        newEntry.transform.localPosition = Vector3.zero;  // Reset local position

        // Find the specific UI elements in the prefab
        Transform panel = newEntry.transform.Find("Panel");

        if (panel != null)
        {
            // Update TextMeshProUGUI components
            TextMeshProUGUI killerText = panel.Find("Killer").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI victimText = panel.Find("Victim").GetComponent<TextMeshProUGUI>();
            Image killSymbol = panel.Find("Kill Symbol").GetComponent<Image>();  // Assuming the kill symbol is an image
            if (Runner.LocalPlayer== playeref|| Object.HasInputAuthority)
            {
                panel.GetComponent<Image>().color = Color.red;
            }
            // Set the text
            killerText.text = killer;
            victimText.text = victim;

            Debug.Log(killer + " killed " + victim);

            // Optionally, remove the entry after some time
            StartCoroutine(RemoveAfterDelay(newEntry, KillfeedManager.Instance.killfeedDuration));
        }

        
        //StartCoroutine(RemoveAfterDelay(newEntry, killfeedDuration));
    }

    // Coroutine to remove the entry after a delay
    IEnumerator RemoveAfterDelay(GameObject entry, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(entry);
    }
} 


//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class Player : MonoBehaviour
//{

//    [SerializeField] private string player_name;

//    [SerializeField] private int maxHealth;
//    [SerializeField] private HealthBar healthBar;
//    [SerializeField] private XPBar xpBar;
//    [SerializeField] private XPBar abilityXpBar;
//    [SerializeField] private int maxXP;
//    [SerializeField] private int maxAbilityXP;
//    [SerializeField] private int numOfSubBar;
//    [SerializeField] private int numOfAbilitySubBar;
//    [HideInInspector][SerializeField] private int currentHealth;
//    [HideInInspector][SerializeField] private float currentXP;
//    [HideInInspector][SerializeField] private float currentAbilityXP;

//    [SerializeField] private Ability currentAbility;
//    [SerializeField] private PowerShot powershot;
//    [SerializeField] private BaseGun gun;

//    private int subBarXP;
//    private int subBarAbilityXP;

//    // XP increase settings
//    [SerializeField] private float xpPerTick = 0.5f;  // How much XP to add per tick
//    [SerializeField] private float xpIncreaseInterval = 1f;  // Time interval (seconds) between XP increases

//    public int CurrentHealth { get { return currentHealth; } }
//    public int MaxHealth { get { return maxHealth; } }
//    public float CurrentXP { get { return currentXP; } }
//    public int MaxXP { get { return maxXP; } }
//    public float CurrentAbilityXp { get { return currentAbilityXP; } }
//    public int MaxAbilityXP { get { return maxAbilityXP; } }

//    // Start is called before the first frame update
//    void Start()
//    {
//        currentHealth = maxHealth;
//        currentXP = maxXP;
//        subBarXP = maxXP / numOfSubBar;
//        currentAbilityXP = maxAbilityXP;
//        subBarAbilityXP = maxAbilityXP / numOfAbilitySubBar;
//        healthBar.SetMaxHealth(maxHealth);
//        xpBar.SetMaxXP(MaxXP, numOfSubBar);
//        abilityXpBar.SetMaxXP(MaxAbilityXP, numOfAbilitySubBar);

//        // Start the Coroutine to increase XP over time
//        StartCoroutine(IncreaseXPOverTime());
//    }

//    void Update()
//    {

//        if (Input.GetMouseButtonDown(0))
//        {
//            AutoShooting();
//       }

//        if (Input.GetKeyDown(KeyCode.Q))
//        {
//            UseCurrentAbility();
//        }

//        if (Input.GetKeyDown(KeyCode.P))
//        {
//            UsePowerShot();
//        }

//        //if (Input.GetMouseButtonDown(0))
//        //{
//        //TakeDamage(10);
//        //}
//    }

//    public void AutoShooting()
//    {
//        //if (CanShoot())
//        //{
//            //gun.Shooting(player_name, this);
//            //DecreaseXP(subBarXP);
//        //}

//        gun.Shooting(player_name, this);
//        DecreaseXP(subBarXP);
//    }

//    public void ManualShooting()
//    {
//        gun.Fire(player_name);
//    }

//    public void TakeDamage(int damage, string shotBy)
//    {
//        currentHealth -= damage;
//        healthBar.SetHealth(currentHealth);
//        IncreaseAbilityXP(subBarAbilityXP / 2);
//        if (currentHealth <= 0)
//        {
//            Die(shotBy);
//        }
//    }

//    public void IncreaseHealth(int health)
//    {
//        currentHealth += health;
//        healthBar.SetHealth(currentHealth);

//        if (currentHealth > maxHealth)
//        {
//            currentHealth = maxHealth;
//            healthBar.SetMaxHealth(maxHealth);
//        }
//    }

//    public void IncreaseXP(float xp)
//    {
//        currentXP += xp;

//        if(currentXP > maxXP)
//        {
//            currentXP = maxXP;
//        }

//        xpBar.SetXP(currentXP);
//    }

//    public void DecreaseXP(float xp)
//    {
//        currentXP -= xp;
//        if (currentXP < 0)
//        {
//            currentXP = 0;
//        }

//        xpBar.SetXP(currentXP);
//    }

//    public void IncreaseAbilityXP(float xp)
//    {
//        currentAbilityXP += xp;

//        if (currentAbilityXP > maxAbilityXP)
//        {
//            currentAbilityXP = maxAbilityXP;
//        }

//        abilityXpBar.SetXP(currentAbilityXP);
//    }

//    public void DecreaseAbilityXP(float xp)
//    {
//        currentAbilityXP -= xp;
//        if (currentAbilityXP < 0)
//        {
//            currentAbilityXP = 0;
//        }

//        abilityXpBar.SetXP(currentAbilityXP);
//    }

//    public bool CanShoot()
//    {
//        return currentXP >= subBarXP;
//    }


//    public bool CanUsePowerShot()
//    {
//        return currentAbilityXP >= subBarAbilityXP;
//    }

//    public bool CanUseAbility()
//    {
//        return currentAbilityXP >= maxAbilityXP;
//    }

//    public void UsePowerShot()
//    {
//        if (CanUsePowerShot())
//        {
//            powershot.Activate(this);
//            DecreaseAbilityXP(subBarAbilityXP);
//        }
//    }

//    public void UseAbility()
//    {
//        if (CanUseAbility())
//        {
//            currentAbilityXP = 0;
//            abilityXpBar.SetXP(0);
//        }
//    }

//    public void UseCurrentAbility()
//    {
//        if (CanUseAbility())
//        {
//            currentAbility.Activate(this);
//            UseAbility();
//        }
//    }

//    public void Die(string killBy)
//    {
//        currentHealth = 0;
//        healthBar.SetHealth(currentHealth);
//        KillfeedManager.Instance.AddKillfeedEntry(killBy, name);
//        // GameManager.Instance.GameIsOver = true;
//        gameObject.SetActive(false);
//        Debug.Log("Gameover");
//    }

//    private IEnumerator IncreaseXPOverTime()
//    {
//        while (true)
//        {
//            // Smoothly increase XP over the duration of xpIncreaseInterval
//            float elapsedTime = 0f;
//            while (elapsedTime < xpIncreaseInterval)
//            {
//                float xpIncreaseThisFrame = xpPerTick * (Time.deltaTime / xpIncreaseInterval);
//                IncreaseXP(xpIncreaseThisFrame);  // Add a fraction of the XP each frame
//                elapsedTime += Time.deltaTime;

//                yield return null;  // Wait for the next frame
//            }
//        }
//    }

//}
