using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI, collectible, menu music and crash feedback for PropelXR.
/// Keep DroneAudioController on the drone for motor sound.
/// Attach this manager to an always-active scene object.
/// </summary>
public class PropelXRAudioManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PropelXRGameFlow gameFlow;
    [SerializeField] private PropelXRCoinField coinField;
    [SerializeField] private DroneFlightController flightController;
    [SerializeField] private Transform droneTransform;

    [Header("Audio Clips (assign your own)")]
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip coinCollected;
    [SerializeField] private AudioClip crashImpact;
    [SerializeField] private AudioClip menuMusic;

    [Header("Volumes")]
    [SerializeField, Range(0f, 1f)] private float uiVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float coinVolume = 0.65f;
    [SerializeField, Range(0f, 1f)] private float crashVolume = 0.75f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.18f;
    [SerializeField, Min(0.01f)] private float musicFadeSpeed = 1.5f;
    [SerializeField, Min(0.01f)] private float crashSoundCooldown = 0.4f;

    private AudioSource feedbackSource;
    private AudioSource crashSource;
    private AudioSource musicSource;
    private bool lastCrashed;
    private float lastCrashSoundTime = -999f;

    private void Awake()
    {
        feedbackSource = gameObject.AddComponent<AudioSource>();
        feedbackSource.playOnAwake = false;
        feedbackSource.spatialBlend = 0f; // UI and collectible confirmation: clear and consistent
        feedbackSource.ignoreListenerPause = true;

        GameObject crashEmitter = new GameObject("Crash SFX Emitter");
        crashEmitter.transform.SetParent(transform, false);
        crashSource = crashEmitter.AddComponent<AudioSource>();
        crashSource.playOnAwake = false;
        crashSource.spatialBlend = 1f;
        crashSource.minDistance = 0.5f;
        crashSource.maxDistance = 12f;
        crashSource.rolloffMode = AudioRolloffMode.Logarithmic;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.loop = true;
        musicSource.volume = 0f;
        musicSource.clip = menuMusic;
    }

    private void OnEnable()
    {
        if (coinField != null) coinField.CoinCollected += OnCoinCollected;
        lastCrashed = flightController != null && flightController.IsCrashed;
    }

    private void OnDisable()
    {
        if (coinField != null) coinField.CoinCollected -= OnCoinCollected;
    }

    private void Update()
    {
        if (gameFlow != null)
        {
            bool allowMusic = gameFlow.Screen == PropelXRGameFlow.ScreenState.Welcome ||
                              gameFlow.Screen == PropelXRGameFlow.ScreenState.MainMenu ||
                              gameFlow.Screen == PropelXRGameFlow.ScreenState.MissionSelect;

            if (menuMusic != null)
            {
                if (musicSource.clip != menuMusic) musicSource.clip = menuMusic;
                if (allowMusic && !musicSource.isPlaying) musicSource.Play();
                float desiredVolume = allowMusic ? musicVolume : 0f;
                musicSource.volume = Mathf.MoveTowards(musicSource.volume, desiredVolume,
                    musicFadeSpeed * Time.unscaledDeltaTime);
                if (!allowMusic && musicSource.isPlaying && musicSource.volume <= 0.001f)
                    musicSource.Stop();
            }
        }

        bool crashedNow = flightController != null && flightController.IsCrashed;
        bool playing = gameFlow != null && gameFlow.Screen == PropelXRGameFlow.ScreenState.Playing;
        if (playing && crashedNow && !lastCrashed &&
            Time.unscaledTime - lastCrashSoundTime >= crashSoundCooldown)
        {
            PlayCrash();
            lastCrashSoundTime = Time.unscaledTime;
        }
        lastCrashed = crashedNow;
    }

    public void PlayButtonClick()
    {
        if (buttonClick != null) feedbackSource.PlayOneShot(buttonClick, uiVolume);
    }

    private void OnCoinCollected(int total, int remaining)
    {
        if (coinCollected != null) feedbackSource.PlayOneShot(coinCollected, coinVolume);
    }

    private void PlayCrash()
    {
        if (crashImpact == null) return;
        if (droneTransform != null) crashSource.transform.position = droneTransform.position;
        crashSource.PlayOneShot(crashImpact, crashVolume);
    }
}
