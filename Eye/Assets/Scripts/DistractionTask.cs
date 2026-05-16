using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DistractionTask : MonoBehaviour
{
    [Header("Spheres Setup")]
    [Tooltip("The center dot (praveen1) that appears first")]
    public GameObject centerDot;
    
    [Tooltip("The 8 distraction spheres placed around the center")]
    public GameObject[] distractionSpheres;
    
    [Header("Timings")]
    [Tooltip("How long to wait after center dot appears before showing the first distraction")]
    public float delayBeforeFirstDistraction = 3f;
    
    [Tooltip("How long each distraction sphere stays visible")]
    public float activeDuration = 3f;
    
    [Tooltip("Delay between one distraction disappearing and the next appearing (0 = immediate)")]
    public float delayBetweenSpheres = 0f;

    [HideInInspector]
    public bool isRunning = false;

    void Awake()
    {
        // First, ensure EVERYTHING is turned off when scene loads
        if (centerDot != null)
        {
            centerDot.SetActive(false);
        }
        
        foreach (GameObject sphere in distractionSpheres)
        {
            if (sphere != null)
                sphere.SetActive(false);
        }
    }

    public void StartSequence()
    {
        if (!isRunning)
        {
            StartCoroutine(SequenceCoroutine());
        }
    }

    private IEnumerator SequenceCoroutine()
    {
        isRunning = true;
        Debug.Log("Scene 5 Distraction Sequence Started!");

        // 1. Center dot appears
        if (centerDot != null)
        {
            centerDot.SetActive(true);
            Debug.Log("Center dot (praveen1) activated.");
        }

        // 2. Wait 3 seconds before first distraction (as requested)
        yield return new WaitForSeconds(delayBeforeFirstDistraction);

        // 3. Create a list of indexes 0 to 7 to shuffle them for random non-repeating order
        List<int> randomIndexes = new List<int>();
        for (int i = 0; i < distractionSpheres.Length; i++)
        {
            randomIndexes.Add(i);
        }

        // Shuffle the list
        for (int i = 0; i < randomIndexes.Count; i++)
        {
            int temp = randomIndexes[i];
            int randomIndex = Random.Range(i, randomIndexes.Count);
            randomIndexes[i] = randomIndexes[randomIndex];
            randomIndexes[randomIndex] = temp;
        }

        // 4. Go through each random sphere one by one
        foreach (int index in randomIndexes)
        {
            GameObject currentSphere = distractionSpheres[index];
            
            if (currentSphere != null)
            {
                // Turn the random distraction sphere ON
                currentSphere.SetActive(true);
                Debug.Log("Distraction Sphere " + currentSphere.name + " activated.");

                // Keep it active for exactly 3 seconds
                yield return new WaitForSeconds(activeDuration);

                // Turn it OFF
                currentSphere.SetActive(false);
            }
            
            // Optional delay before the next one appears
            if (delayBetweenSpheres > 0)
            {
                yield return new WaitForSeconds(delayBetweenSpheres);
            }
        }

        // 5. Sequence Finished
        if (centerDot != null)
        {
            centerDot.SetActive(false); // Hide center dot at the end
        }

        Debug.Log("Scene 5 Distraction Sequence Finished!");
        isRunning = false;
    }
}
