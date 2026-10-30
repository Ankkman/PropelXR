
// using System;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;

// /// <summary>
// /// Minimalist headset HUD and results overlay for PropelXR.
// /// Styled to match the deep matte charcoal, blue accent, and chevron button aesthetic.
// /// </summary>
// public class PropelXRMissionUI : MonoBehaviour
// {
//     [Header("References")]
//     [SerializeField] private Canvas menuCanvas;
//     [SerializeField] private Transform headset;
//     [SerializeField] private TMP_FontAsset customFont;

//     [Header("HUD Positioning (Avoids Center Overlap)")]
//     [SerializeField, Min(0.5f)] private float distance = 1.4f;
//     [SerializeField] private Vector2 hudOffset = new Vector2(0f, 0.45f); // "Visor" top placement
//     [SerializeField, Min(0.01f)] private float smoothing = 14f;
//     [SerializeField] private bool tiltTowardsEyes = true;

//     private Canvas hudCanvas;
//     private TextMeshProUGUI hudLabel;
//     private GameObject resultsOverlay;
    
//     private Action retryCallback;
//     private Action homeCallback;

//     // Dark Voxel Works / PropelXR Palette
//     private static readonly Color MatteBlackBg = FromHex("#141416");
//     private static readonly Color CardSurface  = FromHex("#2A2A2D"); // Secondary Grey Button
//     private static readonly Color PrimaryBlue  = FromHex("#1881D8"); // Primary Blue Button
//     private static readonly Color TextWhite    = FromHex("#F5F5F7");
//     private static readonly Color TextMuted    = FromHex("#9A9AA2");

//     private static Color FromHex(string hex)
//     {
//         ColorUtility.TryParseHtmlString(hex, out var c);
//         return c;
//     }

//     private void Awake()
//     {
//         if (headset == null && Camera.main != null) headset = Camera.main.transform;
        
//         // Build HUD Canvas
//         GameObject canvasObj = new GameObject("PropelXR_MissionHUD", typeof(RectTransform), typeof(Canvas));
//         canvasObj.transform.SetParent(transform, false);
//         hudCanvas = canvasObj.GetComponent<Canvas>();
//         hudCanvas.renderMode = RenderMode.WorldSpace;
        
//         RectTransform rect = canvasObj.GetComponent<RectTransform>();
//         rect.sizeDelta = new Vector2(800, 58); // Sleek wide pill
//         rect.localScale = Vector3.one * 0.001f;
        
//         // HUD Backdrop
//         Image panel = new GameObject("HUD_Background", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
//         panel.transform.SetParent(canvasObj.transform, false);
//         Stretch((RectTransform)panel.transform);
//         panel.color = new Color(MatteBlackBg.r, MatteBlackBg.g, MatteBlackBg.b, 0.85f);
//         panel.raycastTarget = false;
        
//         // HUD Typography
//         hudLabel = Label("HUD_Text", panel.transform, 21, TextAlignmentOptions.Center, TextWhite, FontStyles.Bold);
//         Stretch((RectTransform)hudLabel.transform);
        
//         hudCanvas.gameObject.SetActive(false);
//     }

//     private void LateUpdate()
//     {
//         if (hudCanvas == null || !hudCanvas.gameObject.activeSelf || headset == null) return;
        
//         Vector3 forward = Vector3.ProjectOnPlane(headset.forward, Vector3.up).normalized;
//         if (forward.sqrMagnitude < .001f) forward = headset.forward;
        
//         // Calculate Target Position based on visor offset
//         Vector3 targetPos = headset.position + forward * distance + headset.right * hudOffset.x + Vector3.up * hudOffset.y;
        
//         // Dynamic Tilt: Billboards directly to the headset so text isn't skewed vertically
//         Quaternion targetRot = tiltTowardsEyes 
//             ? Quaternion.LookRotation(targetPos - headset.position) 
//             : Quaternion.LookRotation(forward, Vector3.up);
        
//         float alpha = smoothing <= 0 ? 1 : 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
//         hudCanvas.transform.position = Vector3.Lerp(hudCanvas.transform.position, targetPos, alpha);
//         hudCanvas.transform.rotation = Quaternion.Slerp(hudCanvas.transform.rotation, targetRot, alpha);
//     }

//     public void ShowHUD(string missionName, int score, int collected, int target, float seconds, bool timed, int livesRemaining = 3)
//     {
//         HideResults();
//         if (hudCanvas == null) return;
        
//         if (!hudCanvas.gameObject.activeSelf && headset != null)
//         {
//             Vector3 forward = Vector3.ProjectOnPlane(headset.forward, Vector3.up).normalized;
//             if (forward.sqrMagnitude < .001f) forward = headset.forward;
//             hudCanvas.transform.SetPositionAndRotation(
//                 headset.position + forward * distance + headset.right * hudOffset.x + Vector3.up * hudOffset.y,
//                 Quaternion.LookRotation(forward, Vector3.up));
//         }
        
//         hudCanvas.gameObject.SetActive(true);
        
//         // Clean Rich Text Formatting: Muted Labels, Bright Values
//         string lblColor = "#9A9AA2";
//         string valColor = "#F5F5F7";
//         string accColor = "#1881D8";

//         string livesStr = timed ? $"    <color={lblColor}>LIVES</color> <color={valColor}>{livesRemaining}</color>" : "";
//         string timeStr = timed ? $"    <color={lblColor}>TIME</color> <color={valColor}>{Mathf.CeilToInt(seconds):00}s</color>" : "";
//         string targetStr = timed ? "" : $"/{target}";

//         hudLabel.text = $"<color={accColor}>{missionName.ToUpper()}</color>    <color={lblColor}>SCORE</color> <color={valColor}>{score:000}</color>    <color={lblColor}>COINS</color> <color={valColor}>{collected}{targetStr}</color>{timeStr}{livesStr}";
//     }

//     public void HideHUD() { if (hudCanvas != null) hudCanvas.gameObject.SetActive(false); }
    
//     public void HideResults() 
//     { 
//         if (resultsOverlay != null) Destroy(resultsOverlay); 
//         resultsOverlay = null; 
//     }

//     public void ShowResults(bool timed, int score, int collected, int target, bool completed, Action retry, Action mainMenu, bool outOfLives = false)
//     {
//         HideHUD();
//         HideResults();
        
//         retryCallback = retry;
//         homeCallback = mainMenu;
        
//         if (menuCanvas == null) { Debug.LogError("PropelXRMissionUI: assign existing PropelXR Menu Canvas.", this); return; }
//         menuCanvas.gameObject.SetActive(true);
        
//         // Full Matte Black Overlay
//         RectTransform overlay = Rect("Mission_Results_Overlay", menuCanvas.transform);
//         resultsOverlay = overlay.gameObject;
//         Stretch(overlay);
        
//         Image backdrop = overlay.gameObject.AddComponent<Image>();
//         backdrop.color = MatteBlackBg;
//         backdrop.raycastTarget = true;
        
//         // Centered Content Container
//         RectTransform content = Rect("Content_Container", overlay);
//         content.anchorMin = content.anchorMax = new Vector2(.5f, .5f);
//         content.sizeDelta = new Vector2(480, 520);
//         content.anchoredPosition = Vector2.zero;
        
//         VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
//         layout.padding = new RectOffset(34, 34, 40, 34);
//         layout.spacing = 10;
//         layout.childAlignment = TextAnchor.UpperLeft;
//         layout.childControlWidth = true; layout.childControlHeight = true;
//         layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        
//         // Title Block
//         string finalTitle = completed ? "Mission Complete" : (outOfLives ? "Drone Crashed" : "Time's Up");
//         AddLabel(content, "Eyebrow", timed ? "TIME RUSH" : "COIN COLLECTOR", 14, TextMuted, 20f, FontStyles.Bold);
//         AddLabel(content, "Title", finalTitle, 44, TextWhite, 56f, FontStyles.Bold);
//         AddSpacer(content, 12f);
        
//         // Stats Block
//         AddLabel(content, "Stats_Score", $"Final Score: <color=#FFFFFF>{score:000}</color>", 21, TextMuted, 30f);
//         AddLabel(content, "Stats_Coins", $"Coins Secured: <color=#FFFFFF>{collected}</color>" + (timed ? "" : $" / {target}"), 21, TextMuted, 30f);
        
//         AddFlexibleSpacer(content); // Pushes buttons down
        
//         // Styled Interactive Buttons matching the Menu aesthetic
//         AddMenuButton(content, "RETRY MISSION", "Launch flight again", () => retryCallback?.Invoke(), true);
//         AddMenuButton(content, "MAIN MENU", "Return to flight deck", () => homeCallback?.Invoke(), false);
//     }

//     // =========================================================================
//     // UI BUILDER HELPERS
//     // =========================================================================

//     private static RectTransform Rect(string name, Transform parent)
//     {
//         GameObject go = new GameObject(name, typeof(RectTransform));
//         go.transform.SetParent(parent, false);
//         return go.GetComponent<RectTransform>();
//     }

//     private static void Stretch(RectTransform r)
//     {
//         r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
//         r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
//     }

//     private TextMeshProUGUI Label(string name, Transform parent, int size, TextAlignmentOptions alignment, Color color, FontStyles style = FontStyles.Normal)
//     {
//         TextMeshProUGUI text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
//         if (customFont != null) text.font = customFont;
//         text.fontSize = size; text.alignment = alignment; text.color = color;
//         text.fontStyle = style;
//         text.raycastTarget = false; text.enableWordWrapping = true;
//         return text;
//     }

//     private TextMeshProUGUI AddLabel(Transform parent, string name, string text, int size, Color color, float height, FontStyles style = FontStyles.Normal)
//     {
//         TextMeshProUGUI label = Label(name, parent, size, TextAlignmentOptions.TopLeft, color, style);
//         label.text = text;
//         label.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
//         return label;
//     }

//     private static void AddSpacer(Transform parent, float height)
//     {
//         GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
//         spacer.transform.SetParent(parent, false);
//         spacer.AddComponent<LayoutElement>().preferredHeight = height;
//     }

//     private static void AddFlexibleSpacer(Transform parent)
//     {
//         GameObject spacer = new GameObject("Flex_Spacer", typeof(RectTransform));
//         spacer.transform.SetParent(parent, false);
//         spacer.AddComponent<LayoutElement>().flexibleHeight = 1f;
//     }

//     // Replicates the rounded button with stacked text and right-chevron from your screenshot
//     private void AddMenuButton(Transform parent, string title, string subtitle, Action callback, bool isPrimary)
//     {
//         RectTransform rect = Rect("Btn_" + title, parent);
        
//         // Simple rounded background using Unity's default UI sprite if available, otherwise solid rect
//         Image bg = rect.gameObject.AddComponent<Image>();
//         bg.color = isPrimary ? PrimaryBlue : CardSurface;
//         bg.raycastTarget = true;
        
//         Button btn = rect.gameObject.AddComponent<Button>(); 
//         btn.targetGraphic = bg;
        
//         // Clean Hover States
//         ColorBlock cb = btn.colors;
//         cb.normalColor = Color.white;
//         cb.highlightedColor = new Color(0.9f, 0.95f, 1f);
//         cb.pressedColor = new Color(0.7f, 0.85f, 1f);
//         cb.selectedColor = cb.highlightedColor;
//         btn.colors = cb;
        
//         btn.onClick.AddListener(() => callback?.Invoke());
//         rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 74f;

//         // Left-Aligned Text Stack
//         RectTransform labelRect = Rect("Label", rect);
//         Stretch(labelRect);
//         labelRect.offsetMin = new Vector2(22f, 0f);
//         labelRect.offsetMax = new Vector2(-40f, 0f); // Leave room for chevron
        
//         TextMeshProUGUI text = Label("Text", labelRect, 20, TextAlignmentOptions.MidlineLeft, TextWhite, FontStyles.Bold);
//         text.text = $"<b>{title}</b>\n<size=13.5><color=#9A9AA2>{subtitle}</color></size>"; 
//         text.lineSpacing = 2f;

//         // Right-Aligned Chevron (>)
//         RectTransform chevronRect = Rect("Chevron", rect);
//         chevronRect.anchorMin = new Vector2(1f, 0f);
//         chevronRect.anchorMax = new Vector2(1f, 1f);
//         chevronRect.pivot = new Vector2(1f, 0.5f);
//         chevronRect.sizeDelta = new Vector2(40f, 0f);
//         chevronRect.anchoredPosition = new Vector2(-5f, 0f);
        
//         TextMeshProUGUI chevron = Label("Icon", chevronRect, 24, TextAlignmentOptions.Midline, TextWhite, FontStyles.Bold);
//         chevron.text = ">";
//         chevron.alpha = isPrimary ? 1f : 0.4f;
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
/// Headset HUD and Results UI for PropelXR.
/// Supports full Editor hierarchy generation for both HUD and Results screens.
/// </summary>
public class PropelXRMissionUI : MonoBehaviour
{
    [Header("Canvas References")]
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private Transform headset;
    [SerializeField] private TMP_FontAsset customFont;

    [Header("HUD Positioning (Top Visor)")]
    [SerializeField, Min(0.5f)] private float distance = 1.35f;
    // Positive Y puts it above your eye line (Visor style) so room center remains clear
    [SerializeField] private Vector2 hudOffset = new Vector2(0f, 0.42f);
    [SerializeField, Min(0.01f)] private float smoothing = 14f;
    [SerializeField] private bool tiltTowardsEyes = true;

    [Header("Generated Results References (Editable in Hierarchy)")]
    [SerializeField] private GameObject resultsOverlay;
    [SerializeField] private TextMeshProUGUI resultsEyebrowLabel;
    [SerializeField] private TextMeshProUGUI resultsTitleLabel;
    [SerializeField] private TextMeshProUGUI resultsScoreLabel;
    [SerializeField] private TextMeshProUGUI resultsCoinsLabel;
    [SerializeField] private Button resultsRetryButton;
    [SerializeField] private Button resultsMainMenuButton;

    [Header("Generated HUD References (Editable in Hierarchy)")]
    [SerializeField] private TextMeshProUGUI hudLabel;

    private Action retryCallback;
    private Action homeCallback;

    // Dark Voxel Works / PropelXR Palette
    private static readonly Color MatteBlackBg = FromHex("#141416");
    private static readonly Color CardSurface  = FromHex("#2A2A2D");
    private static readonly Color PrimaryBlue  = FromHex("#1881D8");
    private static readonly Color TextWhite    = FromHex("#F5F5F7");
    private static readonly Color TextMuted    = FromHex("#9A9AA2");

    private static Color FromHex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }

    // =========================================================================
    // EDITOR GENERATION (Run once via Inspector Context Menu)
    // =========================================================================

    [ContextMenu("Generate Mission UI Hierarchy (Editor Only)")]
    public void GenerateUIInEditor()
    {
        ResolveHeadset();

        // 1. Build HUD Canvas
        BuildHUDHierarchy();

        // 2. Build Results Overlay inside MenuCanvas
        if (menuCanvas != null)
        {
            BuildResultsHierarchy();
        }
        else
        {
            Debug.LogWarning("PropelXRMissionUI: Assign 'menuCanvas' to generate the Results Overlay.", this);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            if (hudCanvas != null) EditorUtility.SetDirty(hudCanvas.gameObject);
            if (menuCanvas != null) EditorUtility.SetDirty(menuCanvas.gameObject);
        }
#endif
        Debug.Log("PropelXRMissionUI: HUD & Results hierarchy generated successfully in Hierarchy!", this);
    }

    private void Awake()
    {
        ResolveHeadset();

        // If not pre-generated in editor, build at runtime fallback
        if (hudCanvas == null || hudLabel == null)
            BuildHUDHierarchy();

        if (hudCanvas != null)
            hudCanvas.gameObject.SetActive(false);

        if (resultsOverlay != null)
            resultsOverlay.SetActive(false);

        // Safety fix: If Inspector is still holding the old negative center offset, fix it
        if (hudOffset.y < 0.1f)
            hudOffset = new Vector2(0f, 0.42f);
    }

    private void ResolveHeadset()
    {
        if (headset == null && Camera.main != null)
            headset = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (hudCanvas == null || !hudCanvas.gameObject.activeSelf) return;
        ResolveHeadset();
        if (headset == null) return;

        Vector3 forward = Vector3.ProjectOnPlane(headset.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = headset.forward;

        // Position at visor height
        Vector3 targetPos = headset.position + forward * distance + headset.right * hudOffset.x + Vector3.up * hudOffset.y;

        // Tilt down directly facing user's eyes
        Quaternion targetRot = tiltTowardsEyes
            ? Quaternion.LookRotation(targetPos - headset.position)
            : Quaternion.LookRotation(forward, Vector3.up);

        float alpha = smoothing <= 0 ? 1f : 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
        hudCanvas.transform.position = Vector3.Lerp(hudCanvas.transform.position, targetPos, alpha);
        hudCanvas.transform.rotation = Quaternion.Slerp(hudCanvas.transform.rotation, targetRot, alpha);
    }

    // =========================================================================
    // HUD DISPLAY LOGIC
    // =========================================================================

    public void ShowHUD(string missionName, int score, int collected, int target, float seconds, bool timed, int livesRemaining = 3)
    {
        HideResults();
        if (hudCanvas == null) return;

        ResolveHeadset();
        if (!hudCanvas.gameObject.activeSelf && headset != null)
        {
            Vector3 forward = Vector3.ProjectOnPlane(headset.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = headset.forward;

            Vector3 startPos = headset.position + forward * distance + headset.right * hudOffset.x + Vector3.up * hudOffset.y;
            Quaternion startRot = tiltTowardsEyes
                ? Quaternion.LookRotation(startPos - headset.position)
                : Quaternion.LookRotation(forward, Vector3.up);

            hudCanvas.transform.SetPositionAndRotation(startPos, startRot);
        }

        hudCanvas.gameObject.SetActive(true);

        string lbl = "#9A9AA2";
        string val = "#F5F5F7";
        string acc = "#1881D8";

        string livesStr = timed ? $"    <color={lbl}>LIVES</color> <color={val}>{livesRemaining}</color>" : "";
        string timeStr  = timed ? $"    <color={lbl}>TIME</color> <color={val}>{Mathf.CeilToInt(seconds):00}s</color>" : "";
        string targetStr = timed ? "" : $"/{target}";

        if (hudLabel != null)
        {
            hudLabel.text = $"<color={acc}>{missionName.ToUpper()}</color>    <color={lbl}>SCORE</color> <color={val}>{score:000}</color>    <color={lbl}>COINS</color> <color={val}>{collected}{targetStr}</color>{timeStr}{livesStr}";
        }
    }

    public void HideHUD()
    {
        if (hudCanvas != null) hudCanvas.gameObject.SetActive(false);
    }

    // =========================================================================
    // RESULTS DISPLAY LOGIC
    // =========================================================================

    public void HideResults()
    {
        if (resultsOverlay != null) resultsOverlay.SetActive(false);
    }

    public void ShowResults(bool timed, int score, int collected, int target, bool completed, Action retry, Action mainMenu, bool outOfLives = false)
    {
        HideHUD();

        retryCallback = retry;
        homeCallback = mainMenu;

        if (resultsOverlay == null && menuCanvas != null)
            BuildResultsHierarchy();

        if (menuCanvas != null)
            menuCanvas.gameObject.SetActive(true);

        if (resultsOverlay != null)
        {
            resultsOverlay.SetActive(true);
            resultsOverlay.transform.SetAsLastSibling(); // Ensure it sits on top of menus
        }

        string finalTitle = completed ? "Mission Complete" : (outOfLives ? "Drone Crashed" : "Time's Up");

        if (resultsEyebrowLabel != null)
            resultsEyebrowLabel.text = timed ? "TIME RUSH" : "COIN COLLECTOR";

        if (resultsTitleLabel != null)
            resultsTitleLabel.text = finalTitle;

        if (resultsScoreLabel != null)
            resultsScoreLabel.text = $"Final Score: <color=#FFFFFF>{score:000}</color>";

        if (resultsCoinsLabel != null)
            resultsCoinsLabel.text = $"Coins Secured: <color=#FFFFFF>{collected}</color>" + (timed ? "" : $" / {target}");

        // Rebind click listeners
        if (resultsRetryButton != null)
        {
            resultsRetryButton.onClick.RemoveAllListeners();
            resultsRetryButton.onClick.AddListener(() => retryCallback?.Invoke());
        }

        if (resultsMainMenuButton != null)
        {
            resultsMainMenuButton.onClick.RemoveAllListeners();
            resultsMainMenuButton.onClick.AddListener(() => homeCallback?.Invoke());
        }
    }

    // =========================================================================
    // HIERARCHY BUILDERS
    // =========================================================================

    private void BuildHUDHierarchy()
    {
        Transform existing = transform.Find("PropelXR_MissionHUD");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        GameObject canvasObj = new GameObject("PropelXR_MissionHUD", typeof(RectTransform), typeof(Canvas));
        canvasObj.transform.SetParent(transform, false);
        hudCanvas = canvasObj.GetComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.WorldSpace;

        RectTransform rect = canvasObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800, 58);
        rect.localScale = Vector3.one * 0.001f;

        // Background
        Image panel = new GameObject("HUD_Background", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        panel.transform.SetParent(canvasObj.transform, false);
        Stretch((RectTransform)panel.transform);
        panel.color = new Color(MatteBlackBg.r, MatteBlackBg.g, MatteBlackBg.b, 0.85f);
        panel.raycastTarget = false;

        // Text
        hudLabel = Label("HUD_Text", panel.transform, 21, TextAlignmentOptions.Center, TextWhite, FontStyles.Bold);
        Stretch((RectTransform)hudLabel.transform);
    }

    private void BuildResultsHierarchy()
    {
        if (menuCanvas == null) return;

        Transform existing = menuCanvas.transform.Find("Mission_Results_Overlay");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        RectTransform overlay = Rect("Mission_Results_Overlay", menuCanvas.transform);
        resultsOverlay = overlay.gameObject;
        Stretch(overlay);

        Image backdrop = overlay.gameObject.AddComponent<Image>();
        backdrop.color = MatteBlackBg;
        backdrop.raycastTarget = true;

        RectTransform content = Rect("Content_Container", overlay);
        content.anchorMin = content.anchorMax = new Vector2(.5f, .5f);
        content.sizeDelta = new Vector2(480, 520);
        content.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(34, 34, 40, 34);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;

        resultsEyebrowLabel = AddLabel(content, "Eyebrow", "MISSION MODE", 14, TextMuted, 20f, FontStyles.Bold);
        resultsTitleLabel   = AddLabel(content, "Title", "Mission Complete", 44, TextWhite, 56f, FontStyles.Bold);
        AddSpacer(content, 12f);

        resultsScoreLabel   = AddLabel(content, "Stats_Score", "Final Score: 000", 21, TextMuted, 30f);
        resultsCoinsLabel   = AddLabel(content, "Stats_Coins", "Coins Secured: 0", 21, TextMuted, 30f);

        AddFlexibleSpacer(content);

        resultsRetryButton    = AddMenuButton(content, "RETRY MISSION", "Launch flight again", true);
        resultsMainMenuButton = AddMenuButton(content, "MAIN MENU", "Return to flight deck", false);
    }

    // =========================================================================
    // UI BUILDER HELPERS
    // =========================================================================

    private static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    private TextMeshProUGUI Label(string name, Transform parent, int size, TextAlignmentOptions alignment, Color color, FontStyles style = FontStyles.Normal)
    {
        TextMeshProUGUI text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (customFont != null) text.font = customFont;
        text.fontSize = size; text.alignment = alignment; text.color = color;
        text.fontStyle = style;
        text.raycastTarget = false; text.enableWordWrapping = true;
        return text;
    }

    private TextMeshProUGUI AddLabel(Transform parent, string name, string text, int size, Color color, float height, FontStyles style = FontStyles.Normal)
    {
        TextMeshProUGUI label = Label(name, parent, size, TextAlignmentOptions.TopLeft, color, style);
        label.text = text;
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        return label;
    }

    private static void AddSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(parent, false);
        spacer.AddComponent<LayoutElement>().preferredHeight = height;
    }

    private static void AddFlexibleSpacer(Transform parent)
    {
        GameObject spacer = new GameObject("Flex_Spacer", typeof(RectTransform));
        spacer.transform.SetParent(parent, false);
        spacer.AddComponent<LayoutElement>().flexibleHeight = 1f;
    }

    private Button AddMenuButton(Transform parent, string title, string subtitle, bool isPrimary)
    {
        RectTransform rect = Rect("Btn_" + title.Replace(" ", "_"), parent);
        Image bg = rect.gameObject.AddComponent<Image>();
        bg.color = isPrimary ? PrimaryBlue : CardSurface;
        bg.raycastTarget = true;

        Button btn = rect.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.9f, 0.95f, 1f);
        cb.pressedColor = new Color(0.7f, 0.85f, 1f);
        cb.selectedColor = cb.highlightedColor;
        btn.colors = cb;

        rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 74f;

        RectTransform labelRect = Rect("Label", rect);
        Stretch(labelRect);
        labelRect.offsetMin = new Vector2(22f, 0f);
        labelRect.offsetMax = new Vector2(-40f, 0f);

        var text = Label("Text", labelRect, 20, TextAlignmentOptions.MidlineLeft, TextWhite, FontStyles.Bold);
        text.text = $"<b>{title}</b>\n<size=13.5><color=#9A9AA2>{subtitle}</color></size>";
        text.lineSpacing = 2f;

        RectTransform chevronRect = Rect("Chevron", rect);
        chevronRect.anchorMin = new Vector2(1f, 0f);
        chevronRect.anchorMax = new Vector2(1f, 1f);
        chevronRect.pivot = new Vector2(1f, 0.5f);
        chevronRect.sizeDelta = new Vector2(40f, 0f);
        chevronRect.anchoredPosition = new Vector2(-5f, 0f);

        var chevron = Label("Icon", chevronRect, 24, TextAlignmentOptions.Midline, TextWhite, FontStyles.Bold);
        chevron.text = ">";
        chevron.alpha = isPrimary ? 1f : 0.4f;

        return btn;
    }
}