using UnityEngine;

public enum EnemyState { Patrol, Suspicious, Investigating, Chase }

public enum BodyPart { Head, Body, Limbs }

[System.Serializable]
public class HitZone
{
    public BodyPart part = BodyPart.Body;
    public Collider collider;
}

public class Enemy : MonoBehaviour, IDamageable
{
    public EnemyData data;

    public Transform[] patrolPoints;

    public Transform eyePoint;
    public LayerMask obstacleMask = ~0;

    public HitZone[] hitZones;

    public Renderer indicatorRenderer;
    public Color patrolColor = Color.green;
    public Color investigateColor = new Color(1f, 0.6f, 0f);
    public Color suspiciousColor = Color.yellow;
    public Color chaseColor = Color.red;
    public Color hitColor = Color.white;

    public EnemyState CurrentState { get; private set; } = EnemyState.Patrol;
    public float DetectionLevel { get; private set; }
    public bool PlayerDetected { get { return DetectionLevel >= data.detectionThreshold; } }

    private PLayer player;
    private float life;
    private bool isDead;

    private int patrolIndex;
    private float waitTimer;
    private float stateTimer;
    private float nextAttackTime;
    private Vector3 lastKnownPosition;
    private Vector3 investigatePoint;

    private readonly RaycastHit[] rayBuffer = new RaycastHit[8];

    private Renderer[] renderers;
    private MaterialPropertyBlock indicatorBlock;
    private MaterialPropertyBlock bodyBlock;
    private float flashTimer;
    private bool flashing;

    void Awake()
    {
        if (data == null) data = ScriptableObject.CreateInstance<EnemyData>();
        life = data.life;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        renderers = GetComponentsInChildren<Renderer>();
        indicatorBlock = new MaterialPropertyBlock();
        bodyBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        player = FindFirstObjectByType<PLayer>();
    }

    void Update()
    {
        UpdateFeedback();

        if (isDead || player == null || player.IsDead) return;
        if (GameManagger.Instance != null && GameManagger.Instance.IsGameOver) return;

        UpdateDetection();

        switch (CurrentState)
        {
            case EnemyState.Patrol:
                Patrol();
                if (PlayerDetected) EnterState(EnemyState.Suspicious);
                break;

            case EnemyState.Suspicious:
                FaceTarget(lastKnownPosition);
                stateTimer += Time.deltaTime;
                if (!PlayerDetected) { EnterState(EnemyState.Patrol); break; }
                if (stateTimer >= data.suspiciousToChaseTime) EnterState(EnemyState.Chase);
                break;

            case EnemyState.Investigating:
                MoveTowards(investigatePoint, data.patrolSpeed);
                if (PlayerDetected) { EnterState(EnemyState.Suspicious); break; }
                if (HorizontalDistance(transform.position, investigatePoint) <= data.waypointTolerance)
                {
                    stateTimer += Time.deltaTime;
                    if (stateTimer >= data.investigateWaitTime) EnterState(EnemyState.Patrol);
                }
                break;

            case EnemyState.Chase:
                if (PlayerDetected)
                {
                    stateTimer = 0f;

                    if (HorizontalDistance(transform.position, lastKnownPosition) > data.attackRange)
                    {
                        MoveTowards(lastKnownPosition, data.chaseSpeed);
                    }
                    else
                    {
                        FaceTarget(lastKnownPosition);
                        TryAttack();
                    }
                }
                else
                {
                    if (HorizontalDistance(transform.position, lastKnownPosition) > data.waypointTolerance)
                    {
                        MoveTowards(lastKnownPosition, data.chaseSpeed);
                    }

                    stateTimer += Time.deltaTime;
                    if (stateTimer >= data.loseTargetTime)
                    {
                        investigatePoint = lastKnownPosition;
                        EnterState(EnemyState.Investigating);
                    }
                }
                break;
        }
    }


    private void EnterState(EnemyState newState)
    {
        CurrentState = newState;
        stateTimer = 0f;
    }

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Transform point = patrolPoints[patrolIndex % patrolPoints.Length];
        if (point == null)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            return;
        }

        if (HorizontalDistance(transform.position, point.position) <= data.waypointTolerance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= data.waitTimeAtPoint)
            {
                waitTimer = 0f;
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            }
            return;
        }

        MoveTowards(point.position, data.patrolSpeed);
    }

    private void MoveTowards(Vector3 destination, float speed)
    {
        Vector3 direction = destination - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        transform.position += direction.normalized * speed * Time.deltaTime;
        FaceTarget(destination);
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, data.rotationSpeed * Time.deltaTime);
    }

    private float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + data.attackCooldown;

        if (player.IsDead) return;
        player.TakeDamage(data.attackDamage, null);
        Feedback.PlaySound(data.attackSound, transform.position);
    }

    private void UpdateDetection()
    {
        bool sees = CheckVision() || CheckCloseRange();

        DetectionLevel += (sees ? data.detectionSpeed : -data.detectionDecaySpeed) * Time.deltaTime;
        DetectionLevel = Mathf.Clamp01(DetectionLevel);

        if (sees)
        {
            lastKnownPosition = player.transform.position;
        }
    }

    private bool CheckVision()
    {
        Vector3 origin = eyePoint != null ? eyePoint.position : transform.position;
        Vector3 toPlayer = player.transform.position - origin;
        float distance = toPlayer.magnitude;

        if (distance > data.viewDistance) return false;
        if (Vector3.Angle(transform.forward, toPlayer) > data.viewAngle * 0.5f) return false;
        if (distance < 0.01f) return true;

        int count = Physics.RaycastNonAlloc(origin, toPlayer / distance, rayBuffer, distance, obstacleMask, QueryTriggerInteraction.Ignore);

        float nearest = float.MaxValue;
        bool nearestIsPlayer = true;
        for (int i = 0; i < count; i++)
        {
            Collider c = rayBuffer[i].collider;
            if (c.transform.IsChildOf(transform)) continue;

            if (rayBuffer[i].distance < nearest)
            {
                nearest = rayBuffer[i].distance;
                nearestIsPlayer = c.GetComponentInParent<PLayer>() != null;
            }
        }
        return nearestIsPlayer;
    }

    private bool CheckCloseRange()
    {
        return Vector3.Distance(transform.position, player.transform.position) <= data.closeRangeRadius;
    }

    public void TakeDamage(float amount, Collider hitCollider)
    {
        if (isDead) return;

        BodyPart part = GetBodyPart(hitCollider);
        life -= amount * GetMultiplier(part);

        if (part == BodyPart.Head) GameManagger.Notify("Headshot!", 0.6f);

        if (life <= 0f)
        {
            Die();
            return;
        }

        Feedback.PlaySound(data.hurtSound, transform.position);
        flashTimer = data.hitFlashTime;

        if (player != null && CurrentState != EnemyState.Chase)
        {
            lastKnownPosition = player.transform.position;
            EnterState(EnemyState.Chase);
        }
    }

    private BodyPart GetBodyPart(Collider hit)
    {
        if (hit != null && hitZones != null)
        {
            foreach (HitZone zone in hitZones)
            {
                if (zone != null && zone.collider == hit) return zone.part;
            }
        }
        return BodyPart.Body;
    }

    private float GetMultiplier(BodyPart part)
    {
        switch (part)
        {
            case BodyPart.Head: return data.headMultiplier;
            case BodyPart.Limbs: return data.limbMultiplier;
            default: return data.bodyMultiplier;
        }
    }

    private void Die()
    {
        isDead = true;
        Feedback.PlaySound(data.deathSound, transform.position);
        Destroy(gameObject);
    }


    private void UpdateFeedback()
    {
        if (indicatorRenderer != null)
        {
            Color target;
            switch (CurrentState)
            {
                case EnemyState.Investigating: target = investigateColor; break;
                case EnemyState.Suspicious: target = suspiciousColor; break;
                case EnemyState.Chase: target = chaseColor; break;
                default: target = patrolColor; break;
            }

            indicatorRenderer.GetPropertyBlock(indicatorBlock);
            indicatorBlock.SetColor("_Color", target);
            indicatorBlock.SetColor("_BaseColor", target);
            indicatorRenderer.SetPropertyBlock(indicatorBlock);
        }

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (!flashing)
            {
                SetBodyColor(hitColor);
                flashing = true;
            }
        }
        else if (flashing)
        {
            ClearBodyColor();
            flashing = false;
        }
    }

    private void SetBodyColor(Color color)
    {
        bodyBlock.SetColor("_Color", color);
        bodyBlock.SetColor("_BaseColor", color);

        foreach (Renderer r in renderers)
        {
            if (r != null && r != indicatorRenderer) r.SetPropertyBlock(bodyBlock);
        }
    }

    private void ClearBodyColor()
    {
        foreach (Renderer r in renderers)
        {
            if (r != null && r != indicatorRenderer) r.SetPropertyBlock(null);
        }
    }

    void OnDrawGizmosSelected()
    {
        float viewDistance = data != null ? data.viewDistance : 12f;
        float viewAngle = data != null ? data.viewAngle : 90f;
        float closeRange = data != null ? data.closeRangeRadius : 2.5f;

        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, closeRange);

        Gizmos.color = Color.cyan;
        Vector3 origin = eyePoint != null ? eyePoint.position : transform.position;
        Vector3 forwardLeft = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 forwardRight = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(origin, forwardLeft * viewDistance);
        Gizmos.DrawRay(origin, forwardRight * viewDistance);
    }
}
