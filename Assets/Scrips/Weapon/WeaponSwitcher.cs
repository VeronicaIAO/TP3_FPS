using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSwitcher : MonoBehaviour
{
    public Weapon[] weapons;
    public int startIndex = 0;
    public AudioClip switchSound;

    public Weapon CurrentWeapon { get; private set; }

    private int currentIndex = -1;

    void Start()
    {
        Select(startIndex, false);
    }

    void Update()
    {
        if (weapons == null || weapons.Length == 0) return;
        if (GameManagger.Instance != null && GameManagger.Instance.IsGameOver) return;

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

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null) weapons[i].gameObject.SetActive(i == index);
        }

        currentIndex = index;
        CurrentWeapon = weapons[index];

        if (playSound) Feedback.PlaySound(switchSound, transform.position);
    }
}