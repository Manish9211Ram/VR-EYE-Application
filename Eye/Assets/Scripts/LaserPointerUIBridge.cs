using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using Valve.VR.Extras;

public class LaserPointerUIBridge : MonoBehaviour
{
    private SteamVR_LaserPointer laserPointer;

    void Start()
    {
        // Get the Laser Pointer script on this hand
        laserPointer = GetComponent<SteamVR_LaserPointer>();
        if (laserPointer != null)
        {
            // Auto-fix missing InteractUI action (common when scene is loaded fresh or Player is DontDestroyOnLoad)
            if (laserPointer.interactWithUI == null)
            {
                laserPointer.interactWithUI = SteamVR_Input.GetBooleanAction("InteractUI");
                if (laserPointer.interactWithUI != null)
                    Debug.Log("LaserPointerUIBridge: Auto-assigned InteractUI action to " + gameObject.name);
                else
                    Debug.LogWarning("LaserPointerUIBridge: Could not find 'InteractUI' SteamVR action. Check your SteamVR Input bindings.");
            }

            // Subscribe to the click event
            laserPointer.PointerClick += OnPointerClick;
        }
        else
        {
            Debug.LogError("LaserPointerUIBridge requires a SteamVR_LaserPointer component on the same GameObject!");
        }
    }

    private void OnPointerClick(object sender, PointerEventArgs e)
    {
        // Did we click an object?
        if (e.target != null)
        {
            // Does the object have a standard Unity Button?
            Button btn = e.target.GetComponent<Button>();
            if (btn != null)
            {
                // Push the button artificially!
                btn.onClick.Invoke();
                Debug.Log("Successfully clicked the " + e.target.name + " button via Laser!");
            }
        }
    }

    void OnDestroy()
    {
        if (laserPointer != null)
        {
            laserPointer.PointerClick -= OnPointerClick;
        }
    }
}
