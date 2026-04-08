using UnityEngine;

/// <summary>
/// Orients a world-space canvas toward the player. Runs after companion follow scripts.
/// </summary>
[DefaultExecutionOrder(6000)]
public class BillboardUI : MonoBehaviour
{
    [Tooltip("HMD / main camera transform (not the companion root).")]
    public Transform head;

    [Tooltip("Keeps the canvas vertical so text stays upright.")]
    public bool lockToWorldUp = true;

    [Tooltip("World-space UI often needs a 180° Y flip so the front faces the camera.")]
    public bool flipY180ForCanvas = true;

    void LateUpdate()
    {
        if (head == null) return;

        Vector3 toViewer = head.position - transform.position;
        if (toViewer.sqrMagnitude < 0.0001f) return;

        Quaternion rot = lockToWorldUp
            ? Quaternion.LookRotation(toViewer, Vector3.up)
            : Quaternion.LookRotation(toViewer);

        if (flipY180ForCanvas)
            rot *= Quaternion.Euler(0f, 180f, 0f);

        transform.rotation = rot;
    }
}
