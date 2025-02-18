using UnityEngine;

public class Gas : MonoBehaviour
{
    [SerializeField] private float initialSize = 10f;       // Initial size of the gas ring (outer size)
    [SerializeField] private float shrinkDuration = 10f;    // Time for the gas ring to shrink fully
    [SerializeField] private float damagePerSecond = 5f;    // Damage to players inside the gas ring
    [SerializeField] private float safeZoneRadius = 3f;     // Radius of the safe zone (the "hole" in the middle)

    private Transform gasRingTransform;
    private float currentTime = 0f;

    void Start()
    {
        // Get reference to the transform
        gasRingTransform = transform;

        // Start at full size
        gasRingTransform.localScale = Vector3.one * initialSize;
    }

    void Update()
    {
        // Increment the time
        currentTime += Time.deltaTime;

        // Calculate how much the gas ring should shrink
        float shrinkProgress = Mathf.Clamp01(currentTime / shrinkDuration);

        // Update the gas ring scale (shrinking effect)
        gasRingTransform.localScale = Vector3.one * (1f - shrinkProgress) * initialSize;

        // Destroy the gas object when fully shrunk
        if (shrinkProgress >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Calculate the distance between the player and the center of the gas ring
            float distanceToCenter = Vector3.Distance(other.transform.position, transform.position);

            // If the player is outside the safe zone, apply damage
            if (distanceToCenter > safeZoneRadius)
            {
                // Cast damagePerSecond * Time.deltaTime to an integer
                int damageToApply = Mathf.FloorToInt(damagePerSecond * Time.deltaTime);

                // Apply the integer damage to the player
             //   other.GetComponent<Player>().TakeDamage(damageToApply, "Gas"); set the damage

                // Apply damage over time
                //other.GetComponent<Player>().TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }
    }
}
