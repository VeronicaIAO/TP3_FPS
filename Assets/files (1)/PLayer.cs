using UnityEngine;
using UnityEngine.InputSystem;

public class PLayer : MonoBehaviour, IDamageable
{
    [Header("Configuración (ScriptableObject)")]
    public PlayerData data;

    public Transform cameraTransform;

    public float moveSpeed = 5f;
    public float turnSpeed = 10f;
    public bool rotateWithCamera = true;

    public float jumpForce = 6f;
    public LayerMask groundMask;
    public float groundCheckDistance = 0.2f;

    public float maxLive = 100f;
    public float live = 100f;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool jumpRequested;
    public bool isGrounded;
    private bool touchingGround;
    private bool isDead;

    public event System.Action<float> Damaged;

    public float gravity = -20f;
    private Vector3 verticalVelocity;
    public float fuerzaSalto = 8f;

    public Vector3 respawnPoint;

    public float respawnGuardTime = 0.2f;
    private bool isRespawning = false;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
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
        if (GameManagger.Instance != null && GameManagger.Instance.IsGameOver)
        {
            moveDirection = Vector3.zero;
            return;
        }

        ReadMovementInput();

        if (isGrounded == true && Keyboard.current != null && Keyboard.current.spaceKey.IsPressed())
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
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                touchingGround = true;
                return;
            }
        }
    }

    private void ReadMovementInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (cameraTransform == null)
        {
            moveDirection = Vector3.zero;
            return;
        }

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        moveDirection = camForward * vertical + camRight * horizontal;
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

        // Tercera persona (comportamiento original): gira hacia donde se mueve
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
        if (isDead) return;

        live = Mathf.Max(0f, live - amount);

        if (Damaged != null) Damaged(amount);
        if (data != null) Feedback.PlaySound(data.hurtSound, transform.position);

        if (live <= 0f)
        {
            isDead = true;
            rb.linearVelocity = Vector3.zero;

            if (GameManagger.Instance != null)
            {
                GameManagger.Instance.Defeat();
            }
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

        rb.linearVelocity = Vector3.zero;
        rb.position = respawnPoint;
        transform.position = respawnPoint;
        Physics.SyncTransforms();

        Invoke(nameof(ClearRespawnGuard), respawnGuardTime);
    }

    private void ClearRespawnGuard()
    {
        isRespawning = false;
    }
}
