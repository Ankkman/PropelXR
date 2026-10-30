using System.Collections;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Replaces the previous Phase 1 GameFlow. Does NOT replace your customized MenuUI.</summary>
public class PropelXRGameFlow : MonoBehaviour
{
    public enum ScreenState { LoadingRoom, Welcome, MainMenu, MissionSelect, Playing, Paused, Results }
    public enum PlayMode { None, FreeFlight, CoinCollector, TimeRush }

    [Header("Existing Phase 1 References")]
    [SerializeField] private DroneFlightController flightController;
    [SerializeField] private DroneInputReader inputReader;
    [SerializeField] private PropelXRMenuUI menuUI;
    [SerializeField] private Transform headset;
    [SerializeField] private InputActionReference toggleMenuAction;

    [Header("Phase 2 References")]
    [SerializeField] private PropelXRDronePresence dronePresence;
    [SerializeField] private PropelXRCoinField coinField;
    [SerializeField] private PropelXRMissionUI missionUI;

    [Header("Phase 3 Missions")]
    [SerializeField, Min(1)] private int pointsPerCoin = 10;
    [SerializeField, Min(5f)] private float timeRushSeconds = 60f;
    [Header("Time Rush Lives")]
    [SerializeField, Range(1, 10)] private int maxTimeRushCrashes = 3;
    private int timeRushCrashes;
    private bool previousCrashed;
    private float timeRemaining;
    private int score;
    private int missionCollected;
    private int targetCoins;

    [Header("Startup")]
    [SerializeField, Min(0f)] private float welcomeSeconds = 3.5f;
    [SerializeField, Min(1f)] private float roomReadyTimeout = 30f;

    public ScreenState Screen { get; private set; } = ScreenState.LoadingRoom;
    public PlayMode Mode { get; private set; } = PlayMode.None;
    private InputAction menuAction;
    private bool menuActionSubscribed;
    private Coroutine startupRoutine;

    private void Awake()
    {
        if (headset == null && Camera.main != null) headset = Camera.main.transform;
        if (inputReader != null) inputReader.enabled = false;
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        BindMenuAction();
        if (coinField != null) coinField.CoinCollected += HandleCoinCollected;
    }

    private void Start()
    {
        if (menuUI == null || flightController == null || inputReader == null || dronePresence == null)
        {
            Debug.LogError("PropelXRGameFlow: Assign Menu UI, Flight Controller, Input Reader, Drone Presence.", this);
            enabled = false;
            return;
        }
        menuUI.Initialize(this, headset);
        startupRoutine = StartCoroutine(InitializeRoomAndWelcome());
    }

    private IEnumerator InitializeRoomAndWelcome()
    {
        Screen = ScreenState.LoadingRoom;
        Mode = PlayMode.None;
        menuUI.ShowMessage("PREPARING YOUR ROOM", "Loading passthrough, room surfaces and safe drone placement...");
        float deadline = Time.realtimeSinceStartup + roomReadyTimeout;
        while (Time.realtimeSinceStartup < deadline)
        {
            MRUKRoom room = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
            if (room != null && room.GetFloorAnchor() != null && flightController.ReadyForGameplay)
                break;
            yield return null;
        }
        MRUKRoom loaded = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
        if (loaded == null || loaded.GetFloorAnchor() == null || !flightController.ReadyForGameplay)
        {
            menuUI.ShowMessage("ROOM NOT READY", "Scan your room in Quest Space Setup, then restart PropelXR. Check MRUK and DroneSmartSpawner logs.");
            Debug.LogError("PropelXR: room or drone spawn did not initialize within timeout.", this);
            yield break;
        }

        dronePresence.HideInMenu();
        ShowWelcome();
        yield return new WaitForSecondsRealtime(welcomeSeconds);
        if (Screen == ScreenState.Welcome) ShowMainMenu();
        startupRoutine = null;
    }

    private void BindMenuAction()
    {
        if (menuActionSubscribed) return;
        menuAction = toggleMenuAction != null ? toggleMenuAction.action : null;
        if (menuAction == null) return;
        menuAction.performed += OnMenuAction;
        menuAction.Enable();
        menuActionSubscribed = true;
    }

    private void OnDisable()
    {
        if (coinField != null) coinField.CoinCollected -= HandleCoinCollected;
        if (missionUI != null) { missionUI.HideHUD(); missionUI.HideResults(); }
        if (startupRoutine != null) StopCoroutine(startupRoutine);
        startupRoutine = null;
        if (menuActionSubscribed && menuAction != null)
        {
            menuAction.performed -= OnMenuAction;
            menuAction.Disable();
        }
        menuActionSubscribed = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Screen == ScreenState.Playing && Mode == PlayMode.TimeRush)
        {
            // Detect one crash per transition into the crashed state.
            // Do not count continuously while the drone lies on the floor.
            bool isCrashed = flightController != null && flightController.IsCrashed;
            if (isCrashed && !previousCrashed)
            {
                timeRushCrashes++;
                Debug.Log($"PropelXR Time Rush crash {timeRushCrashes}/{maxTimeRushCrashes}", this);
                if (timeRushCrashes >= maxTimeRushCrashes)
                {
                    FinishMission(false, true);
                    return;
                }
            }
            previousCrashed = isCrashed;
            timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
            RefreshHUD();
            if (timeRemaining <= 0f) FinishMission(false);
        }
#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ToggleMenu();
#endif
    }

    private void OnMenuAction(InputAction.CallbackContext context) => ToggleMenu();

    public void ToggleMenu()
    {
        switch (Screen)
        {
            case ScreenState.Playing: PauseGame(); break;
            case ScreenState.Paused: ResumeGame(); break;
            case ScreenState.MissionSelect: ShowMainMenu(); break;
            case ScreenState.Welcome: ShowMainMenu(); break;
            case ScreenState.Results: ShowMainMenu(); break;
        }
    }

    public void ShowWelcome()
    {
        Time.timeScale = 1f;
        Screen = ScreenState.Welcome;
        Mode = PlayMode.None;
        inputReader.enabled = false;
        if (coinField != null) coinField.ClearMission();
        dronePresence.HideInMenu();
        if (missionUI != null) { missionUI.HideHUD(); missionUI.HideResults(); }
        menuUI.ShowWelcome();
    }

    public void ShowMainMenu()
    {
        Time.timeScale = 1f;
        inputReader.enabled = false;
        if (coinField != null) coinField.ClearMission();
        dronePresence.HideInMenu();
        if (missionUI != null) { missionUI.HideHUD(); missionUI.HideResults(); }
        Mode = PlayMode.None;
        Screen = ScreenState.MainMenu;
        menuUI.ShowMainMenu();
    }

    public void ShowMissionSelect()
    {
        if (Screen != ScreenState.MainMenu) return;
        Screen = ScreenState.MissionSelect;
        menuUI.ShowMissionSelect();
    }

    public void StartFreeFlight() => StartMode(PlayMode.FreeFlight);
    public void StartCoinCollector() => StartMode(PlayMode.CoinCollector);
    public void StartTimeRush() => StartMode(PlayMode.TimeRush);

    private void StartMode(PlayMode selected)
    {
        if (missionUI != null) { missionUI.HideResults(); missionUI.HideHUD(); }
        if (!flightController.ReadyForGameplay)
        {
            menuUI.ShowMessage("ROOM PREPARING", "The drone has not finished safe placement.");
            return;
        }
        if (coinField != null) coinField.ClearMission();
        dronePresence.ShowForGameplay();
        Time.timeScale = 1f;
        if (selected != PlayMode.FreeFlight)
        {
            if (coinField == null || !coinField.BeginMission())
            {
                dronePresence.HideInMenu();
                Screen = ScreenState.MissionSelect;
                Mode = PlayMode.None;
                menuUI.ShowMessage("NO SAFE TARGETS", "No clear room positions were found for coins. Check MRUK room data and obstacle layers, then return to mission selection.");
                return;
            }
        }
        Mode = selected;
        score = 0;
        missionCollected = 0;
        timeRemaining = timeRushSeconds;
        targetCoins = coinField != null ? coinField.DesiredCoinCount : 0;
        timeRushCrashes = 0;
        previousCrashed = flightController != null && flightController.IsCrashed;
        Screen = ScreenState.Playing;
        menuUI.Hide();
        inputReader.enabled = true;
        RefreshHUD();
        Debug.Log("PropelXR mode started: " + selected, this);
    }

    public void PauseGame()
    {
        if (Screen != ScreenState.Playing) return;
        Screen = ScreenState.Paused;
        inputReader.enabled = false;
        if (coinField != null) coinField.SetPaused(true);
        Time.timeScale = 0f;
        if (missionUI != null) missionUI.HideHUD();
        menuUI.ShowPause(Mode);
    }

    public void ResumeGame()
    {
        if (Screen != ScreenState.Paused) return;
        Time.timeScale = 1f;
        if (coinField != null) coinField.SetPaused(false);
        Screen = ScreenState.Playing;
        menuUI.Hide();
        inputReader.enabled = true;
        RefreshHUD();
    }

    private void HandleCoinCollected(int collected, int remaining)
    {
        if (Screen != ScreenState.Playing || Mode == PlayMode.FreeFlight) return;
        score += pointsPerCoin;
        missionCollected++;
        if (remaining == 0 && coinField != null)
        {
            if (Mode == PlayMode.CoinCollector)
            {
                if (missionCollected >= targetCoins)
                {
                    FinishMission(true);
                    return;
                }
                // Small real rooms may fit only two or three coins at once.
                // Refill safely until five total have been collected.
                if (!coinField.BeginMission())
                    Debug.LogWarning("PropelXR Coin Collector: no new safe positions found.", this);
            }
            else if (Mode == PlayMode.TimeRush)
            {
                if (!coinField.BeginMission())
                    Debug.LogWarning("PropelXR Time Rush: no safe positions for the next batch.", this);
            }
        }
        RefreshHUD();
    }

    private void RefreshHUD()
    {
        if (missionUI == null || Screen != ScreenState.Playing) return;
        if (Mode == PlayMode.FreeFlight) { missionUI.HideHUD(); return; }
        missionUI.ShowHUD(Mode == PlayMode.TimeRush ? "TIME RUSH" : "COIN COLLECTOR",
            score, missionCollected,
            targetCoins, timeRemaining, Mode == PlayMode.TimeRush,
            Mathf.Max(0, maxTimeRushCrashes - timeRushCrashes));
    }

    private void FinishMission(bool completed, bool outOfLives = false)
    {
        if (Screen != ScreenState.Playing) return;
        Screen = ScreenState.Results;
        inputReader.enabled = false;
        if (coinField != null) coinField.SetPaused(true);
        Time.timeScale = 0f;
        if (missionUI != null)
            missionUI.ShowResults(Mode == PlayMode.TimeRush, score,
                missionCollected,
                targetCoins, completed, RetryMission, ShowMainMenu, outOfLives);
        else
            menuUI.ShowMessage(completed ? "MISSION COMPLETE" : (outOfLives ? "GAME OVER" : "TIME'S UP"), $"Score: {score}");
    }

    public void RetryMission()
    {
        if (Screen != ScreenState.Results) return;
        PlayMode previous = Mode;
        Time.timeScale = 1f;
        if (missionUI != null) { missionUI.HideResults(); missionUI.HideHUD(); }
        inputReader.enabled = false;
        if (coinField != null) coinField.ClearMission();
        dronePresence.HideInMenu();
        StartMode(previous);
    }

    public void QuitApp()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        Debug.Log("PropelXR: Quit selected (no effect in Editor).", this);
#else
        Application.Quit();
#endif
    }
}
