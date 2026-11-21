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

    public static void SpawnFlash(Vector3 position, Color color, float size, float lifetime)
    {
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "Flash";
        Object.Destroy(flash.GetComponent<Collider>());
        flash.transform.position = position;
        flash.transform.localScale = Vector3.one * size;

        Renderer renderer = flash.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        renderer.SetPropertyBlock(block);

        Object.Destroy(flash, lifetime);
    }

    public static void SpawnTracer(Vector3 from, Vector3 to, float size, float speed)
    {
        GameObject tracer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        tracer.name = "Tracer";
        Object.Destroy(tracer.GetComponent<Collider>());
        tracer.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tracer.transform.localScale = new Vector3(size, size * 2f, size);

        Tracer mover = tracer.AddComponent<Tracer>();
        mover.Launch(from, to, speed);
    }
}

public class Tracer : MonoBehaviour
{
    private Vector3 target;
    private float speed;

    public void Launch(Vector3 from, Vector3 to, float travelSpeed)
    {
        transform.position = from;
        target = to;
        speed = Mathf.Max(travelSpeed, 1f);

        Vector3 direction = to - from;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
        }
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if ((transform.position - target).sqrMagnitude < 0.0001f) Destroy(gameObject);
    }
}

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    public static event System.Action TargetHit;

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
        rb.isKinematic = false;
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

        Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        Vector3 normal = collision.contactCount > 0 ? collision.GetContact(0).normal : -transform.forward;

        if (weaponData != null)
        {
            Feedback.SpawnEffect(weaponData.impactEffect, point, Quaternion.LookRotation(normal));
            Feedback.PlaySound(weaponData.impactSound, point);
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
