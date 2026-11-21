using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameManagger : MonoBehaviour
{

    public static GameManagger Instance;

    public bool IsGameOver = false;

    public int currentCoin = 0;
    public int keys = 0;

    public Vector3 currentSpawnPoint;
    public UnityEvent magment;

    public GameObject victoryPanel;
    public GameObject defeatPanel;

    public PLayer player;
    public Weapon weapon;

    public TextMeshProUGUI lifeText;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI keysText;
    public TextMeshProUGUI messageText;
    public Slider lifeBar;

    public Image damageFlash;
    public float damageFlashAlpha = 0.4f;
    public float damageFlashTime = 0.3f;

    public GameObject hitMarker;
    public float hitMarkerTime = 0.1f;

    private float damageFlashTimer;
    private float hitMarkerTimer;
    private float messageTimer;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        Bullet.TargetHit += OnTargetHit;
        if (player != null) player.Damaged += OnPlayerDamaged;
    }

    void OnDisable()
    {
        Bullet.TargetHit -= OnTargetHit;
        if (player != null) player.Damaged -= OnPlayerDamaged;
    }

    void Update()
    {
        UpdateStats();
        UpdateFeedback();
    }

    public void AddCoin(int coinToAdd)
    {
        currentCoin += coinToAdd;
    }

    public void AddKey(int amount = 1)
    {
        keys += amount;
    }

    public bool UseKeys(int amount)
    {
        if (keys < amount) return false;
        keys -= amount;
        return true;
    }

    public void Victory()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        if (victoryPanel != null) victoryPanel.SetActive(true);
        ReleaseCursor();
    }


    public void Defeat()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        if (defeatPanel != null) defeatPanel.SetActive(true);
        ReleaseCursor();
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void Notify(string text, float seconds = 2f)
    {
        if (Instance != null) Instance.ShowMessage(text, seconds);
    }

    public void ShowMessage(string text, float seconds = 2f)
    {
        if (messageText == null) return;

        messageText.text = text;
        messageTimer = seconds;
    }

    private void OnPlayerDamaged(float amount)
    {
        damageFlashTimer = damageFlashTime;
    }

    private void OnTargetHit()
    {
        hitMarkerTimer = hitMarkerTime;
    }

    private void UpdateStats()
    {
        if (player != null)
        {
            if (lifeText != null) lifeText.text = "Vida: " + Mathf.CeilToInt(player.live);
            if (lifeBar != null) lifeBar.value = player.maxLive > 0f ? player.live / player.maxLive : 0f;
        }

        WeaponSlot slot = weapon != null ? weapon.Current : null;
        if (slot != null && slot.data != null)
        {
            if (ammoText != null)
            {
                ammoText.text = weapon.IsReloading ? "Recargando..." : slot.amountBullets + " / " + slot.reserveBullets;
            }
            if (weaponNameText != null) weaponNameText.text = slot.data.weaponName;
        }

        if (keysText != null) keysText.text = "Llaves: " + keys;
    }

    private void UpdateFeedback()
    {
        if (damageFlash != null)
        {
            damageFlashTimer = Mathf.Max(0f, damageFlashTimer - Time.deltaTime);

            Color c = damageFlash.color;
            c.a = damageFlashTime > 0f ? damageFlashAlpha * (damageFlashTimer / damageFlashTime) : 0f;
            damageFlash.color = c;
        }

        if (hitMarker != null)
        {
            hitMarkerTimer = Mathf.Max(0f, hitMarkerTimer - Time.deltaTime);
            hitMarker.SetActive(hitMarkerTimer > 0f);
        }

        if (messageText != null && messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f) messageText.text = "";
        }
    }
}
