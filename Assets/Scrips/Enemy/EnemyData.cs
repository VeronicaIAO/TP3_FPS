using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "FPS/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public float life = 50f;
    public float headMultiplier = 2f;
    public float bodyMultiplier = 1f;
    public float limbMultiplier = 0.5f;

    public float patrolSpeed = 2f;
    public float chaseSpeed = 4.5f;
    public float rotationSpeed = 6f;
    public float waypointTolerance = 0.3f;
    public float waitTimeAtPoint = 1.5f;

    public float suspiciousToChaseTime = 0.6f;
    public float loseTargetTime = 3f;
    public float investigateWaitTime = 2f;

    public float viewDistance = 12f;
    [Range(0, 180)] public float viewAngle = 90f;
    public float closeRangeRadius = 2.5f;
    public float detectionSpeed = 2.5f;
    public float detectionDecaySpeed = 0.6f;
    [Range(0, 1)] public float detectionThreshold = 1f;

    public float attackRange = 1.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1f;

    public float hitFlashTime = 0.1f;
    public AudioClip hurtSound;
    public AudioClip deathSound;
    public AudioClip attackSound;
}
