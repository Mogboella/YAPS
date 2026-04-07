using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    public Transform head;

    void LateUpdate()
    {
        if (head == null) return;

        Vector3 direction = transform.position - head.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(direction);
    }
}
