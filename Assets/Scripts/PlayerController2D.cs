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
    [Tooltip("Layers that count as ground. Set to Ground + Ghost + Default so you can jump on platforms, off the ghost, and on pressure plates (plates use Default so the ghost can trigger them).")]
    [SerializeField] private float groundCheckDistance = 0.25f;
    [SerializeField] private LayerMask groundMask;

    [Header("Wall Stick Prevention")]
    [Tooltip("Distance to detect wall contact. When against a wall and holding that direction, horizontal input is ignored so you slide down instead of sticking. Use ~0.12 so we detect the wall when jumping into it (small values can miss when the ray starts inside the wall).")]
    [SerializeField] private float wallCheckDistance = 0.12f;
    [Tooltip("When jumping while against a wall, add this horizontal speed away from the wall so the player doesn't stick. 0 = no push.")]
    [SerializeField] private float wallJumpOffSpeed = 3.5f;

    [Header("Rewind (Braid-like)")]
    [SerializeField] private float rewindDurationSeconds = 6f;
    [Tooltip("Smoothing time when rewinding (lower = snappier, higher = smoother). 0.02–0.05 works well.")]
    [SerializeField] private float rewindSmoothTime = 0.03f;
    [Tooltip("Seconds to wait after spawning a ghost before the player can rewind again. 0 = no cooldown.")]
    [SerializeField] private float rewindCooldownSeconds = 3f;
    [Tooltip("Minimum recorded frames required to start rewind. Prevents 'one-step' rewinds that snap; 15–25 is smooth.")]
    [SerializeField] private int minRecordedFramesForRewind = 15;
    [Tooltip("Maximum time to rewind back (seconds). Stops rewinding so you don't snap to an old platform. 0 = no limit.")]
    [SerializeField] private float maxRewindSeconds = 1.5f;
    [Tooltip("Optional: prefab to spawn as 'ghost' that replays rewound segment.")]
    [SerializeField] private GameObject ghostPrefab;

    [Header("Debug")]
    [Tooltip("Enable to log ground/input state every 0.5s in Console (to find why jump/rewind don't work).")]
    [SerializeField] private bool debugLog;

    private Rigidbody2D rb;
    private Collider2D col;
    private float moveInput;

    private bool isGrounded;
    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool _warnedGroundCheck;
    private bool _warnedWrongLayer;
    private bool _warnedRewindFrames;
    private bool _jumpKeyHeldPreviousFrame;
    private bool _rewindHeldPreviousFrame;
    private bool _wasGroundedPreviousFrame;
    private float _debugLogTimer;
    private float _lastRewindEndTime = -999f;
    private float _rewindStartTime;

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
        col = GetComponent<Collider2D>();
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

        // Cap recording at rewindDurationSeconds (use project Fixed Timestep)
        maxRecordedFrames = Mathf.RoundToInt((1f / Time.fixedDeltaTime) * rewindDurationSeconds);
    }

    private void Update()
    {
        if (rb == null) return;

        Keyboard k = Keyboard.current;
        if (k != null)
        {
            bool shiftHeld = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;

            // Rewind: use polling so it works even when Game view focus is weird; cooldown prevents ghost spam
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
            if (!_warnedGroundCheck) { Debug.LogWarning("PlayerController2D: Ground Mask is 'Nothing'. Set it to Ground + Ghost + Default."); _warnedGroundCheck = true; }
            isGrounded = false;
        }
        else
        {
            int mask = groundMask.value;
            Vector2 feet = groundCheck.position;

            // Horizontal extent for multi-point check (when against a wall, center feet can be off the platform; left/right still over it)
            float halfW = (col != null) ? (col.bounds.size.x * 0.45f) : 0.3f;
            Vector2 leftFeet = feet + Vector2.left * halfW;
            Vector2 rightFeet = feet + Vector2.right * halfW;

            bool HitIsGround(Vector2 origin)
            {
                RaycastHit2D rayHit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, mask);
                return rayHit.collider != null && !IsSelfOrChild(rayHit.collider.gameObject);
            }

            // 1) Raycast down from center and from left/right so we're grounded when against a wall (one foot on platform)
            bool hitRay = HitIsGround(feet) || HitIsGround(leftFeet) || HitIsGround(rightFeet);

            // 2) OverlapBox at center and at left/right so edge-standing still counts
            bool hitBox = false;
            foreach (Vector2 origin in new[] { feet, leftFeet, rightFeet })
            {
                Collider2D[] overlaps = Physics2D.OverlapBoxAll(origin, groundCheckSize, 0f, mask);
                for (int i = 0; i < overlaps.Length; i++)
                {
                    if (overlaps[i] != null && !IsSelfOrChild(overlaps[i].gameObject))
                    {
                        hitBox = true;
                        break;
                    }
                }
                if (hitBox) break;
            }

            isGrounded = hitRay || hitBox;

            // 3) Fallback: raycast ALL layers from all three points (fixes layer/mask quirks)
            if (!isGrounded)
            {
                foreach (Vector2 origin in new[] { feet, leftFeet, rightFeet })
                {
                    RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, groundCheckDistance);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        if (hits[i].collider == null) continue;
                        if (IsSelfOrChild(hits[i].collider.gameObject)) continue;
                        int layer = hits[i].collider.gameObject.layer;
                        if ((mask & (1 << layer)) != 0)
                        {
                            isGrounded = true;
                            break;
                        }
                        if (!_warnedWrongLayer)
                        {
                            _warnedWrongLayer = true;
                            Debug.LogWarning($"PlayerController2D: Standing on '{hits[i].collider.gameObject.name}' (layer '{LayerMask.LayerToName(layer)}') but that layer isn't in your Ground Mask. On the Player, set Ground Mask to Ground + Ghost + Default so jump works on platforms, the ghost, and pressure plates.");
                        }
                    }
                    if (isGrounded) break;
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
            GameJuice.Instance?.OnJump();
        }

        // Land detection for juice (squish, small shake)
        if (!_wasGroundedPreviousFrame && isGrounded)
            GameJuice.Instance?.OnLand();
        _wasGroundedPreviousFrame = isGrounded;

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

        // Ignore horizontal input when pushing into a wall so the player slides down instead of sticking
        float effectiveMoveInput = moveInput;
        bool pushingIntoWall = false;
        if (col != null && groundMask.value != 0)
        {
            Bounds b = col.bounds;
            // Cast from slightly inside the bounds so we still hit the wall when flush (ray from inside a collider doesn't hit it)
            float inset = 0.02f;
            Vector2 leftOrigin = new Vector2(b.min.x + inset, b.center.y);
            Vector2 rightOrigin = new Vector2(b.max.x - inset, b.center.y);
            float castDist = wallCheckDistance + inset;
            RaycastHit2D hitLeft = Physics2D.Raycast(leftOrigin, Vector2.left, castDist, groundMask);
            RaycastHit2D hitRight = Physics2D.Raycast(rightOrigin, Vector2.right, castDist, groundMask);
            bool touchingWallLeft = hitLeft.collider != null && !IsSelfOrChild(hitLeft.collider.gameObject);
            bool touchingWallRight = hitRight.collider != null && !IsSelfOrChild(hitRight.collider.gameObject);
            if (touchingWallLeft && effectiveMoveInput < 0f) { effectiveMoveInput = 0f; pushingIntoWall = true; }
            if (touchingWallRight && effectiveMoveInput > 0f) { effectiveMoveInput = 0f; pushingIntoWall = true; }
        }

        // When pushing into a wall, zero horizontal velocity so we slide down instead of sticking
        if (pushingIntoWall)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Target velocity
        float targetSpeed = effectiveMoveInput * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        // Accel vs decel
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = speedDiff * accelRate;

        rb.AddForce(Vector2.right * movement);

        // Clamp max speed (prevents over-acceleration due to AddForce)
        float clampedX = Mathf.Clamp(rb.linearVelocity.x, -moveSpeed, moveSpeed);
        rb.linearVelocity = new Vector2(clampedX, rb.linearVelocity.y);

        ApplyBetterJump(pushingIntoWall);
    }

    /// <summary>
    /// Clears the rewind recording buffer. Call this on respawn so the player doesn't rewind
    /// into pre-death positions (avoids weird "rewind from before death" behavior).
    /// RespawnManager calls this automatically after respawn.
    /// </summary>
    public void ClearRecordingBuffer()
    {
        recordedFrames.Clear();
        rewindSegment = null;
        if (isRewinding && rb != null)
        {
            isRewinding = false;
            rb.simulated = true;
            rb.interpolation = _interpolationBeforeRewind;
            rb.linearVelocity = Vector2.zero;
        }
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
        if (rewindCooldownSeconds > 0f && (Time.time - _lastRewindEndTime) < rewindCooldownSeconds)
            return;
        int minFrames = Mathf.Max(2, minRecordedFramesForRewind);
        if (recordedFrames.Count < minFrames)
        {
            if (!_warnedRewindFrames) { Debug.LogWarning($"PlayerController2D: Rewind needs at least {minFrames} recorded frames. Play for a second or two, then hold Shift to rewind."); _warnedRewindFrames = true; }
            return;
        }
        _warnedRewindFrames = false;
        isRewinding = true;
        GameJuice.Instance?.OnRewindStart();
        _rewindStartTime = Time.time;
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
        // Don't rewind further than maxRewindSeconds so we don't snap to an old platform
        RecordedFrame wouldLandOn = recordedFrames[recordedFrames.Count - 2];
        if (maxRewindSeconds > 0f && (_rewindStartTime - wouldLandOn.time) > maxRewindSeconds)
            return;
        // Store the "future" frame we're undoing so the ghost can replay it (Add + Reverse is O(1) per step vs Insert(0) O(n))
        rewindSegment.Add(recordedFrames[recordedFrames.Count - 1]);
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
        {
            rewindSegment.Reverse(); // we built rewindSegment in reverse order (newest first); flip for chronological playback
            SpawnGhost(rewindSegment);
            _lastRewindEndTime = Time.time;
        }
        GameJuice.Instance?.OnRewindStop();
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

    private bool IsSelfOrChild(GameObject other)
    {
        if (other == gameObject) return true;
        if (other.transform.IsChildOf(transform)) return true;
        return false;
    }

    private void Jump()
    {
        // Reset Y velocity so jump height is consistent
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // If we're against a wall, add a small velocity away so we don't stick when jumping from the wall
        if (wallJumpOffSpeed > 0f && col != null && groundMask.value != 0)
        {
            Bounds b = col.bounds;
            float inset = 0.02f;
            float castDist = wallCheckDistance + inset;
            Vector2 leftOrigin = new Vector2(b.min.x + inset, b.center.y);
            Vector2 rightOrigin = new Vector2(b.max.x - inset, b.center.y);
            RaycastHit2D hitLeft = Physics2D.Raycast(leftOrigin, Vector2.left, castDist, groundMask);
            RaycastHit2D hitRight = Physics2D.Raycast(rightOrigin, Vector2.right, castDist, groundMask);
            bool againstLeftWall = hitLeft.collider != null && !IsSelfOrChild(hitLeft.collider.gameObject);
            bool againstRightWall = hitRight.collider != null && !IsSelfOrChild(hitRight.collider.gameObject);
            float pushX = 0f;
            if (againstLeftWall) pushX = wallJumpOffSpeed;
            else if (againstRightWall) pushX = -wallJumpOffSpeed;
            if (pushX != 0f)
                rb.linearVelocity = new Vector2(pushX, rb.linearVelocity.y);
        }
    }

    private void ApplyBetterJump(bool pushingIntoWall)
    {
        // Makes falling snappier & short-hops feel good
        if (rb.linearVelocity.y < 0f)
        {
            rb.AddForce(Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1f) * rb.mass);
        }
        else if (rb.linearVelocity.y > 0f)
        {
            // When against a wall, always use low-jump gravity so we don't float up the wall when holding space
            bool useLowJumpGravity = pushingIntoWall || (Keyboard.current == null || !Keyboard.current.spaceKey.isPressed);
            if (useLowJumpGravity)
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
