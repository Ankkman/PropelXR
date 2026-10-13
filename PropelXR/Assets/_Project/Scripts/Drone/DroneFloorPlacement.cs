using UnityEngine;
using Meta.XR.MRUtilityKit;

[RequireComponent(typeof(Rigidbody))]
public class DroneFloorPlacement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private float spawnDistance = 1.5f;
    [SerializeField] private float floorClearance = 0.02f;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // Freeze gravity

        if (xrOrigin != null)
        {
            // Spawn the drone floating at eye level immediately so you can see it!
            Vector3 forward = Vector3.ProjectOnPlane(xrOrigin.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            
            // Placed 1.2 meters high
            rb.position = xrOrigin.position + (forward * spawnDistance) + new Vector3(0, 1.2f, 0);
        }

        if (MRUK.Instance != null)
        {
            // Wait for you to press B and MRUK to load the room
            MRUK.Instance.SceneLoadedEvent.AddListener(DropToFloor);
        }
    }

    private void DropToFloor()
    {
        // When MRUK finishes loading, find the true physical floor height
        Collider[] colliders = GetComponentsInChildren<Collider>();
        if (colliders.Length == 0) return;

        float lowestPoint = float.PositiveInfinity;
        foreach (Collider col in colliders)
        {
            if (col.enabled) lowestPoint = Mathf.Min(lowestPoint, col.bounds.min.y);
        }

        float bottomOffset = transform.position.y - lowestPoint;
        Vector3 forward = Vector3.ProjectOnPlane(xrOrigin.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;

        Vector3 spawnPosition = xrOrigin.position + forward * spawnDistance;
        spawnPosition.y = xrOrigin.position.y + bottomOffset + floorClearance;

        rb.position = spawnPosition;
        rb.linearVelocity = Vector3.zero;
        
        // Turn gravity back on so it rests on the newly generated floor
        rb.isKinematic = false;
        rb.WakeUp();
    }
}