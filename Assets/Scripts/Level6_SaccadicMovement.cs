using UnityEngine;
using System.Collections;

/// <summary>
/// Level 6 – Saccadic Eye Movements
/// Glowing dots appear one at a time at random positions in front of the camera.
/// User must quickly shift gaze to each new dot as it appears.
/// Attach this script to any GameObject in Scene 6.
/// </summary>
public class Level6_SaccadicMovement : MonoBehaviour
{
    [Header("Dot Settings")]
    public GameObject dotPrefab;          // Assign a glowing sphere prefab (or leave null to auto-create)
    public int totalDots = 15;            // How many dots appear in one session
    public float dotDisplayTime = 2f;     // How long each dot stays visible (seconds)
    public float dotRadius = 1.2f;        // Spread radius around center (meters in VR)
    public float distanceFromCamera = 2f; // How far in front of the player the dots appear

    [Header("Visual")]
    public Color dotColor = new Color(0f, 1f, 0.5f, 1f); // Glowing green by default
    public float dotSize = 0.15f;         // Size of each dot — bigger = easier to see in VR

    [Header("Audio (Optional)")]
    public AudioSource popSound;          // Sound when a new dot appears

    // Runtime
    public bool isRunning { get; private set; } = false;
    private GameObject currentDot;
    private Camera mainCamera;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = FindBestCamera();
            Debug.Log($"Level 6: Using camera: {(mainCamera != null ? mainCamera.gameObject.name : "NULL - NO CAMERA FOUND!")}");
        }
    }

    /// <summary>
    /// Finds the best available camera, handling SteamVR / Oculus VR rigs
    /// where Camera.main may not exist.
    /// </summary>
    private Camera FindBestCamera()
    {
        // 1. Try Camera.main first
        if (Camera.main != null) return Camera.main;

        // 2. Try finding camera tagged MainCamera
        GameObject mainCamGO = GameObject.FindWithTag("MainCamera");
        if (mainCamGO != null) return mainCamGO.GetComponent<Camera>();

        // 3. Try common SteamVR camera names
        string[] vrCameraNames = { "Camera (eye)", "Camera (head)", "VRCamera",
                                   "CenterEyeAnchor", "Camera", "Main Camera" };
        foreach (string name in vrCameraNames)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
            {
                Camera cam = go.GetComponent<Camera>();
                if (cam != null) return cam;
            }
        }

        // 4. Last resort: get ANY camera in the scene
        Camera[] allCams = FindObjectsOfType<Camera>();
        if (allCams.Length > 0)
        {
            Debug.LogWarning($"Level 6: Using fallback camera: {allCams[0].gameObject.name}");
            return allCams[0];
        }

        Debug.LogError("Level 6: NO CAMERA FOUND IN SCENE!");
        return null;
    }

    public void StartSequence()
    {
        if (mainCamera == null) mainCamera = FindBestCamera();

        if (!isRunning)
            StartCoroutine(RunSaccadicSequence());
    }

    // ── Call THIS from the Start button onClick ──────────────────────────
    // Hides the canvas, runs the dots, then shows canvas again when done.
    public void StartExercise()
    {
        if (mainCamera == null) mainCamera = FindBestCamera();

        if (!isRunning)
            StartCoroutine(RunWithCanvasHide());
    }

    private IEnumerator RunWithCanvasHide()
    {
        // Find and hide the parent canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            // fallback: find by name
            GameObject menuGO = GameObject.Find("Menu");
            if (menuGO != null) canvas = menuGO.GetComponent<Canvas>();
        }

        if (canvas != null)
        {
            canvas.enabled = false;
            Debug.Log("Level 6: Canvas hidden.");
        }
        else
        {
            Debug.LogWarning("Level 6: Could not find Canvas to hide!");
        }

        // Run the exercise
        yield return StartCoroutine(RunSaccadicSequence());

        // Show canvas again when done
        if (canvas != null)
        {
            canvas.enabled = true;
            Debug.Log("Level 6: Canvas shown again.");
        }
    }

    private IEnumerator RunSaccadicSequence()
    {
        isRunning = true;
        Debug.Log("Level 6: Saccadic sequence started.");

        for (int i = 0; i < totalDots; i++)
        {
            // Remove previous dot
            if (currentDot != null)
                Destroy(currentDot);

            // Spawn new dot at random position
            Vector3 spawnPos = GetRandomPosition();
            currentDot = SpawnDot(spawnPos);

            if (popSound != null) popSound.Play();

            Debug.Log($"Level 6: Dot {i + 1}/{totalDots} appeared at {spawnPos}");

            yield return new WaitForSeconds(dotDisplayTime);
        }

        // Clean up last dot
        if (currentDot != null)
            Destroy(currentDot);

        isRunning = false;
        Debug.Log("Level 6: Saccadic sequence complete.");
    }

    private Vector3 GetRandomPosition()
    {
        if (mainCamera == null) return Vector3.forward * distanceFromCamera;

        // Random angle and height within dotRadius
        float angle = Random.Range(0f, 360f);
        float r     = Random.Range(0.3f, dotRadius);
        float x     = Mathf.Cos(angle * Mathf.Deg2Rad) * r;
        float y     = Mathf.Sin(angle * Mathf.Deg2Rad) * r * 0.6f; // slightly flatter vertically
        Vector3 localOffset = new Vector3(x, y, distanceFromCamera);
        return mainCamera.transform.TransformPoint(localOffset);
    }

    private GameObject SpawnDot(Vector3 position)
    {
        GameObject dot;

        if (dotPrefab != null)
        {
            dot = Instantiate(dotPrefab, position, Quaternion.identity);
            dot.SetActive(true); // Ensure it is active
        }
        else
        {
            // Auto-create a glowing sphere
            dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.transform.position = position;
            dot.transform.localScale = Vector3.one * dotSize;

            // Remove collider so it doesn't interfere with VR pointers
            Destroy(dot.GetComponent<Collider>());
        }

        // Force glow material (Standard shader with emission) 
        // whether we spawned a prefab or auto-created a sphere.
        Renderer renderer = dot.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material glowMat = new Material(Shader.Find("Standard"));
            glowMat.color = dotColor;
            glowMat.SetColor("_EmissionColor", dotColor * 5f); // High intensity
            glowMat.EnableKeyword("_EMISSION");
            renderer.material = glowMat;
        }

        // Add a Point Light inside the dot so it emits real light in the scene
        Light ptLight = dot.AddComponent<Light>();
        ptLight.type = LightType.Point;
        ptLight.color = dotColor;
        ptLight.intensity = 3f;
        ptLight.range = 2f;

        return dot;
    }

    void OnDestroy()
    {
        if (currentDot != null)
            Destroy(currentDot);
    }
}
