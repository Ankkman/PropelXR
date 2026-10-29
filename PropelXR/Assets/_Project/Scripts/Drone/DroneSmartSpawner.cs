using System.Collections;
using Meta.XR.MRUtilityKit;
using UnityEngine;

/// <summary>
/// Places an existing drone root in a clear area of the MRUK room.
/// Add to the drone ROOT (same GameObject as Rigidbody/DroneFlightController).
/// MRUK and scene collider generation must be configured separately.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DroneSmartSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DroneFlightController flightController;
    [SerializeField] private Transform headset;

    [Header("Placement")]
    [SerializeField, Min(0.01f)] private float clearanceAboveFloor = 0.08f;
    [SerializeField, Min(0f)] private float additionalSideClearance = 0.15f;
    [SerializeField, Min(0f)] private float minimumDistanceFromHeadset = 0.65f;
    [SerializeField, Min(1)] private int maximumCandidates = 120;
    [SerializeField, Min(0.1f)] private float preferredDistance = 1.2f;

    [Header("Physics Checks")]
    [Tooltip("Include layers used by MRUK EffectMesh colliders and other room obstacles. Usually Everything.")]
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [Tooltip("Time allowed for MRUK and room colliders to become ready.")]
    [SerializeField, Min(0.5f)] private float readyTimeout = 10f;
    [SerializeField, Min(0f)] private float colliderBuildDelay = 0.3f;

    private Rigidbody body;
    private Collider[] ownColliders;
    private Vector3 localBoxCenter;
    private Vector3 localHalfExtents;
    private bool hasShape;
    private bool spawned;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (flightController == null)
            flightController = GetComponent<DroneFlightController>();

        // Prevent FlightController.Start() from recording an unsafe initial reset pose.
        // It will initialize only after this component has placed the drone.
        if (flightController != null)
            flightController.enabled = false;
        body.isKinematic = true;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        ownColliders = GetComponentsInChildren<Collider>(true);
        hasShape = ComputeLocalColliderBox();
    }

    private IEnumerator Start()
    {
        if (flightController == null || !hasShape)
        {
            Debug.LogError("DroneSmartSpawner: Missing DroneFlightController or valid body/arm colliders. Drone remains unspawned.", this);
            yield break;
        }

        float deadline = Time.realtimeSinceStartup + readyTimeout;
        MRUKRoom room = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (MRUK.Instance != null)
                room = MRUK.Instance.GetCurrentRoom();
            if (room != null && room.GetFloorAnchor() != null)
                break;
            yield return null;
        }

        if (room == null || room.GetFloorAnchor() == null)
        {
            Debug.LogError("DroneSmartSpawner: No MRUK device room/floor detected within timeout. Keeping drone inactive; check Scene permission and MRUK logs.", this);
            yield break;
        }

        // Allow EffectMesh/scene collider scripts to finish their scene-loaded setup.
        if (colliderBuildDelay > 0f)
            yield return new WaitForSeconds(colliderBuildDelay);
        Physics.SyncTransforms();

        Transform view = headset != null ? headset : (Camera.main != null ? Camera.main.transform : null);
        if (view == null)
        {
            Debug.LogError("DroneSmartSpawner: No headset assigned and no MainCamera found.", this);
            yield break;
        }

        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        Quaternion orientation = Quaternion.LookRotation(forward, Vector3.up);

        float floorY = room.GetFloorAnchor().transform.position.y;
        Bounds roomBounds = room.GetRoomBounds();
        Vector3 proposed = view.position + forward * preferredDistance;

        Vector3 validRoot;
        if (!TryCandidate(proposed.x, proposed.z, floorY, orientation, room, view.position, out validRoot))
        {
            bool found = false;
            validRoot = Vector3.zero;
            for (int i = 0; i < maximumCandidates; i++)
            {
                float x = Random.Range(roomBounds.min.x, roomBounds.max.x);
                float z = Random.Range(roomBounds.min.z, roomBounds.max.z);
                if (TryCandidate(x, z, floorY, orientation, room, view.position, out validRoot))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Debug.LogError($"DroneSmartSpawner: No clear ground placement among {maximumCandidates + 1} candidates. Check room anchors, EffectMesh colliders, obstacle layers and clearance values. Drone remains inactive.", this);
                yield break;
            }
        }

        // Keep rigidbody kinematic until FlightController.Start() initializes its reset pose.
        body.position = validRoot;
        body.rotation = orientation;
        transform.SetPositionAndRotation(validRoot, orientation);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        Physics.SyncTransforms();
        spawned = true;
        flightController.enabled = true;
        Debug.Log($"DroneSmartSpawner: Safe spawn at {validRoot}; scanned floor Y={floorY:F2}.", this);
        // Do not destroy the component in this frame: it is harmless and useful for diagnostics.
    }

    private bool TryCandidate(float x, float z, float floorY, Quaternion rotation,
        MRUKRoom room, Vector3 viewPosition, out Vector3 rootPosition)
    {
        // The local collider AABB bottom must sit above the real scanned floor.
        float rotatedOffsetY = (rotation * localBoxCenter).y;
        float rootY = floorY + clearanceAboveFloor + localHalfExtents.y - rotatedOffsetY;
        rootPosition = new Vector3(x, rootY, z);
        Vector3 center = rootPosition + rotation * localBoxCenter;
        Vector3 half = localHalfExtents + new Vector3(additionalSideClearance, 0.02f, additionalSideClearance);

        Vector2 horizontalGap = new Vector2(x - viewPosition.x, z - viewPosition.z);
        if (horizontalGap.magnitude < minimumDistanceFromHeadset)
            return false;

        // All corners and side midpoints must remain within the actual room outline.
        // Scene volume tests catch detected tables, couches, storage, etc.
        for (int xi = -1; xi <= 1; xi++)
        for (int zi = -1; zi <= 1; zi++)
        for (int yi = -1; yi <= 1; yi++)
        {
            if (xi == 0 && zi == 0 && yi == 0)
                continue;
            Vector3 sample = center + rotation * new Vector3(xi * half.x, yi * half.y, zi * half.z);
            if (!room.IsPositionInRoom(sample) || room.IsPositionInSceneVolume(sample))
                return false;
        }
        if (!room.IsPositionInRoom(center) || room.IsPositionInSceneVolume(center))
            return false;

        // Catch actual MRUK scene mesh/EffectMesh and other physics obstacles.
        // The drone's existing own colliders are excluded from the overlap results.
        Collider[] collisions = Physics.OverlapBox(center, half, rotation,
            obstacleLayers, QueryTriggerInteraction.Ignore);
        foreach (Collider other in collisions)
        {
            if (other == null)
                continue;
            bool isOurCollider = false;
            foreach (Collider own in ownColliders)
            {
                if (other == own)
                {
                    isOurCollider = true;
                    break;
                }
            }
            if (!isOurCollider)
                return false;
        }
        return true;
    }

    private bool ComputeLocalColliderBox()
    {
        Bounds aggregate = new Bounds();
        bool any = false;
        foreach (Collider c in ownColliders)
        {
            if (c == null || !c.enabled || c.isTrigger)
                continue;
            // Ignore MRUK colliders, which should never be children of the drone.
            Bounds b = c.bounds;
            for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
            {
                Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3(xi, yi, zi));
                Vector3 local = transform.InverseTransformPoint(corner);
                if (!any)
                {
                    aggregate = new Bounds(local, Vector3.zero);
                    any = true;
                }
                else
                    aggregate.Encapsulate(local);
            }
        }
        if (!any)
            return false;
        localBoxCenter = aggregate.center;
        localHalfExtents = aggregate.extents;
        Debug.Log($"DroneSmartSpawner: Local collider half-size={localHalfExtents}, offset={localBoxCenter}.", this);
        return true;
    }
}
