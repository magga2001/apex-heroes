using Fusion;
using Unity.VisualScripting;
using UnityEngine;

public class RocketLauncher : Ability
{
    public GameObject rocketPrefab;  // Prefab for the rocket
    public Transform firePoint;      // Where the rocket will be fired from

    [SerializeField] private float rocketSpeed = 500f;  // Speed of the rocket
    [SerializeField] private float downwardForce = 10f;  // Additional downward force over time

    public override void Activate(PlayerRef playeref, Player player)
    {
        ShootRocket(playeref,player);
    }
    private void ShootRocket(PlayerRef playeref, Player player)
    {
        // Get the rocket from the pooling manager
        NetworkObject rocket = ObjectPoolingManager.Instance.GetRocket();

        // Set the rocket's position and rotation
        rocket.transform.position = firePoint.transform.position;
        rocket.transform.rotation = firePoint.transform.rotation;

        NetworkObject rocketMuzzleEffect = EffectPoolingManager.Instance.GetRocketMuzzleEffect();
        rocketMuzzleEffect.transform.position = firePoint.transform.position;
        rocketMuzzleEffect.transform.rotation = firePoint.transform.rotation;

        // Apply forward force to the rocket to simulate shooting forward
        Rigidbody rb = rocket.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;  // Disable gravity initially to control the trajectory
            rb.velocity = Vector3.zero; // Clear any existing velocity
            rb.AddForce(firePoint.forward * rocketSpeed, ForceMode.VelocityChange);
        }

        // Initialize the RocketTrajectory script
        RocketTrajectory trajectory = rocket.GetComponent<RocketTrajectory>();
        if (trajectory != null)
        {
            trajectory.Initialize(Runner.LocalPlayer,downwardForce,GetComponent<Player>().Nickname.ToString());
        }
        else
        {
            Debug.LogError("RocketTrajectory component is missing from the rocket prefab!");
        }

        Debug.Log("Rocket fired with controlled downward trajectory!");
    }

    //private void ShootRocket(Player player)
    //{
    //    // Instantiate the rocket at the fire point
    //    //GameObject rocket = Instantiate(rocketPrefab, firePoint.position, firePoint.rotation);
    //    NetworkObject rocket = ObjectPoolingManager.Instance.GetRocket();
    //    rocket.transform.position = firePoint.transform.position;
    //    rocket.transform.rotation = firePoint.transform.rotation;

    //    // Apply forward force to the rocket to simulate shooting forward
    //    Rigidbody rb = rocket.GetComponent<Rigidbody>();
    //    rb.useGravity = false;  // Disable gravity initially to control the trajectory
    //    rb.AddForce(firePoint.forward * rocketSpeed, ForceMode.VelocityChange);

    //    // Add a coroutine to gradually apply a downward force to simulate the rocket falling
    //    rocket.AddComponent<RocketTrajectory>().Initialize(downwardForce);

    //    Debug.Log("Rocket fired with controlled downward trajectory!");
    //}
}
