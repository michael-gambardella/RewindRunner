using UnityEngine;

/// <summary>
/// Generic hazard: spikes, saws, lava, etc. Attach to any GameObject with a Collider2D set to IsTrigger.
/// On contact with the player, calls RespawnManager to kill/respawn.
/// For spikes: set Use Tip Only so only the upper part (tip) is deadly, not the underside/base.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HazardKill : MonoBehaviour
{
    [Tooltip("If true, only the tip (top fraction) of the hazard kills the player; the underside/base is safe. For upward spikes, tip = top.")]
    [SerializeField] private bool useTipOnly = true;
    [Tooltip("Fraction of the hazard height that is deadly (0.3 = top 30%% is deadly). Ignored if Use Tip Only is off.")]
    [SerializeField] [Range(0.15f, 0.7f)] private float deadlyHeightFraction = 0.35f;

    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }

    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c != null)
            c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || !other.CompareTag("Player")) return;
        if (!useTipOnly)
        {
            RespawnManager.Instance?.KillPlayer();
            return;
        }
        if (_collider == null) _collider = GetComponent<Collider2D>();
        if (_collider == null) return;

        // Only kill if the player is in the "tip" zone (upper part). Assumes spike points up (tip at max Y).
        Bounds spikeBounds = _collider.bounds;
        float tipZoneBottom = spikeBounds.max.y - spikeBounds.size.y * deadlyHeightFraction;
        float playerFeetY = other.bounds.min.y;
        if (playerFeetY >= tipZoneBottom)
            RespawnManager.Instance?.KillPlayer();
    }
}
