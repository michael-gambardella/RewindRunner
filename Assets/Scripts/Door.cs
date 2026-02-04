using UnityEngine;

/// <summary>
/// Door that opens when plate condition is met. Supports multiple plates and optional timed open.
/// Assign plate(s); door opens when condition is true (all plates pressed, or any plate pressed).
/// Optional: "Timed" = when condition becomes true, stay open for X seconds then close (for timed runs).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Door : MonoBehaviour
{
    [Header("Plates")]
    [Tooltip("Plates that control this door.")]
    [SerializeField] private PressurePlate[] plates;
    [Tooltip("If true: door opens only when ALL plates are pressed. If false: opens when ANY plate is pressed.")]
    [SerializeField] private bool requireAllPlates = true;

    [Header("Timed (optional)")]
    [Tooltip("If > 0, door stays open for this many seconds after opening, then closes (even if plates still pressed).")]
    [SerializeField] private float openDurationSeconds = 0f;
    private float _openTimer;

    private BoxCollider2D _collider;
    private SpriteRenderer _spriteRenderer;
    private bool _isOpen;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (plates == null || plates.Length == 0)
            Debug.LogWarning("Door: No plates assigned. Assign at least one PressurePlate in the Inspector.");
    }

    private void Update()
    {
        if (openDurationSeconds > 0f && _isOpen)
        {
            _openTimer -= Time.deltaTime;
            if (_openTimer <= 0f)
            {
                _isOpen = false;
                SetOpen(false);
            }
        }
        else
        {
            bool condition = CheckPlateCondition();
            if (condition != _isOpen)
            {
                _isOpen = condition;
                if (_isOpen && openDurationSeconds > 0f)
                    _openTimer = openDurationSeconds;
                SetOpen(_isOpen);
            }
        }
    }

    private bool CheckPlateCondition()
    {
        if (plates == null || plates.Length == 0) return false;
        if (requireAllPlates)
        {
            for (int i = 0; i < plates.Length; i++)
                if (plates[i] == null || !plates[i].IsPressed) return false;
            return true;
        }
        for (int i = 0; i < plates.Length; i++)
            if (plates[i] != null && plates[i].IsPressed) return true;
        return false;
    }

    private void SetOpen(bool open)
    {
        if (_collider != null)
            _collider.enabled = !open;
        if (_spriteRenderer != null)
            _spriteRenderer.enabled = !open;
    }
}
