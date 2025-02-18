using Fusion;
using System.Collections;
using UnityEngine;
public class RocketTrajectory : NetworkBehaviour
{
    private Rigidbody rb;
    private float downwardForce;

    public void Initialize(PlayerRef playerref, float force,string shotby)
    {
        rb = GetComponent<Rigidbody>();
        downwardForce = force;
        GetComponent<Rocket>().ShotBy = shotby;
        GetComponent<Rocket>()._playerref = playerref;
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            // Apply a downward force over time to simulate the rocket descending
            rb.AddForce(Vector3.down * downwardForce, ForceMode.Acceleration);
        }
    }
}
