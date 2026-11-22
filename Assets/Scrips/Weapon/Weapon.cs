using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class WeaponSlot
{
    public WeaponData data;
    public GameObject model;
    public Transform muzzle;
    [HideInInspector] public int amountBullets;
    [HideInInspector] public int reserveBullets;
}

public class Weapon : MonoBehaviour
{
    public WeaponSlot[] weapons;
    public int startIndex = 0;

    public Transform cameraTransform;
    public Transform muzzle;
    public LayerMask hitMask = ~0;
    public AudioClip switchSound;

    public Key switchKey = Key.R;
    public Key reloadKey = Key.Q;
    public bool autoReload = true;

    public WeaponSlot Current { get; private set; }
    public int CurrentIndex { get { return currentIndex; } }
    public bool IsReloading { get; private set; }

    public event System.Action Fired;

    private FolllowCamera3D followCamera;
    private Transform owner;
    private Collider[] ownerColliders;
    private int currentIndex = -1;
    private float nextShotTime;

    void Awake()
    {
        if (weapons == null || weapons.Length == 0)
        {
            Debug.LogError("Weapon: the weapon list is empty.");
            enabled = false;
            return;
        }

        foreach (WeaponSlot slot in weapons)
        {
            if (slot == null || slot.data == null)
            {
                Debug.LogError("Weapon: a weapon slot has no WeaponData assigned.");
                enabled = false;
                return;
            }

            if (slot.data.useProjectile && slot.data.projectilePrefab == null)
            {
                Debug.LogError("Weapon: " + slot.data.weaponName + " uses projectiles but has no Projectile Prefab.");
                enabled = false;
                return;
            }

            slot.amountBullets = slot.data.magazineSize;
            slot.reserveBullets = slot.data.startReserveBullets;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            followCamera = cameraTransform.GetComponent<FolllowCamera3D>();
        }

        PLayer player = GetComponentInParent<PLayer>();
        owner = player != null ? player.transform : transform;
        ownerColliders = owner.GetComponentsInChildren<Collider>(true);

        Select(startIndex, false);
    }

    void Update()
    {
        if (cameraTransform == null || Current == null) return;
        if (GameManagger.Instance != null && GameManagger.Instance.IsGameOver) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[switchKey].wasPressedThisFrame)
        {
            Select(currentIndex + 1);
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            bool fire = Current.data.automatic ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame;
            if (fire) TryShoot();
        }

        if (keyboard != null && keyboard[reloadKey].wasPressedThisFrame)
        {
            TryReload();
        }
    }

    public void Select(int index, bool playSound = true)
    {
        if (weapons == null || weapons.Length == 0) return;

        index = ((index % weapons.Length) + weapons.Length) % weapons.Length;
        if (index == currentIndex) return;

        StopAllCoroutines();
        IsReloading = false;

        currentIndex = index;
        Current = weapons[index];

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null && weapons[i].model != null)
            {
                weapons[i].model.SetActive(i == index);
            }
        }

        if (playSound) Feedback.PlaySound(switchSound, transform.position);
    }

    void TryShoot()
    {
        if (IsReloading || Time.time < nextShotTime) return;

        WeaponData d = Current.data;

        if (Current.amountBullets <= 0)
        {
            if (!TryReload())
            {
                nextShotTime = Time.time + 0.3f;
                Feedback.PlaySound(d.emptySound, cameraTransform.position);
            }
            return;
        }

        nextShotTime = Time.time + 1f / Mathf.Max(d.velicityShoot, 0.01f);
        Current.amountBullets--;

        Vector3 origin = cameraTransform.position;
        Vector3 direction = cameraTransform.forward;
        Vector3 muzzlePosition = GetMuzzlePosition(origin, direction);

        if (d.useProjectile) ShootProjectile(d, origin, direction, muzzlePosition);
        else ShootRaycast(d, origin, direction, muzzlePosition);

        Feedback.PlaySound(d.shotSound, muzzlePosition, d.shotVolume);
        if (d.muzzleFlashPrefab != null)
        {
            Feedback.SpawnEffect(d.muzzleFlashPrefab, muzzlePosition, Quaternion.LookRotation(direction), 0.3f);
        }
        else
        {
            Feedback.SpawnFlash(muzzlePosition, d.flashColor, 0.35f, 0.04f);
        }

        if (followCamera != null)
        {
            followCamera.Kick(d.kickDistance, d.kickDuration);
        }

        if (Fired != null) Fired();

        if (autoReload && Current.amountBullets <= 0) TryReload();
    }

    Vector3 GetMuzzlePosition(Vector3 origin, Vector3 direction)
    {
        if (Current != null && Current.muzzle != null) return Current.muzzle.position;
        if (muzzle != null) return muzzle.position;
        return origin + direction * 2f + cameraTransform.right * 0.6f - cameraTransform.up * 0.4f;
    }

    bool IsOwnerCollider(Collider other)
    {
        return other != null && other.transform.IsChildOf(owner);
    }

    bool FindTarget(Vector3 origin, Vector3 direction, float range, out RaycastHit result)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, range, hitMask, QueryTriggerInteraction.Ignore);
        result = default(RaycastHit);
        float nearest = float.MaxValue;
        bool found = false;

        foreach (RaycastHit hit in hits)
        {
            if (IsOwnerCollider(hit.collider)) continue;
            if (hit.distance < nearest)
            {
                nearest = hit.distance;
                result = hit;
                found = true;
            }
        }
        return found;
    }

    void ShootRaycast(WeaponData d, Vector3 origin, Vector3 direction, Vector3 muzzlePosition)
    {
        Vector3 end = origin + direction * d.range;

        if (FindTarget(origin, direction, d.range, out RaycastHit hit))
        {
            end = hit.point;
            Bullet.ApplyDamage(hit.collider, d.damage);

            Feedback.SpawnEffect(d.impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Feedback.PlaySound(d.impactSound, hit.point);
        }

        Feedback.SpawnTracer(muzzlePosition, end, d.tracerSize, d.tracerSpeed);
    }

    void ShootProjectile(WeaponData d, Vector3 origin, Vector3 direction, Vector3 spawnPosition)
    {
        Vector3 target = FindTarget(origin, direction, d.range, out RaycastHit hit)
            ? hit.point
            : origin + direction * d.range;

        Vector3 shotDirection = target - spawnPosition;
        if (shotDirection.sqrMagnitude < 0.01f) shotDirection = direction;
        shotDirection.Normalize();

        Bullet bullet = Instantiate(d.projectilePrefab, spawnPosition, Quaternion.LookRotation(shotDirection));
        bullet.transform.localScale = Vector3.one * d.projectileScale;
        bullet.Setup(d);

        Collider bulletCollider = bullet.GetComponent<Collider>();
        if (bulletCollider != null)
        {
            bulletCollider.isTrigger = false;
            foreach (Collider c in ownerColliders)
            {
                if (c != null) Physics.IgnoreCollision(bulletCollider, c);
            }
        }

        bullet.Launch(shotDirection);
    }

    public bool TryReload()
    {
        if (Current == null) return false;
        WeaponData d = Current.data;

        if (!d.canReload || IsReloading) return false;
        if (Current.amountBullets >= d.magazineSize || Current.reserveBullets <= 0) return false;

        if (d.reloadTime <= 0f)
        {
            Feedback.PlaySound(d.reloadSound, transform.position);
            Refill(Current);
            return true;
        }

        StartCoroutine(ReloadRoutine(Current));
        return true;
    }

    IEnumerator ReloadRoutine(WeaponSlot slot)
    {
        IsReloading = true;
        Feedback.PlaySound(slot.data.reloadSound, transform.position);

        yield return new WaitForSeconds(slot.data.reloadTime);

        Refill(slot);
        IsReloading = false;
    }

    void Refill(WeaponSlot slot)
    {
        int needed = slot.data.magazineSize - slot.amountBullets;
        int taken = Mathf.Min(needed, slot.reserveBullets);
        slot.amountBullets += taken;
        slot.reserveBullets -= taken;
    }

    public void AddReserve(int amount)
    {
        if (Current != null) Current.reserveBullets += amount;
    }
}
