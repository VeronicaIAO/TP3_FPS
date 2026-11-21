using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public enum InteractableType { Door, Switch, Key, Ammo, Health }


[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    public InteractableType type = InteractableType.Key;
    public AudioClip sound;

    public Vector3 openOffset = new Vector3(0f, 3f, 0f);
    public float openTime = 1f;
    public int keysRequired = 0;
    public AudioClip lockedSound;

    public UnityEvent onActivated;
    public string prompt = "Presiona E para activar";
    public Renderer indicator;
    public Color activatedColor = Color.green;

    [Header("Recogible (Key / Ammo / Health)")]
    public int amount = 1;
    public float rotationSpeed = 90f;

    public bool IsOpen { get; private set; }

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool playerInside;
    private bool used;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = type != InteractableType.Door;

        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;
    }

    void Update()
    {
        switch (type)
        {
            case InteractableType.Key:
            case InteractableType.Ammo:
            case InteractableType.Health:
                transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
                break;

            case InteractableType.Switch:
                Keyboard keyboard = Keyboard.current;
                if (playerInside && !used && keyboard != null && keyboard.eKey.wasPressedThisFrame)
                {
                    ActivateSwitch();
                }
                break;
        }
    }


    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PLayer")) return;

        if (type == InteractableType.Switch)
        {
            playerInside = true;
            if (!used) GameManagger.Notify(prompt);
        }
        else if (type != InteractableType.Door)
        {
            Collect(other);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (type == InteractableType.Switch && other.CompareTag("PLayer")) playerInside = false;
    }


    private void ActivateSwitch()
    {
        used = true;

        Feedback.PlaySound(sound, transform.position);
        if (indicator != null) indicator.material.color = activatedColor;

        onActivated.Invoke();
        GameManagger.Notify("¡Mecanismo activado!");
    }


    private void Collect(Collider other)
    {
        PLayer player = other.GetComponentInParent<PLayer>();
        if (player == null) return;

        bool collected = false;

        switch (type)
        {
            case InteractableType.Key:
            {
                if (GameManagger.Instance != null)
                {
                    GameManagger.Instance.AddKey(amount);
                    GameManagger.Notify("Llave obtenida");
                    collected = true;
                }
                break;
            }

            case InteractableType.Ammo:
            {
                Weapon weapon = player.GetComponentInChildren<Weapon>();
                if (weapon != null && weapon.Current != null)
                {
                    weapon.AddReserve(amount);
                    GameManagger.Notify("+" + amount + " balas");
                    collected = true;
                }
                break;
            }

            case InteractableType.Health:
            {
                if (player.live < player.maxLive)
                {
                    player.Heal(amount);
                    GameManagger.Notify("+" + amount + " de vida");
                    collected = true;
                }
                break;
            }
        }

        if (collected)
        {
            Feedback.PlaySound(sound, transform.position);
            Destroy(gameObject);
        }
    }

    public void Open()
    {
        if (type != InteractableType.Door || IsOpen) return;
        IsOpen = true;

        Feedback.PlaySound(sound, transform.position);
        StopAllCoroutines();
        StartCoroutine(MoveTo(openPosition));
    }

    public void Close()
    {
        if (type != InteractableType.Door || !IsOpen) return;
        IsOpen = false;

        StopAllCoroutines();
        StartCoroutine(MoveTo(closedPosition));
    }

    void OnCollisionEnter(Collision collision)
    {
        if (type != InteractableType.Door || IsOpen || keysRequired <= 0) return;
        if (!collision.collider.CompareTag("PLayer")) return;
        if (GameManagger.Instance == null) return;

        if (GameManagger.Instance.UseKeys(keysRequired))
        {
            GameManagger.Notify("Puerta abierta");
            Open();
        }
        else
        {
            GameManagger.Notify("Necesitas " + keysRequired + " llave(s) para abrir esta puerta");
            Feedback.PlaySound(lockedSound, transform.position);
        }
    }

    private IEnumerator MoveTo(Vector3 destination)
    {
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(openTime, 0.01f);
            transform.position = Vector3.Lerp(start, destination, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        transform.position = destination;
    }
}
