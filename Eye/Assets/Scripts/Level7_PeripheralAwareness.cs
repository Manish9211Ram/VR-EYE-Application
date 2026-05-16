using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Level 7 – Dynamic Peripheral Awareness
/// A central red fixation dot stays fixed. Peripheral targets appear,
/// move slowly around the edges, and sometimes disappear/reappear.
/// Attach this script to any GameObject in Scene 7.
/// </summary>
public class Level7_PeripheralAwareness : MonoBehaviour
{
    [Header("Session Settings")]
    public float sessionDuration = 45f;       // Total exercise duration in seconds

    [Header("Central Fixation Dot")]
    public float fixationDotSize = 0.04f;
    public Color fixationDotColor = Color.red;
    public float fixationDistance = 2f;

    [Header("Peripheral Targets")]
    public int numberOfTargets = 4;           // How many peripheral targets at once
    public float peripheralRadius = 1.4f;     // How far from center the peripherals appear
    public float targetMoveSpeed = 0.15f;     // How fast they orbit (degrees per second)
    public float targetSize = 0.06f;
    public Color targetColor = new Color(0f, 0.8f, 1f); // Cyan
    public float blinkInterval = 6f;          // How often targets randomly disappear/reappear
    public float blinkDuration = 1.5f;        // How long they stay hidden

    // Runtime
    public bool isRunning { get; private set; } = false;
    private Camera mainCamera;
    private GameObject fixationDot;
    private List<GameObject> peripheralTargets = new List<GameObject>();
    private List<float> orbitAngles = new List<float>();

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = FindBestCamera();
            Debug.Log($"Level 7: Using camera: {(mainCamera != null ? mainCamera.gameObject.name : "NULL - NO CAMERA FOUND!")}");
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
        if (mainCamera == null) mainCamera = FindBestCamera();

        if (!isRunning)
            StartCoroutine(RunPeripheralSession());
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
        // Find and hide canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject menuGO = GameObject.Find("Menu");
            if (menuGO != null) canvas = menuGO.GetComponent<Canvas>();
        }

        if (canvas != null) canvas.enabled = false;

        yield return StartCoroutine(RunPeripheralSession());

        if (canvas != null) canvas.enabled = true;
    }

    private IEnumerator RunPeripheralSession()
    {
        isRunning = true;
        Debug.Log("Level 7: Peripheral awareness session started.");

        // Create central fixation dot
        fixationDot = CreateSphere(Vector3.zero, fixationDotSize, fixationDotColor);
        PositionInFrontOfCamera(fixationDot, 0f, 0f);

        // Create peripheral targets evenly spaced
        peripheralTargets.Clear();
        orbitAngles.Clear();
        for (int i = 0; i < numberOfTargets; i++)
        {
            float angle = (360f / numberOfTargets) * i;
            orbitAngles.Add(angle);
            GameObject target = CreateSphere(Vector3.zero, targetSize, targetColor);
            UpdateTargetPosition(target, angle);
            peripheralTargets.Add(target);

            // Start random blink coroutine for each
            StartCoroutine(BlinkTarget(target));
        }

        float elapsed = 0f;
        while (elapsed < sessionDuration)
        {
            // Update all target positions (orbit around center)
            for (int i = 0; i < peripheralTargets.Count; i++)
            {
                orbitAngles[i] += targetMoveSpeed * Time.deltaTime;
                if (orbitAngles[i] >= 360f) orbitAngles[i] -= 360f;
                UpdateTargetPosition(peripheralTargets[i], orbitAngles[i]);
            }

            // Keep fixation dot in front of camera
            PositionInFrontOfCamera(fixationDot, 0f, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Cleanup
        if (fixationDot != null) Destroy(fixationDot);
        foreach (var t in peripheralTargets)
            if (t != null) Destroy(t);
        peripheralTargets.Clear();

        isRunning = false;
        Debug.Log("Level 7: Peripheral awareness session complete.");
    }

    private IEnumerator BlinkTarget(GameObject target)
    {
        while (target != null && isRunning)
        {
            yield return new WaitForSeconds(blinkInterval + Random.Range(0f, 3f));
            if (target == null) yield break;
            target.SetActive(false);
            yield return new WaitForSeconds(blinkDuration);
            if (target != null) target.SetActive(true);
        }
    }

    private void UpdateTargetPosition(GameObject target, float angleDeg)
    {
        if (target == null || mainCamera == null) return;
        float rad = angleDeg * Mathf.Deg2Rad;
        float x = Mathf.Cos(rad) * peripheralRadius;
        float y = Mathf.Sin(rad) * peripheralRadius * 0.55f;
        Vector3 localPos = new Vector3(x, y, fixationDistance);
        target.transform.position = mainCamera.transform.TransformPoint(localPos);
    }

    private void PositionInFrontOfCamera(GameObject obj, float offsetX, float offsetY)
    {
        if (obj == null || mainCamera == null) return;
        Vector3 localPos = new Vector3(offsetX, offsetY, fixationDistance);
        obj.transform.position = mainCamera.transform.TransformPoint(localPos);
    }

    private GameObject CreateSphere(Vector3 pos, float size, Color color)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = pos;
        sphere.transform.localScale = Vector3.one * size;
        Destroy(sphere.GetComponent<Collider>());
        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetColor("_EmissionColor", color * 2.5f);
        mat.EnableKeyword("_EMISSION");
        sphere.GetComponent<Renderer>().material = mat;
        return sphere;
    }

    void OnDestroy()
    {
        if (fixationDot != null) Destroy(fixationDot);
        foreach (var t in peripheralTargets)
            if (t != null) Destroy(t);
    }
}
