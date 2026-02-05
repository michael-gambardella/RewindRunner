using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DeathZone : MonoBehaviour
{
    private void Reset()
    {
        var c = GetComponent<BoxCollider2D>();
        c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!other.CompareTag("Player"))
        {
            Debug.LogWarning($"DeathZone: '{other.name}' entered but is not tagged 'Player'. Select your player in the Hierarchy and set Tag to 'Player' in the Inspector.", other);
            return;
        }
        if (RespawnManager.Instance == null)
        {
            Debug.LogWarning("DeathZone: No RespawnManager in scene. Add a GameManager with RespawnManager and assign Player + Respawn Point.");
            return;
        }
        RespawnManager.Instance.KillPlayer();
    }
}
