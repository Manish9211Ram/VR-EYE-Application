using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Level 9 – Color & Contrast Differentiation
/// Multiple balls appear with slightly different colors.
/// One ball is designated as the "target" — it will periodically change color.
/// User must track or identify the ball that changes color.
/// Attach this script to any GameObject in Scene 9.
/// </summary>
public class Level9_ColorContrast : MonoBehaviour
{
    [Header("Session Settings")]
    public float sessionDuration = 45f;

    [Header("Ball Settings")]
    public int numberOfBalls = 5;
    public float ballSize = 0.1f;
    public float distanceFromCamera = 2f;
    public float spreadRadius = 1.0f;
    public float ballMoveSpeed = 0.3f;            // Slow drift speed (m/s)

    [Header("Color Settings")]
    public Color baseColor = new Color(0.2f, 0.5f, 1f);     // Base blue family
    public Color[] decoyVariants = new Color[]               // Slightly different shades
    {
        new Color(0.25f, 0.55f, 0.95f),
        new Color(0.15f, 0.45f, 1.0f),
        new Color(0.3f,  0.5f,  0.9f),
        new Color(0.2f,  0.6f,  0.85f),
    };
    public Color targetHighlightColor = new Color(1f, 0.2f, 0.1f);  // Red — target when changing
    public float colorChangeDuration = 1.2f;    // How long the target stays highlighted
    public float colorChangeInterval = 5f;       // How often target changes color

    // Runtime
    public bool isRunning { get; private set; } = false;
    private Camera mainCamera;
    private List<GameObject> balls       = new List<GameObject>();
    private List<Vector3>    velocities  = new List<Vector3>();
    private int targetBallIndex          = 0;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = FindBestCamera();
            Debug.Log($"Level 9: Using camera: {(mainCamera != null ? mainCamera.gameObject.name : "NULL - NO CAMERA FOUND!")}");
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
            StartCoroutine(RunColorSession());
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

        yield return StartCoroutine(RunColorSession());

        if (canvas != null) canvas.enabled = true;
    }

    private IEnumerator RunColorSession()
    {
        isRunning = true;
        Debug.Log("Level 9: Color & Contrast session started.");

        // Spawn balls
        balls.Clear();
        velocities.Clear();
        for (int i = 0; i < numberOfBalls; i++)
        {
            Color c = (i < decoyVariants.Length) ? decoyVariants[i] : baseColor;
            Vector3 pos = GetRandomPosition();
            GameObject ball = CreateBall(pos, ballSize, c);
            balls.Add(ball);

            // Random slow drift direction
            Vector3 vel = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-0.5f, 0.5f),
                0f).normalized * ballMoveSpeed;
            velocities.Add(vel);
        }

        // Pick random target
        targetBallIndex = Random.Range(0, balls.Count);
        Debug.Log($"Level 9: Target ball index = {targetBallIndex}");

        // Start color-change coroutine for target
        StartCoroutine(FlashTargetBall());

        float elapsed = 0f;
        while (elapsed < sessionDuration)
        {
            // Drift balls slowly, bounce off invisible boundary
            for (int i = 0; i < balls.Count; i++)
            {
                if (balls[i] == null) continue;

                balls[i].transform.position += velocities[i] * Time.deltaTime;

                // Get local position relative to camera
                Vector3 local = mainCamera.transform.InverseTransformPoint(balls[i].transform.position);
                bool bounced = false;
                if (Mathf.Abs(local.x) > spreadRadius)  { velocities[i] = new Vector3(-velocities[i].x, velocities[i].y, 0); bounced = true; }
                if (Mathf.Abs(local.y) > spreadRadius * 0.6f) { velocities[i] = new Vector3(velocities[i].x, -velocities[i].y, 0); bounced = true; }
                if (bounced) velocities[i] = velocities[i].normalized * ballMoveSpeed;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Cleanup
        foreach (var b in balls)
            if (b != null) Destroy(b);
        balls.Clear();

        isRunning = false;
        Debug.Log("Level 9: Color & Contrast session complete.");
    }

    private IEnumerator FlashTargetBall()
    {
        while (isRunning)
        {
            yield return new WaitForSeconds(colorChangeInterval + Random.Range(0f, 2f));
            if (!isRunning || balls.Count == 0) yield break;

            // Flash target ball to highlight color
            SetBallColor(balls[targetBallIndex], targetHighlightColor);
            Debug.Log("Level 9: Target ball flashed!");
            yield return new WaitForSeconds(colorChangeDuration);

            // Reset back to base color
            if (balls.Count > targetBallIndex && balls[targetBallIndex] != null)
            {
                Color resetColor = (targetBallIndex < decoyVariants.Length)
                    ? decoyVariants[targetBallIndex] : baseColor;
                SetBallColor(balls[targetBallIndex], resetColor);
            }
        }
    }

    private Vector3 GetRandomPosition()
    {
        float angle = Random.Range(0f, 360f);
        float r     = Random.Range(0.2f, spreadRadius * 0.8f);
        float x     = Mathf.Cos(angle * Mathf.Deg2Rad) * r;
        float y     = Mathf.Sin(angle * Mathf.Deg2Rad) * r * 0.55f;
        return mainCamera.transform.TransformPoint(new Vector3(x, y, distanceFromCamera));
    }

    private GameObject CreateBall(Vector3 pos, float size, Color color)
    {
        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.transform.position = pos;
        ball.transform.localScale = Vector3.one * size;
        Destroy(ball.GetComponent<Collider>());
        SetBallColor(ball, color);
        return ball;
    }

    private void SetBallColor(GameObject ball, Color color)
    {
        if (ball == null) return;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetColor("_EmissionColor", color * 1.5f);
        mat.EnableKeyword("_EMISSION");
        ball.GetComponent<Renderer>().material = mat;
    }

    void OnDestroy()
    {
        foreach (var b in balls)
            if (b != null) Destroy(b);
    }
}
