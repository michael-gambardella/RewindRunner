using UnityEngine;

/// <summary>
/// Central place for juice: camera shake, screen tint, whoosh, squish/stretch, ghost trail.
/// Add ONE GameJuice to the scene (e.g. on Main Camera or empty "GameJuice" GameObject).
/// Assign optional references; leave blank to skip that feature.
///
/// SCENE SETUP (step by step):
/// 1) Camera shake: Add CameraShake to Main Camera. Set Script Execution Order so CameraShake runs after CameraFollow2D.
/// 2) Screen tint: Add ScreenTint to same object as GameJuice (or child). It creates a Canvas+Image at runtime if Tint Image is null.
/// 3) Whoosh: Add AudioSource to Main Camera (or this object). Assign Jump Clip and Rewind Start/Stop Clips in GameJuice.
/// 4) Squish/stretch: Assign "Squish Transform" to the player's visual (e.g. the sprite child Transform). Leave null to skip.
/// 5) Ghost trail: Add a TrailRenderer to the Player (e.g. as child). Assign it to "Player Trail"; it will emit only during rewind.
/// </summary>
public class GameJuice : MonoBehaviour
{
    public static GameJuice Instance { get; private set; }

    [Header("Camera shake")]
    [Tooltip("Optional. Add CameraShake to Main Camera and assign here (or leave null to find on same object).")]
    [SerializeField] private CameraShake cameraShake;
    [Tooltip("Rewind start: duration and intensity. Lower intensity = less disorienting.")]
    [SerializeField] private float rewindShakeDuration = 0.12f;
    [SerializeField] private float rewindShakeIntensity = 0.06f;
    [Tooltip("Rewind stop (when ghost spawns).")]
    [SerializeField] private float rewindStopShakeDuration = 0.08f;
    [SerializeField] private float rewindStopShakeIntensity = 0.04f;

    [Header("Screen tint")]
    [Tooltip("Optional. Add ScreenTint to scene; assign here or leave null to find on same object.")]
    [SerializeField] private ScreenTint screenTint;

    [Header("Whoosh (audio)")]
    [Tooltip("Optional. AudioSource to play one-shots (e.g. on Main Camera).")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpClip;
    [Tooltip("Play when rewind starts (hold Shift).")]
    [SerializeField] private AudioClip rewindStartClip;
    [Tooltip("Play when rewind ends and ghost spawns.")]
    [SerializeField] private AudioClip rewindStopClip;
    [Range(0f, 1f)] [SerializeField] private float whooshVolume = 0.6f;

    [Header("Squish / stretch")]
    [Tooltip("Optional. Transform to scale for jump (stretch up) and land (squish). Use player sprite child so physics is unchanged.")]
    [SerializeField] private Transform squishTransform;
    [SerializeField] private Vector2 jumpScale = new Vector2(0.85f, 1.2f);
    [SerializeField] private Vector2 landScale = new Vector2(1.15f, 0.85f);
    [SerializeField] private float squishRecoverTime = 0.12f;
    private Vector3 _squishBaseScale = Vector3.one;
    private float _squishRecoverTimer;

    [Header("Ghost trail")]
    [Tooltip("Optional. TrailRenderer on the player; emitting is turned on during rewind only.")]
    [SerializeField] private TrailRenderer playerTrail;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (cameraShake == null) cameraShake = GetComponent<CameraShake>();
        if (screenTint == null) screenTint = GetComponent<ScreenTint>();
        if (squishTransform != null) _squishBaseScale = squishTransform.localScale;
    }

    private void Update()
    {
        if (squishTransform != null && _squishRecoverTimer > 0f)
        {
            _squishRecoverTimer -= Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(_squishRecoverTimer / squishRecoverTime);
            squishTransform.localScale = Vector3.Lerp(squishTransform.localScale, _squishBaseScale, t);
        }
    }

    /// <summary>Call when the player jumps.</summary>
    public void OnJump()
    {
        if (cameraShake != null) cameraShake.Shake(0.08f, 0.06f);
        if (screenTint != null) screenTint.TintJump();
        if (audioSource != null && jumpClip != null) audioSource.PlayOneShot(jumpClip, whooshVolume);
        if (squishTransform != null)
        {
            squishTransform.localScale = new Vector3(jumpScale.x * _squishBaseScale.x, jumpScale.y * _squishBaseScale.y, _squishBaseScale.z);
            _squishRecoverTimer = squishRecoverTime;
        }
    }

    /// <summary>Call when rewind starts (player holds Shift).</summary>
    public void OnRewindStart()
    {
        if (cameraShake != null) cameraShake.Shake(rewindShakeDuration, rewindShakeIntensity);
        if (screenTint != null) screenTint.TintRewind();
        if (audioSource != null && rewindStartClip != null) audioSource.PlayOneShot(rewindStartClip, whooshVolume);
        if (playerTrail != null) playerTrail.emitting = true;
    }

    /// <summary>Call when rewind ends (ghost spawns).</summary>
    public void OnRewindStop()
    {
        if (cameraShake != null) cameraShake.Shake(rewindStopShakeDuration, rewindStopShakeIntensity);
        if (audioSource != null && rewindStopClip != null) audioSource.PlayOneShot(rewindStopClip, whooshVolume);
        if (playerTrail != null) playerTrail.emitting = false;
    }

    /// <summary>Call when the player lands on ground.</summary>
    public void OnLand()
    {
        if (cameraShake != null) cameraShake.Shake(0.1f, 0.08f);
        if (squishTransform != null)
        {
            squishTransform.localScale = new Vector3(landScale.x * _squishBaseScale.x, landScale.y * _squishBaseScale.y, _squishBaseScale.z);
            _squishRecoverTimer = squishRecoverTime;
        }
    }
}
