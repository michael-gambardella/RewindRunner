using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct RecordedFrame
{
    public Vector3 position;
    public Vector2 velocity;
    public float time;
}

/// <summary>
/// 2D platformer with one-button time rewind. Records positions for replay;
/// past self plays back as a ghost to help solve jumps/puzzles (Braid-like).
/// </summary>
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 80f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float coyoteTime = 0.08f;      // small grace window
    [SerializeField] private float jumpBuffer = 0.10f;       // press slightly early
    [SerializeField] private float fallGravityMultiplier = 2.0f;
    [SerializeField] private float lowJumpGravityMultiplier = 2.0f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);
    [Tooltip("Ray down from feet; if this hits Ground layer, you're grounded. Increase if jump still fails.")]
    [SerializeField] private float groundCheckDistance = 0.25f;
    [SerializeField] private LayerMask groundMask;

    [Header("Rewind (Braid-like)")]
    [SerializeField] private float rewindDurationSeconds = 6f;
    [Tooltip("Smoothing time when rewinding (lower = snappier, higher = smoother). 0.02–0.05 works well.")]
    [SerializeField] private float rewindSmoothTime = 0.03f;
    [Tooltip("Optional: prefab to spawn as 'ghost' that replays rewound segment.")]
    [SerializeField] private GameObject ghostPrefab;

    [Header("Debug")]
    [Tooltip("Enable to log ground/input state every 0.5s in Console (to find why jump/rewind don't work).")]
    [SerializeField] private bool debugLog;

    private Rigidbody2D rb;
    private float moveInput;

    private bool isGrounded;
    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool _warnedGroundCheck;
    private bool _warnedWrongLayer;
    private bool _warnedRewindFrames;
    private bool _jumpKeyHeldPreviousFrame;
    private bool _rewindHeldPreviousFrame;
    private float _debugLogTimer;

    // Recording: cap at ~50 samples/sec * rewindDurationSeconds
    private List<RecordedFrame> recordedFrames = new List<RecordedFrame>();
    private int maxRecordedFrames;
    private bool isRewinding;
    private List<RecordedFrame> rewindSegment; // segment we're rewinding through (for ghost)

    // Smooth rewind: target from StepRewind, smoothed in Update
    private Vector3 _rewindTargetPosition;
    private Vector2 _rewindTargetVelocity;
    private Vector3 _rewindSmoothVelocity;
    private RigidbodyInterpolation2D _interpolationBeforeRewind;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("PlayerController2D: No Rigidbody2D on this GameObject. Add a Rigidbody 2D component.");
            return;
        }
        if (rb.bodyType != RigidbodyType2D.Dynamic)
        {
            Debug.LogWarning("PlayerController2D: Rigidbody2D must be Dynamic for movement and jump to work. Change Body Type to Dynamic.");
        }

        // Jam defaults: stable + snappy
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Cap recording at rewindDurationSeconds (assume 50 FixedUpdate/sec)
        maxRecordedFrames = Mathf.RoundToInt(50f * rewindDurationSeconds);
    }

    private void Update()
    {
        if (rb == null) return;

        Keyboard k = Keyboard.current;
        if (k != null)
        {
            bool shiftHeld = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;

            // Rewind: use polling so it works even when Game view focus is weird
            if (shiftHeld && !_rewindHeldPreviousFrame)
                StartRewind();
            if (!shiftHeld && _rewindHeldPreviousFrame)
                StopRewind();
            _rewindHeldPreviousFrame = shiftHeld;

            // Input (new Input System) — no movement input while rewinding
            if (!isRewinding)
            {
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) moveInput = -1f;
                else if (k.dKey.isPressed || k.rightArrowKey.isPressed) moveInput = 1f;
                else moveInput = 0f;
            }
            else
                moveInput = 0f;

            // Jump buffer: use both event and polling so jump works even if wasPressedThisFrame doesn't fire
            bool jumpPressed = k.spaceKey.wasPressedThisFrame || (k.spaceKey.isPressed && !_jumpKeyHeldPreviousFrame);
            if (jumpPressed)
                jumpBufferTimer = jumpBuffer;
            else
                jumpBufferTimer -= Time.deltaTime;
            _jumpKeyHeldPreviousFrame = k.spaceKey.isPressed;
        }
        else
        {
            moveInput = 0f;
            jumpBufferTimer -= Time.deltaTime;
            _rewindHeldPreviousFrame = false;
            _jumpKeyHeldPreviousFrame = false;
        }
        if (jumpBufferTimer < 0f) jumpBufferTimer = 0f;

        // Ground check: raycast down + OverlapBox so jump is reliable
        if (groundCheck == null)
        {
            if (!_warnedGroundCheck) { Debug.LogWarning("PlayerController2D: Ground Check is not assigned. Assign the GroundCheck child in the Inspector."); _warnedGroundCheck = true; }
            isGrounded = false;
        }
        else if (groundMask.value == 0)
        {
            if (!_warnedGroundCheck) { Debug.LogWarning("PlayerController2D: Ground Mask is 'Nothing'. Set it to the 'Ground' layer and put your platforms on the Ground layer."); _warnedGroundCheck = true; }
            isGrounded = false;
        }
        else
        {
            int mask = groundMask.value;
            Vector2 feet = groundCheck.position;

            // 1) Normal ray/box against Ground layer
            bool hitRay = Physics2D.Raycast(feet, Vector2.down, groundCheckDistance, mask);
            bool hitBox = Physics2D.OverlapBox(feet, groundCheckSize, 0f, mask);
            isGrounded = hitRay || hitBox;

            // 2) Fallback: raycast ALL layers, see what we hit (fixes layer/mask quirks; also logs what layer the platform is on)
            if (!isGrounded)
            {
                RaycastHit2D[] hits = Physics2D.RaycastAll(feet, Vector2.down, groundCheckDistance);
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider == null) continue;
                    if (hits[i].collider.gameObject == gameObject) continue; // ignore self
                    int layer = hits[i].collider.gameObject.layer;
                    if ((mask & (1 << layer)) != 0)
                    {
                        isGrounded = true;
                        break;
                    }
                    if (!_warnedWrongLayer)
                    {
                        _warnedWrongLayer = true;
                        Debug.LogWarning($"PlayerController2D: Standing on '{hits[i].collider.gameObject.name}' but it's on layer '{LayerMask.LayerToName(layer)}' (not Ground). Select that object and set its Layer to 'Ground' in the Inspector so jump works.");
                    }
                }
            }
        }

        // Coyote time
        coyoteTimer = isGrounded ? coyoteTime : coyoteTimer - Time.deltaTime;

        // Jump if allowed
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            Jump();
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        // Smooth rewind: move toward target each frame so it doesn’t stutter
        if (isRewinding && rb != null)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                _rewindTargetPosition,
                ref _rewindSmoothVelocity,
                rewindSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );
        }

        // Debug: log state so we can see why jump/rewind might not work
        if (debugLog)
        {
            _debugLogTimer -= Time.deltaTime;
            if (_debugLogTimer <= 0f)
            {
                _debugLogTimer = 0.5f;
                bool shift = k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed);
                Debug.Log($"PlayerController2D | grounded={isGrounded} coyote={coyoteTimer:F2} jumpBuf={jumpBufferTimer:F2} recorded={recordedFrames.Count} shift={shift}");
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (isRewinding)
        {
            StepRewind();
            return;
        }

        // Record this frame for rewind (oldest at index 0, newest at end)
        RecordCurrentFrame();

        // Target velocity
        float targetSpeed = moveInput * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        // Accel vs decel
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = speedDiff * accelRate;

        rb.AddForce(Vector2.right * movement);

        // Clamp max speed (prevents over-acceleration due to AddForce)
        float clampedX = Mathf.Clamp(rb.linearVelocity.x, -moveSpeed, moveSpeed);
        rb.linearVelocity = new Vector2(clampedX, rb.linearVelocity.y);

        ApplyBetterJump();
    }

    private void RecordCurrentFrame()
    {
        recordedFrames.Add(new RecordedFrame
        {
            position = transform.position,
            velocity = rb.linearVelocity,
            time = Time.time
        });
        while (recordedFrames.Count > maxRecordedFrames)
            recordedFrames.RemoveAt(0);
    }

    private void StartRewind()
    {
        if (recordedFrames.Count < 2)
        {
            if (!_warnedRewindFrames) { Debug.LogWarning("PlayerController2D: Rewind needs at least 2 recorded frames. Play for a second or two, then hold Shift to rewind."); _warnedRewindFrames = true; }
            return;
        }
        _warnedRewindFrames = false;
        isRewinding = true;
        rewindSegment = new List<RecordedFrame>();
        _rewindTargetPosition = transform.position;
        _rewindTargetVelocity = Vector2.zero;
        _rewindSmoothVelocity = Vector3.zero;

        // Disable physics during rewind so we control position; turn off interpolation to avoid double-smoothing
        _interpolationBeforeRewind = rb.interpolation;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.simulated = false;
        rb.linearVelocity = Vector2.zero;
    }

    private void StepRewind()
    {
        if (recordedFrames.Count < 2)
        {
            StopRewind();
            return;
        }
        // Store the "future" frame we're undoing so the ghost can replay it
        rewindSegment.Insert(0, recordedFrames[recordedFrames.Count - 1]);
        recordedFrames.RemoveAt(recordedFrames.Count - 1);
        // Set smooth rewind target (position is smoothed in Update)
        RecordedFrame prev = recordedFrames[recordedFrames.Count - 1];
        _rewindTargetPosition = prev.position;
        _rewindTargetVelocity = prev.velocity;
    }

    private void StopRewind()
    {
        isRewinding = false;

        // Snap to final rewind position and restore physics
        transform.position = _rewindTargetPosition;
        rb.simulated = true;
        rb.interpolation = _interpolationBeforeRewind;
        rb.linearVelocity = _rewindTargetVelocity;

        if (rewindSegment != null && rewindSegment.Count > 0 && ghostPrefab != null)
            SpawnGhost(rewindSegment);
        rewindSegment = null;
    }

    private void SpawnGhost(List<RecordedFrame> segment)
    {
        GameObject ghost = Instantiate(ghostPrefab, segment[0].position, Quaternion.identity);
        ghost.SetActive(true); // ensure clone is active so GhostReplay.Play() coroutine can run
        var replay = ghost.GetComponent<GhostReplay>();
        if (replay != null)
            replay.Play(segment);
        else
            Debug.LogWarning("Ghost prefab has no GhostReplay component; it won't animate.");
    }

    private void Jump()
    {
        // Reset Y velocity so jump height is consistent
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void ApplyBetterJump()
    {
        // Makes falling snappier & short-hops feel good
        if (rb.linearVelocity.y < 0f)
        {
            rb.AddForce(Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1f) * rb.mass);
        }
        else if (rb.linearVelocity.y > 0f && (Keyboard.current == null || !Keyboard.current.spaceKey.isPressed))
        {
            rb.AddForce(Vector2.up * Physics2D.gravity.y * (lowJumpGravityMultiplier - 1f) * rb.mass);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;
        Vector3 feet = groundCheck.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(feet, groundCheckSize);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(feet, feet + Vector3.down * groundCheckDistance);
    }
}
