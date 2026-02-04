using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns platforms at runtime from a list of positions and scales.
/// Use for quick tutorial blocking: assign a platform prefab and tweak the list, then press Play.
/// Prefab must have Box Collider 2D and be on the Ground layer (or it will be set at runtime).
/// </summary>
public class TutorialLevelBuilder : MonoBehaviour
{
    [System.Serializable]
    public struct PlatformEntry
    {
        public Vector2 position;
        public Vector2 scale;
    }

    [Header("Prefab")]
    [Tooltip("Platform prefab with Sprite Renderer + Box Collider 2D. Set its layer to Ground.")]
    [SerializeField] private GameObject platformPrefab;

    [Header("Platforms (position = center, scale = width x height)")]
    [SerializeField] private List<PlatformEntry> platforms = new List<PlatformEntry>
    {
        new PlatformEntry { position = new Vector2(-1f, -2f), scale = new Vector2(8f, 1f) },
        new PlatformEntry { position = new Vector2(6f, -2f), scale = new Vector2(4f, 1f) },
        new PlatformEntry { position = new Vector2(11f, -2f), scale = new Vector2(6f, 1f) },
        new PlatformEntry { position = new Vector2(14f, 0f), scale = new Vector2(4f, 1f) }
    };

    [Header("Optional")]
    [Tooltip("Layer index (0–31) for spawned platforms. Set to Ground layer index (e.g. 6) so jump works. -1 = don't override prefab layer.")]
    [SerializeField] private int platformLayerIndex = 6;

    private void Awake()
    {
        if (platformPrefab == null)
        {
            Debug.LogWarning("TutorialLevelBuilder: No platform prefab assigned. Create a platform with Box Collider 2D, set Layer to Ground, save as prefab, then assign it here.");
            return;
        }

        for (int i = 0; i < platforms.Count; i++)
        {
            PlatformEntry e = platforms[i];
            GameObject p = Instantiate(platformPrefab, new Vector3(e.position.x, e.position.y, 0f), Quaternion.identity, transform);
            p.name = $"Platform_{i + 1}";
            p.transform.localScale = new Vector3(e.scale.x, e.scale.y, 1f);

            if (platformLayerIndex >= 0 && platformLayerIndex <= 31)
                p.layer = platformLayerIndex;
        }
    }
}
