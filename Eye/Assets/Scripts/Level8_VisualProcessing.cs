using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// Level 8 – Speed of Visual Processing
/// Numbers, letters, or symbols flash briefly at random positions on screen.
/// User must identify what they see before it disappears.
/// Attach this script to any GameObject in Scene 8.
/// </summary>
public class Level8_VisualProcessing : MonoBehaviour
{
    [Header("Session Settings")]
    public int totalFlashes = 20;              // Total number of flashes in one session
    public float flashDuration = 0.6f;         // How long each symbol stays visible (seconds)
    public float gapBetweenFlashes = 0.8f;     // Pause between flashes

    [Header("Symbol Settings")]
    public float distanceFromCamera = 2f;
    public float spreadRadius = 1.2f;          // How far from center symbols can appear
    public float symbolSize = 0.25f;           // World-space text size

    [Header("Content")]
    // Three pools: numbers, letters, symbols
    private static readonly string[] Numbers = { "1","2","3","4","5","6","7","8","9","0" };
    private static readonly string[] Letters = { "A","B","C","D","E","F","G","H","J","K","L","M","N","P","R","T","V","W","X","Z" };
    private static readonly string[] Symbols = { "★","◆","●","▲","■","✦","✸","❋","⬟","⬡" };

    // Runtime
    public bool isRunning { get; private set; } = false;
    private Camera mainCamera;
    private GameObject flashObject;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = FindBestCamera();
            Debug.Log($"Level 8: Using camera: {(mainCamera != null ? mainCamera.gameObject.name : "NULL - NO CAMERA FOUND!")}");
        }
    }

    private Camera FindBestCamera()
    {
        if (Camera.main != null) return Camera.main;
        GameObject mainCamGO = GameObject.FindWithTag("MainCamera");
        if (mainCamGO != null) return mainCamGO.GetComponent<Camera>();
        string[] vrCameraNames = { "Camera (eye)", "Camera (head)", "VRCamera", "CenterEyeAnchor", "Camera", "Main Camera" };
        foreach (string name in vrCameraNames)
        {
            GameObject go = GameObject.Find(name);
            if (go != null) { Camera cam = go.GetComponent<Camera>(); if (cam != null) return cam; }
        }
        Camera[] allCams = FindObjectsOfType<Camera>();
        if (allCams.Length > 0) return allCams[0];
        return null;
    }

    public void StartSequence()
    {
        if (mainCamera == null)
        {
            mainCamera = FindBestCamera();
        }

        if (!isRunning)
            StartCoroutine(RunFlashSequence());
    }

    // ── Call THIS from the Start button onClick ──────────────────────────
    public void StartExercise()
    {
        if (mainCamera == null) mainCamera = FindBestCamera();

        if (!isRunning)
            StartCoroutine(RunWithCanvasHide());
    }

    private IEnumerator RunWithCanvasHide()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject menuGO = GameObject.Find("Menu");
            if (menuGO != null) canvas = menuGO.GetComponent<Canvas>();
        }

        if (canvas != null) canvas.enabled = false;

        yield return StartCoroutine(RunFlashSequence());

        if (canvas != null) canvas.enabled = true;
    }

    private IEnumerator RunFlashSequence()
    {
        isRunning = true;
        Debug.Log("Level 8: Visual processing sequence started.");

        for (int i = 0; i < totalFlashes; i++)
        {
            // Pick random symbol category and character
            string symbol = PickRandomSymbol();

            // Pick random position
            Vector3 pos = GetRandomPosition();

            // Flash it
            ShowFlash(symbol, pos);
            yield return new WaitForSeconds(flashDuration);
            HideFlash();
            yield return new WaitForSeconds(gapBetweenFlashes);
        }

        HideFlash();
        isRunning = false;
        Debug.Log("Level 8: Visual processing sequence complete.");
    }

    private string PickRandomSymbol()
    {
        int category = Random.Range(0, 3);
        switch (category)
        {
            case 0: return Numbers[Random.Range(0, Numbers.Length)];
            case 1: return Letters[Random.Range(0, Letters.Length)];
            default: return Symbols[Random.Range(0, Symbols.Length)];
        }
    }

    private Vector3 GetRandomPosition()
    {
        float angle = Random.Range(0f, 360f);
        float r     = Random.Range(0.1f, spreadRadius);
        float x     = Mathf.Cos(angle * Mathf.Deg2Rad) * r;
        float y     = Mathf.Sin(angle * Mathf.Deg2Rad) * r * 0.6f;
        return mainCamera.transform.TransformPoint(new Vector3(x, y, distanceFromCamera));
    }

    private void ShowFlash(string text, Vector3 position)
    {
        // Create a world-space TextMeshPro object
        if (flashObject == null)
        {
            flashObject = new GameObject("FlashSymbol");
            var tmpText = flashObject.AddComponent<TextMeshPro>();
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontSize = 4f;
            tmpText.color = GetRandomFlashColor();
            flashObject.transform.localScale = Vector3.one * symbolSize;
        }

        var tmp = flashObject.GetComponent<TextMeshPro>();
        tmp.text = text;
        tmp.color = GetRandomFlashColor();
        flashObject.transform.position = position;
        // Face the camera
        flashObject.transform.LookAt(mainCamera.transform);
        flashObject.transform.Rotate(0f, 180f, 0f);
        flashObject.SetActive(true);
    }

    private void HideFlash()
    {
        if (flashObject != null)
            flashObject.SetActive(false);
    }

    private Color GetRandomFlashColor()
    {
        Color[] colors = {
            Color.white,
            Color.yellow,
            new Color(0f, 1f, 0.5f),   // Mint green
            new Color(1f, 0.5f, 0f),   // Orange
            new Color(0.5f, 0.8f, 1f)  // Light blue
        };
        return colors[Random.Range(0, colors.Length)];
    }

    void OnDestroy()
    {
        if (flashObject != null) Destroy(flashObject);
    }
}
