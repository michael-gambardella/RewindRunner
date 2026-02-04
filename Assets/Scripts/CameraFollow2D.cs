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

    [Header("Dead Zone (optional)")]
    [Tooltip("Half-size of a box centered on the camera. Camera doesn't move until the target leaves this box. Set to (0,0) to disable. e.g. (2, 1) = 4 units wide, 2 units tall.")]
    [SerializeField] private Vector2 deadZoneHalfSize = Vector2.zero;

    private Vector3 _velocity;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 current = transform.position;
        float goalX = target.position.x;
        float goalY = target.position.y;

        if (deadZoneHalfSize.x > 0f || deadZoneHalfSize.y > 0f)
        {
            float dx = target.position.x - current.x;
            float dy = target.position.y - current.y;
            if (Mathf.Abs(dx) <= deadZoneHalfSize.x && Mathf.Abs(dy) <= deadZoneHalfSize.y)
            {
                goalX = current.x;
                goalY = current.y;
            }
        }

        Vector3 goal = new Vector3(goalX, goalY, cameraZ);
        transform.position = Vector3.SmoothDamp(current, goal, ref _velocity, smoothTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position);
        if (deadZoneHalfSize.x > 0f || deadZoneHalfSize.y > 0f)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Gizmos.DrawWireCube(transform.position, new Vector3(deadZoneHalfSize.x * 2f, deadZoneHalfSize.y * 2f, 0.01f));
        }
    }
}
