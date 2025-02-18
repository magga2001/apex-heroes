using UnityEngine;
using Fusion;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float speed = 5.0f; // Movement speed
    [SerializeField] private float rotationSpeed = 720.0f; // Rotation speed
    [SerializeField] private Animator animator; // Reference to the Animator component
    [SerializeField] private Rigidbody selfRigidbody;
    private NetworkMecanimAnimator networkAnimator; // Reference to the NetworkMecanimAnimator

    private void Awake()
    {
        // Ensure the Animator is assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator component is missing!");
            }
        }

        // Get the NetworkMecanimAnimator component
        networkAnimator = GetComponent<NetworkMecanimAnimator>();
        if (networkAnimator == null)
        {
            Debug.LogError("NetworkMecanimAnimator component is missing!");
        }
    }

    public override void FixedUpdateNetwork()
    {
        //Debug.LogWarning($"Trying to get this bullshit running with {Object.HasInputAuthority}");
        //if (!Object.HasStateAuthority)
        //if (!Object.HasInputAuthority)
        //    return;

        // Retrieve input data set by OnInput
        PlayerInputData? inputData = Runner.GetInputForPlayer<PlayerInputData>(Object.InputAuthority);
        if (inputData == null)
        {
            return; // No input this frame
        }
        
        float horizontal = inputData.Value.Horizontal;
        float vertical = inputData.Value.Vertical;

        // Now use horizontal and vertical to move the player
        Vector3 movement = new Vector3(horizontal, 0, vertical).normalized * speed * Runner.DeltaTime;
        transform.Translate(movement, Space.World);
        //selfRigidbody.velocity = movement;

        if (movement != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(movement, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Runner.DeltaTime);
        }

        bool isRunning = movement.magnitude > 0;
        networkAnimator.Animator.SetBool("isRunning", isRunning);
    }


    //public override void FixedUpdateNetwork()
    //{
    //    // Only allow the player with InputAuthority to process movement
    //    if (!Object.HasInputAuthority)
    //        return;

    //    // Get movement input
    //    float horizontal = Input.GetAxis("Horizontal");
    //    float vertical = Input.GetAxis("Vertical");

    //    // Calculate movement vector
    //    Vector3 movement = new Vector3(horizontal, 0, vertical).normalized * speed * Runner.DeltaTime;

    //    // Move the player
    //    transform.Translate(movement, Space.World);

    //    // Rotate the player if there's movement input
    //    if (movement != Vector3.zero)
    //    {
    //        Quaternion toRotation = Quaternion.LookRotation(movement, Vector3.up);
    //        transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Runner.DeltaTime);
    //    }

    //    // Update the Animator parameter for running
    //    bool isRunning = movement.magnitude > 0;
    //    networkAnimator.Animator.SetBool("isRunning", isRunning);
    //}
}

public struct PlayerInputData : INetworkInput
{
    public float Horizontal;
    public float Vertical;
}




//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//// Dont forget to change rotation

//public class PlayerMovement : MonoBehaviour
//{
//    [SerializeField] private float speed = 5.0f; // Speed of the character movement
//    [SerializeField] private float rotationSpeed = 720.0f; // Speed of the character rotation

//    [SerializeField] private Animator animator;

//    void Awake()
//    {
//        if (animator == null)
//        {
//            animator = GetComponent<Animator>();
//            if (animator == null)
//            {
//                Debug.LogError("Animator component is missing!");
//            }
//        }

//        // Ensure the Animator is active
//        animator.Rebind();
//        animator.Update(0f);
//    }

//    void Update()
//    {
//        // Getting the input from the keyboard on the horizontal (left/right) and vertical (forward/backward) axes
//        float horizontal = Input.GetAxis("Horizontal");
//        float vertical = Input.GetAxis("Vertical");

//        // Creating a Vector3 based on inputs for movement in the X (left/right) and Z (forward/backward) planes
//        Vector3 movement = new Vector3(horizontal, 0.0f, vertical);

//        // Normalizing the movement vector to ensure consistent movement speed in all directions
//        movement = movement.normalized * speed * Time.deltaTime;

//        // Moving the character
//        transform.Translate(movement, Space.World);

//        // Rotating the character
//        if (movement != Vector3.zero)
//        {
//            Quaternion toRotation = Quaternion.LookRotation(movement, Vector3.up);
//            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
//        }

//        // Handle animation
//        bool isRunning = movement.magnitude > 0;
//        animator.SetBool("isRunning", isRunning);
//    }

//}

