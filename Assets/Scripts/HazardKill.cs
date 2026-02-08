using UnityEngine;

/// <summary>
/// Generic hazard: spikes, saws, lava, etc. Attach to any GameObject with a Collider2D set to IsTrigger.
/// On contact with the player, calls RespawnManager to kill/respawn.
/// Use DeathZone for pits/void; use HazardKill for solid-looking hazards (spikes, saws).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HazardKill : MonoBehaviour
{
    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c != null)
            c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player"))
            RespawnManager.Instance?.KillPlayer();
    }
}
