using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    public Vector3 centerPosition = new Vector3(0, 20, -10); // Center of the map
    public float transitionSpeed = 1.0f; // Speed of the transition
    public CinemachineVirtualCamera virtualCamera; // Cinemachine virtual camera
    public CinemachineBrain cinemachineBrain; // Cinemachine Brain

    private Transform targetPlayer; // Local player's transform
    private bool isTransitioning = true;

    private Vector3 initialFollowOffset; // Store the follow offset of the Cinemachine Virtual Camera
    private Quaternion initialRotation; // Store the rotation of the Cinemachine Virtual Camera
    private Vector3 smoothStartPosition; // Position for smooth start

    private void Start()
    {
        // Temporarily disable Cinemachine control
        if (cinemachineBrain != null)
        {
            cinemachineBrain.enabled = false;
        }

        // Store the Cinemachine Transposer offset and rotation
        if (virtualCamera != null)
        {
            initialFollowOffset = virtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
            initialRotation = virtualCamera.transform.rotation;
        }

        // Set the Main Camera to the center of the map
        transform.position = centerPosition;
        transform.rotation = initialRotation;

        // Initialize the smooth starting position
        smoothStartPosition = centerPosition;
    }

    private void LateUpdate()
    {
        if (isTransitioning && targetPlayer != null)
        {
            // Calculate the target position for the transition
            Vector3 targetPosition = targetPlayer.position + initialFollowOffset;

            // Smoothly move the camera toward the target position
            smoothStartPosition = Vector3.Lerp(smoothStartPosition, targetPosition, Time.deltaTime * transitionSpeed);
            transform.position = smoothStartPosition;

            // Smoothly align rotation during the transition
            transform.rotation = Quaternion.Lerp(transform.rotation, initialRotation, Time.deltaTime * transitionSpeed);

            // Check if the camera has reached the target position
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                CompleteTransition();
            }
        }
    }

    public void SetTargetPlayer(Transform playerTransform)
    {
        targetPlayer = playerTransform;

        // Align camera to center and ensure no snapping
        if (isTransitioning)
        {
            transform.position = centerPosition;
        }
    }

    private void CompleteTransition()
    {
        isTransitioning = false;

        // Enable Cinemachine to take over
        if (cinemachineBrain != null)
        {
            cinemachineBrain.enabled = true;
        }

        if (virtualCamera != null)
        {
            virtualCamera.Follow = targetPlayer;
        }
    }
}
