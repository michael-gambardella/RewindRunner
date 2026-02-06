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
    [Tooltip("For non-player targets: disable on hit, or send Kill message.")]
    [SerializeField] private bool disablePlayerOnHit = true;

    [Header("Visual")]
    [Tooltip("Color of the beam line (default red).")]
    [SerializeField] private Color beamColor = new Color(1f, 0.2f, 0.2f, 0.9f);
    [Tooltip("Width of the beam at start and end.")]
    [SerializeField] private float beamWidth = 0.15f;

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
        if (_lineRenderer != null)
        {
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 2;
            _lineRenderer.startWidth = beamWidth;
            _lineRenderer.endWidth = beamWidth * 0.5f;
            EnsureBeamMaterial();
            _lineRenderer.startColor = beamColor;
            _lineRenderer.endColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0.5f);
        }
    }

    /// <summary>
    /// Use a material that shows Beam Color. In URP, default Line Renderer material is often gray and ignores vertex color;
    /// we try vertex-color shaders first, then URP Unlit and set _BaseColor so the beam is the right color.
    /// </summary>
    private void EnsureBeamMaterial()
    {
        if (_lineRenderer == null) return;

        Shader shader = Shader.Find("Sprites/Default")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
        if (shader == null) return;

        _lineRenderer.material = new Material(shader);
        Material mat = _lineRenderer.material;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", beamColor);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", beamColor);
    }

    private void Start()
    {
        if (blockingLayer.value == 0) Debug.LogWarning("HazardBeam: Assign Blocking Layer (e.g. Ghost) in Inspector.");
        if (damageLayer.value == 0) Debug.LogWarning("HazardBeam: Assign Damage Layer (e.g. Default for player) in Inspector.");
    }

    private void Update()
    {
        UpdateBeamVisual();
    }

    private void UpdateBeamVisual()
    {
        if (_lineRenderer == null) return;
        Vector2 origin = transform.position;
        Vector2 dir = direction.normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
        Vector2 end = origin + dir * maxDistance;
        _lineRenderer.SetPosition(0, origin);
        _lineRenderer.SetPosition(1, end);
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
        if (target.CompareTag("Player"))
        {
            RespawnManager.Instance?.KillPlayer();
            return;
        }

        // optional fallback for non-player targets
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
