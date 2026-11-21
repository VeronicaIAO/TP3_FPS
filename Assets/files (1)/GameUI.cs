using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public Color panelColor = new Color(0f, 0f, 0f, 0.55f);
    public Color healthHighColor = new Color(0.25f, 0.85f, 0.35f);
    public Color healthLowColor = new Color(0.9f, 0.2f, 0.2f);
    public Color accentColor = new Color(1f, 0.82f, 0.25f);
    public Color victoryColor = new Color(0.3f, 0.95f, 0.45f);

    public float hitMarkerTime = 0.12f;

    private PLayer player;
    private Weapon weapon;

    private RectTransform healthFill;
    private Image healthFillImage;
    private TextMeshProUGUI healthText;
    private TextMeshProUGUI ammoText;
    private TextMeshProUGUI reserveText;
    private TextMeshProUGUI weaponNameText;
    private TextMeshProUGUI weaponListText;
    private TextMeshProUGUI messageText;
    private GameObject hitMarker;
    private GameObject victoryPanel;

    private float hitMarkerTimer;
    private float messageTimer;
    private float shownHealth = -1f;

    void Start()
    {
        GameManagger manager = GameManagger.Instance;
        if (manager != null)
        {
            player = manager.player;
            weapon = manager.weapon;
            manager.Won += ShowVictory;
        }

        if (player == null) player = FindFirstObjectByType<PLayer>();
        if (weapon == null && player != null) weapon = player.GetComponentInChildren<Weapon>();

        if (player != null) player.Respawned += OnPlayerRespawned;
        Bullet.TargetHit += OnTargetHit;

        EnsureEventSystem();
        Build();
    }

    void OnDestroy()
    {
        if (player != null) player.Respawned -= OnPlayerRespawned;
        Bullet.TargetHit -= OnTargetHit;

        GameManagger manager = GameManagger.Instance;
        if (manager != null)
        {
            manager.Won -= ShowVictory;
        }
    }

    void Update()
    {
        UpdateHealth();
        UpdateWeapon();
        UpdateEffects();
    }

    public void ShowMessage(string text, float seconds)
    {
        if (messageText == null) return;
        messageText.text = text;
        messageTimer = seconds;
    }

    private void OnPlayerRespawned()
    {
        shownHealth = -1f;
    }

    private void OnTargetHit()
    {
        hitMarkerTimer = hitMarkerTime;
    }

    private void ShowVictory()
    {
        if (victoryPanel != null) victoryPanel.SetActive(true);
    }

    private void UpdateHealth()
    {
        if (player == null || healthFill == null) return;

        float ratio = player.maxLive > 0f ? Mathf.Clamp01(player.live / player.maxLive) : 0f;
        shownHealth = shownHealth < 0f ? ratio : Mathf.MoveTowards(shownHealth, ratio, Time.deltaTime * 1.5f);

        healthFill.anchorMax = new Vector2(shownHealth, 1f);
        healthFillImage.color = healthHighColor;
        healthText.text = Mathf.CeilToInt(player.live) + " / " + Mathf.CeilToInt(player.maxLive);
    }

    private void UpdateWeapon()
    {
        if (weapon == null || weapon.Current == null || weapon.Current.data == null) return;

        WeaponSlot slot = weapon.Current;
        weaponNameText.text = slot.data.weaponName;

        if (weapon.IsReloading)
        {
            ammoText.text = "<size=60%>RELOADING</size>";
            ammoText.color = accentColor;
        }
        else
        {
            ammoText.text = slot.amountBullets.ToString();
            ammoText.color = slot.amountBullets == 0 ? healthLowColor : Color.white;
        }
        reserveText.text = "/ " + slot.reserveBullets;

        string list = "";
        for (int i = 0; i < weapon.weapons.Length; i++)
        {
            WeaponSlot item = weapon.weapons[i];
            if (item == null || item.data == null) continue;

            string entry = item.data.weaponName;
            if (i == weapon.CurrentIndex) entry = "<color=#" + ColorUtility.ToHtmlStringRGB(accentColor) + "><b>" + entry + "</b></color>";
            else entry = "<color=#9A9A9A>" + entry + "</color>";

            list += (list.Length > 0 ? "   " : "") + entry;
        }
        weaponListText.text = list;
    }

    private void UpdateEffects()
    {
        hitMarkerTimer = Mathf.Max(0f, hitMarkerTimer - Time.deltaTime);
        if (hitMarker != null) hitMarker.SetActive(hitMarkerTimer > 0f);

        if (messageText != null && messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f) messageText.text = "";
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private void Build()
    {
        GameObject canvasObject = new GameObject("GameUI Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        RectTransform root = canvasObject.GetComponent<RectTransform>();

        BuildCrosshair(root);
        BuildHealth(root);
        BuildWeapon(root);

        messageText = CreateText("Message", root, "", 40, TextAlignmentOptions.Center);
        Place(messageText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1200f, 80f));

        TextMeshProUGUI controls = CreateText("Controls", root, "WASD move   Mouse aim   LMB shoot   R switch weapon   Q reload   Space jump", 22, TextAlignmentOptions.Center);
        controls.color = new Color(1f, 1f, 1f, 0.6f);
        Place(controls.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1400f, 40f));

        victoryPanel = BuildEndPanel(root, "VICTORY", victoryColor);
    }

    private void BuildCrosshair(RectTransform root)
    {
        float length = 14f;
        float thickness = 3f;
        float gap = 6f;
        Vector2[] offsets = { new Vector2(0f, gap + length / 2f), new Vector2(0f, -gap - length / 2f), new Vector2(gap + length / 2f, 0f), new Vector2(-gap - length / 2f, 0f) };

        for (int i = 0; i < offsets.Length; i++)
        {
            Image line = CreateImage("Crosshair", root, new Color(1f, 1f, 1f, 0.9f));
            line.raycastTarget = false;
            Vector2 size = i < 2 ? new Vector2(thickness, length) : new Vector2(length, thickness);
            Place(line.rectTransform, new Vector2(0.5f, 0.5f), offsets[i], size);
        }

        TextMeshProUGUI marker = CreateText("Hit Marker", root, "X", 46, TextAlignmentOptions.Center);
        marker.color = new Color(1f, 0.25f, 0.25f);
        Place(marker.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
        hitMarker = marker.gameObject;
        hitMarker.SetActive(false);
    }

    private void BuildHealth(RectTransform root)
    {
        Image panel = CreateImage("Health Panel", root, panelColor);
        Place(panel.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(520f, 120f), new Vector2(0f, 0f));

        TextMeshProUGUI label = CreateText("Health Label", panel.rectTransform, "HEALTH", 26, TextAlignmentOptions.Left);
        label.color = new Color(1f, 1f, 1f, 0.75f);
        Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -14f), new Vector2(200f, 36f), new Vector2(0f, 1f));

        healthText = CreateText("Health Value", panel.rectTransform, "100 / 100", 30, TextAlignmentOptions.Right);
        Place(healthText.rectTransform, new Vector2(1f, 1f), new Vector2(-24f, -12f), new Vector2(240f, 40f), new Vector2(1f, 1f));

        Image background = CreateImage("Health Bar", panel.rectTransform, new Color(1f, 1f, 1f, 0.15f));
        Place(background.rectTransform, new Vector2(0f, 0f), new Vector2(24f, 22f), new Vector2(472f, 34f), new Vector2(0f, 0f));

        healthFillImage = CreateImage("Health Fill", background.rectTransform, healthHighColor);
        healthFill = healthFillImage.rectTransform;
        healthFill.anchorMin = Vector2.zero;
        healthFill.anchorMax = Vector2.one;
        healthFill.offsetMin = Vector2.zero;
        healthFill.offsetMax = Vector2.zero;
    }

    private void BuildWeapon(RectTransform root)
    {
        Image panel = CreateImage("Weapon Panel", root, panelColor);
        Place(panel.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(520f, 170f), new Vector2(1f, 0f));

        weaponNameText = CreateText("Weapon Name", panel.rectTransform, "", 30, TextAlignmentOptions.Right);
        weaponNameText.color = accentColor;
        weaponNameText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        Place(weaponNameText.rectTransform, new Vector2(1f, 1f), new Vector2(-24f, -12f), new Vector2(470f, 40f), new Vector2(1f, 1f));

        reserveText = CreateText("Reserve", panel.rectTransform, "/ 0", 34, TextAlignmentOptions.BottomLeft);
        reserveText.color = new Color(1f, 1f, 1f, 0.7f);
        Place(reserveText.rectTransform, new Vector2(1f, 0f), new Vector2(-24f, 52f), new Vector2(130f, 60f), new Vector2(1f, 0f));

        ammoText = CreateText("Ammo", panel.rectTransform, "0", 72, TextAlignmentOptions.BottomRight);
        ammoText.fontStyle = FontStyles.Bold;
        Place(ammoText.rectTransform, new Vector2(1f, 0f), new Vector2(-160f, 44f), new Vector2(320f, 90f), new Vector2(1f, 0f));

        weaponListText = CreateText("Weapon List", panel.rectTransform, "", 22, TextAlignmentOptions.Right);
        Place(weaponListText.rectTransform, new Vector2(1f, 0f), new Vector2(-24f, 12f), new Vector2(470f, 32f), new Vector2(1f, 0f));
    }

    private GameObject BuildEndPanel(RectTransform root, string title, Color color)
    {
        Image overlay = CreateImage(title + " Panel", root, new Color(0f, 0f, 0f, 0.75f));
        Stretch(overlay.rectTransform);

        TextMeshProUGUI titleText = CreateText("Title", overlay.rectTransform, title, 140, TextAlignmentOptions.Center);
        titleText.color = color;
        titleText.fontStyle = FontStyles.Bold;
        Place(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(1200f, 180f));

        Image buttonImage = CreateImage("Retry Button", overlay.rectTransform, color);
        buttonImage.raycastTarget = true;
        Place(buttonImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(360f, 90f));

        Button button = buttonImage.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        button.colors = colors;
        button.onClick.AddListener(Retry);

        TextMeshProUGUI buttonText = CreateText("Label", buttonImage.rectTransform, "RETRY", 44, TextAlignmentOptions.Center);
        buttonText.color = new Color(0.08f, 0.08f, 0.08f);
        buttonText.fontStyle = FontStyles.Bold;
        Stretch(buttonText.rectTransform);

        overlay.gameObject.SetActive(false);
        return overlay.gameObject;
    }

    private void Retry()
    {
        if (GameManagger.Instance != null) GameManagger.Instance.Restart();
    }

    private Image CreateImage(string objectName, RectTransform parent, Color color)
    {
        GameObject item = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        item.transform.SetParent(parent, false);
        Image image = item.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI CreateText(string objectName, RectTransform parent, string text, float size, TextAlignmentOptions alignment)
    {
        GameObject item = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        item.transform.SetParent(parent, false);
        TextMeshProUGUI label = item.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Place(rect, anchor, position, size, new Vector2(0.5f, 0.5f));
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
