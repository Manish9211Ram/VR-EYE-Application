using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Valve.VR;

public class Task1UI_Anim : MonoBehaviour
{
    public Animator ballAnimator;
    public Canvas targetCanvas;

    // Static flag to track if we are restarting (persists between scene loads)
    public static bool AutoPlayOnRestart = false;

    private void Awake()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // ── 1. Robust EventSystem Cleanup ──
        // Prioritize keeping the one with SteamVR_InputModule
        var eventSystems = Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>(true);
        if (eventSystems.Length > 1)
        {
            // Prioritize keeping the one that is NOT the default scene one (usually child of Player)
            UnityEngine.EventSystems.EventSystem bestOne = null;
            foreach (var es in eventSystems)
            {
                // Most VR rigs have the EventSystem as a child of the Player or VR rig object
                if (es.transform.root.name.Contains("Player") || es.transform.root.name.Contains("Rig"))
                {
                    bestOne = es;
                    break;
                }
            }
            if (bestOne == null) bestOne = eventSystems[0];

            foreach (var es in eventSystems)
            {
                if (es != bestOne) Destroy(es.gameObject);
            }
            Debug.Log($"[Task1UI_Anim] Deduplicated EventSystems. Kept {bestOne.name}");
        }

        // ── 2. Remove duplicate AudioListeners ───────────────────────────────────
        var listeners = Object.FindObjectsOfType<AudioListener>(true);
        if (listeners.Length > 1)
        {
            AudioListener bestOne = null;
            foreach (var l in listeners)
            {
                if (l.CompareTag("MainCamera") || l.transform.root.name.Contains("Player"))
                {
                    bestOne = l;
                    break;
                }
            }
            if (bestOne == null) bestOne = listeners[0];

            foreach (var l in listeners)
                if (l != bestOne) Destroy(l);
        }

        // ── 3. In task scenes only: pre-disable all non-Player Animators ─────────
        //    so they don't auto-play before the user presses Start.
        if (sceneName == "0") return;

        foreach (Animator anim in Object.FindObjectsOfType<Animator>(true))
        {
            // Do NOT disable Animators that belong to the VR Player, Hands, or SteamVR!
            // This prevents the hands from becoming frozen ("stuck")!
            if (anim.transform.root.GetComponent<Valve.VR.InteractionSystem.Player>() != null || 
                anim.transform.root.GetComponentInChildren<SteamVR_Behaviour>() != null)
            {
                continue;
            }

            string n = anim.gameObject.name.ToLower();
            if (n.Contains("hand") || n.Contains("player") || n.Contains("controller") ||
                n.Contains("steamvr") || n.Contains("camera") || n.Contains("vr_glove") || n.Contains("model"))
                continue;

            if (anim.enabled)
            {
                anim.enabled = false;
                Debug.Log($"[Task1UI_Anim] Pre-disabled Animator on '{anim.gameObject.name}' to prevent auto-play.");
            }
        }
    }


    // -------------------------------------------------------
    // Call this from the START button on the main menu screen.
    // Hides the first canvas and shows the level-select canvas.
    // -------------------------------------------------------
    public void ShowLevelSelect()
    {
        // Hide first screen ("Menu") - the parent canvas of this button
        GameObject mainMenuGO = GameObject.Find("Menu");
        if (mainMenuGO != null)
        {
            mainMenuGO.SetActive(false);
            Debug.Log("ShowLevelSelect: Hid 'Menu' canvas.");
        }
        else
        {
            Debug.LogWarning("ShowLevelSelect: Could not find 'Menu' GameObject.");
        }

        // Show second screen ("Menu (1)") — it's inactive so GameObject.Find() won't work.
        // Use GetRootGameObjects() to search inactive objects too.
        GameObject levelSelectGO = null;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "Menu (1)")
            {
                levelSelectGO = root;
                break;
            }
        }

        if (levelSelectGO != null)
        {
            levelSelectGO.SetActive(true);
            Debug.Log("ShowLevelSelect: Showed 'Menu (1)' level-select canvas.");
        }
        else
        {
            Debug.LogError("ShowLevelSelect: Could not find 'Menu (1)' in scene!");
        }
    }

    // -------------------------------------------------------
    // Call this from the BACK button anywhere!
    // If in Scene 0, it switches canvas. If in Level 1-10, it loads Scene 0.
    // -------------------------------------------------------
    public void GoBack()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // If we are already in the main menu scene (Scene 0)
        if (currentScene == "0")
        {
            // Hide level-select screen ("Menu (1)")
            GameObject levelSelectGO = null;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == "Menu (1)")
                {
                    levelSelectGO = root;
                    break;
                }
            }
            if (levelSelectGO != null)
            {
                levelSelectGO.SetActive(false);
                Debug.Log("GoBack: Hid 'Menu (1)' canvas.");
            }

            // Show main menu screen ("Menu")
            GameObject mainMenuGO = null;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == "Menu")
                {
                    mainMenuGO = root;
                    break;
                }
            }
            if (mainMenuGO != null)
            {
                mainMenuGO.SetActive(true);
                Debug.Log("GoBack: Showed 'Menu' canvas.");
            }
        }
        else
        {
            // We are inside a task/level scene. Load the main menu (Scene 0)
            Debug.Log("GoBack: Returning to Main Menu (Scene 0)...");
            AutoPlayOnRestart = false; 
            SceneManager.LoadScene("0", LoadSceneMode.Single);
        }
    }

    public void Start()
    {
        // FIX: Ensure Canvas is set up for VR/World Space
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                Debug.LogWarning("Canvas was not in World Space. Fixing it for VR visibility.");
                canvas.renderMode = RenderMode.WorldSpace;
            }

            if (transform.localScale == Vector3.zero)
            {
                Debug.LogWarning("UI Scale was zero! Resetting to default.");
                transform.localScale = Vector3.one * 0.001f;
            }
        }

        // Reposition menu in front of user (Task scenes only)
        RepositionMenuInFront();

        // Check if we just clicked Restart
        if (AutoPlayOnRestart)
        {
            Debug.Log("Auto-starting task due to Restart.");
            AutoPlayOnRestart = false; // Reset flag
            StartTask();
        }
        else
        {
            Debug.Log("Task1UI_Anim script started. Waiting for Start button click...");
            
            // Try to find animator if null
            if (ballAnimator == null)
            {
                // Check all known object names for the task ball across various scenes
                GameObject ball = GameObject.Find("EYE praveen1") ?? 
                                  GameObject.Find("Sphere 3") ?? 
                                  GameObject.Find("Sphere 2") ?? 
                                  GameObject.Find("Sphere 1") ?? 
                                  GameObject.Find("Sphere") ?? 
                                  GameObject.Find("praveen1");
                                  
                if (ball != null) ballAnimator = ball.GetComponent<Animator>();

                // Fallback: find ANY Animator in the scene that isn't part of the VR Rig
                if (ballAnimator == null)
                {
                    foreach (Animator anim in Object.FindObjectsOfType<Animator>(true))
                    {
                        if (anim.transform.root.GetComponent<Valve.VR.InteractionSystem.Player>() != null || 
                            anim.transform.root.GetComponentInChildren<SteamVR_Behaviour>() != null)
                            continue;

                        string n = anim.gameObject.name.ToLower();
                        if (n.Contains("hand") || n.Contains("player") || n.Contains("rig") || n.Contains("camera") || n.Contains("vr_glove") || n.Contains("model") || n.Contains("steamvr"))
                            continue;
                            
                        ballAnimator = anim;
                        Debug.Log($"[Task1UI_Anim] Auto-found fallback Animator on: {anim.gameObject.name}");
                        break;
                    }
                }
            }

            // Keep animator disabled until Start is pressed
            if (ballAnimator != null) ballAnimator.enabled = false;
        }
    }

    private void RepositionMenuInFront()
    {
        // DISABLED: The user prefers manual placement. 
        // If the menu is invisible, check its position in the Inspector.
        return; 
    }

    public void StartTask()
    {
        // Try to find animator automatically if not assigned
        if (ballAnimator == null)
        {
            GameObject ball = GameObject.Find("EYE praveen1") ?? 
                              GameObject.Find("Sphere 3") ?? 
                              GameObject.Find("Sphere 2") ?? 
                              GameObject.Find("Sphere 1") ?? 
                              GameObject.Find("Sphere") ?? 
                              GameObject.Find("praveen1");

            if (ball != null)
            {
                ballAnimator = ball.GetComponent<Animator>();
                if (ballAnimator != null) Debug.Log("StartTask: Found Ball Animator on: " + ball.name);
            }
            
            // Fallback: search ALL animators in scene
            if (ballAnimator == null)
            {
                foreach (Animator anim in Object.FindObjectsOfType<Animator>(true))
                {
                    if (anim.transform.root.GetComponent<Valve.VR.InteractionSystem.Player>() != null || 
                        anim.transform.root.GetComponentInChildren<SteamVR_Behaviour>() != null)
                        continue;

                    string n = anim.gameObject.name.ToLower();
                    if (n.Contains("hand") || n.Contains("player") || n.Contains("rig") || n.Contains("camera") || n.Contains("vr_glove") || n.Contains("model") || n.Contains("steamvr"))
                        continue;

                    ballAnimator = anim;
                    Debug.Log($"[StartTask] Fallback: Found Animator on '{anim.gameObject.name}'");
                    break;
                }
            }
        }

        DistractionTask distractionTask = Object.FindObjectOfType<DistractionTask>();

        // Check for Level 6-10 scripts
        Level6_SaccadicMovement    level6  = Object.FindObjectOfType<Level6_SaccadicMovement>();
        Level7_PeripheralAwareness level7  = Object.FindObjectOfType<Level7_PeripheralAwareness>();
        Level8_VisualProcessing    level8  = Object.FindObjectOfType<Level8_VisualProcessing>();
        Level9_ColorContrast       level9  = Object.FindObjectOfType<Level9_ColorContrast>();
        Level10_DepthMotion        level10 = Object.FindObjectOfType<Level10_DepthMotion>();
        bool hasLevelScript = level6 != null || level7 != null || level8 != null
                           || level9 != null || level10 != null;

        if (ballAnimator != null || distractionTask != null || hasLevelScript)
        {
            StartCoroutine(PlaySequence());
        }
        else
        {
            Debug.LogError("ERROR: Could not find Ball Animator, DistractionTask, or any Level script!");
        }
    }

    private IEnumerator PlaySequence()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string animToPlay = "";

        // ── SCENE 6: Saccadic Eye Movements ──────────────────────────────
        Level6_SaccadicMovement level6 = Object.FindObjectOfType<Level6_SaccadicMovement>();
        if (sceneName == "6" && level6 != null)
        {
            Canvas pCanvas = GetComponentInParent<Canvas>();
            if (pCanvas != null) pCanvas.enabled = false;
            level6.StartSequence();
            while (level6.isRunning) yield return null;
            if (pCanvas != null) pCanvas.enabled = true;
            yield break;
        }

        // ── SCENE 7: Dynamic Peripheral Awareness ────────────────────────
        Level7_PeripheralAwareness level7 = Object.FindObjectOfType<Level7_PeripheralAwareness>();
        if (sceneName == "7" && level7 != null)
        {
            Canvas pCanvas = GetComponentInParent<Canvas>();
            if (pCanvas != null) pCanvas.enabled = false;
            level7.StartSequence();
            while (level7.isRunning) yield return null;
            if (pCanvas != null) pCanvas.enabled = true;
            yield break;
        }

        // ── SCENE 8: Speed of Visual Processing ──────────────────────────
        Level8_VisualProcessing level8 = Object.FindObjectOfType<Level8_VisualProcessing>();
        if (sceneName == "8" && level8 != null)
        {
            Canvas pCanvas = GetComponentInParent<Canvas>();
            if (pCanvas != null) pCanvas.enabled = false;
            level8.StartSequence();
            while (level8.isRunning) yield return null;
            if (pCanvas != null) pCanvas.enabled = true;
            yield break;
        }

        // ── SCENE 9: Color & Contrast Differentiation ────────────────────
        Level9_ColorContrast level9 = Object.FindObjectOfType<Level9_ColorContrast>();
        if (sceneName == "9" && level9 != null)
        {
            Canvas pCanvas = GetComponentInParent<Canvas>();
            if (pCanvas != null) pCanvas.enabled = false;
            level9.StartSequence();
            while (level9.isRunning) yield return null;
            if (pCanvas != null) pCanvas.enabled = true;
            yield break;
        }

        // ── SCENE 10: Depth and Motion Tracking ──────────────────────────
        Level10_DepthMotion level10 = Object.FindObjectOfType<Level10_DepthMotion>();
        if (sceneName == "10" && level10 != null)
        {
            Canvas pCanvas = GetComponentInParent<Canvas>();
            if (pCanvas != null) pCanvas.enabled = false;
            level10.StartSequence();
            while (level10.isRunning) yield return null;
            if (pCanvas != null) pCanvas.enabled = true;
            yield break;
        }

        // ── SCENE 5: DistractionTask ──────────────────────────────────────
        DistractionTask distractionTask = Object.FindObjectOfType<DistractionTask>();
        if (sceneName == "5" && distractionTask != null)
        {
            Debug.Log($"Playing Distraction Task for scene: {sceneName}");
            distractionTask.StartSequence();
            Canvas pCanvas = GetComponentInParent<Canvas>();
            if (pCanvas != null) pCanvas.enabled = false;
            while (distractionTask.isRunning) yield return null;
            Debug.Log("Distraction Task ended. Showing UI.");
            if (pCanvas != null) pCanvas.enabled = true;
            yield break;
        }

        // ── SCENE 3: Pendulum / Path Tracking ─────────────────────────────
        PendulumMotion pendulum = Object.FindObjectOfType<PendulumMotion>();

        if (sceneName == "3")
        {
            if (ballAnimator != null) ballAnimator.enabled = true;

            if (pendulum != null)
            {
                Debug.Log($"[Scene 3] Starting Pendulum Motion on: {pendulum.gameObject.name}");
                if (ballAnimator != null) ballAnimator.enabled = false;
                pendulum.StartMotion();

                Canvas pCanvas = targetCanvas != null ? targetCanvas : GetComponentInParent<Canvas>();
                if (pCanvas != null) pCanvas.enabled = false;

                yield return new WaitForSeconds(pendulum.duration);

                Debug.Log("[Scene 3] Pendulum ended. Showing UI.");
                if (pCanvas != null) pCanvas.enabled = true;
            }
            else if (ballAnimator != null)
            {
                Debug.LogWarning("[Scene 3] No PendulumMotion found. Playing animator clip.");
                ballAnimator.gameObject.SetActive(true);
                ballAnimator.enabled = true;
                ballAnimator.Play("ballPathaTask4 1", 0, 0f); 
                Canvas pCanvas = targetCanvas != null ? targetCanvas : GetComponentInParent<Canvas>();
                if (pCanvas != null) pCanvas.enabled = false;
                yield return new WaitForSeconds(20f);
                if (pCanvas != null) pCanvas.enabled = true;
            }
            yield break;
        }

        // ── SCENE 4: Ball Path Task 4 ─────────────────────────────────────
        if (sceneName == "4")
        {
            Debug.Log("[Scene 4] Playing BallPathTask4 animation.");
            if (ballAnimator == null)
            {
                GameObject eye = GameObject.Find("EYE praveen1") ?? GameObject.Find("Sphere 3");
                if (eye != null) ballAnimator = eye.GetComponent<Animator>();
            }

            if (ballAnimator != null)
            {
                ballAnimator.gameObject.SetActive(true);
                ballAnimator.enabled = true;
                ballAnimator.Play("BallPathTask4", 0, 0f);

                Canvas pCanvas = targetCanvas != null ? targetCanvas : GetComponentInParent<Canvas>();
                if (pCanvas != null) pCanvas.enabled = false;

                yield return new WaitForSeconds(20f);

                Debug.Log("[Scene 4] Animation ended. Showing UI.");
                ballAnimator.enabled = false;
                if (pCanvas != null) pCanvas.enabled = true;
            }
            yield break;
        }

        // ── SCENES 1 & 2: Ball Path Animations ───────────────────────────
        if (ballAnimator == null)
        {
            foreach (Animator anim in Object.FindObjectsOfType<Animator>(true))
            {
                if (anim.transform.root.GetComponent<Valve.VR.InteractionSystem.Player>() != null || 
                    anim.transform.root.GetComponentInChildren<SteamVR_Behaviour>() != null)
                    continue;

                string n = anim.gameObject.name.ToLower();
                if (n.Contains("hand") || n.Contains("player") || n.Contains("controller") ||
                    n.Contains("steamvr") || n.Contains("camera") || n.Contains("vr_glove") || n.Contains("model")) 
                    continue;

                ballAnimator = anim;
                break;
            }
        }

        if (ballAnimator != null)
        {
            ballAnimator.gameObject.SetActive(true);
            ballAnimator.enabled = true;
            animToPlay = (sceneName == "2") ? "BallTask2" : "BallPathAnim";

            Debug.Log($"[Scene {sceneName}] Playing animation: {animToPlay}");
            ballAnimator.Play(animToPlay, 0, 0f);

            yield return null; 

            float duration = ballAnimator.GetCurrentAnimatorStateInfo(0).length;
            if (duration <= 0f) duration = 30f;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvas.enabled = false;

            yield return new WaitForSeconds(duration);

            Debug.Log("Animation ended. Showing UI.");
            if (canvas != null) canvas.enabled = true;
        }
    }

    public void RestartTask()
    {
        AutoPlayOnRestart = true; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    public void GoToTask1() { SceneManager.LoadScene("1", LoadSceneMode.Single); }
    public void GoToTask2() { SceneManager.LoadScene("2", LoadSceneMode.Single); }
    public void GoToTask3() { SceneManager.LoadScene("3", LoadSceneMode.Single); }
    public void GoToTask4() { SceneManager.LoadScene("4", LoadSceneMode.Single); }
    public void GoToTask5() { SceneManager.LoadScene("5", LoadSceneMode.Single); }
    public void GoToTask6() { SceneManager.LoadScene("6", LoadSceneMode.Single); }
    public void GoToTask7() { SceneManager.LoadScene("7", LoadSceneMode.Single); }
    public void GoToTask8() { SceneManager.LoadScene("8", LoadSceneMode.Single); }
    public void GoToTask9() { SceneManager.LoadScene("9", LoadSceneMode.Single); }
    public void GoToTask10() { SceneManager.LoadScene("10", LoadSceneMode.Single); }

    public void ExitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
