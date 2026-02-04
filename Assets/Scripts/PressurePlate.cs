using UnityEngine;

/// <summary>
/// Trigger volume that counts how many objects (Player + Ghost) are on it.
/// Use for doors: assign this plate to a Door; door opens when count > 0 (or when all linked plates are pressed).
/// Set BoxCollider2D to Is Trigger. Assign "Pressing Layers" to Default + Ghost.
/// Important: put this GameObject on layer Default (not Ground), so Ghost can trigger it—Ghost does not interact with Ground in the collision matrix.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PressurePlate : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Layers that count as 'standing on' the plate (e.g. Default + Ghost). This GameObject should be on layer Default so the Ghost can trigger it.")]
    [SerializeField] private LayerMask pressingLayers;

    [Header("Visual")]
    [SerializeField] private Color unpressedColor = Color.white;
    [SerializeField] private Color pressedColor = new Color(0.6f, 0.9f, 0.6f);

    private int _count;
    private SpriteRenderer _spriteRenderer;

    /// <summary> True when at least one object is on the plate. </summary>
    public bool IsPressed => _count > 0;

    /// <summary> Number of objects currently on the plate. </summary>
    public int Count => _count;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
            _spriteRenderer.color = unpressedColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & pressingLayers) != 0)
        {
            _count++;
            UpdateVisual();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & pressingLayers) != 0)
        {
            _count = Mathf.Max(0, _count - 1);
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.color = _count > 0 ? pressedColor : unpressedColor;
    }
}
