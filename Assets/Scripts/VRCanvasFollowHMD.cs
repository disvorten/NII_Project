using UnityEngine;

[DisallowMultipleComponent]
public sealed class VRCanvasFollowHMD : MonoBehaviour
{
    [Header("Target")]
    public Transform hmd;

    [Header("Follow")]
    public bool followRotation = true;
    public bool followPosition = false;

    [Tooltip("If true, only follow yaw (keep canvas upright).")]
    public bool yawOnly = true;

    [Header("Position (when followPosition=true)")]
    public float distance = 1.0f;
    public float heightOffset = 0.0f;
    public float lateralOffset = 0.0f;

    [Header("Smoothing")]
    [Range(0f, 30f)] public float rotationLerp = 0f;
    [Range(0f, 30f)] public float positionLerp = 0f;

    void LateUpdate()
    {
        if (hmd == null)
        {
            var cam = Camera.main;
            if (cam != null) hmd = cam.transform;
            else return;
        }

        if (followRotation)
        {
            Quaternion targetRot = hmd.rotation;
            if (yawOnly)
            {
                Vector3 e = targetRot.eulerAngles;
                targetRot = Quaternion.Euler(0f, e.y, 0f);
            }

            transform.rotation = rotationLerp > 0f
                ? Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotationLerp * Time.deltaTime))
                : targetRot;
        }

        if (followPosition)
        {
            Vector3 right = hmd.right;
            Vector3 forward = hmd.forward;
            if (yawOnly)
            {
                // remove vertical component to keep it on the horizon
                right.y = 0f;
                forward.y = 0f;
                right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;
                forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            }

            Vector3 targetPos =
                hmd.position
                + forward * distance
                + right * lateralOffset
                + Vector3.up * heightOffset;

            transform.position = positionLerp > 0f
                ? Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-positionLerp * Time.deltaTime))
                : targetPos;
        }
    }
}

