using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : NetworkBehaviour
{
    [SerializeField] private string _name;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int damage;

    [SerializeField] bool canShoot = true;
    [SerializeField] private float fireDelay = 2f;
    [SerializeField] float initialDelay = 10f;
    private float delay;
    
    private int currentHealth;

    private Transform player;
    private Vector3 movement;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float chasingDistance = 10f; // Max distance to start chasing
    [SerializeField] private float minChasingDistance = 2f; // Min distance to stop getting closer
    [SerializeField] private float randomMoveTime = 2f; // Time to walk in random direction

    private bool coroutineAllowed = true;
    private bool isChasingPlayer = false;

    // Additions for status effects
    private List<StatusEffect> activeEffects = new List<StatusEffect>();
    public float damageMultiplier = 1f;

    public float MoveSpeed
    {
        get { return moveSpeed; }
        set { moveSpeed = value; }
    }

    public override void Spawned()
    {
        // Initialization for networked spawning if needed
        // Ensure health, timers, etc. are set here if required.
        ResetEnemyState();
        StartCoroutine(RandomWalk());
    }

    private void OnEnable()
    {
        ResetEnemyState();
    }

    private void ResetEnemyState()
    {
        delay = initialDelay;
        canShoot = true;
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
        coroutineAllowed = true;
        InitPlayerReference();
    }

    private void InitPlayerReference()
    {
        try
        {
            player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
            Debug.Log("Player detected by Enemy");
        }
        catch
        {
            Debug.Log("Player not found or died");
            player = null;
        }
    }

    private void Update()
    {
        if (!Object.HasStateAuthority) return;
        // Only the host runs AI logic.

        delay -= Time.deltaTime;

        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= chasingDistance)
            {
                isChasingPlayer = true;
                Vector3 direction = player.position - transform.position;
                Quaternion rotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * moveSpeed);

                direction.Normalize();
                movement = direction;
            }
            else
            {
                isChasingPlayer = false;
            }
        }
        else
        {
            // Player not found, try again later
            if (coroutineAllowed)
            {
                coroutineAllowed = false;
                StartCoroutine(FindPlayer());
            }
        }
    }

    private void FixedUpdate()
    {
        if (!Object.HasStateAuthority) return;
        // Only host moves and attacks

        if (isChasingPlayer)
        {
            MoveEnemy(movement);
            Attack();
        }
    }

    private void MoveEnemy(Vector3 direction)
    {
        if (player == null) return;
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= chasingDistance && distance >= minChasingDistance)
        {
            rb.MovePosition(transform.position + (direction * moveSpeed * Time.deltaTime));
        }
    }

    private void Attack()
    {
        if (!Object.HasStateAuthority) return;
        // Host only

        if (canShoot && player != null && delay <= 0)
        {
            canShoot = false;
            Fire();
            StartCoroutine(AttackPause());
        }
    }

    public void Fire()
    {
        if (!Object.HasStateAuthority) return;
        // Host only

        // Get a bullet from the ObjectPoolingManager (host side)
        NetworkObject bullet = ObjectPoolingManager.Instance.GetBullet(_name, false, damage);
        bullet.transform.position = firePoint.transform.position;
        bullet.transform.rotation = firePoint.transform.rotation;
    }

    IEnumerator FindPlayer()
    {
        yield return new WaitForSeconds(3);

        InitPlayerReference();

        coroutineAllowed = true;
    }

    IEnumerator AttackPause()
    {
        yield return new WaitForSeconds(fireDelay);
        canShoot = true;
    }

    public void TakeDamage(PlayerRef playerref, int damage, string shotBy)
    {
        if (!Object.HasStateAuthority) return;
        // Only host adjusts health

        int finalDamage = Mathf.RoundToInt(damage * damageMultiplier);
        currentHealth -= finalDamage;
        healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0)
        {
            Die(playerref,shotBy);
        }
    }

    private void Die(PlayerRef playerref, string killBy)
    {
        if (!Object.HasStateAuthority) return;
        // Host handles death

        Debug.Log("Enemy died.");
        AddKillfeedEntry(playerref, killBy, _name);
        gameObject.SetActive(false); // Return to pool or just deactivate
    }

    // Status Effects management - host only if state changes are critical
    public void AddStatusEffect(StatusEffect effect)
    {
        if (!Object.HasStateAuthority) return;

        activeEffects.Add(effect);
        effect.ApplyEffect();
    }

    public void RemoveStatusEffect(StatusEffect effect)
    {
        if (!Object.HasStateAuthority) return;

        activeEffects.Remove(effect);
        effect.RemoveEffect();
    }

    private IEnumerator RandomWalk()
    {
        while (true)
        {
            if (!Object.HasStateAuthority)
            {
                // Only host decides movement. Clients get updates automatically.
                yield return null;
                continue;
            }

            if (!isChasingPlayer)
            {
                Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
                float startTime = Time.time;

                while (Time.time - startTime < randomMoveTime)
                {
                    rb.MovePosition(transform.position + (randomDirection * moveSpeed * Time.deltaTime));
                    yield return null;
                }
            }
            yield return null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!Object.HasStateAuthority) return;

        if (!isChasingPlayer && collision.gameObject.CompareTag("Obstacle"))
        {
            // Change direction upon hitting an obstacle
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            movement = randomDirection;
        }
    }

    public void AddKillfeedEntry(PlayerRef playeref, string killer, string victim)
    {
        if (Object.HasStateAuthority)
        {
            // Broadcast the killfeed entry to all clients
            AddKillfeedEntryRpc(playeref, killer, victim);
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void AddKillfeedEntryRpc(PlayerRef playeref, string killer, string victim)
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

            if (Runner.LocalPlayer == playeref)
            {
                killSymbol.color = Color.red;
            }
            // Set the text
            killerText.text = killer;
            victimText.text = victim;

            Debug.Log(killer + " killed " + victim);

            // Optionally, change the kill symbol image if you have multiple symbols
            // killSymbol.sprite = someCustomSprite;
            StartCoroutine(RemoveAfterDelay(newEntry, KillfeedManager.Instance.killfeedDuration));
        }

        // Optionally, remove the entry after some time
        //StartCoroutine(RemoveAfterDelay(newEntry, killfeedDuration));
    }

    // Coroutine to remove the entry after a delay
    IEnumerator RemoveAfterDelay(GameObject entry, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(entry);
    }
}


//using Fusion;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//// Simple bot to demonstrate the gameplay

//public class Enemy : MonoBehaviour
//{
//    [SerializeField] private string _name;
//    [SerializeField] private int maxHealth = 100;
//    [SerializeField] private HealthBar healthBar;
//    [SerializeField] private Transform firePoint;
//    [SerializeField] private int damage;

//    [SerializeField] bool canShoot = true;
//    [SerializeField] private float fireDelay = 2f;
//    [SerializeField] float initialDelay = 10f;
//    private float delay;

//    private int currentHealth;

//    private Transform player;
//    private Vector3 movement;
//    [SerializeField] private float moveSpeed = 5f;
//    [SerializeField] private Rigidbody rb;
//    [SerializeField] private float chasingDistance = 10f; // Maximum distance at which the enemy will start chasing
//    [SerializeField] private float minChasingDistance = 2f; // Minimum distance at which the enemy will stop chasing
//    [SerializeField] private float randomMoveTime = 2f; // Time to walk in one random direction

//    private bool coroutineAllowed = true;
//    private bool isChasingPlayer = false;

//    public float MoveSpeed
//    {
//        get { return moveSpeed; } // Getter returns the value of moveSpeed
//        set { moveSpeed = value; } // Setter assigns the value to moveSpeed
//    }

//    // Additions for status effects
//    private List<StatusEffect> activeEffects = new List<StatusEffect>();
//    public float damageMultiplier = 1f;

//    void OnEnable()
//    {
//        delay = initialDelay;
//        canShoot = true;
//        currentHealth = maxHealth;
//        healthBar.SetMaxHealth(maxHealth);
//        coroutineAllowed = true;
//        Init();
//    }

//    private void Start()
//    {
//        delay = initialDelay;
//        canShoot = true;
//        currentHealth = maxHealth;
//        healthBar.SetMaxHealth(maxHealth);
//        coroutineAllowed = true;
//        Init();
//        StartCoroutine(RandomWalk());
//    }

//    private void Init()
//    {
//        try
//        {
//            player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
//            Debug.Log("Player detected");
//        }
//        catch
//        {
//            Debug.Log("Player not found or died");
//        }
//    }

//    private void Update()
//    {
//        delay -= Time.deltaTime;

//        if (player != null)
//        {
//            float distance = Vector3.Distance(transform.position, player.position);

//            if (distance <= chasingDistance)
//            {
//                isChasingPlayer = true;
//                Vector3 direction = player.position - transform.position;

//                // Calculate rotation towards the player
//                Quaternion rotation = Quaternion.LookRotation(direction);
//                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * moveSpeed);

//                direction.Normalize();
//                movement = direction;
//            }
//            else
//            {
//                isChasingPlayer = false;
//            }
//        }
//        else
//        {
//            if (coroutineAllowed)
//            {
//                coroutineAllowed = false;
//                StartCoroutine(FindPlayer());
//            }
//        }
//    }

//    private void FixedUpdate()
//    {
//        if (isChasingPlayer)
//        {
//            MoveEnemy(movement);
//            Attack();
//        }
//    }

//    private void MoveEnemy(Vector3 direction)
//    {
//        float distance = Vector3.Distance(transform.position, player.position);
//        if (distance <= chasingDistance && distance >= minChasingDistance)
//        {
//            rb.MovePosition(transform.position + (direction * moveSpeed * Time.deltaTime));
//        }
//    }

//    private void Attack()
//    {
//        if (canShoot && player != null && delay <= 0)
//        {
//            canShoot = false;
//            Fire();
//            StartCoroutine(AttackPause());
//        }
//    }

//    public void Fire()
//    {
//        // Get a bullet from object pooling manager and shoot toward the direction the weapon is facing
//        NetworkObject bullet = ObjectPoolingManager.Instance.GetBullet(_name, false, damage);
//        bullet.transform.position = firePoint.transform.position;
//        bullet.transform.rotation = firePoint.transform.rotation;
//    }

//    IEnumerator FindPlayer()
//    {
//        yield return new WaitForSeconds(3);

//        try
//        {
//            player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
//        }
//        catch
//        {
//            Debug.Log("Player not found or died");
//        }

//        coroutineAllowed = true;
//    }

//    IEnumerator AttackPause()
//    {
//        yield return new WaitForSeconds(fireDelay);
//        canShoot = true;
//    }

//    public void TakeDamage(int damage, string shotBy)
//    {
//        int finalDamage = Mathf.RoundToInt(damage * damageMultiplier); // Use damage multiplier
//        currentHealth -= finalDamage;
//        healthBar.SetHealth(currentHealth);

//        if (currentHealth <= 0)
//        {
//            Die(shotBy);
//        }
//    }

//    private void Die(string killBy)
//    {
//        Debug.Log("Die");
//        KillfeedManager.Instance.AddKillfeedEntry(killBy, _name);
//        gameObject.SetActive(false);
//    }

//    // Method to add a status effect to the enemy
//    public void AddStatusEffect(StatusEffect effect)
//    {
//        activeEffects.Add(effect);
//        effect.ApplyEffect();
//    }

//    public void RemoveStatusEffect(StatusEffect effect)
//    {
//        activeEffects.Remove(effect);
//        effect.RemoveEffect();
//    }

//    private IEnumerator RandomWalk()
//    {
//        while (true)
//        {
//            if (!isChasingPlayer)
//            {
//                // Generate a random direction to move in
//                Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
//                float startTime = Time.time;

//                while (Time.time - startTime < randomMoveTime)
//                {
//                    rb.MovePosition(transform.position + (randomDirection * moveSpeed * Time.deltaTime));
//                    yield return null;
//                }
//            }
//            yield return null;
//        }
//    }

//    private void OnCollisionEnter(Collision collision)
//    {
//        if (!isChasingPlayer && collision.gameObject.CompareTag("Obstacle"))
//        {
//            // Change direction upon hitting an obstacle
//            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
//            movement = randomDirection;
//        }
//    }
//}
