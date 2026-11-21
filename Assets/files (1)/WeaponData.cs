using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "FPS/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "Pistola";

    public float damage = 20f;          
    public int magazineSize = 12;
    public int startReserveBullets = 36;
    public bool canReload = true;
    public float reloadTime = 1.5f;

    public float velicityShoot = 4f;
    public bool automatic = false;
    public float range = 100f;

    public bool useProjectile = false;
    public Bullet projectilePrefab;
    public float projectileSpeed = 40f;
    public float projectileLifeTime = 3f;

    public float kickDistance = 0.25f;
    public float kickDuration = 0.5f;
    
    public GameObject muzzleFlashPrefab;
    public GameObject impactEffect;
    public AudioClip shotSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    public AudioClip impactSound;
}
