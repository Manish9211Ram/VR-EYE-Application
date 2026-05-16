using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Valve.VR;
using Valve.VR.Extras;
using System.Collections;

/// <summary>
/// Attach this to ANY GameObject in every scene (or to the Menu prefab).
/// It automatically runs at startup and ensures every level is VR-ready:
///   1. All Canvases → World Space with correct scale
///   2. All Buttons  → have a BoxCollider so laser-pointer can hit them
///   3. LaserPointers → InteractUI action auto-assigned if missing
///   4. Duplicate EventSystem / AudioListener removed
/// </summary>
public class VRSceneSetup : MonoBehaviour
{
    [Tooltip("Scale applied to canvases (0.0133 is the original setup scale)")]
    public float defaultCanvasScale = 0.0133f;

    [Tooltip("How far in front of the canvas to position it when no explicit position exists")]
    public float canvasFaceDistance = 1.5f;

    // Automatically inject this script into the game when it plays, even if the user forgot to attach it!
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrapper()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Find or create the VRSceneSetup manager
        VRSceneSetup existing = Object.FindObjectOfType<VRSceneSetup>();
        if (existing == null)
        {
            GameObject go = new GameObject("[Auto_VRSceneSetup]");
            existing = go.AddComponent<VRSceneSetup>();
        }
        
        // Force cleanup immediately upon scene load
        existing.CleanupDuplicates();
        existing.StartCoroutine(existing.DelayedSetup());
    }

    private void Awake()
    {
        // Awake may run if the user manually placed this in the scene.
        // It's safe to run multiple times, but OnSceneLoaded guarantees execution.
    }

    private IEnumerator DelayedSetup()
    {
        // Wait one frame so all Awake/Start calls finish first
        yield return null;

        SetupAllCanvases();
        AddCollidersToButtons();
        FixLaserPointerActions();
        FixFourHandsBug();
    }

    // ─────────────────────────────────────────────────────────────
    // 1. Remove duplicate EventSystem and AudioListener
    // ─────────────────────────────────────────────────────────────
    private void CleanupDuplicates()
    {
        // We disabled aggressive Player and SteamVR_Behaviour destruction because it breaks SteamVR tracking between scenes.
        // The 4-hands and vibration issues are already fixed by FixFourHandsBug() and box.isTrigger = true.

        var eventSystems = Object.FindObjectsOfType<EventSystem>(true);
        if (eventSystems.Length > 1)
        {
            EventSystem bestOne = null;
            foreach (var es in eventSystems)
            {
                if (es.transform.root.name.Contains("Player") || es.transform.root.name.Contains("Rig") || es.transform.root.name.Contains("SteamVR"))
                {
                    bestOne = es;
                    break;
                }
            }
            if (bestOne == null) bestOne = eventSystems[0];
            foreach (var es in eventSystems)
                if (es != bestOne) Destroy(es.gameObject);
            
            Debug.Log($"[VRSceneSetup] Removed {eventSystems.Length - 1} duplicate EventSystem(s). Kept {bestOne.name}");
        }

        var listeners = Object.FindObjectsOfType<AudioListener>(true);
        if (listeners.Length > 1)
        {
            AudioListener bestOne = null;
            // Prioritize keeping the one on the main camera or in the player rig
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
                
            Debug.Log($"[VRSceneSetup] Removed {listeners.Length - 1} duplicate AudioListener(s). Kept {bestOne.name}");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 2. Ensure every Canvas is World Space + sensible scale
    // ─────────────────────────────────────────────────────────────
    private void SetupAllCanvases()
    {
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true); // include inactive
        Camera vrCam = Camera.main;

        foreach (Canvas canvas in canvases)
        {
            // Skip sub-canvases (children of other canvases)
            if (canvas.transform.parent != null &&
                canvas.transform.parent.GetComponentInParent<Canvas>() != null)
                continue;

            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                Debug.Log($"[VRSceneSetup] Set '{canvas.name}' to World Space.");
            }

            // ONLY fix if scale is effectively zero (invisible)
            Vector3 s = canvas.transform.localScale;
            if (s.magnitude < 0.0001f)
            {
                canvas.transform.localScale = Vector3.one * defaultCanvasScale;
                Debug.Log($"[VRSceneSetup] Fixed zero-scale on '{canvas.name}'.");
            }

            // Assign world camera so UI raycasting works
            if (vrCam != null && canvas.worldCamera == null)
                canvas.worldCamera = vrCam;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 3. Add BoxCollider to every Button that lacks one
    //    (required for laser-pointer physics raycast to hit them)
    // ─────────────────────────────────────────────────────────────
    private void AddCollidersToButtons()
    {
        Button[] buttons = Object.FindObjectsOfType<Button>(true);
        int added = 0;

        foreach (Button btn in buttons)
        {
            if (btn.GetComponent<Collider>() != null) continue;

            RectTransform rt = btn.GetComponent<RectTransform>();
            if (rt == null) continue;

            BoxCollider box = btn.gameObject.AddComponent<BoxCollider>();
            // Size the collider to the button's rect (z = 10mm for depth)
            box.size = new Vector3(rt.rect.width, rt.rect.height, 10f);
            
            // Adjust center based on the pivot to ensure it covers the full canvas area
            box.center = new Vector3(
                (0.5f - rt.pivot.x) * rt.rect.width,
                (0.5f - rt.pivot.y) * rt.rect.height,
                0f
            );
            
            // IMPORTANT: Make it a trigger so it doesn't violently collide with the user's hands/camera, causing vibration
            box.isTrigger = true;
            added++;
        }

        if (added > 0)
            Debug.Log($"[VRSceneSetup] Added BoxCollider to {added} button(s).");
    }

    // ─────────────────────────────────────────────────────────────
    // 4. Auto-assign InteractUI SteamVR action to any LaserPointer
    //    whose interactWithUI field is null in the Inspector
    // ─────────────────────────────────────────────────────────────
    private void FixLaserPointerActions()
    {
        SteamVR_LaserPointer[] pointers = Object.FindObjectsOfType<SteamVR_LaserPointer>(true);
        int fixed_count = 0;

        foreach (SteamVR_LaserPointer lp in pointers)
        {
            if (lp.interactWithUI == null)
            {
                lp.interactWithUI = SteamVR_Input.GetBooleanAction("InteractUI");
                if (lp.interactWithUI != null) fixed_count++;
            }
        }

        if (fixed_count > 0)
            Debug.Log($"[VRSceneSetup] Auto-assigned InteractUI action to {fixed_count} LaserPointer(s).");
    }

    // ─────────────────────────────────────────────────────────────
    // 5. Force-hide the controllers and extra meshes to fix the "4 hands" bug
    // ─────────────────────────────────────────────────────────────
    private void FixFourHandsBug()
    {
        var hands = Object.FindObjectsOfType<Valve.VR.InteractionSystem.Hand>(true);
        foreach (var hand in hands)
        {
            // If the hand has a skeleton, we DON'T need the controller or fallback models
            if (hand.HasSkeleton())
            {
                hand.HideController(true); // forces the controller mesh off permanently
            }

            // Disable glowing highlight models if they're acting up as duplicate meshes
            if (hand.hoverhighlightRenderModel != null)
            {
                hand.hoverhighlightRenderModel.gameObject.SetActive(false);
            }
        }
    }
}
