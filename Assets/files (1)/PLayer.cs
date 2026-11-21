using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PLayer : MonoBehaviour, IDamageable
{
    public PlayerData data;

    public Transform cameraTransform;

    public float moveSpeed = 5f;
    public float turnSpeed = 10f;
    public bool rotateWithCamera = true;

    public float jumpForce = 6f;

    public float maxLive = 100f;
    public float live = 100f;

    public float damageCooldown = 0.25f;
    public float hitKickDistance = 0.8f;
    public float hitKickDuration = 0.4f;

    public Vector3 respawnPoint;
    public float respawnGuardTime = 0.2f;

    public bool isGrounded;
    public bool IsDead { get { return isDead; } }

    public event System.Action<float> Damaged;
    public event System.Action Respawned;

    private Rigidbody rb;
    private FolllowCamera3D followCamera;
    private Vector3 moveDirection;
    private bool jumpRequested;
    private bool touchingGround;
    private bool isDead;
    private bool isRespawning;
    private float nextDamageTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            followCamera = cameraTransform.GetComponent<FolllowCamera3D>();
        }

        respawnPoint = transform.position;

        if (data != null)
        {
            moveSpeed = data.moveSpeed;
            jumpForce = data.jumpForce;
            maxLive = data.maxLive;
        }
        live = maxLive;
    }

    void Update()
    {
        if (isDead || (GameManagger.Instance != null && GameManagger.Instance.IsGameOver))
        {
            moveDirection = Vector3.zero;
            return;
        }

        ReadMovementInput();

        Keyboard keyboard = Keyboard.current;
        if (isGrounded && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        isGrounded = touchingGround;
        touchingGround = false;

        ApplyMovement();
        ApplyRotation();
        ApplyJump();
    }

    void OnCollisionStay(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
            {
                touchingGround = true;
                return;
            }
        }
    }

    private void ReadMovementInput()
    {
        float horizontal = 0f;
        float vertical = 0f;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            horizontal += stick.x;
            vertical += stick.y;
        }

        Transform reference = cameraTransform != null ? cameraTransform : transform;

        Vector3 forward = reference.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = reference.right;
        right.y = 0f;
        right.Normalize();

        moveDirection = forward * vertical + right * horizontal;
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }
    }

    private void ApplyMovement()
    {
        Vector3 horizontalMove = moveDirection * moveSpeed;
        rb.linearVelocity = new Vector3(horizontalMove.x, rb.linearVelocity.y, horizontalMove.z);
    }

    private void ApplyRotation()
    {
        if (rotateWithCamera)
        {
            if (cameraTransform == null) return;
            rb.MoveRotation(Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f));
            return;
        }

        if (moveDirection.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
    }

    private void ApplyJump()
    {
        if (!jumpRequested) return;
        jumpRequested = false;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }

    public void TakeDamage(float amount, Collider hitCollider = null)
    {
        if (isDead || amount <= 0f) return;
        if (GameManagger.Instance != null && GameManagger.Instance.IsGameOver) return;
        if (Time.time < nextDamageTime) return;
        nextDamageTime = Time.time + damageCooldown;

        live = Mathf.Max(0f, live - amount);

        if (Damaged != null) Damaged(amount);
        if (data != null) Feedback.PlaySound(data.hurtSound, transform.position);
        if (followCamera != null) followCamera.Kick(hitKickDistance, hitKickDuration);

        if (live <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        live = Mathf.Min(maxLive, live + amount);
    }

    public void SetSpawnPoint(Vector3 position)
    {
        respawnPoint = position;
    }

    public void Die()
    {
        if (isRespawning) return;
        isRespawning = true;

        live = maxLive;
        isDead = false;
        moveDirection = Vector3.zero;
        jumpRequested = false;

        rb.linearVelocity = Vector3.zero;
        rb.position = respawnPoint;
        transform.position = respawnPoint;
        Physics.SyncTransforms();

        if (Respawned != null) Respawned();

        Invoke(nameof(ClearRespawnGuard), respawnGuardTime);
    }

    private void ClearRespawnGuard()
    {
        isRespawning = false;
    }
}
