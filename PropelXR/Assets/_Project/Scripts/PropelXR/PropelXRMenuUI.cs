
// using System;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
// #if UNITY_EDITOR
// using UnityEditor;
// #endif

// /// <summary>
// /// World-space Canvas UI controller for PropelXR.
// /// Supports one-time Editor hierarchy generation so all UI elements remain in the scene
// /// for manual tweaking, while preserving runtime state switching and button bindings.
// /// </summary>
// public class PropelXRMenuUI : MonoBehaviour
// {
//     [Header("Core References")]
//     [SerializeField] private Canvas worldCanvas;
//     [SerializeField] private Transform headset;
//     [SerializeField] private Sprite onboardingArtwork;
//     [SerializeField] private TMP_FontAsset customFont; // Optional: Assign your geometric/pixel/modern TMP font here

//     [Header("Audio")]
//     [SerializeField] private PropelXRAudioManager audioManager;

//     [Header("Dimensions")]
//     [SerializeField] private Vector2 onboardingSize = new Vector2(960f, 560f);
//     [SerializeField] private Vector2 standardMenuSize = new Vector2(540f, 560f);
//     [SerializeField] private float leftColumnWidth = 430f;

//     [Header("Head-Follow Settings")]
//     [SerializeField, Min(0.4f)] private float distanceMeters = 1.4f;
//     [SerializeField] private float downwardOffset = 0.08f;
//     [SerializeField] private bool followHeadset = true;
//     [SerializeField, Min(0f)] private float positionFollowSpeed = 12f;
//     [SerializeField, Min(0f)] private float rotationFollowSpeed = 12f;
//     [SerializeField] private bool followVerticalGaze = false;

//     [Header("Generated Hierarchy References (Editable in Scene)")]
//     [SerializeField] private RectTransform cardRoot;
//     [SerializeField] private RectTransform leftColumn;
//     [SerializeField] private RectTransform rightArtColumn;
//     [SerializeField] private Image onboardingArtImage;
//     [SerializeField] private GameObject artPlaceholderGroup;

//     [Header("Screen Panels")]
//     [SerializeField] private GameObject welcomePanel;
//     [SerializeField] private GameObject mainMenuPanel;
//     [SerializeField] private GameObject missionSelectPanel;
//     [SerializeField] private GameObject pausePanel;
//     [SerializeField] private GameObject messagePanel;
//     [SerializeField] private GameObject controlsPanel;

//     [Header("Dynamic Text Elements")]
//     [SerializeField] private TextMeshProUGUI pauseModeLabel;
//     [SerializeField] private TextMeshProUGUI messageTitleLabel;
//     [SerializeField] private TextMeshProUGUI messageBodyLabel;

//     [Header("Interactive Buttons")]
//     [SerializeField] private Button welcomeSkipButton;
//     [SerializeField] private Button freeFlightButton;
//     [SerializeField] private Button missionModeButton;
//     [SerializeField] private Button quitButton;
//     [SerializeField] private Button coinCollectorButton;
//     [SerializeField] private Button timeRushButton;
//     [SerializeField] private Button missionBackButton;
//     [SerializeField] private Button pauseResumeButton;
//     [SerializeField] private Button pauseMainMenuButton;
//     [SerializeField] private Button messageMainMenuButton;
//     [SerializeField] private Button mainControlsButton;
//     [SerializeField] private Button pauseControlsButton;
//     [SerializeField] private Button controlsBackButton;

//     private bool controlsOpenedFromPause;

//     private PropelXRGameFlow flow;
//     private bool needsSnap = true;

//     // Voxel Works Quest Inspired Palette: Deep Matte Charcoal & Crisp Stark White
//     private static readonly Color MatteBlackBg   = FromHex("#141416");
//     private static readonly Color CardSurface    = FromHex("#202024");
//     private static readonly Color CardPrimary    = FromHex("#2A2A30");
//     private static readonly Color AccentWhite    = FromHex("#FFFFFF");
//     private static readonly Color AccentCyan     = FromHex("#5CE1E6");
//     private static readonly Color TextWhite      = FromHex("#F5F5F7");
//     private static readonly Color TextMuted      = FromHex("#9A9AA2");
//     private static readonly Color ArtPlaceholder = FromHex("#75C7DD");

//     private static Color FromHex(string hex)
//     {
//         ColorUtility.TryParseHtmlString(hex, out var c);
//         return c;
//     }

//     // =========================================================================
//     // EDITOR GENERATION (Run once via Inspector Context Menu -> Edit manually!)
//     // =========================================================================

//     [ContextMenu("Generate UI Hierarchy (Editor Only)")]
//     public void GenerateUIInEditor()
//     {
//         if (worldCanvas == null) worldCanvas = GetComponent<Canvas>();
//         if (worldCanvas == null)
//         {
//             Debug.LogError("PropelXRMenuUI: Assign or attach a Canvas before generating UI.", this);
//             return;
//         }

//         SetupCanvasProperties();

//         // Remove old generated card if user explicitly triggers regeneration
//         if (cardRoot != null)
//         {
//             if (Application.isPlaying) Destroy(cardRoot.gameObject);
//             else DestroyImmediate(cardRoot.gameObject);
//         }

//         BuildFullHierarchy();

// #if UNITY_EDITOR
//         if (!Application.isPlaying)
//         {
//             EditorUtility.SetDirty(this);
//             EditorUtility.SetDirty(worldCanvas.gameObject);
//         }
// #endif
//         Debug.Log("PropelXRMenuUI: UI Hierarchy generated! You can now manually edit any child object in the Scene.", this);
//     }

//     // =========================================================================
//     // RUNTIME INITIALIZATION & FLOW
//     // =========================================================================

//     public void Initialize(PropelXRGameFlow owner, Transform cameraTransform)
//     {
//         flow = owner;
//         if (audioManager == null)
//             audioManager = FindFirstObjectByType<PropelXRAudioManager>();
//         if (headset == null) headset = cameraTransform;
//         if (worldCanvas == null) worldCanvas = GetComponent<Canvas>();

//         if (worldCanvas == null)
//         {
//             Debug.LogError("PropelXRMenuUI: A Canvas component is required.", this);
//             enabled = false;
//             return;
//         }

//         // Only build at runtime if you haven't generated it in the Editor yet
//         if (cardRoot == null)
//         {
//             SetupCanvasProperties();
//             BuildFullHierarchy();
//         }
//         else
//         {
//             RefreshArtworkSprite();
//         }

//         BindButtonEvents();
//         needsSnap = true;
//     }

//     private void SetupCanvasProperties()
//     {
//         worldCanvas.renderMode = RenderMode.WorldSpace;
//         RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
//         canvasRect.sizeDelta = onboardingSize;
//         canvasRect.localScale = Vector3.one * 0.001f;

//         if (worldCanvas.GetComponent<GraphicRaycaster>() == null)
//             worldCanvas.gameObject.AddComponent<GraphicRaycaster>();
//     }

//     private void BindButtonEvents()
//     {
//         if (flow == null) return;

//         BindButton(welcomeSkipButton, flow.ShowMainMenu);
//         BindButton(freeFlightButton, flow.StartFreeFlight);
//         BindButton(missionModeButton, flow.ShowMissionSelect);
//         BindButton(quitButton, flow.QuitApp);

//         BindButton(coinCollectorButton, flow.StartCoinCollector);
//         BindButton(timeRushButton, flow.StartTimeRush);
//         BindButton(missionBackButton, flow.ShowMainMenu);

//         BindButton(pauseResumeButton, flow.ResumeGame);
//         BindButton(pauseMainMenuButton, flow.ShowMainMenu);

//         BindButton(messageMainMenuButton, flow.ShowMainMenu);
//         BindButton(mainControlsButton, () => ShowControls(false));
//         BindButton(pauseControlsButton, () => ShowControls(true));
//         BindButton(controlsBackButton, CloseControls);
//     }

//     private void BindButton(Button btn, Action action)
//     {
//         if (btn == null) return;
//         btn.onClick.RemoveAllListeners();
//         btn.onClick.AddListener(() =>
//         {
//             if (audioManager != null) audioManager.PlayButtonClick();
//             action?.Invoke();
//         });
//     }

//     private void LateUpdate()
//     {
//         if (worldCanvas != null && worldCanvas.gameObject.activeInHierarchy && followHeadset)
//             UpdateHeadPosition(needsSnap);
//     }

//     private void UpdateHeadPosition(bool snap)
//     {
//         if (headset == null && Camera.main != null) headset = Camera.main.transform;
//         if (headset == null || worldCanvas == null) return;

//         Vector3 forward = followVerticalGaze
//             ? headset.forward
//             : Vector3.ProjectOnPlane(headset.forward, Vector3.up).normalized;
//         if (forward.sqrMagnitude < 0.0001f) forward = headset.forward;

//         Vector3 targetPosition = headset.position + forward * distanceMeters + Vector3.down * downwardOffset;
//         Quaternion targetRotation = Quaternion.LookRotation(forward, Vector3.up);
//         Transform t = worldCanvas.transform;

//         if (snap || positionFollowSpeed <= 0f || rotationFollowSpeed <= 0f)
//         {
//             t.SetPositionAndRotation(targetPosition, targetRotation);
//         }
//         else
//         {
//             float pt = 1f - Mathf.Exp(-positionFollowSpeed * Time.unscaledDeltaTime);
//             float rt = 1f - Mathf.Exp(-rotationFollowSpeed * Time.unscaledDeltaTime);
//             t.SetPositionAndRotation(
//                 Vector3.Lerp(t.position, targetPosition, pt),
//                 Quaternion.Slerp(t.rotation, targetRotation, rt));
//         }
//         needsSnap = false;
//     }

//     // =========================================================================
//     // SCREEN SWITCHING (Preserves Hierarchy Objects)
//     // =========================================================================

//     private void ActivateScreen(GameObject targetScreen, bool showOnboardingArt)
//     {
//         if (worldCanvas == null) return;

//         if (welcomePanel != null) welcomePanel.SetActive(targetScreen == welcomePanel);
//         if (mainMenuPanel != null) mainMenuPanel.SetActive(targetScreen == mainMenuPanel);
//         if (missionSelectPanel != null) missionSelectPanel.SetActive(targetScreen == missionSelectPanel);
//         if (pausePanel != null) pausePanel.SetActive(targetScreen == pausePanel);
//         if (messagePanel != null) messagePanel.SetActive(targetScreen == messagePanel);
//         if (controlsPanel != null) controlsPanel.SetActive(targetScreen == controlsPanel);

//         // Toggle Right Artwork Panel ONLY during Onboarding
//         if (rightArtColumn != null)
//             rightArtColumn.gameObject.SetActive(showOnboardingArt);

//         // Adjust overall card width depending on whether right-side art is shown
//         RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
//         if (canvasRect != null)
//             canvasRect.sizeDelta = showOnboardingArt ? onboardingSize : standardMenuSize;

//         if (leftColumn != null)
//         {
//             var le = leftColumn.GetComponent<LayoutElement>();
//             if (le != null)
//             {
//                 le.preferredWidth = showOnboardingArt ? leftColumnWidth : standardMenuSize.x;
//                 le.flexibleWidth = showOnboardingArt ? 0f : 1f;
//             }
//         }

//         worldCanvas.gameObject.SetActive(true);
//         needsSnap = true;
//         UpdateHeadPosition(true);
//     }

//     public void ShowWelcome()
//     {
//         RefreshArtworkSprite();
//         ActivateScreen(welcomePanel, showOnboardingArt: true);
//     }

//     public void ShowMainMenu()
//     {
//         ActivateScreen(mainMenuPanel, showOnboardingArt: false);
//     }

//     public void ShowMissionSelect()
//     {
//         ActivateScreen(missionSelectPanel, showOnboardingArt: false);
//     }

//     public void ShowPause(PropelXRGameFlow.PlayMode mode)
//     {
//         if (pauseModeLabel != null)
//             pauseModeLabel.text = mode == PropelXRGameFlow.PlayMode.FreeFlight ? "MODE // FREE FLIGHT" : "MODE // " + mode.ToString().ToUpper();

//         ActivateScreen(pausePanel, showOnboardingArt: false);
//     }

//     public void ShowControls(bool fromPause)
//     {
//         controlsOpenedFromPause = fromPause;
//         ActivateScreen(controlsPanel, showOnboardingArt: false);
//     }

//     public void CloseControls()
//     {
//         if (controlsOpenedFromPause)
//             ShowPause(flow != null ? flow.Mode : PropelXRGameFlow.PlayMode.FreeFlight);
//         else
//             ShowMainMenu();
//     }

//     public void ShowMessage(string title, string message)
//     {
//         if (messageTitleLabel != null) messageTitleLabel.text = title;
//         if (messageBodyLabel != null) messageBodyLabel.text = message;

//         ActivateScreen(messagePanel, showOnboardingArt: false);
//     }

//     public void Hide()
//     {
//         if (worldCanvas != null) worldCanvas.gameObject.SetActive(false);
//     }

//     // =========================================================================
//     // HIERARCHY BUILDER
//     // =========================================================================

//     private void BuildFullHierarchy()
//     {
//         cardRoot = MakeRect("PropelXR_MainCard", worldCanvas.transform);
//         SetStretch(cardRoot);
//         AddImage(cardRoot, MatteBlackBg);

//         var columns = cardRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
//         columns.padding = new RectOffset(0, 0, 0, 0);
//         columns.spacing = 0f;
//         columns.childControlWidth = true;
//         columns.childControlHeight = true;
//         columns.childForceExpandWidth = false;
//         columns.childForceExpandHeight = true;

//         // 1. Left Content Column (Holds all Screen Panels)
//         leftColumn = MakeRect("Left_ContentColumn", cardRoot);
//         AddImage(leftColumn, MatteBlackBg);
//         var leftLayout = leftColumn.gameObject.AddComponent<LayoutElement>();
//         leftLayout.preferredWidth = leftColumnWidth;
//         leftLayout.flexibleWidth = 0f;

//         // 2. Right Artwork Column (Only visible on Onboarding)
//         rightArtColumn = MakeRect("Right_OnboardingArtwork", cardRoot);
//         var rightLayout = rightArtColumn.gameObject.AddComponent<LayoutElement>();
//         rightLayout.preferredWidth = onboardingSize.x - leftColumnWidth;
//         rightLayout.flexibleWidth = 1f;
//         AddImage(rightArtColumn, ArtPlaceholder);
//         BuildArtworkPanel();

//         // 3. Build Individual Screen Panels inside Left Column
//         welcomePanel = BuildWelcomeScreen(leftColumn);
//         mainMenuPanel = BuildMainMenuScreen(leftColumn);
//         missionSelectPanel = BuildMissionSelectScreen(leftColumn);
//         pausePanel = BuildPauseScreen(leftColumn);
//         messagePanel = BuildMessageScreen(leftColumn);
//         controlsPanel = BuildControlsScreen(leftColumn);

//         // Default to showing Welcome in Editor preview
//         ActivateScreen(welcomePanel, showOnboardingArt: true);
//     }

//     private void BuildArtworkPanel()
//     {
//         // Placeholder graphics when no sprite is assigned yet
//         var placeholder = MakeRect("Placeholder_Graphics", rightArtColumn);
//         SetStretch(placeholder);
//         artPlaceholderGroup = placeholder.gameObject;

//         AddArtText(placeholder, "P", 190, FromHex("#E6FAFF"), new Vector2(0, 40), new Vector2(300, 240));
//         AddArtText(placeholder, "PROPEL XR // MIXED REALITY", 18, FromHex("#103B4A"), new Vector2(0, -190), new Vector2(420, 40));

//         // Actual Artwork Image (Full bleed right side like Voxel Works Quest)
//         var imageRect = MakeRect("Onboarding_SpriteImage", rightArtColumn);
//         SetStretch(imageRect);
//         onboardingArtImage = AddImage(imageRect, Color.white);
//         onboardingArtImage.preserveAspect = false;

//         RefreshArtworkSprite();
//     }

//     private void RefreshArtworkSprite()
//     {
//         if (onboardingArtImage == null) return;
//         onboardingArtImage.sprite = onboardingArtwork;
//         bool hasArt = onboardingArtwork != null;
//         onboardingArtImage.color = hasArt ? Color.white : Color.clear;
//         if (artPlaceholderGroup != null)
//             artPlaceholderGroup.SetActive(!hasArt);
//     }

//     private GameObject BuildWelcomeScreen(Transform parent)
//     {
//         RectTransform panel = CreateScreenContainer("Screen_01_Welcome", parent, new RectOffset(44, 40, 48, 40), 10f);

//         // Voxel Works Quest style stacked Title + Version + Body paragraphs
//         AddLabel(panel, "Title", "PropelXR\nQuest", 54, TextWhite, 132f, FontStyles.Bold, lineSpacing: -12f);
//         AddLabel(panel, "Version", "0.1.0 // EARLY PROTOTYPE", 18, TextWhite, 34f, FontStyles.Normal);
//         AddSpacer(panel, 10f);

//         AddLabel(panel, "Intro_Header", "PropelXR Mixed Reality\nWelcome!", 19, TextWhite, 52f, FontStyles.Bold);
//         AddLabel(panel, "Body_1",
//             "In this prototype you can pilot a virtual drone directly inside your physical room. All spatial mechanics and flight modes are experimental and will evolve in future updates.",
//             17, TextMuted, 95f, FontStyles.Normal);
//         AddLabel(panel, "Body_2",
//             "Ensure your room boundary is clear before arming the motors. Press B at any time to toggle the flight menu.",
//             16, TextMuted, 64f, FontStyles.Normal);

//         AddFlexibleSpacer(panel);
//         welcomeSkipButton = AddStyledButton(panel, "Btn_SkipWelcome", "CONTINUE TO MENU", "Or wait to advance automatically", true, 58f);

//         return panel.gameObject;
//     }

//     private GameObject BuildMainMenuScreen(Transform parent)
//     {
//         RectTransform panel = CreateScreenContainer("Screen_02_MainMenu", parent, new RectOffset(44, 44, 32, 28), 7f);

//         AddLabel(panel, "Header_Tag", "FLIGHT DECK // 0.1.0", 15, AccentCyan, 22f, FontStyles.Bold);
//         AddLabel(panel, "Title", "PropelXR", 52, TextWhite, 64f, FontStyles.Bold);
//         AddLabel(panel, "Subtitle", "Select your mixed reality flight mode.", 18, TextMuted, 32f);
//         AddSpacer(panel, 8f);

//         freeFlightButton = AddStyledButton(panel, "Btn_FreeFlight", "FREE FLIGHT", "Fly and practice freely inside your room", true, 72f);
//         missionModeButton = AddStyledButton(panel, "Btn_Missions", "MISSION MODE", "Coin Collector & Time Rush challenges", false, 72f);
//         AddFlexibleSpacer(panel);
//         mainControlsButton = AddStyledButton(panel, "Btn_Controls", "CONTROLS", "Quick controller reference", false, 58f);
//         quitButton = AddStyledButton(panel, "Btn_Quit", "QUIT", "Exit PropelXR to Home", false, 58f);

//         return panel.gameObject;
//     }

//     private GameObject BuildMissionSelectScreen(Transform parent)
//     {
//         RectTransform panel = CreateScreenContainer("Screen_03_MissionSelect", parent, new RectOffset(44, 44, 44, 40), 12f);

//         AddLabel(panel, "Header_Tag", "CHALLENGE PROTOCOLS", 15, AccentCyan, 22f, FontStyles.Bold);
//         AddLabel(panel, "Title", "Missions", 52, TextWhite, 64f, FontStyles.Bold);
//         AddLabel(panel, "Subtitle", "Test your spatial piloting precision.", 18, TextMuted, 32f);
//         AddSpacer(panel, 8f);

//         coinCollectorButton = AddStyledButton(panel, "Btn_CoinCollector", "COIN COLLECTOR", "Collect spatial targets at your own pace", true);
//         timeRushButton = AddStyledButton(panel, "Btn_TimeRush", "TIME RUSH", "Race against the clock through your room", false);
//         AddFlexibleSpacer(panel);
//         missionBackButton = AddStyledButton(panel, "Btn_Back", "BACK", "Return to main menu", false, 72f);

//         return panel.gameObject;
//     }

//     private GameObject BuildPauseScreen(Transform parent)
//     {
//         RectTransform panel = CreateScreenContainer("Screen_04_Pause", parent, new RectOffset(44, 44, 48, 40), 12f);

//         pauseModeLabel = AddLabel(panel, "Mode_Tag", "MODE // FREE FLIGHT", 15, AccentCyan, 24f, FontStyles.Bold);
//         AddLabel(panel, "Title", "Paused", 52, TextWhite, 64f, FontStyles.Bold);
//         AddLabel(panel, "Subtitle", "Drone physics suspended. Press B or select below to continue.", 18, TextMuted, 48f);
//         AddSpacer(panel, 16f);

//         pauseResumeButton = AddStyledButton(panel, "Btn_Resume", "RESUME FLIGHT", "Continue from current position", true);
//         pauseMainMenuButton = AddStyledButton(panel, "Btn_MainMenu", "MAIN MENU", "Disarm drone and return home", false);
//         AddFlexibleSpacer(panel);
//         pauseControlsButton = AddStyledButton(panel, "Btn_Controls", "CONTROLS", "Quick controller reference", false, 58f);

//         return panel.gameObject;
//     }

//     private GameObject BuildMessageScreen(Transform parent)
//     {
//         RectTransform panel = CreateScreenContainer("Screen_05_Message", parent, new RectOffset(44, 44, 48, 40), 14f);

//         AddLabel(panel, "Header_Tag", "SYSTEM NOTICE", 15, AccentCyan, 24f, FontStyles.Bold);
//         messageTitleLabel = AddLabel(panel, "Message_Title", "ROOM PREPARING", 44, TextWhite, 60f, FontStyles.Bold);
//         messageBodyLabel = AddLabel(panel, "Message_Body", "Drone initialization is not finished. Try again when the room is ready.", 19, TextMuted, 140f);
//         AddFlexibleSpacer(panel);
//         messageMainMenuButton = AddStyledButton(panel, "Btn_MessageBack", "BACK TO MENU", "Return to main screen", true);

//         return panel.gameObject;
//     }

//     private GameObject BuildControlsScreen(Transform parent)
//     {
//         RectTransform panel = CreateScreenContainer("Screen_06_Controls", parent,
//             new RectOffset(34, 34, 26, 24), 7f);
//         AddLabel(panel, "Tag", "QUICK REFERENCE", 14, AccentCyan, 21f, FontStyles.Bold);
//         AddLabel(panel, "Title", "Controls", 43, TextWhite, 54f, FontStyles.Bold);
//         AddSpacer(panel, 3f);
//         AddControlRow(panel, "L STICK", "Altitude  /  Yaw");
//         AddControlRow(panel, "R STICK", "Move  /  Direction");
//         AddControlRow(panel, "X", "Arm drone");
//         AddControlRow(panel, "A", "Disarm");
//         AddControlRow(panel, "Y", "Reset drone");
//         AddControlRow(panel, "B", "Pause  /  Resume");
//         AddLabel(panel, "Trigger_Hint", "TRIGGER   Select menu buttons", 15, TextMuted, 29f);
//         AddFlexibleSpacer(panel);
//         controlsBackButton = AddStyledButton(panel, "Btn_ControlsBack", "BACK", "Return to previous screen", true, 58f);
//         return panel.gameObject;
//     }

//     private void AddControlRow(Transform parent, string key, string description)
//     {
//         RectTransform row = MakeRect("Control_" + key.Replace(" ", "_"), parent);
//         AddImage(row, CardSurface);
//         row.gameObject.AddComponent<LayoutElement>().preferredHeight = 41f;
//         var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
//         h.padding = new RectOffset(12, 10, 5, 3);
//         h.spacing = 12f;
//         h.childControlWidth = true;
//         h.childControlHeight = true;
//         h.childForceExpandWidth = false;
//         h.childForceExpandHeight = true;
//         var chip = AddLabel(row, "Key", key, 17, AccentCyan, 32f, FontStyles.Bold);
//         var chipLayout = chip.GetComponent<LayoutElement>();
//         chipLayout.preferredWidth = 98f;
//         chipLayout.flexibleWidth = 0f;
//         var detail = AddLabel(row, "Action", description, 17, TextWhite, 32f);
//         detail.GetComponent<LayoutElement>().flexibleWidth = 1f;
//     }

//     // =========================================================================
//     // UI ELEMENT HELPERS
//     // =========================================================================

//     private static RectTransform CreateScreenContainer(string name, Transform parent, RectOffset padding, float spacing)
//     {
//         RectTransform rect = MakeRect(name, parent);
//         SetStretch(rect);
//         var vlg = rect.gameObject.AddComponent<VerticalLayoutGroup>();
//         vlg.padding = padding;
//         vlg.spacing = spacing;
//         vlg.childAlignment = TextAnchor.UpperLeft;
//         vlg.childControlWidth = true;
//         vlg.childControlHeight = true;
//         vlg.childForceExpandWidth = true;
//         vlg.childForceExpandHeight = false;
//         return rect;
//     }

//     private TextMeshProUGUI AddLabel(Transform parent, string objName, string text, int fontSize, Color color, float preferredHeight, FontStyles style = FontStyles.Normal, float lineSpacing = 0f)
//     {
//         RectTransform r = MakeRect(objName, parent);
//         var label = r.gameObject.AddComponent<TextMeshProUGUI>();
//         if (customFont != null) label.font = customFont;
//         label.raycastTarget = false;
//         label.text = text;
//         label.fontSize = fontSize;
//         label.color = color;
//         label.fontStyle = style;
//         label.lineSpacing = lineSpacing;
//         label.alignment = TextAlignmentOptions.TopLeft;
//         label.enableWordWrapping = true;

//         var layout = r.gameObject.AddComponent<LayoutElement>();
//         layout.preferredHeight = preferredHeight;
//         return label;
//     }

//     private Button AddStyledButton(Transform parent, string objName, string title, string subtitle, bool primary, float height = 84f)
//     {
//         RectTransform rect = MakeRect(objName, parent);
//         Image bg = AddImage(rect, primary ? CardPrimary : CardSurface, true);

//         Button btn = rect.gameObject.AddComponent<Button>();
//         btn.targetGraphic = bg;
//         ColorBlock cb = btn.colors;
//         cb.normalColor = Color.white;
//         cb.highlightedColor = FromHex("#D8F8FF");
//         cb.pressedColor = FromHex("#9CE5F0");
//         cb.selectedColor = cb.highlightedColor;
//         btn.colors = cb;

//         var layout = rect.gameObject.AddComponent<LayoutElement>();
//         layout.preferredHeight = height;

//         // Clean Left Accent Bar (White/Cyan minimal strip)
//         RectTransform accent = MakeRect("AccentBar", rect);
//         accent.anchorMin = new Vector2(0f, 0f);
//         accent.anchorMax = new Vector2(0f, 1f);
//         accent.pivot = new Vector2(0f, 0.5f);
//         accent.sizeDelta = new Vector2(5f, 0f);
//         accent.anchoredPosition = Vector2.zero;
//         AddImage(accent, primary ? AccentCyan : AccentWhite, false);

//         // Button Label
//         RectTransform labelRect = MakeRect("Label", rect);
//         SetStretch(labelRect);
//         labelRect.offsetMin = new Vector2(22f, 6f);
//         labelRect.offsetMax = new Vector2(-16f, -6f);

//         var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
//         if (customFont != null) label.font = customFont;
//         label.raycastTarget = false;
//         label.text = $"<b>{title}</b>\n<size=16><color=#9A9AA2>{subtitle}</color></size>";
//         label.fontSize = 23;
//         label.color = TextWhite;
//         label.alignment = TextAlignmentOptions.MidlineLeft;

//         return btn;
//     }

//     private void AddArtText(Transform parent, string value, int size, Color color, Vector2 pos, Vector2 dimensions)
//     {
//         var rect = MakeRect("Artwork_Label", parent);
//         rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
//         rect.sizeDelta = dimensions;
//         rect.anchoredPosition = pos;
//         var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
//         if (customFont != null) label.font = customFont;
//         label.raycastTarget = false;
//         label.text = value;
//         label.fontSize = size;
//         label.fontStyle = FontStyles.Bold;
//         label.color = color;
//         label.alignment = TextAlignmentOptions.Center;
//     }

//     private static void AddSpacer(Transform parent, float height)
//     {
//         var r = MakeRect("Spacer", parent);
//         r.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
//     }

//     private static void AddFlexibleSpacer(Transform parent)
//     {
//         var r = MakeRect("FlexSpacer", parent);
//         r.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
//     }

//     private static RectTransform MakeRect(string objectName, Transform parent)
//     {
//         var go = new GameObject(objectName, typeof(RectTransform));
//         go.transform.SetParent(parent, false);
//         return go.GetComponent<RectTransform>();
//     }

//     private static void SetStretch(RectTransform rect)
//     {
//         rect.anchorMin = Vector2.zero;
//         rect.anchorMax = Vector2.one;
//         rect.offsetMin = Vector2.zero;
//         rect.offsetMax = Vector2.zero;
//     }

//     private static Image AddImage(RectTransform rect, Color color, bool raycast = false)
//     {
//         Image img = rect.gameObject.AddComponent<Image>();
//         img.color = color;
//         img.raycastTarget = raycast;
//         return img;
//     }
// }






using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// World-space Canvas UI controller for PropelXR.
/// Styled with an Apple visionOS-inspired glassmorphism aesthetic, procedural rounded corners,
/// specular borders, and concise spatial typography.
/// </summary>
public class PropelXRMenuUI : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Transform headset;
    [SerializeField] private Sprite onboardingArtwork;
    [SerializeField] private TMP_FontAsset customFont;

    [Header("Audio")]
    [SerializeField] private PropelXRAudioManager audioManager;

    [Header("Dimensions")]
    [SerializeField] private Vector2 onboardingSize = new Vector2(920f, 540f);
    [SerializeField] private Vector2 standardMenuSize = new Vector2(480f, 540f);
    [SerializeField] private float leftColumnWidth = 440f;

    [Header("Head-Follow Settings")]
    [SerializeField, Min(0.4f)] private float distanceMeters = 1.4f;
    [SerializeField] private float downwardOffset = 0.08f;
    [SerializeField] private bool followHeadset = true;
    [SerializeField, Min(0f)] private float positionFollowSpeed = 12f;
    [SerializeField, Min(0f)] private float rotationFollowSpeed = 12f;
    [SerializeField] private bool followVerticalGaze = false;

    [Header("Generated Hierarchy References (Editable in Scene)")]
    [SerializeField] private RectTransform cardRoot;
    [SerializeField] private RectTransform leftColumn;
    [SerializeField] private RectTransform rightArtColumn;
    [SerializeField] private Image onboardingArtImage;
    [SerializeField] private GameObject artPlaceholderGroup;

    [Header("Screen Panels")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject missionSelectPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private GameObject controlsPanel;

    [Header("Dynamic Text Elements")]
    [SerializeField] private TextMeshProUGUI pauseModeLabel;
    [SerializeField] private TextMeshProUGUI messageTitleLabel;
    [SerializeField] private TextMeshProUGUI messageBodyLabel;

    [Header("Interactive Buttons")]
    [SerializeField] private Button welcomeSkipButton;
    [SerializeField] private Button freeFlightButton;
    [SerializeField] private Button missionModeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button coinCollectorButton;
    [SerializeField] private Button timeRushButton;
    [SerializeField] private Button missionBackButton;
    [SerializeField] private Button pauseResumeButton;
    [SerializeField] private Button pauseMainMenuButton;
    [SerializeField] private Button messageMainMenuButton;
    [SerializeField] private Button mainControlsButton;
    [SerializeField] private Button pauseControlsButton;
    [SerializeField] private Button controlsBackButton;

    private bool controlsOpenedFromPause;
    private PropelXRGameFlow flow;
    private bool needsSnap = true;

    // =========================================================================
    // APPLE VISION PRO / SPATIAL GLASS PALETTE
    // =========================================================================
    private static readonly Color GlassWindowBg     = new Color(0.09f, 0.09f, 0.11f, 0.84f); // Deep Smoked Glass
    private static readonly Color GlassBorder       = new Color(1.00f, 1.00f, 1.00f, 0.14f); // Specular Rim
    private static readonly Color CardGlass         = new Color(1.00f, 1.00f, 1.00f, 0.07f); // Frosted Secondary Card
    private static readonly Color CardGlassBorder   = new Color(1.00f, 1.00f, 1.00f, 0.10f);
    private static readonly Color PrimaryBlue       = new Color(0.04f, 0.52f, 1.00f, 0.90f); // Apple System Blue (#0A84FF)
    private static readonly Color PrimaryBlueBorder = new Color(0.45f, 0.75f, 1.00f, 0.35f);
    private static readonly Color PillBg            = new Color(1.00f, 1.00f, 1.00f, 0.11f);
    private static readonly Color ArtGlassBg        = new Color(0.13f, 0.15f, 0.19f, 0.90f);

    private static readonly Color TextPrimary       = new Color(0.97f, 0.97f, 0.99f, 1.00f); // #F7F7FC
    private static readonly Color TextSecondary     = new Color(0.92f, 0.92f, 0.96f, 0.62f); // Apple Secondary Label
    private static readonly Color TextTertiary      = new Color(0.92f, 0.92f, 0.96f, 0.38f); // Subtle Eyebrow / Hint

    // Cached Procedural 9-Sliced Rounded Sprites
    private static Sprite s_WindowFillSprite;
    private static Sprite s_WindowBorderSprite;
    private static Sprite s_CardFillSprite;
    private static Sprite s_CardBorderSprite;
    private static Sprite s_PillFillSprite;

    private static void EnsureRoundedSprites()
    {
        if (s_WindowFillSprite == null)   s_WindowFillSprite   = CreateRoundedSprite(128, 28f, 0f);
        if (s_WindowBorderSprite == null) s_WindowBorderSprite = CreateRoundedSprite(128, 28f, 1.5f);
        if (s_CardFillSprite == null)     s_CardFillSprite     = CreateRoundedSprite(96, 16f, 0f);
        if (s_CardBorderSprite == null)   s_CardBorderSprite   = CreateRoundedSprite(96, 16f, 1.2f);
        if (s_PillFillSprite == null)     s_PillFillSprite     = CreateRoundedSprite(64, 12f, 0f);
    }

    /// <summary>
    /// Generates a smooth, anti-aliased 9-sliced rounded rectangle or border sprite in memory.
    /// </summary>
    private static Sprite CreateRoundedSprite(int size, float radius, float borderThickness)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        Vector2 half = new Vector2(size * 0.5f, size * 0.5f);
        Vector2 b = half - new Vector2(radius + 1f, radius + 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - b;
                float dist = Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;

                float outerAlpha = Mathf.Clamp01(0.5f - dist);
                float alpha = outerAlpha;

                if (borderThickness > 0f)
                {
                    float innerAlpha = Mathf.Clamp01(0.5f - (dist + borderThickness));
                    alpha = Mathf.Clamp01(outerAlpha - innerAlpha);
                }

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        float slice = Mathf.Ceil(radius + 2f);
        Sprite sp = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(slice, slice, slice, slice));
        sp.name = borderThickness > 0f ? $"VisionOS_Border_{radius}" : $"VisionOS_Rounded_{radius}";
        return sp;
    }

    // =========================================================================
    // EDITOR GENERATION
    // =========================================================================

    [ContextMenu("Generate UI Hierarchy (Editor Only)")]
    public void GenerateUIInEditor()
    {
        if (worldCanvas == null) worldCanvas = GetComponent<Canvas>();
        if (worldCanvas == null)
        {
            Debug.LogError("PropelXRMenuUI: Assign or attach a Canvas before generating UI.", this);
            return;
        }

        SetupCanvasProperties();

        if (cardRoot != null)
        {
            if (Application.isPlaying) Destroy(cardRoot.gameObject);
            else DestroyImmediate(cardRoot.gameObject);
        }

        BuildFullHierarchy();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(worldCanvas.gameObject);
        }
#endif
        Debug.Log("PropelXRMenuUI: visionOS Spatial Glass UI generated! You can inspect or edit elements in the Hierarchy.", this);
    }

    // =========================================================================
    // RUNTIME INITIALIZATION & FLOW
    // =========================================================================

    public void Initialize(PropelXRGameFlow owner, Transform cameraTransform)
    {
        flow = owner;
        if (audioManager == null)
            audioManager = FindFirstObjectByType<PropelXRAudioManager>();
        if (headset == null) headset = cameraTransform;
        if (worldCanvas == null) worldCanvas = GetComponent<Canvas>();

        if (worldCanvas == null)
        {
            Debug.LogError("PropelXRMenuUI: A Canvas component is required.", this);
            enabled = false;
            return;
        }

        EnsureRoundedSprites();

        if (cardRoot == null)
        {
            SetupCanvasProperties();
            BuildFullHierarchy();
        }
        else
        {
            ReapplyProceduralSpritesToExistingHierarchy();
            RefreshArtworkSprite();
        }

        BindButtonEvents();
        needsSnap = true;
    }

    /// <summary>
    /// Ensures procedural rounded sprites remain intact when entering Play mode
    /// on a pre-generated Editor hierarchy.
    /// </summary>
    private void ReapplyProceduralSpritesToExistingHierarchy()
    {
        EnsureRoundedSprites();
        if (cardRoot == null) return;

        foreach (Image img in cardRoot.GetComponentsInChildren<Image>(true))
        {
            if (img == onboardingArtImage) continue;
            string n = img.gameObject.name;

            if (n == "PropelXR_MainCard" || n == "Right_ArtworkContainer")
            {
                img.sprite = s_WindowFillSprite;
                img.type = Image.Type.Sliced;
            }
            else if (n == "GlassRimBorder")
            {
                img.sprite = s_WindowBorderSprite;
                img.type = Image.Type.Sliced;
            }
            else if (n == "ButtonBorder")
            {
                img.sprite = s_CardBorderSprite;
                img.type = Image.Type.Sliced;
            }
            else if (n.StartsWith("Btn_") || n.StartsWith("Control_"))
            {
                img.sprite = s_CardFillSprite;
                img.type = Image.Type.Sliced;
            }
            else if (n.Contains("Pill"))
            {
                img.sprite = s_PillFillSprite;
                img.type = Image.Type.Sliced;
            }
        }
    }

    private void SetupCanvasProperties()
    {
        worldCanvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = onboardingSize;
        canvasRect.localScale = Vector3.one * 0.001f;

        if (worldCanvas.GetComponent<GraphicRaycaster>() == null)
            worldCanvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    private void BindButtonEvents()
    {
        if (flow == null) return;

        BindButton(welcomeSkipButton, flow.ShowMainMenu);
        BindButton(freeFlightButton, flow.StartFreeFlight);
        BindButton(missionModeButton, flow.ShowMissionSelect);
        BindButton(quitButton, flow.QuitApp);

        BindButton(coinCollectorButton, flow.StartCoinCollector);
        BindButton(timeRushButton, flow.StartTimeRush);
        BindButton(missionBackButton, flow.ShowMainMenu);

        BindButton(pauseResumeButton, flow.ResumeGame);
        BindButton(pauseMainMenuButton, flow.ShowMainMenu);

        BindButton(messageMainMenuButton, flow.ShowMainMenu);
        BindButton(mainControlsButton, () => ShowControls(false));
        BindButton(pauseControlsButton, () => ShowControls(true));
        BindButton(controlsBackButton, CloseControls);
    }

    private void BindButton(Button btn, Action action)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            if (audioManager != null) audioManager.PlayButtonClick();
            action?.Invoke();
        });
    }

    private void LateUpdate()
    {
        if (worldCanvas != null && worldCanvas.gameObject.activeInHierarchy && followHeadset)
            UpdateHeadPosition(needsSnap);
    }

    private void UpdateHeadPosition(bool snap)
    {
        if (headset == null && Camera.main != null) headset = Camera.main.transform;
        if (headset == null || worldCanvas == null) return;

        Vector3 forward = followVerticalGaze
            ? headset.forward
            : Vector3.ProjectOnPlane(headset.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.0001f) forward = headset.forward;

        Vector3 targetPosition = headset.position + forward * distanceMeters + Vector3.down * downwardOffset;
        Quaternion targetRotation = Quaternion.LookRotation(forward, Vector3.up);
        Transform t = worldCanvas.transform;

        if (snap || positionFollowSpeed <= 0f || rotationFollowSpeed <= 0f)
        {
            t.SetPositionAndRotation(targetPosition, targetRotation);
        }
        else
        {
            float pt = 1f - Mathf.Exp(-positionFollowSpeed * Time.unscaledDeltaTime);
            float rt = 1f - Mathf.Exp(-rotationFollowSpeed * Time.unscaledDeltaTime);
            t.SetPositionAndRotation(
                Vector3.Lerp(t.position, targetPosition, pt),
                Quaternion.Slerp(t.rotation, targetRotation, rt));
        }
        needsSnap = false;
    }

    // =========================================================================
    // SCREEN SWITCHING
    // =========================================================================

    private void ActivateScreen(GameObject targetScreen, bool showOnboardingArt)
    {
        if (worldCanvas == null) return;

        if (welcomePanel != null) welcomePanel.SetActive(targetScreen == welcomePanel);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(targetScreen == mainMenuPanel);
        if (missionSelectPanel != null) missionSelectPanel.SetActive(targetScreen == missionSelectPanel);
        if (pausePanel != null) pausePanel.SetActive(targetScreen == pausePanel);
        if (messagePanel != null) messagePanel.SetActive(targetScreen == messagePanel);
        if (controlsPanel != null) controlsPanel.SetActive(targetScreen == controlsPanel);

        if (rightArtColumn != null)
            rightArtColumn.gameObject.SetActive(showOnboardingArt);

        RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
        if (canvasRect != null)
            canvasRect.sizeDelta = showOnboardingArt ? onboardingSize : standardMenuSize;

        if (leftColumn != null)
        {
            var le = leftColumn.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth = showOnboardingArt ? leftColumnWidth : standardMenuSize.x;
                le.flexibleWidth = showOnboardingArt ? 0f : 1f;
            }
        }

        worldCanvas.gameObject.SetActive(true);
        needsSnap = true;
        UpdateHeadPosition(true);
    }

    public void ShowWelcome()
    {
        RefreshArtworkSprite();
        ActivateScreen(welcomePanel, showOnboardingArt: true);
    }

    public void ShowMainMenu()
    {
        ActivateScreen(mainMenuPanel, showOnboardingArt: false);
    }

    public void ShowMissionSelect()
    {
        ActivateScreen(missionSelectPanel, showOnboardingArt: false);
    }

    public void ShowPause(PropelXRGameFlow.PlayMode mode)
    {
        if (pauseModeLabel != null)
        {
            string modeName = mode == PropelXRGameFlow.PlayMode.FreeFlight ? "FREE FLIGHT" :
                              mode == PropelXRGameFlow.PlayMode.CoinCollector ? "COIN COLLECTOR" :
                              mode == PropelXRGameFlow.PlayMode.TimeRush ? "TIME RUSH" : "ACTIVE SESSION";
            pauseModeLabel.text = modeName;
        }

        ActivateScreen(pausePanel, showOnboardingArt: false);
    }

    public void ShowControls(bool fromPause)
    {
        controlsOpenedFromPause = fromPause;
        ActivateScreen(controlsPanel, showOnboardingArt: false);
    }

    public void CloseControls()
    {
        if (controlsOpenedFromPause)
            ShowPause(flow != null ? flow.Mode : PropelXRGameFlow.PlayMode.FreeFlight);
        else
            ShowMainMenu();
    }

    public void ShowMessage(string title, string message)
    {
        if (messageTitleLabel != null) messageTitleLabel.text = title;
        if (messageBodyLabel != null) messageBodyLabel.text = message;

        ActivateScreen(messagePanel, showOnboardingArt: false);
    }

    public void Hide()
    {
        if (worldCanvas != null) worldCanvas.gameObject.SetActive(false);
    }

    // =========================================================================
    // VISION PRO / GLASSMORPHISM HIERARCHY BUILDER
    // =========================================================================

    private void BuildFullHierarchy()
    {
        EnsureRoundedSprites();

        cardRoot = MakeRect("PropelXR_MainCard", worldCanvas.transform);
        SetStretch(cardRoot);
        AddSlicedImage(cardRoot, s_WindowFillSprite, GlassWindowBg, false);

        // Specular Glass Rim Overlay (Ignores layout so it wraps the whole window)
        RectTransform rim = MakeRect("GlassRimBorder", cardRoot);
        SetStretch(rim);
        AddSlicedImage(rim, s_WindowBorderSprite, GlassBorder, false);
        rim.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        var columns = cardRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        columns.padding = new RectOffset(14, 14, 14, 14);
        columns.spacing = 12f;
        columns.childControlWidth = true;
        columns.childControlHeight = true;
        columns.childForceExpandWidth = false;
        columns.childForceExpandHeight = true;

        // 1. Left Content Column
        leftColumn = MakeRect("Left_ContentColumn", cardRoot);
        var leftLayout = leftColumn.gameObject.AddComponent<LayoutElement>();
        leftLayout.preferredWidth = leftColumnWidth;
        leftLayout.flexibleWidth = 0f;

        // 2. Right Artwork Column (Inset Rounded Card inside the Glass Window)
        rightArtColumn = MakeRect("Right_OnboardingArtwork", cardRoot);
        var rightLayout = rightArtColumn.gameObject.AddComponent<LayoutElement>();
        rightLayout.preferredWidth = onboardingSize.x - leftColumnWidth - 40f;
        rightLayout.flexibleWidth = 1f;
        BuildArtworkPanel();

        // 3. Build Individual Screen Panels
        welcomePanel       = BuildWelcomeScreen(leftColumn);
        mainMenuPanel      = BuildMainMenuScreen(leftColumn);
        missionSelectPanel = BuildMissionSelectScreen(leftColumn);
        pausePanel         = BuildPauseScreen(leftColumn);
        messagePanel       = BuildMessageScreen(leftColumn);
        controlsPanel      = BuildControlsScreen(leftColumn);

        rim.SetAsLastSibling();
        ActivateScreen(welcomePanel, showOnboardingArt: true);
    }

    private void BuildArtworkPanel()
    {
        RectTransform artContainer = MakeRect("Right_ArtworkContainer", rightArtColumn);
        SetStretch(artContainer);
        Image maskImg = AddSlicedImage(artContainer, s_WindowFillSprite, ArtGlassBg, false);
        Mask mask = artContainer.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // Minimalist Apple-style Placeholder
        var placeholder = MakeRect("Placeholder_Graphics", artContainer);
        SetStretch(placeholder);
        artPlaceholderGroup = placeholder.gameObject;

        RectTransform badge = MakeRect("CenterIconPill", placeholder);
        badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 0.54f);
        badge.sizeDelta = new Vector2(96f, 96f);
        AddSlicedImage(badge, s_WindowFillSprite, new Color(1f, 1f, 1f, 0.08f), false);
        AddArtText(badge, "✦", 44, TextPrimary, Vector2.zero, new Vector2(96f, 96f));

        AddArtText(placeholder, "Spatial Flight", 20, TextPrimary, new Vector2(0, -48), new Vector2(320, 32), FontStyles.Bold);
        AddArtText(placeholder, "Mixed Reality Environment", 14, TextSecondary, new Vector2(0, -74), new Vector2(320, 26), FontStyles.Normal);

        // Actual Artwork Image
        var imageRect = MakeRect("Onboarding_SpriteImage", artContainer);
        SetStretch(imageRect);
        onboardingArtImage = AddImage(imageRect, Color.white);
        onboardingArtImage.preserveAspect = false;

        // Subtle inner rim on artwork panel
        RectTransform artBorder = MakeRect("ButtonBorder", artContainer);
        SetStretch(artBorder);
        AddSlicedImage(artBorder, s_WindowBorderSprite, CardGlassBorder, false);

        RefreshArtworkSprite();
    }

    private void RefreshArtworkSprite()
    {
        if (onboardingArtImage == null) return;
        onboardingArtImage.sprite = onboardingArtwork;
        bool hasArt = onboardingArtwork != null;
        onboardingArtImage.color = hasArt ? Color.white : Color.clear;
        if (artPlaceholderGroup != null)
            artPlaceholderGroup.SetActive(!hasArt);
    }

    // =========================================================================
    // INDIVIDUAL SCREENS (Minimalist Copy, Clean Hierarchy)
    // =========================================================================

    private GameObject BuildWelcomeScreen(Transform parent)
    {
        RectTransform panel = CreateScreenContainer("Screen_01_Welcome", parent, new RectOffset(30, 26, 32, 26), 10f);

        AddPillBadge(panel, "SPATIAL PROTOTYPE  ·  v0.1");
        AddSpacer(panel, 6f);

        AddLabel(panel, "Title", "PropelXR", 46, TextPrimary, 56f, FontStyles.Bold);
        AddLabel(panel, "Subtitle", "Your room is your airspace.", 20, TextPrimary, 30f, FontStyles.Normal);
        AddSpacer(panel, 4f);

        AddLabel(panel, "Body",
            "Pilot a virtual drone directly inside your physical space. Keep your surroundings clear and press B anytime for menu.",
            16, TextSecondary, 68f, FontStyles.Normal, lineSpacing: 4f);

        AddFlexibleSpacer(panel);
        welcomeSkipButton = AddStyledButton(panel, "Btn_SkipWelcome", "Continue", "Enter Flight Deck", true, 68f);

        return panel.gameObject;
    }

    private GameObject BuildMainMenuScreen(Transform parent)
    {
        RectTransform panel = CreateScreenContainer("Screen_02_MainMenu", parent, new RectOffset(30, 30, 28, 24), 10f);

        AddEyebrow(panel, "SPATIAL FLIGHT");
        AddLabel(panel, "Title", "PropelXR", 42, TextPrimary, 50f, FontStyles.Bold);
        AddSpacer(panel, 4f);

        freeFlightButton  = AddStyledButton(panel, "Btn_FreeFlight", "Free Flight", "Explore your room freely", true, 74f);
        missionModeButton = AddStyledButton(panel, "Btn_Missions", "Missions", "Coin Collector & Time Rush", false, 74f);

        AddFlexibleSpacer(panel);

        // Clean bottom utility row
        mainControlsButton = AddStyledButton(panel, "Btn_Controls", "Controls", "Controller map", false, 58f);
        quitButton         = AddStyledButton(panel, "Btn_Quit", "Exit App", "Close PropelXR", false, 54f);

        return panel.gameObject;
    }

    private GameObject BuildMissionSelectScreen(Transform parent)
    {
        RectTransform panel = CreateScreenContainer("Screen_03_MissionSelect", parent, new RectOffset(30, 30, 28, 24), 10f);

        AddEyebrow(panel, "CHALLENGES");
        AddLabel(panel, "Title", "Missions", 42, TextPrimary, 50f, FontStyles.Bold);
        AddLabel(panel, "Subtitle", "Precision piloting in mixed reality.", 16, TextSecondary, 26f);
        AddSpacer(panel, 6f);

        coinCollectorButton = AddStyledButton(panel, "Btn_CoinCollector", "Coin Collector", "Collect targets at your own pace", true, 76f);
        timeRushButton      = AddStyledButton(panel, "Btn_TimeRush", "Time Rush", "Race against the clock", false, 76f);

        AddFlexibleSpacer(panel);
        missionBackButton   = AddStyledButton(panel, "Btn_Back", "Back", "Main Menu", false, 58f);

        return panel.gameObject;
    }

    private GameObject BuildPauseScreen(Transform parent)
    {
        RectTransform panel = CreateScreenContainer("Screen_04_Pause", parent, new RectOffset(30, 30, 30, 24), 10f);

        pauseModeLabel = AddEyebrow(panel, "FREE FLIGHT");
        AddLabel(panel, "Title", "Paused", 42, TextPrimary, 50f, FontStyles.Bold);
        AddLabel(panel, "Subtitle", "Flight physics suspended.", 16, TextSecondary, 26f);
        AddSpacer(panel, 10f);

        pauseResumeButton   = AddStyledButton(panel, "Btn_Resume", "Resume Flight", "Continue session", true, 74f);
        pauseControlsButton = AddStyledButton(panel, "Btn_Controls", "Controls", "Controller reference", false, 64f);

        AddFlexibleSpacer(panel);
        pauseMainMenuButton = AddStyledButton(panel, "Btn_MainMenu", "End Flight", "Return to Main Menu", false, 58f);

        return panel.gameObject;
    }

    private GameObject BuildMessageScreen(Transform parent)
    {
        RectTransform panel = CreateScreenContainer("Screen_05_Message", parent, new RectOffset(30, 30, 32, 26), 12f);

        AddEyebrow(panel, "SYSTEM STATUS");
        messageTitleLabel = AddLabel(panel, "Message_Title", "Preparing Space", 36, TextPrimary, 48f, FontStyles.Bold);
        messageBodyLabel  = AddLabel(panel, "Message_Body", "Scanning room surfaces and safe placement...", 17, TextSecondary, 110f, FontStyles.Normal, 4f);

        AddFlexibleSpacer(panel);
        messageMainMenuButton = AddStyledButton(panel, "Btn_MessageBack", "Dismiss", "Return to Main Menu", true, 64f);

        return panel.gameObject;
    }

    private GameObject BuildControlsScreen(Transform parent)
    {
        RectTransform panel = CreateScreenContainer("Screen_06_Controls", parent, new RectOffset(30, 30, 24, 22), 6f);

        AddEyebrow(panel, "CONTROLLER MAP");
        AddLabel(panel, "Title", "Controls", 36, TextPrimary, 44f, FontStyles.Bold);
        AddSpacer(panel, 4f);

        AddControlRow(panel, "L Stick", "Altitude & Yaw");
        AddControlRow(panel, "R Stick", "Pitch & Roll");
        AddControlRow(panel, "X / A", "Arm / Disarm Motors");
        AddControlRow(panel, "Y", "Reset Drone Position");
        AddControlRow(panel, "B", "Pause & Menu");
        AddControlRow(panel, "Trigger", "Select UI");

        AddFlexibleSpacer(panel);
        controlsBackButton = AddStyledButton(panel, "Btn_ControlsBack", "Done", "Return", true, 56f);
        return panel.gameObject;
    }

    // =========================================================================
    // VISION PRO UI COMPONENT HELPERS
    // =========================================================================

    private void AddPillBadge(Transform parent, string text)
    {
        RectTransform row = MakeRect("Badge_Row", parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        RectTransform pill = MakeRect("Badge_Pill", row);
        pill.anchorMin = new Vector2(0f, 0.5f);
        pill.anchorMax = new Vector2(0f, 0.5f);
        pill.pivot = new Vector2(0f, 0.5f);
        pill.sizeDelta = new Vector2(210f, 26f);
        AddSlicedImage(pill, s_PillFillSprite, PillBg, false);

        var label = MakeRect("Badge_Text", pill).gameObject.AddComponent<TextMeshProUGUI>();
        SetStretch(label.rectTransform);
        if (customFont != null) label.font = customFont;
        label.raycastTarget = false;
        label.text = text;
        label.fontSize = 11.5f;
        label.characterSpacing = 6f;
        label.fontStyle = FontStyles.Bold;
        label.color = TextSecondary;
        label.alignment = TextAlignmentOptions.Center;
    }

    private TextMeshProUGUI AddEyebrow(Transform parent, string text)
    {
        var label = AddLabel(parent, "Eyebrow_Tag", text, 12, TextTertiary, 18f, FontStyles.Bold);
        label.characterSpacing = 10f;
        return label;
    }

    private void AddControlRow(Transform parent, string key, string description)
    {
        RectTransform row = MakeRect("Control_" + key.Replace(" ", "_"), parent);
        AddSlicedImage(row, s_CardFillSprite, CardGlass, false);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;

        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(10, 14, 6, 6);
        h.spacing = 12f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;

        // Keycap Pill
        RectTransform keyPill = MakeRect("Key_Pill", row);
        AddSlicedImage(keyPill, s_PillFillSprite, PillBg, false);
        var keyLayout = keyPill.gameObject.AddComponent<LayoutElement>();
        keyLayout.preferredWidth = 88f;
        keyLayout.flexibleWidth = 0f;

        var keyLabel = MakeRect("KeyLabel", keyPill).gameObject.AddComponent<TextMeshProUGUI>();
        SetStretch(keyLabel.rectTransform);
        if (customFont != null) keyLabel.font = customFont;
        keyLabel.raycastTarget = false;
        keyLabel.text = key;
        keyLabel.fontSize = 14f;
        keyLabel.fontStyle = FontStyles.Bold;
        keyLabel.color = TextPrimary;
        keyLabel.alignment = TextAlignmentOptions.Center;

        // Action Description
        var detail = AddLabel(row, "Action", description, 15, TextSecondary, 30f);
        detail.alignment = TextAlignmentOptions.MidlineLeft;
        detail.GetComponent<LayoutElement>().flexibleWidth = 1f;
    }

    private static RectTransform CreateScreenContainer(string name, Transform parent, RectOffset padding, float spacing)
    {
        RectTransform rect = MakeRect(name, parent);
        SetStretch(rect);
        var vlg = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = padding;
        vlg.spacing = spacing;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        return rect;
    }

    private TextMeshProUGUI AddLabel(Transform parent, string objName, string text, float fontSize, Color color, float preferredHeight, FontStyles style = FontStyles.Normal, float lineSpacing = 0f)
    {
        RectTransform r = MakeRect(objName, parent);
        var label = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (customFont != null) label.font = customFont;
        label.raycastTarget = false;
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.fontStyle = style;
        label.lineSpacing = lineSpacing;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.enableWordWrapping = true;

        var layout = r.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        return label;
    }

    private Button AddStyledButton(Transform parent, string objName, string title, string subtitle, bool primary, float height = 74f)
    {
        RectTransform rect = MakeRect(objName, parent);
        Image bg = AddSlicedImage(rect, s_CardFillSprite, primary ? PrimaryBlue : CardGlass, true);

        Button btn = rect.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1.12f, 1.12f, 1.15f, 1f);
        cb.pressedColor     = new Color(0.82f, 0.82f, 0.86f, 1f);
        cb.selectedColor    = cb.highlightedColor;
        cb.fadeDuration     = 0.08f;
        btn.colors = cb;

        var layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;

        // Subtle Specular Rim around Button
        RectTransform border = MakeRect("ButtonBorder", rect);
        SetStretch(border);
        AddSlicedImage(border, s_CardBorderSprite, primary ? PrimaryBlueBorder : CardGlassBorder, false);

        // Main Text Stack (Left-aligned Title + Subtitle)
        RectTransform labelRect = MakeRect("Label", rect);
        SetStretch(labelRect);
        labelRect.offsetMin = new Vector2(20f, 6f);
        labelRect.offsetMax = new Vector2(-42f, -6f);

        var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        if (customFont != null) label.font = customFont;
        label.raycastTarget = false;

        string subHex = primary ? "#D6EBFF" : "#9A9AA6";
        label.text = string.IsNullOrEmpty(subtitle)
            ? $"<b>{title}</b>"
            : $"<b>{title}</b>\n<size=13.5><color={subHex}>{subtitle}</color></size>";
        label.fontSize = 19f;
        label.lineSpacing = 2f;
        label.color = TextPrimary;
        label.alignment = TextAlignmentOptions.MidlineLeft;

        // Minimalist Apple Chevron on the right
        RectTransform chevronRect = MakeRect("Chevron", rect);
        chevronRect.anchorMin = new Vector2(1f, 0f);
        chevronRect.anchorMax = new Vector2(1f, 1f);
        chevronRect.pivot = new Vector2(1f, 0.5f);
        chevronRect.sizeDelta = new Vector2(36f, 0f);
        chevronRect.anchoredPosition = new Vector2(-6f, 0f);

        var chevron = chevronRect.gameObject.AddComponent<TextMeshProUGUI>();
        if (customFont != null) chevron.font = customFont;
        chevron.raycastTarget = false;
        chevron.text = "›";
        chevron.fontSize = 26f;
        chevron.color = primary ? new Color(1f, 1f, 1f, 0.85f) : TextTertiary;
        chevron.alignment = TextAlignmentOptions.Midline;

        return btn;
    }

    private void AddArtText(Transform parent, string value, int size, Color color, Vector2 pos, Vector2 dimensions, FontStyles style = FontStyles.Bold)
    {
        var rect = MakeRect("Artwork_Label", parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = dimensions;
        rect.anchoredPosition = pos;
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (customFont != null) label.font = customFont;
        label.raycastTarget = false;
        label.text = value;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
    }

    private static void AddSpacer(Transform parent, float height)
    {
        var r = MakeRect("Spacer", parent);
        r.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
    }

    private static void AddFlexibleSpacer(Transform parent)
    {
        var r = MakeRect("FlexSpacer", parent);
        r.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
    }

    private static RectTransform MakeRect(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void SetStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Image AddImage(RectTransform rect, Color color, bool raycast = false)
    {
        Image img = rect.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    private static Image AddSlicedImage(RectTransform rect, Sprite sprite, Color color, bool raycast = false)
    {
        Image img = rect.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }
}