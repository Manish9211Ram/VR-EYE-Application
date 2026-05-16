using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Level 10 – Depth and Motion Tracking
/// Multiple balls move along 3D paths at varying depths (Z distances).
/// Some come closer, some move away, some move laterally.
/// User must follow specific target(s) while ignoring distractors.
/// Attach this script to any GameObject in Scene 10.
/// </summary>
public class Level10_DepthMotion : MonoBehaviour
{
    [Header("Session Settings")]
    public float sessionDuration = 45f;

    [Header("Ball Settings")]
    public int numberOfBalls = 5;
    public float ballSize = 0.1f;

    [Header("Depth Range")]
    public float minDepth = 1.0f;   // Closest point to camera (meters)
    public float maxDepth = 3.5f;   // Furthest point from camera (meters)

    [Header("Movement")]
    public float lateralSpeed  = 0.4f;   // Left-right / up-down speed
    public float depthSpeed    = 0.3f;   // In-out (toward / away) speed
    public float spreadWidth   = 1.2f;   // Max lateral offset

    [Header("Colors")]
    public Color targetColor     = new Color(1f, 0.3f, 0f);     // Orange = target
    public Color distractorColor = new Color(0.2f, 0.6f, 1f);   // Blue  = distractor

    // Runtime
    public bool isRunning { get; private set; } = false;
    private Camera mainCamera;

    // Per-ball state
    private List<GameObject> balls       = new List<GameObject>();
    private List<Vector3>    velocities  = new List<Vector3>();   // XYZ velocity
    private List<float>      depths      = new List<float>();     // Current Z depth
    private List<float>      depthVels   = new List<float>();     // Depth velocity (toward/away)

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = FindBestCamera();
            Debug.Log($"Level 10: Using camera: {(mainCamera != null ? mainCamera.gameObject.name : "NULL - NO CAMERA FOUND!")}");
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
            StartCoroutine(RunDepthSession());
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

        yield return StartCoroutine(RunDepthSession());

        if (canvas != null) canvas.enabled = true;
    }

    private IEnumerator RunDepthSession()
    {
        isRunning = true;
        Debug.Log("Level 10: Depth & Motion session started.");

        // Spawn balls
        balls.Clear(); velocities.Clear(); depths.Clear(); depthVels.Clear();
        for (int i = 0; i < numberOfBalls; i++)
        {
            float depth = Random.Range(minDepth, maxDepth);
            depths.Add(depth);

            float x = Random.Range(-spreadWidth, spreadWidth);
            float y = Random.Range(-spreadWidth * 0.5f, spreadWidth * 0.5f);
            Vector3 localPos = new Vector3(x, y, depth);
            Vector3 worldPos = mainCamera.transform.TransformPoint(localPos);

            bool isTarget = (i == 0); // First ball is always the target
            Color col = isTarget ? targetColor : distractorColor;
            GameObject ball = CreateBall(worldPos, ballSize * (isTarget ? 1.3f : 1f), col);
            balls.Add(ball);

            // Random lateral velocity
            Vector3 vel = new Vector3(
                Random.Range(-lateralSpeed, lateralSpeed),
                Random.Range(-lateralSpeed * 0.5f, lateralSpeed * 0.5f),
                0f);
            velocities.Add(vel);

            // Random depth velocity (in/out)
            float dv = Random.Range(-depthSpeed, depthSpeed);
            if (Mathf.Abs(dv) < 0.05f) dv = depthSpeed * 0.5f;
            depthVels.Add(dv);
        }

        Debug.Log("Level 10: Target ball = orange (larger). Follow it!");

        float elapsed = 0f;
        while (elapsed < sessionDuration)
        {
            for (int i = 0; i < balls.Count; i++)
            {
                if (balls[i] == null) continue;

                // Update depth
                depths[i] += depthVels[i] * Time.deltaTime;
                if (depths[i] < minDepth) { depths[i] = minDepth; depthVels[i] *= -1f; }
                if (depths[i] > maxDepth) { depths[i] = maxDepth; depthVels[i] *= -1f; }

                // Update lateral position in camera-local space
                Vector3 localPos = mainCamera.transform.InverseTransformPoint(balls[i].transform.position);
                localPos += velocities[i] * Time.deltaTime;
                localPos.z = depths[i]; // Apply depth

                // Bounce off lateral boundaries
                if (Mathf.Abs(localPos.x) > spreadWidth)   velocities[i] = new Vector3(-velocities[i].x, velocities[i].y, 0);
                if (Mathf.Abs(localPos.y) > spreadWidth * 0.5f) velocities[i] = new Vector3(velocities[i].x, -velocities[i].y, 0);

                // Scale ball by depth (closer = bigger, further = smaller) for depth perception cue
                float depthT = Mathf.InverseLerp(maxDepth, minDepth, depths[i]);
                float scaleMultiplier = Mathf.Lerp(0.6f, 1.4f, depthT);
                bool isTarget = (i == 0);
                balls[i].transform.localScale = Vector3.one * ballSize * scaleMultiplier * (isTarget ? 1.3f : 1f);

                balls[i].transform.position = mainCamera.transform.TransformPoint(localPos);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Cleanup
        foreach (var b in balls)
            if (b != null) Destroy(b);
        balls.Clear();

        isRunning = false;
        Debug.Log("Level 10: Depth & Motion session complete.");
    }

    private GameObject CreateBall(Vector3 pos, float size, Color color)
    {
        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.transform.position = pos;
        ball.transform.localScale = Vector3.one * size;
        Destroy(ball.GetComponent<Collider>());

        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetColor("_EmissionColor", color * 1.8f);
        mat.EnableKeyword("_EMISSION");
        ball.GetComponent<Renderer>().material = mat;

        return ball;
    }

    void OnDestroy()
    {
        foreach (var b in balls)
            if (b != null) Destroy(b);
    }
}
