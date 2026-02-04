using UnityEngine;

/// <summary>
/// Raycast beam that kills the player unless blocked by the Ghost.
/// Place on a GameObject; beam shoots in local right direction (or set direction in Inspector).
/// Blocking layer = Ghost; damage layer = Default (player). First hit wins: Ghost blocks, Player dies.
/// </summary>
public class HazardBeam : MonoBehaviour
{
    [Header("Beam")]
    [Tooltip("World direction of the beam (e.g. right = 1,0).")]
    [SerializeField] private Vector2 direction = Vector2.right;
    [Tooltip("Max distance to cast.")]
    [SerializeField] private float maxDistance = 10f;

    [Header("Layers")]
    [SerializeField] private LayerMask blockingLayer;
    [SerializeField] private LayerMask damageLayer;

    [Header("Kill")]
    [Tooltip("If set, player GameObject is disabled on hit. Otherwise send message Kill() or use your own logic.")]
    [SerializeField] private bool disablePlayerOnHit = true;

    private void Start()
    {
        if (blockingLayer.value == 0) Debug.LogWarning("HazardBeam: Assign Blocking Layer (e.g. Ghost) in Inspector.");
        if (damageLayer.value == 0) Debug.LogWarning("HazardBeam: Assign Damage Layer (e.g. Default for player) in Inspector.");
    }

    private void FixedUpdate()
    {
        Vector2 origin = transform.position;
        Vector2 dir = direction.normalized;
        if (dir.sqrMagnitude < 0.01f) return;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, maxDistance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null) continue;
            int layer = hits[i].collider.gameObject.layer;
            if (((1 << layer) & blockingLayer) != 0)
                return;
            if (((1 << layer) & damageLayer) != 0)
            {
                KillTarget(hits[i].collider.gameObject);
                return;
            }
        }
    }

    private void KillTarget(GameObject target)
    {
        if (disablePlayerOnHit)
            target.SetActive(false);
        else
            target.SendMessage("Kill", SendMessageOptions.DontRequireReceiver);
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 origin = transform.position;
        Vector2 dir = direction.normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + dir * maxDistance);
    }
}
