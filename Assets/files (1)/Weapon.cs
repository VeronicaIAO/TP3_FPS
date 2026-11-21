using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class WeaponSlot
{
    public WeaponData data;
    [HideInInspector] public int amountBullets;
    [HideInInspector] public int reserveBullets;
}


public class Weapon : MonoBehaviour

    public WeaponSlot[] weapons;
    public int startIndex = 0;

    public Transform cameraTransform;
    public Transform muzzle;
    public LayerMask hitMask = ~0;
    public AudioClip switchSound;

    public WeaponSlot Current { get; private set; }
    public bool IsReloading { get; private set; }

    private FolllowCamera3D followCamera;
    private Collider[] ownerColliders;
    private int currentIndex = -1;
    private float nextShotTime;

    void Awake()
    {
        if (weapons == null || weapons.Length == 0)
        {
            Debug.LogError("Weapon: la lista de armas está vacía.");
            enabled = false;
            return;
        }

        foreach (WeaponSlot slot in weapons)
        {
            if (slot == null || slot.data == null)
            {
                Debug.LogError("Weapon: hay una arma sin WeaponData asignado.");
                enabled = false;
                return;
            }

            if (slot.data.useProjectile && slot.data.projectilePrefab == null)
            {
                Debug.LogError("Weapon: " + slot.data.weaponName + " usa proyectiles pero no tiene Projectile Prefab.");
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

        PLayer owner = GetComponentInParent<PLayer>();
        ownerColliders = owner != null ? owner.GetComponentsInChildren<Collider>() : GetComponentsInChildren<Collider>();

        Select(startIndex, false);
    }

    void Update()
    {
        if (cameraTransform == null || Current == null) return;
        if (GameManagger.Instance != null && GameManagger.Instance.IsGameOver) return;

        HandleSwitching();

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            bool fire = Current.data.automatic ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame;
            if (fire) TryShoot();
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            TryReload();
        }
    }

    private void HandleSwitching()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            int count = Mathf.Min(weapons.Length, 9);
            for (int i = 0; i < count; i++)
            {
                if (keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                {
                    Select(i);
                }
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0.1f) Select(currentIndex + 1);
            else if (scroll < -0.1f) Select(currentIndex - 1);
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

        if (playSound) Feedback.PlaySound(switchSound, transform.position);
    }


    private void TryShoot()
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

        if (d.useProjectile) ShootProjectile(d, origin, direction);
        else ShootRaycast(d, origin, direction);

        // Feedback del disparo: sonido y fogonazo
        Feedback.PlaySound(d.shotSound, origin);
        Vector3 flashPosition = muzzle != null ? muzzle.position : origin + direction * 0.8f;
        Feedback.SpawnEffect(d.muzzleFlashPrefab, flashPosition, Quaternion.LookRotation(direction), 0.3f);

        if (followCamera != null)
        {
            followCamera.Kick(d.kickDistance, d.kickDuration);
        }
    }

    private void ShootRaycast(WeaponData d, Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, d.range, hitMask, QueryTriggerInteraction.Ignore))
        {
            Bullet.ApplyDamage(hit.collider, d.damage);

            Feedback.SpawnEffect(d.impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Feedback.PlaySound(d.impactSound, hit.point);

            Debug.DrawLine(origin, hit.point, Color.red, 0.2f);
        }
        else
        {
            Debug.DrawRay(origin, direction * d.range, Color.yellow, 0.2f);
        }
    }

    private void ShootProjectile(WeaponData d, Vector3 origin, Vector3 direction)
    {
        Vector3 target = Physics.Raycast(origin, direction, out RaycastHit hit, d.range, hitMask, QueryTriggerInteraction.Ignore)
            ? hit.point
            : origin + direction * d.range;

        Vector3 spawnPosition = muzzle != null ? muzzle.position : origin + direction * 0.8f;

        Vector3 shotDirection = target - spawnPosition;
        if (shotDirection.sqrMagnitude < 0.01f) shotDirection = direction;
        shotDirection.Normalize();

        Bullet bullet = Instantiate(d.projectilePrefab, spawnPosition, Quaternion.LookRotation(shotDirection));
        bullet.Setup(d);

        Collider bulletCollider = bullet.GetComponent<Collider>();
        if (bulletCollider != null)
        {
            foreach (Collider c in ownerColliders)
            {
                Physics.IgnoreCollision(bulletCollider, c);
            }
        }

        bullet.Launch(shotDirection);
    }

    public bool TryReload()
    {
        WeaponData d = Current.data;

        if (!d.canReload || IsReloading) return false;
        if (Current.amountBullets >= d.magazineSize || Current.reserveBullets <= 0) return false;

        StartCoroutine(ReloadRoutine(Current));
        return true;
    }

    private IEnumerator ReloadRoutine(WeaponSlot slot)
    {
        IsReloading = true;
        Feedback.PlaySound(slot.data.reloadSound, transform.position);

        yield return new WaitForSeconds(slot.data.reloadTime);

        int needed = slot.data.magazineSize - slot.amountBullets;
        int taken = Mathf.Min(needed, slot.reserveBullets);
        slot.amountBullets += taken;
        slot.reserveBullets -= taken;

        IsReloading = false;
    }

    public void AddReserve(int amount)
    {
        if (Current != null) Current.reserveBullets += amount;
    }
}
