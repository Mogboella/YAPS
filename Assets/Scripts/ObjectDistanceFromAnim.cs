using UnityEngine;

public class ObjectDistanceFromAnim : MonoBehaviour
{
    [Header("References")]
    public Transform head;        // XR/Main Camera
    public Animator animator;     // Inky Animator
    public InkySpeechBubble speechBubble;

    [Header("Left Shift Per Mood")]
    public float calmLeftShift = 0f;
    public float restlessLeftShift = 0.08f;
    public float urgentLeftShift = 0.16f;
    public float distressedLeftShift = 0.24f;

    [Header("Smoothing")]
    public float moveSmooth = 4f;
    public float rotateSmooth = 6f;

    [Header("Debug")]
    public bool useDebugMood = false;
    public int debugMood = 0;

    private static readonly int MoodHash = Animator.StringToHash("Mood");

    // Inky's original position relative to the camera
    private Vector3 baseLocalOffset;

    void Start()
    {
        if (head == null)
        {
            Debug.LogError("InkyFollower: Head is not assigned.");
            return;
        }

        // Save where Inky starts relative to the camera
        baseLocalOffset = head.InverseTransformPoint(transform.position);
    }

    void Update()
    {
        if (animator == null) return;

        if (useDebugMood && animator.GetInteger(MoodHash) != debugMood)
        {
            SetMood(debugMood);
        }

        // Keyboard testing
        //if (Input.GetKeyDown(KeyCode.Alpha0)) SetMood(0); // Calm
        //if (Input.GetKeyDown(KeyCode.Alpha1)) SetMood(1); // Restless
        //if (Input.GetKeyDown(KeyCode.Alpha2)) SetMood(2); // Urgent
        //if (Input.GetKeyDown(KeyCode.Alpha3)) SetMood(3); // Distressed
    }

    void LateUpdate()
    {
        if (head == null || animator == null) return;

        Vector3 localOffset = baseLocalOffset;

        // Move slightly left depending on mood
        localOffset.x -= GetLeftShiftForMood();

        // Rebuild the target world position from the camera's local space
        Vector3 targetPosition = head.TransformPoint(localOffset);

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            moveSmooth * Time.deltaTime
        );

        // Face the user
        Vector3 lookDir = head.position - transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotateSmooth * Time.deltaTime
            );
        }
    }

    float GetLeftShiftForMood()
    {
        int mood = animator.GetInteger(MoodHash);

        switch (mood)
        {
            case 0: return calmLeftShift;
            case 1: return restlessLeftShift;
            case 2: return urgentLeftShift;
            case 3: return distressedLeftShift;
            default: return calmLeftShift;
        }
    }

    public void SetMood(int mood)
    {
        if (animator == null) return;

        animator.SetInteger(MoodHash, mood);

        if (speechBubble != null)
            speechBubble.UpdateBubbleText();
    }
}