using UnityEngine;

/// <summary>
/// Shows flight-only HUD and controls the motor AudioSource separately from
/// the motor controller component. Attach to an always-active scene object.
/// </summary>
public class PropelXRGameplayPresentation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PropelXRGameFlow gameFlow;
    [SerializeField] private DroneAudioController droneMotorAudio;
    [Tooltip("Assign the exact AudioSource used by DroneAudioController (motor hum), not the crash impact source.")]
    [SerializeField] private AudioSource motorAudioSource;
    [SerializeField] private GameObject stickDebugCanvas;

    private bool? wasPlaying;

    private void Awake()
    {
        if (stickDebugCanvas != null)
            stickDebugCanvas.SetActive(false);

        // AudioSource.mute is independent of component enabled state and
        // protects against a still-playing motor loop during startup.
        if (motorAudioSource != null)
        {
            motorAudioSource.mute = true;
            motorAudioSource.Pause();
        }
    }

    private void Start() => Refresh(true);
    private void Update() => Refresh(false);

    private void Refresh(bool force)
    {
        if (gameFlow == null) return;

        bool playing = gameFlow.Screen == PropelXRGameFlow.ScreenState.Playing;
        if (!force && wasPlaying.HasValue && wasPlaying.Value == playing)
            return;
        wasPlaying = playing;

        if (stickDebugCanvas != null)
            stickDebugCanvas.SetActive(playing);

        if (!playing)
        {
            // Disabling the controller alone is NOT enough: its AudioSource
            // is a separate component and may keep playing.
            if (droneMotorAudio != null)
                droneMotorAudio.enabled = false;

            if (motorAudioSource != null)
            {
                motorAudioSource.mute = true;
                motorAudioSource.Pause();
            }
        }
        else
        {
            if (motorAudioSource != null)
            {
                motorAudioSource.mute = false;
                // Resume a paused clip before enabling DroneAudioController,
                // so its OnEnable doesn't call Play() and restart the clip.
                motorAudioSource.UnPause();
            }

            if (droneMotorAudio != null)
                droneMotorAudio.enabled = true;
        }
    }
}
