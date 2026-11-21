using UnityEngine;


public interface IDamageable
{
    void TakeDamage(float amount, Collider hitCollider);
}


public static class Feedback
{
    public static void PlaySound(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    public static void SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = 2f)
    {
        if (prefab == null) return;

        GameObject effect = Object.Instantiate(prefab, position, rotation);
        Object.Destroy(effect, lifetime);
    }
}

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    public static event System.Action TargetHit;

    [Header("Valores por defecto (los pisa el WeaponData cuando el arma dispara)")]
    public float damage = 25f;
    public float speed = 40f;
    public float lifeTime = 3f;

    private WeaponData weaponData;
    private Rigidbody rb;
    private bool hasHit;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Setup(WeaponData weapon)
    {
        weaponData = weapon;
        if (weaponData == null) return;

        damage = weaponData.damage;
        speed = weaponData.projectileSpeed;
        lifeTime = weaponData.projectileLifeTime;
    }

    public void Launch(Vector3 direction)
    {
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = direction.normalized * speed;

        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        ApplyDamage(collision.collider, damage);

        // Feedback del impacto
        if (weaponData != null)
        {
            ContactPoint contact = collision.GetContact(0);
            Feedback.SpawnEffect(weaponData.impactEffect, contact.point, Quaternion.LookRotation(contact.normal));
            Feedback.PlaySound(weaponData.impactSound, contact.point);
        }

        Destroy(gameObject);
    }

    public static bool ApplyDamage(Collider other, float amount)
    {
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null) return false;

        target.TakeDamage(amount, other);

        if (TargetHit != null) TargetHit();
        return true;
    }
}
