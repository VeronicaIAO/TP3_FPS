using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public enum InteractableType { Door = 0, Switch = 1, Ammo = 3, Health = 4 }

[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    public InteractableType type = InteractableType.Ammo;
    public AudioClip sound;

    public Vector3 openOffset = new Vector3(0f, 3f, 0f);
    public float openTime = 1f;

    public UnityEvent onActivated;
    public string prompt = "Press E to activate";
    public Renderer indicator;
    public Color activatedColor = Color.green;

    public int amount = 1;
    public float rotationSpeed = 90f;
    public float bobHeight = 0.3f;
    public float bobSpeed = 2f;

    public bool IsOpen { get; private set; }

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Vector3 startPosition;
    private Rigidbody body;
    private bool playerInside;
    private bool used;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = type != InteractableType.Door;

        body = GetComponent<Rigidbody>();
        if (type == InteractableType.Door && body != null) body.isKinematic = true;

        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;
        startPosition = transform.position;
    }

    void Update()
    {
        switch (type)
        {
            case InteractableType.Ammo:
            case InteractableType.Health:
                transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
                transform.position = startPosition + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
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
        PLayer player = other.GetComponentInParent<PLayer>();
        if (player == null) return;

        if (type == InteractableType.Switch)
        {
            playerInside = true;
            if (!used) GameManagger.Notify(prompt);
        }
        else if (type != InteractableType.Door)
        {
            Collect(player);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (type == InteractableType.Switch && other.GetComponentInParent<PLayer>() != null) playerInside = false;
    }

    private void ActivateSwitch()
    {
        used = true;

        Feedback.PlaySound(sound, transform.position);
        if (indicator != null) indicator.material.color = activatedColor;

        onActivated.Invoke();
        GameManagger.Notify("Mechanism activated!");
    }

    private void Collect(PLayer player)
    {
        if (used) return;
        bool collected = false;

        switch (type)
        {
            case InteractableType.Ammo:
            {
                Weapon weapon = player.GetComponentInChildren<Weapon>();
                if (weapon != null && weapon.Current != null)
                {
                    weapon.AddReserve(amount);
                    collected = true;
                }
                break;
            }

            case InteractableType.Health:
            {
                if (player.live < player.maxLive)
                {
                    player.Heal(amount);
                    collected = true;
                }
                break;
            }
        }

        if (collected)
        {
            used = true;
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

    private IEnumerator MoveTo(Vector3 destination)
    {
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(openTime, 0.01f);
            Vector3 next = Vector3.Lerp(start, destination, Mathf.SmoothStep(0f, 1f, t));
            if (body != null) body.MovePosition(next);
            else transform.position = next;
            yield return null;
        }

        if (body != null) body.position = destination;
        transform.position = destination;
    }
}
