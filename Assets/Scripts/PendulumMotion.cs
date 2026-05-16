using UnityEngine;

public class PendulumMotion : MonoBehaviour
{
    [Header("Pendulum Motion Settings")]
    [Tooltip("How wide the arc is (equivalent to string length)")]
    public float arcRadius = 2.5f;     
    public float maxAngle = 45f;       // Maximum swing angle in degrees
    public float swingSpeed = 2f;      // Speed of the swing
    public float duration = 30f;       // Duration of the motion in seconds
    
    public enum MoveAxis { X_Axis, Z_Axis }
    [Tooltip("X_Axis will swing Left/Right. Z_Axis will swing Forward/Back.")]
    public MoveAxis swingDirection = MoveAxis.X_Axis;

    private bool isSwinging = false;
    private float startTime;
    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        // Store the initial position so it always returns or starts correctly
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Update()
    {
        if (isSwinging)
        {
            float elapsed = Time.time - startTime;
            
            // Reached duration limit?
            if (elapsed <= duration)
            {
                // Calculate the current angle using Sine wave
                float currentAngleDegrees = maxAngle * Mathf.Sin(elapsed * swingSpeed);
                float currentAngleRad = currentAngleDegrees * Mathf.Deg2Rad;
                
                // Calculate pendulum math (X = sin, Y = 1 - cos)
                // Offset calculation (distance from resting position)
                float horizontalOffset = arcRadius * Mathf.Sin(currentAngleRad);
                float verticalOffset = -arcRadius * (1f - Mathf.Cos(currentAngleRad)); 
                
                Vector3 newPosition = startPosition;

                // Apply Left to Right (X) or Forward to Back (Z) depending on choice
                if (swingDirection == MoveAxis.X_Axis)
                {
                    newPosition.x += horizontalOffset;
                    // Rotate on Z to tilt left/right during swing
                    transform.rotation = startRotation * Quaternion.Euler(0, 0, -currentAngleDegrees);
                }
                else
                {
                    newPosition.z += horizontalOffset;
                    // Rotate on X to tilt forward/back during swing
                    transform.rotation = startRotation * Quaternion.Euler(currentAngleDegrees, 0, 0);
                }
                
                // Apply the slight up/down curve to make it a perfect arc
                newPosition.y += verticalOffset;
                
                transform.position = newPosition;
            }
            else
            {
                // Stop swinging after the set duration
                isSwinging = false;
                
                // Reset to exact start position when done
                transform.position = startPosition;
                transform.rotation = startRotation;
                Debug.Log("Pendulum motion finished after " + duration + " seconds.");
            }
        }
    }

    // Call this function from your Start Button's OnClick event
    public void StartMotion()
    {
        if (!isSwinging)
        {
            Debug.Log("Pendulum motion started!");
            startPosition = transform.position; // refresh just in case
            startRotation = transform.rotation;
            isSwinging = true;
            startTime = Time.time;
        }
    }
}
