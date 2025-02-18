using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimAssist : NetworkBehaviour
{
    [SerializeField] private Transform aimOrigin;      // Point from where the shot originates (e.g., the player or weapon)
    [SerializeField] private LineRenderer lineRenderer; // LineRenderer for the aim line
    [SerializeField] private float startWidth = 0.05f;
    [SerializeField] private float endWidth = 3f;
    [SerializeField] private float maxAimDistance = 10f; // Max distance the aim can go
    [SerializeField] private LayerMask aimLayerMask;    // Layers for obstacles or enemies
    [SerializeField] private Color aimLineColor = Color.white; // Color for aim assist line
    [SerializeField] private float capOffset = 1f;

    void Start()
    {
        // Set initial settings for the LineRenderer
        lineRenderer.positionCount = 2; // Always 2 points for a simple straight line

        // Create an AnimationCurve for the width over the line's length with exaggerated values
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0.0f, 0.05f);  // Narrow at the start
        widthCurve.AddKey(1.0f, 5f);     // Exaggerated width at the end

        // Assign the width curve to the LineRenderer
        lineRenderer.widthCurve = widthCurve;

        // Optionally, you can also set a default width
        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;

        // Make sure the LineRenderer uses world space
        lineRenderer.useWorldSpace = true;

        lineRenderer.alignment = LineAlignment.TransformZ;
    }

    void Update()
    {
        // Only allow the player with InputAuthority
        if (!Object.HasInputAuthority)
            return;

        Vector3 aimDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if (aimDirection != Vector3.zero)
        {
            aimDirection.Normalize();
            UpdateAimAssist(aimDirection);

            // Rotate the LineRenderer to ensure it's aligned with the XZ plane
            lineRenderer.transform.eulerAngles = new Vector3(90, 0, 0); // Rotate to match XZ plane
        }
    }


    void UpdateAimAssist(Vector3 direction)
    {
        // Starting point for the line is the player's aim origin
        Vector3 startPoint = aimOrigin.position;

        // Default endpoint is the maximum aim distance in the aim direction
        Vector3 endPoint = startPoint + direction * maxAimDistance;

        // Raycast to detect enemies or obstacles, keeping the ray horizontal
        RaycastHit hit;
        if (Physics.Raycast(startPoint, direction, out hit, maxAimDistance, aimLayerMask))
        {
            // Adjust the endpoint if an obstacle or enemy is hit
            endPoint = hit.point;

            // Calculate the distance between the player and the obstacle
            float distanceToHit = Vector3.Distance(startPoint, hit.point);

            // Always apply the cap offset but ensure it doesn't push the line behind the player
            Vector3 offset = direction * Mathf.Min(capOffset, distanceToHit);  // Clamp the offset so it can't exceed distance
            endPoint -= offset;

        }

        // Set the positions for the LineRenderer
        lineRenderer.SetPosition(0, startPoint); // Start at aim origin
        lineRenderer.SetPosition(1, endPoint);   // End at the hit point or max distance

        // Update the color of the line
        lineRenderer.startColor = aimLineColor;
        lineRenderer.endColor = aimLineColor;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(aimOrigin.position, aimOrigin.position + aimOrigin.forward * maxAimDistance);
    }
}
