using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "FPS/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "Pistol";

    public float damage = 20f;
    public int magazineSize = 12;
    public int startReserveBullets = 36;
    public bool canReload = true;
    public float reloadTime = 0f;

    public float velicityShoot = 4f;
    public bool automatic = false;
    public float range = 100f;

    public bool useProjectile = false;
    public Bullet projectilePrefab;
    public float projectileSpeed = 40f;
    public float projectileLifeTime = 3f;
    public float projectileScale = 0.4f;
    public float tracerSize = 0.12f;
    public float tracerSpeed = 150f;

    public float kickDistance = 0.25f;
    public float kickDuration = 0.5f;

    public Color flashColor = new Color(1f, 0.85f, 0.3f);
    public GameObject muzzleFlashPrefab;
    public GameObject impactEffect;

    [Header("Audio")]
    public AudioClip shotSound;
    [Range(0f, 1f)] public float shotVolume = 1f;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    public AudioClip impactSound;
}
