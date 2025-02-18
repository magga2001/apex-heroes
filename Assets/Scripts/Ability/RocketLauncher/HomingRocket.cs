using UnityEngine;

public class HomingRocket : MonoBehaviour
{
    public float speed = 10f;  // Rocket movement speed
    public float turnSpeed = 5f;  // How quickly the rocket turns toward the target
    public float explosionRadius = 5f;
    public float explosionForce = 700f;
    public int damage = 50;

    private Transform target;  // The enemy the rocket will follow

    private string shotBy;
    public string ShotBy { get { return shotBy; } set { shotBy = value; } }

    private void Start()
    {
        // Find the nearest enemy
        FindNearestEnemy();
    }

    private void Update()
    {
        if (target != null)
        {
            // Adjust the rocket's direction to follow the enemy
            Vector3 direction = (target.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);

            // Move the rocket forward
            transform.position += transform.forward * speed * Time.deltaTime;
        }
        else
        {
            // If no target, just move forward
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }

    private void FindNearestEnemy()
    {
        // Find all enemies in the scene
        Enemy[] enemies = GameObject.FindObjectsOfType<Enemy>();
        float shortestDistance = Mathf.Infinity;
        Enemy nearestEnemy = null;

        // Find the closest enemy
        foreach (Enemy enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy;
            }
        }

        // Set the target to the nearest enemy
        if (nearestEnemy != null)
        {
            target = nearestEnemy.transform;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Same explosion logic as before
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider nearbyObject in colliders)
        {
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }

            Enemy enemy = nearbyObject.GetComponent<Enemy>();
            if (enemy != null)
            {
              //  enemy.TakeDamage(damage, shotBy);   / set the damage
            }
        }

        Destroy(gameObject);  // Destroy the rocket after impact
    }
}
