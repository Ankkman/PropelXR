using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class DroneStickUIBuilder : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector2 panelSize = new Vector2(120f, 120f);
    [SerializeField] private Vector2 dotSize = new Vector2(16f, 16f);
    [SerializeField] private float bottomOffset = 100f;
    [SerializeField] private float leftOffset = 100f;
    [SerializeField] private float panelSpacing = 40f;

    [Header("Colors")]
    public Color panelColor = new Color(0f, 0f, 0f, 0.6f);      
    public Color gridLineColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); 
    public Color leftDotColor = new Color(0f, 0.8f, 0f, 1f);     
    public Color rightDotColor = new Color(0.8f, 0.8f, 0.8f, 1f); 

    // This creates a right-click option on the component in the Inspector
    [ContextMenu("Generate Permanent UI Elements")]
    public void BuildUIInEditor()
    {
        // 1. Configure Canvas Scaler safely in Editor
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

        // Clean up old generated elements if you press the button multiple times
        ClearExistingGeneratedElements();

        // 2. Build the Left Panel
        float leftPanelX = leftOffset + (panelSize.x * 0.5f);
        RectTransform leftArea = CreatePanel("LeftStickArea", leftPanelX, leftDotColor);

        // 3. Build the Right Panel
        float rightPanelX = leftOffset + panelSize.x + panelSpacing + (panelSize.y * 0.5f);
        RectTransform rightArea = CreatePanel("RightStickArea", rightPanelX, rightDotColor);

        // 4. Handle Visualizer Script
        DroneStickVisualizer visualizer = GetComponent<DroneStickVisualizer>();
        if (visualizer == null) visualizer = gameObject.AddComponent<DroneStickVisualizer>();

        var leftDot = leftArea.Find("TrackingDot") as RectTransform;
        var rightDot = rightArea.Find("TrackingDot") as RectTransform;

        // 5. Permanently assign references via reflection directly into your script fields
        AssignVisualizerFields(visualizer, leftArea, leftDot, rightArea, rightDot);
        
        Debug.Log("Drone UI Generated and Saved Permanently! You can now remove or keep this UIBuilder script component.");
    }

    private RectTransform CreatePanel(string name, float posX, Color dotColor)
    {
        GameObject panelObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(this.transform, false);
        
        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.sizeDelta = panelSize;
        
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(posX, bottomOffset + (panelSize.y * 0.5f));

        Image img = panelObj.GetComponent<Image>();
        img.color = panelColor;

        CreateLine("HorizontalLine", rect, new Vector2(panelSize.x, 2f));
        CreateLine("VerticalLine", rect, new Vector2(2f, panelSize.y));

        GameObject dotObj = new GameObject("TrackingDot", typeof(RectTransform), typeof(Image));
        dotObj.transform.SetParent(rect, false);
        
        RectTransform dotRect = dotObj.GetComponent<RectTransform>();
        dotRect.sizeDelta = dotSize;
        dotRect.anchorMin = new Vector2(0.5f, 0.5f);
        dotRect.anchorMax = new Vector2(0.5f, 0.5f);
        dotRect.anchoredPosition = Vector2.zero;

        Image dotImg = dotObj.GetComponent<Image>();
        dotImg.color = dotColor;

        return rect;
    }

    private void CreateLine(string name, Transform parent, Vector2 size)
    {
        GameObject lineObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        lineObj.transform.SetParent(parent, false);
        
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        lineRect.sizeDelta = size;
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = Vector2.zero;

        Image lineImg = lineObj.GetComponent<Image>();
        lineImg.color = gridLineColor;
    }

    private void AssignVisualizerFields(DroneStickVisualizer vis, RectTransform lArea, RectTransform lDot, RectTransform rArea, RectTransform rDot)
    {
        System.Type type = typeof(DroneStickVisualizer);
        
        // Find existing scene reader if possible
        DroneInputReader foundReader = Object.FindFirstObjectByType<DroneInputReader>();
        if (foundReader != null)
        {
            type.GetField("inputReader", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(vis, foundReader);
        }

        type.GetField("leftStickArea", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(vis, lArea);
        type.GetField("leftStickDot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(vis, lDot);
        type.GetField("rightStickArea", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(vis, rArea);
        type.GetField("rightStickDot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(vis, rDot);
    }

    private void ClearExistingGeneratedElements()
    {
        Transform left = transform.Find("LeftStickArea");
        Transform right = transform.Find("RightStickArea");
        if (left != null) DestroyImmediate(left.gameObject);
        if (right != null) DestroyImmediate(right.gameObject);
    }
}
