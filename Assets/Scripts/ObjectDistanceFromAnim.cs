using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;
using YAPS;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Places the companion using the camera's world right/up/forward (true view axes).
/// Avoids parenting to the camera by default so placement matches what you see in Game/Scene views.
/// </summary>
[DefaultExecutionOrder(5000)]
public class ObjectDistanceFromAnim : MonoBehaviour
{
    [Header("References")]
    public Transform head;        // Fallback if no XROrigin
    public Animator animator;
    public InkySpeechBubble speechBubble;

    [Header("Placement (camera-relative, world basis)")]
    [Tooltip("If true, use Camera Local Offset. If false, offset is captured once at play (see Auto Capture Delay).")]
    public bool useManualOffset = true;

    [Tooltip("Along camera right(+)/left(-), up(+)/down(-), and forward = distance in front of the lens along camera.forward.")]
    public Vector3 cameraLocalOffset = new Vector3(0.45f, -0.35f, 1.35f);

    [Tooltip("Off by default: parenting under the HMD often desyncs from the real view frustum. Prefer world follow.")]
    public bool parentCompanionToXRCamera = false;

    [Tooltip("When not parenting to the camera, keep Inky under this transform (e.g. Inky_SYSTEM). Used to fix hierarchy if it was previously parented to the HMD.")]
    public Transform companionHierarchyParent;

    [Tooltip("Resolve HMD via XROrigin.Camera when available.")]
    public bool useXROriginCamera = true;

    [Tooltip("When not using manual offset: wait before sampling (XR startup).")]
    [Min(0f)]
    public float autoCaptureDelaySeconds = 0.35f;

    [Tooltip("Distance in front of the camera along camera.forward (meters).")]
    public float minCameraForward = 0.5f;
    public float maxCameraForward = 4f;

    [Tooltip("If still behind the lens, nudge along true camera.forward.")]
    public bool enforceInFrontOfView = true;

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

    private Vector3 capturedBaseLocalOffset;
    private bool hasCapturedBaseOffset;
    private bool warnedMissingHead;
    private Coroutine setupRoutine;

    /// <summary>Forwarded to parent <see cref="CompanionVisualController"/> — animation events only fire on this object (Animator root), not on parents.</summary>
    public void TriggerStressBurst()
    {
        var visual = GetComponentInParent<CompanionVisualController>();
        if (visual != null)
            visual.TriggerStressBurst();
    }

    [ContextMenu("Capture offset from head")]
    void CaptureOffsetFromHead()
    {
        Transform cam = ResolveHeadTransform();
        if (cam == null)
        {
            Debug.LogWarning("[ObjectDistanceFromAnim] No head / XR camera to capture from.", this);
            return;
        }

        cameraLocalOffset = DecomposeWorldOffsetFromCamera(cam, transform.position);
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[ObjectDistanceFromAnim] cameraLocalOffset set to {cameraLocalOffset}", this);
    }

    /// <summary>Camera-space offset using world basis (matches our follow math).</summary>
    static Vector3 DecomposeWorldOffsetFromCamera(Transform cam, Vector3 worldPosition)
    {
        Vector3 w = worldPosition - cam.position;
        return new Vector3(
            Vector3.Dot(w, cam.right),
            Vector3.Dot(w, cam.up),
            Vector3.Dot(w, cam.forward));
    }

    void OnEnable()
    {
        if (Application.isPlaying && setupRoutine == null)
            setupRoutine = StartCoroutine(SetupFollowRoutine());
    }

    void OnDisable()
    {
        if (setupRoutine != null)
        {
            StopCoroutine(setupRoutine);
            setupRoutine = null;
        }
    }

    Transform ResolveHeadTransform()
    {
        if (useXROriginCamera)
        {
            var origin = Object.FindFirstObjectByType<XROrigin>();
            if (origin != null && origin.Camera != null)
                return origin.Camera.transform;
        }

        if (head != null)
            return head;

        if (Camera.main != null)
            return Camera.main.transform;

        return null;
    }

    IEnumerator SetupFollowRoutine()
    {
        yield return null;
        yield return null;

        Transform cam = ResolveHeadTransform();
        if (cam == null)
        {
            if (!warnedMissingHead)
            {
                warnedMissingHead = true;
                Debug.LogError("[ObjectDistanceFromAnim] No XR Origin camera, head, or Camera.main. Assign head or add XROrigin.", this);
            }
            setupRoutine = null;
            yield break;
        }

        if (!parentCompanionToXRCamera && transform.parent == cam)
        {
            transform.SetParent(companionHierarchyParent, true);
        }

        if (parentCompanionToXRCamera)
        {
            transform.SetParent(cam, true);

            if (useManualOffset)
            {
                Vector3 w = cam.position + cam.right * cameraLocalOffset.x + cam.up * cameraLocalOffset.y +
                            cam.forward * Mathf.Clamp(Mathf.Abs(cameraLocalOffset.z), minCameraForward, maxCameraForward);
                transform.position = w;
                capturedBaseLocalOffset = cameraLocalOffset;
            }
            else
            {
                capturedBaseLocalOffset = DecomposeWorldOffsetFromCamera(cam, transform.position);
                SanitizeOffset(ref capturedBaseLocalOffset);
                ApplyWorldTargetFromOffset(cam, capturedBaseLocalOffset);
            }

            hasCapturedBaseOffset = true;
        }
        else
        {
            if (!useManualOffset)
            {
                if (autoCaptureDelaySeconds > 0f)
                    yield return new WaitForSecondsRealtime(autoCaptureDelaySeconds);

                cam = ResolveHeadTransform();
                if (cam == null)
                {
                    setupRoutine = null;
                    yield break;
                }

                capturedBaseLocalOffset = DecomposeWorldOffsetFromCamera(cam, transform.position);
                SanitizeOffset(ref capturedBaseLocalOffset);
                hasCapturedBaseOffset = true;
            }
            else
                hasCapturedBaseOffset = true;
        }

        setupRoutine = null;
    }

    void SanitizeOffset(ref Vector3 off)
    {
        float depth = Mathf.Clamp(Mathf.Abs(off.z), minCameraForward, maxCameraForward);
        off.z = Mathf.Sign(off.z) * depth;
        if (Mathf.Abs(off.z) < minCameraForward * 0.5f)
            off.z = minCameraForward;

        float horiz = Mathf.Sqrt(off.x * off.x + off.y * off.y);
        if (horiz > 3f)
        {
            float s = 3f / horiz;
            off.x *= s;
            off.y *= s;
        }
    }

    void ApplyWorldTargetFromOffset(Transform cam, Vector3 off)
    {
        float depth = Mathf.Clamp(Mathf.Abs(off.z), minCameraForward, maxCameraForward);
        transform.position = cam.position + cam.right * off.x + cam.up * off.y + cam.forward * depth;
    }

    void Update()
    {
        if (animator == null) return;

        if (useDebugMood && animator.GetInteger(MoodHash) != debugMood)
            SetMood(debugMood);
    }

    void LateUpdate()
    {
        if (animator == null) return;

        Transform cam = ResolveHeadTransform();
        if (cam == null)
        {
            if (!warnedMissingHead)
            {
                warnedMissingHead = true;
                Debug.LogError("[ObjectDistanceFromAnim] Head transform missing.", this);
            }
            return;
        }

        if (!hasCapturedBaseOffset)
            return;

        Vector3 baseOffset = useManualOffset ? cameraLocalOffset : capturedBaseLocalOffset;
        Vector3 off = baseOffset;
        off.x -= GetLeftShiftForMood();

        float depth = Mathf.Clamp(Mathf.Abs(off.z), minCameraForward, maxCameraForward);
        Vector3 targetWorld = cam.position + cam.right * off.x + cam.up * off.y + cam.forward * depth;

        transform.position = Vector3.Lerp(transform.position, targetWorld, moveSmooth * Time.deltaTime);

        Vector3 lookDir = cam.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSmooth * Time.deltaTime);
        }

        if (enforceInFrontOfView)
            EnforceMinimumForwardDistance(cam);
    }

    void EnforceMinimumForwardDistance(Transform view)
    {
        if (view == null) return;

        Vector3 origin = view.position;
        Vector3 fwd = view.forward;
        Vector3 toCompanion = transform.position - origin;
        float along = Vector3.Dot(toCompanion, fwd);
        if (along >= minCameraForward)
            return;

        float lateralR = Vector3.Dot(toCompanion, view.right);
        float lateralU = Vector3.Dot(toCompanion, view.up);
        transform.position = origin + fwd * minCameraForward + view.right * lateralR + view.up * lateralU;
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

        var visuals = GetComponentInParent<CompanionVisualController>();
        if (visuals != null)
            visuals.SetUrgencyFromMood(mood);
    }
}
