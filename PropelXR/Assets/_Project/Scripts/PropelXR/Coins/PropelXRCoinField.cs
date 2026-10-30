using System;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;

/// <summary>Spawns lightweight test coins in scanned free room space for the two mission modes.</summary>
public class PropelXRCoinField : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody droneBody;
    [SerializeField] private Transform headset;
    [SerializeField] private Transform droneRoot;

    [Header("Coin Placement (metres)")]
    [SerializeField, Range(1, 20)] private int coinCount = 5;
    [SerializeField, Min(0.05f)] private float coinRadius = 0.085f;
    [SerializeField, Min(0.1f)] private float minimumFloorHeight = 0.45f;
    [SerializeField, Min(0.1f)] private float maximumFloorHeight = 1.6f;
    [SerializeField, Min(0.05f)] private float safetyRadius = 0.24f;
    [SerializeField, Min(0.1f)] private float distanceBetweenCoins = 0.55f;
    [SerializeField, Min(0f)] private float distanceFromHeadset = 0.65f;
    [SerializeField, Min(0f)] private float distanceFromDrone = 0.55f;
    [SerializeField, Min(1)] private int attemptsPerCoin = 350;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField, Min(0f)] private float minimumSideClearance = 0.16f;
    [SerializeField, Range(0f, 1f)] private float coinFloatClearance = 0.04f;

    private readonly List<PropelXRCoin> liveCoins = new List<PropelXRCoin>();
    private Material coinMaterial;
    private bool missionActive;
    private bool paused;
    private int collectedCount;
    public Rigidbody DroneBody => droneBody;
    public bool CanCollect => missionActive && !paused;
    public int CollectedCount => collectedCount;
    public int Remaining => liveCoins.Count;
    public int DesiredCoinCount => coinCount;
    public event Action<int, int> CoinCollected; // total collected, remaining

    private void Awake()
    {
        if (headset == null && Camera.main != null) headset = Camera.main.transform;
        if (droneBody == null && droneRoot != null) droneBody = droneRoot.GetComponent<Rigidbody>();
        if (droneRoot == null && droneBody != null) droneRoot = droneBody.transform;
    }

    public bool BeginMission()
    {
        ClearMission();
        if (droneBody == null || headset == null)
        {
            Debug.LogError("PropelXRCoinField: assign drone Rigidbody and headset.", this);
            return false;
        }
        MRUKRoom room = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
        if (room == null || room.GetFloorAnchor() == null)
        {
            Debug.LogWarning("PropelXRCoinField: room/floor not ready.", this);
            return false;
        }
        Physics.SyncTransforms();
        Bounds bounds = room.GetRoomBounds();
        float floorY = room.GetFloorAnchor().transform.position.y;
        float ceilingY = room.GetCeilingAnchor() != null
            ? room.GetCeilingAnchor().transform.position.y
            : bounds.max.y;
        List<Vector3> placed = new List<Vector3>();
        // Try generous spacing first, then modestly relax coin-to-coin spacing
        // rather than giving up as soon as one candidate cannot be placed.
        for (int n = 0; n < coinCount; n++)
        {
            bool found = false;
            Vector3 candidate = Vector3.zero;
            for (int attempt = 0; attempt < attemptsPerCoin; attempt++)
            {
                float clearance = safetyRadius + coinRadius + coinFloatClearance;
                float high = Mathf.Min(maximumFloorHeight, ceilingY - floorY - clearance);
                float low = Mathf.Max(minimumFloorHeight, clearance);
                // If vertical clearance cannot fit, no valid flight target exists.

                if (high < low) break;
                candidate = new Vector3(
                    UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                    floorY + UnityEngine.Random.Range(low, high),
                    UnityEngine.Random.Range(bounds.min.z, bounds.max.z));
                float spacing = Mathf.Lerp(distanceBetweenCoins,
                    Mathf.Max(0.2f, distanceBetweenCoins * 0.7f),
                    (float)attempt / attemptsPerCoin);
                if (!IsSafe(room, candidate, placed, spacing)) continue;
                found = true;
                break;
            }
            if (!found)
            {
                Debug.LogWarning($"PropelXRCoinField: could not place coin {n + 1}/{coinCount}; continuing with {liveCoins.Count} safe positions.", this);
                break;
            }
            placed.Add(candidate);
            SpawnCoin(candidate);
        }
        missionActive = liveCoins.Count > 0;
        if (!missionActive)
            Debug.LogWarning("PropelXRCoinField: No safe coin locations. Check scan, room mesh, and clearances.", this);
        else
            Debug.Log($"PropelXRCoinField: placed {liveCoins.Count}/{coinCount} coins safely.", this);
        return missionActive;
    }

    private bool IsSafe(MRUKRoom room, Vector3 center, List<Vector3> placed, float spacing)
    {
        float r = safetyRadius + coinRadius + coinFloatClearance;
        if (Vector3.Distance(center, headset.position) < distanceFromHeadset + r) return false;
        if (droneRoot != null && Vector3.Distance(center, droneRoot.position) < distanceFromDrone + r) return false;
        foreach (Vector3 prior in placed)
            if (Vector3.Distance(center, prior) < spacing) return false;

        // The previous 27-point full cube at a large radius rejected many
        // otherwise usable real-room candidates near walls or furniture.
        // Check the center and six clearance directions, then physics overlap.
        if (!room.IsPositionInRoom(center) || room.IsPositionInSceneVolume(center)) return false;
        float side = Mathf.Max(minimumSideClearance, r);
        Vector3[] offsets = {
            Vector3.right * side, Vector3.left * side,
            Vector3.forward * side, Vector3.back * side,
            Vector3.up * r, Vector3.down * r
        };
        foreach (Vector3 offset in offsets)
        {
            Vector3 sample = center + offset;
            if (!room.IsPositionInRoom(sample) || room.IsPositionInSceneVolume(sample)) return false;
        }
        // Physical clearance from MRUK EffectMesh colliders and any other solid obstacles.
        Collider[] overlaps = Physics.OverlapSphere(center, r, obstacleLayers,
            QueryTriggerInteraction.Ignore);
        foreach (Collider collider in overlaps)
        {
            if (collider == null) continue;
            if (droneRoot != null && collider.transform.IsChildOf(droneRoot)) continue;
            return false;
        }
        return true;
    }

    private void SpawnCoin(Vector3 position)
    {
        GameObject root = new GameObject("PropelXR_Coin");
        root.transform.SetParent(transform, true);
        root.transform.position = position;
        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.radius = coinRadius + 0.06f; // generous collection contact zone
        trigger.isTrigger = true;
        PropelXRCoin coin = root.AddComponent<PropelXRCoin>();

        // Torus-like coin without imported assets: flattened cylinder plus a small core.
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Coin_Visual";
        body.transform.SetParent(root.transform, false);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(coinRadius * 2f, 0.014f, coinRadius * 2f);
        Collider visualCollider = body.GetComponent<Collider>();
        if (visualCollider != null) Destroy(visualCollider);
        if (coinMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            coinMaterial = new Material(shader);
            coinMaterial.color = new Color(1f, 0.74f, 0.16f);
            coinMaterial.EnableKeyword("_EMISSION");
            coinMaterial.SetColor("_EmissionColor", new Color(0.7f, 0.35f, 0.02f));
        }
        body.GetComponent<Renderer>().sharedMaterial = coinMaterial;
        coin.Initialize(this);
        liveCoins.Add(coin);
    }

    public void Collect(PropelXRCoin coin)
    {
        if (!CanCollect || !liveCoins.Remove(coin)) return;
        collectedCount++;
        Destroy(coin.gameObject);
        if (liveCoins.Count == 0) missionActive = false;
        CoinCollected?.Invoke(collectedCount, liveCoins.Count);
        Debug.Log($"PropelXR: coin collected ({collectedCount}), remaining {liveCoins.Count}.", this);
    }

    public void SetPaused(bool isPaused) => paused = isPaused;

    public void ClearMission()
    {
        missionActive = false;
        paused = false;
        for (int i = 0; i < liveCoins.Count; i++)
            if (liveCoins[i] != null) Destroy(liveCoins[i].gameObject);
        liveCoins.Clear();
        collectedCount = 0;
    }

    private void OnDestroy()
    {
        if (coinMaterial != null) Destroy(coinMaterial);
    }
}
