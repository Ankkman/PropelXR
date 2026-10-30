using UnityEngine;

/// <summary>Keep the successfully initialized drone in the scene; hide/freeze it in menus.</summary>
public class PropelXRDronePresence : MonoBehaviour
{
    [SerializeField] private DroneFlightController flightController;
    [SerializeField] private DroneInputReader inputReader;
    [SerializeField] private Rigidbody droneRigidbody;
    [SerializeField] private GameObject droneRoot;

    private Renderer[] renderers;
    private Collider[] colliders;
    private bool[] originalColliderStates;
    private bool[] originalRendererStates;
    private bool captured;
    private bool initialVisualsCaptured;

    private void Awake()
    {
        if (flightController == null || droneRoot == null)
        {
            Debug.LogError("PropelXRDronePresence: assign drone root and flight controller.", this);
            enabled = false;
            return;
        }
        if (droneRigidbody == null) droneRigidbody = droneRoot.GetComponent<Rigidbody>();
        if (inputReader == null) inputReader = droneRoot.GetComponent<DroneInputReader>();
        // Hide graphics immediately, but leave all colliders and scripts untouched
        // until the existing MRUK spawner has finished initialization.
        renderers = droneRoot.GetComponentsInChildren<Renderer>(true);
        originalRendererStates = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalRendererStates[i] = renderers[i].enabled;
            renderers[i].enabled = false;
        }
        initialVisualsCaptured = true;
    }

    private void Capture()
    {
        if (captured) return;
        if (!initialVisualsCaptured) renderers = droneRoot.GetComponentsInChildren<Renderer>(true);
        colliders = droneRoot.GetComponentsInChildren<Collider>(true);
        if (!initialVisualsCaptured) originalRendererStates = new bool[renderers.Length];
        originalColliderStates = new bool[colliders.Length];
        if (!initialVisualsCaptured)
            for (int i = 0; i < renderers.Length; i++) originalRendererStates[i] = renderers[i].enabled;
        for (int i = 0; i < colliders.Length; i++) originalColliderStates[i] = colliders[i].enabled;
        captured = true;
    }

    public void HideInMenu()
    {
        if (!flightController.ReadyForGameplay) return; // don't interfere with initial spawner
        Capture();
        if (inputReader != null) inputReader.enabled = false;
        // Existing reset owns motor state and resets to the previously validated MRUK spawn.
        flightController.ReturnToMenuDisarmed();
        if (droneRigidbody != null)
        {
            droneRigidbody.isKinematic = true;
            droneRigidbody.linearVelocity = Vector3.zero;
            droneRigidbody.angularVelocity = Vector3.zero;
        }
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].enabled = false;
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = false;
    }

    public void ShowForGameplay()
    {
        if (!flightController.ReadyForGameplay)
        {
            Debug.LogWarning("PropelXRDronePresence: drone not initialized yet.", this);
            return;
        }
        Capture();
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = originalColliderStates[i];
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].enabled = originalRendererStates[i];
        // FlightController Reset leaves the drone kinematic & disarmed, as intended.
        // Arm action will set isKinematic=false in the existing controller.
    }
}
