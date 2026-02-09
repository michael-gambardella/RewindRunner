using UnityEngine;

/// <summary>
/// Adds screen shake. Add to the Main Camera (same as CameraFollow2D).
/// Set Script Execution Order so this runs AFTER CameraFollow2D (e.g. CameraFollow2D 0, CameraShake 10).
/// </summary>
public class CameraShake : MonoBehaviour
{
    [Header("Default")]
    [Tooltip("Default duration if not passed to Shake().")]
    [SerializeField] private float defaultDuration = 0.15f;
    [Tooltip("Default intensity (world units).")]
    [SerializeField] private float defaultIntensity = 0.15f;

    private float _timeLeft;
    private float _intensity;
    private Vector3 _lastOffset;

    /// <summary>Trigger a shake. Call from GameJuice (e.g. on jump, rewind start/stop).</summary>
    public void Shake(float? duration = null, float? intensity = null)
    {
        _timeLeft = duration ?? defaultDuration;
        _intensity = intensity ?? defaultIntensity;
    }

    private void LateUpdate()
    {
        if (_timeLeft <= 0f)
        {
            if (_lastOffset.sqrMagnitude > 0.0001f)
            {
                transform.position -= _lastOffset;
                _lastOffset = Vector3.zero;
            }
            return;
        }

        float strength = _intensity * (_timeLeft / defaultDuration);
        Vector3 offset = new Vector3(
            (Random.value - 0.5f) * 2f * strength,
            (Random.value - 0.5f) * 2f * strength,
            0f
        );
        transform.position -= _lastOffset;
        transform.position += offset;
        _lastOffset = offset;
        _timeLeft -= Time.unscaledDeltaTime;
    }
}
