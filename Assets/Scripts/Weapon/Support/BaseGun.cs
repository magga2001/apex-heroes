using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO, Rotate toward crate boxes

// BaseGun is now abstract, meaning it can't be instantiated directly but can be inherited from.
public abstract class BaseGun : NetworkBehaviour
{
    [SerializeField] protected float shootingRange;
    [SerializeField] protected float detectionRadius;
    [SerializeField] protected string enemyTag = "Enemy";
    [SerializeField] protected float turnSpeed = 5f;

    private Transform closestEnemy;  // Protected to allow access by derived classes

    private void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //{
            //Shooting();
        //}
    }

    // Virtual so derived classes can override this if they need custom shooting behavior
    public virtual void Shooting(PlayerRef playerref, string shotBy, Player player)
    {
        DetectClosestEnemy();

        if (closestEnemy != null)
        {
            Vector3 directionToEnemy = closestEnemy.position - transform.position;
            float distanceToEnemy = directionToEnemy.magnitude;

            // Rotate the player towards the enemy smoothly if within range
            if (distanceToEnemy <= shootingRange)
            {
                RotateTowardsEnemySmoothly(directionToEnemy);
            }
        }

        // Shoot at the enemy regardless of the range
        Fire(playerref,shotBy, player);
    }

    // This can be overridden in derived classes if needed
    protected virtual void DetectClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        float closestDistance = Mathf.Infinity;
        Transform closestEnemyTemp = null;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemyTemp = enemy.transform;
            }
        }

        closestEnemy = closestEnemyTemp;
    }

    // This method will remain the same, but it can be overridden in derived classes
    protected void RotateTowardsEnemySmoothly(Vector3 directionToEnemy)
    {
        Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    // Abstract so derived classes must implement their own Fire method
    public abstract void Fire(PlayerRef playerref, string shotBy, Player player = null);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
