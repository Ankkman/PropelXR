using UnityEngine;

/// <summary>
/// Drives a single motor-hum AudioSource so it reacts dynamically to the
/// drone's flight state. Pitch tracks motor load (climb rate, tilt,
/// horizontal speed); volume tracks armed state and (optionally) distance
/// to the listener when running without spatial audio.
///
/// Place on the same GameObject as the Rigidbody. The AudioSource is
/// created automatically if not assigned. In XR, leave useSpatialAudio
/// true so the XR AudioListener handles direction and rolloff naturally.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DroneAudioController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to auto-find on this GameObject.")]
    [SerializeField] private DroneFlightController flightController;

    [Tooltip("Leave empty to auto-find or auto-create.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("The looping motor hum clip.")]
    [SerializeField] private AudioClip motorLoop;

    [Header("Spatial Audio")]
    [Tooltip("True for XR / 3D audio (recommended on Quest). " +
             "False uses the manual 2D distance fallback below.")]
    [SerializeField] private bool useSpatialAudio = true;

    [Tooltip("Only used when useSpatialAudio is false.")]
    [SerializeField] private Transform listenerOverride;

    [Tooltip("Below this distance, volume is at its maximum (2D fallback).")]
    [SerializeField] private float nearDistance = 1.5f;

    [Tooltip("Beyond this distance, volume is at its minimum (2D fallback).")]
    [SerializeField] private float farDistance = 25f;

    [Tooltip("Volume multiplier at farDistance (2D fallback).")]
    [SerializeField, Range(0f, 1f)] private float farVolume = 0.05f;

    [Header("Base Motor Tone")]
    [Tooltip("Volume at hover when armed.")]
    [SerializeField, Range(0f, 1f)] private float baseVolume = 0.6f;

    [Tooltip("Pitch at hover. 1.0 = unmodified clip.")]
    [SerializeField, Range(0.5f, 2f)] private float basePitch = 1f;

    [Header("Vertical Modulation")]
    [Tooltip("Pitch added at maximum climb speed. Scaled by vertical velocity / maxVerticalSpeed.")]
    [SerializeField, Range(0f, 1f)] private float climbPitchBoost = 0.18f;

    [Tooltip("Pitch subtracted at maximum descent speed. Smaller than climb: " +
             "motors lose more RPM going up than they gain going down.")]
    [SerializeField, Range(0f, 1f)] private float descentPitchDrop = 0.10f;

    [Header("Horizontal / Tilt Modulation")]
    [Tooltip("Pitch added at maximum horizontal speed.")]
    [SerializeField, Range(0f, 1f)] private float speedPitchBoost = 0.12f;

    [Tooltip("Pitch added at maximum tilt angle. Reflects the extra RPM " +
             "needed to hold altitude while tilted.")]
    [SerializeField, Range(0f, 1f)] private float tiltPitchBoost = 0.10f;

    [Tooltip("Volume added at maximum horizontal speed.")]
    [SerializeField, Range(0f, 0.5f)] private float speedVolumeBoost = 0.15f;

    [Header("Yaw Modulation")]
    [Tooltip("Pitch wobble amplitude at maximum yaw rate. Small; yaw mostly " +
             "cancels out across the rotor pair.")]
    [SerializeField, Range(0f, 0.1f)] private float yawPitchWobble = 0.02f;

    [Tooltip("Wobble frequency in Hz at maximum yaw rate.")]
    [SerializeField] private float yawWobbleFrequency = 6f;

    [Header("Landing")]
    [Tooltip("Pitch multiplier while in the landing descent.")]
    [SerializeField, Range(0.5f, 1f)] private float landingPitchScale = 0.9f;

    [Header("Response")]
    [Tooltip("How fast pitch and volume track their targets (per second).")]
    [SerializeField] private float responseSpeed = 6f;

    [Tooltip("Volume fade-in speed after arming (per second).")]
    [SerializeField] private float armFadeSpeed = 4f;

    [Tooltip("Volume fade-out speed after disarming (per second).")]
    [SerializeField] private float disarmFadeSpeed = 2.5f;

    [Tooltip("Volume fade-out speed after ground contact while disarmed.")]
    [SerializeField] private float groundFadeSpeed = 3f;

    private Rigidbody rb;

    // Smoothed state (separate from AudioSource so we can apply easing
    // curves independently of the underlying source properties).
    private float smoothedVolume;
    private float smoothedPitch = 1f;

    // Cached at Start so we don't re-read the flight controller every frame.
    private float maxVerticalSpeed = 2f;
    private float maxHorizontalSpeed = 3f;
    private float maxTiltAngle = 15f;
    private float maxYawRateDegPerSec = 50f;

    private float yawWobblePhase;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (flightController == null)
            flightController = GetComponent<DroneFlightController>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure the source once. We drive volume and pitch manually.
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = useSpatialAudio ? 1f : 0f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 0.5f;
        audioSource.maxDistance = 30f;
        audioSource.volume = 0f;
        audioSource.pitch = basePitch;

        if (motorLoop != null)
        {
            audioSource.clip = motorLoop;
        }
        else
        {
            Debug.LogWarning(
                "DroneAudioController: No motorLoop clip assigned. " +
                "The drone will be silent until one is set.", this);
        }
    }

    private void OnEnable()
    {
        // Start the loop. Volume is faded in as soon as the drone is armed.
        if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void Update()
    {
        if (audioSource == null || rb == null)
            return;

        bool armed = flightController == null || flightController.IsArmed;
        bool landing = flightController != null && flightController.IsLanding;

        // --------------------------------------------------------------
        // 1. Inputs from flight state
        // --------------------------------------------------------------
        Vector3 worldVel = rb.linearVelocity;

        float verticalNorm = Mathf.Clamp(
            worldVel.y / Mathf.Max(maxVerticalSpeed, 0.01f),
            -1f, 1f);

        Vector3 horizontalVel = new Vector3(worldVel.x, 0f, worldVel.z);
        float horizontalNorm = Mathf.Clamp01(
            horizontalVel.magnitude / Mathf.Max(maxHorizontalSpeed, 0.01f));

        // Tilt is measured as the angle between the drone's up vector and
        // world up. On a stabilized drone this is the pitch/roll magnitude.
        float tiltDeg = Vector3.Angle(transform.up, Vector3.up);
        float tiltNorm = Mathf.Clamp01(
            tiltDeg / Mathf.Max(maxTiltAngle, 0.01f));

        // Yaw rate from angular velocity projected onto world up.
        float yawRateDegPerSec = Vector3.Dot(
            rb.angularVelocity, Vector3.up) * Mathf.Rad2Deg;
        float yawNorm = Mathf.Clamp01(
            Mathf.Abs(yawRateDegPerSec) / Mathf.Max(maxYawRateDegPerSec, 0.01f));

        // --------------------------------------------------------------
        // 2. Target pitch (motor RPM proxy)
        // --------------------------------------------------------------
        // Baseline is 1.0 (hover). Climb adds, descent subtracts, horizontal
        // speed and tilt add. Yaw adds a small oscillation because the rotor
        // pair swaps RPM faster than the ear can track as a static value.
        float pitchTarget = basePitch;

        if (verticalNorm > 0f)
            pitchTarget += verticalNorm * climbPitchBoost;
        else
            pitchTarget -= (-verticalNorm) * descentPitchDrop;

        pitchTarget += horizontalNorm * speedPitchBoost;
        pitchTarget += tiltNorm * tiltPitchBoost;

        yawWobblePhase += yawNorm * yawWobbleFrequency * Time.deltaTime * Mathf.PI * 2f;
        pitchTarget += Mathf.Sin(yawWobblePhase) * yawNorm * yawPitchWobble;

        if (landing)
            pitchTarget *= landingPitchScale;

        // --------------------------------------------------------------
        // 3. Target volume
        // --------------------------------------------------------------
        float volumeTarget;

        if (!armed)
        {
            // Fade the motor hum down. Once fully faded, allow the source
            // to stop so it doesn't keep ticking at zero volume.
            volumeTarget = 0f;
        }
        else
        {
            volumeTarget = baseVolume + horizontalNorm * speedVolumeBoost;

            if (!useSpatialAudio)
            {
                // Manual 2D distance fallback for editor / simulator use.
                Transform listener = listenerOverride;
                if (listener == null)
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                        listener = mainCam.transform;
                }

                if (listener != null)
                {
                    float dist = Vector3.Distance(
                        listener.position, transform.position);
                    float t = Mathf.InverseLerp(nearDistance, farDistance, dist);
                    float distScale = Mathf.Lerp(1f, farVolume, t);
                    volumeTarget *= distScale;
                }
            }
        }

        volumeTarget = Mathf.Clamp01(volumeTarget);
        pitchTarget = Mathf.Clamp(pitchTarget, 0.5f, 2f);

        // --------------------------------------------------------------
        // 4. Smoothing
        // --------------------------------------------------------------
        float volumeSpeed = armed
            ? (landing && flightController != null && flightController.IsGrounded
                ? groundFadeSpeed
                : armFadeSpeed)
            : disarmFadeSpeed;

        smoothedVolume = Mathf.MoveTowards(
            smoothedVolume, volumeTarget, volumeSpeed * Time.deltaTime);

        smoothedPitch = Mathf.MoveTowards(
            smoothedPitch, pitchTarget, responseSpeed * Time.deltaTime);

        audioSource.volume = smoothedVolume;
        audioSource.pitch = smoothedPitch;

        // Pause the source when fully faded out to save a tiny bit of CPU.
        if (smoothedVolume <= 0.0001f && audioSource.isPlaying && !armed)
            audioSource.Pause();
        else if (!audioSource.isPlaying && armed && audioSource.clip != null)
            audioSource.UnPause();
    }

    private void OnDisable()
    {
        // Zero out so a re-enable starts from silence instead of whatever
        // the last frame happened to be.
        smoothedVolume = 0f;
        smoothedPitch = basePitch;

        if (audioSource != null)
        {
            audioSource.volume = 0f;
            audioSource.pitch = basePitch;
        }
    }

    /// <summary>
    /// Call from the flight controller (or anywhere) when a statically
    /// known tuning value changes. Safe to call every frame; cheap.
    /// </summary>
    public void Configure(
        float maxVertical,
        float maxHorizontal,
        float tiltLimit,
        float yawRate)
    {
        maxVerticalSpeed = Mathf.Max(0.01f, maxVertical);
        maxHorizontalSpeed = Mathf.Max(0.01f, maxHorizontal);
        maxTiltAngle = Mathf.Max(0.01f, tiltLimit);
        maxYawRateDegPerSec = Mathf.Max(0.01f, yawRate);
    }
}