using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManagger : MonoBehaviour
{
    public static GameManagger Instance;

    public bool IsGameOver = false;

    public PLayer player;
    public Weapon weapon;
    public GameUI ui;

    public event System.Action Won;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Time.timeScale = 1f;

        if (player == null) player = FindFirstObjectByType<PLayer>();
        if (weapon == null && player != null) weapon = player.GetComponentInChildren<Weapon>();
        if (ui == null) ui = GetComponent<GameUI>();
        if (ui == null) ui = gameObject.AddComponent<GameUI>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Victory()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        ReleaseCursor();
        if (Won != null) Won();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void Notify(string text, float seconds = 2f)
    {
        if (Instance != null && Instance.ui != null) Instance.ui.ShowMessage(text, seconds);
    }
}
