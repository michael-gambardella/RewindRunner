using UnityEngine;

/// <summary>
/// Makes the camera follow a target (e.g. the player) in 2D.
/// Use LateUpdate so the camera moves after the player has moved.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [Tooltip("How fast the camera catches up. 0 = instant, higher = smoother follow.")]
    [SerializeField] private float smoothTime = 0.2f;
    [Tooltip("Z position to keep (2D cameras usually use -10).")]
    [SerializeField] private float cameraZ = -10f;

    private Vector3 _velocity;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 current = transform.position;
        Vector3 goal = new Vector3(target.position.x, target.position.y, cameraZ);

        transform.position = Vector3.SmoothDamp(current, goal, ref _velocity, smoothTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position);
    }
}
