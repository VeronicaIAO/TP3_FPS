using UnityEngine;
using UnityEngine.InputSystem;

public class FolllowCamera3D : MonoBehaviour
{
    public Transform target;

    public bool firstPerson = true;
    public float distance = 5f;
    public Vector3 targetOffset = new Vector3(0f, 0.6f, 0f);

    public float sensitivityX = 3f;
    public float sensitivityY = 2f;
    public float mouseScale = 0.1f;
    public float minPitch = -20f;
    public float maxPitch = 70f;
    public float fpsMinPitch = -80f;
    public float fpsMaxPitch = 80f;

    public LayerMask collisionMask;
    public float collisionBuffer = 0.25f;

    public float kickDistance = 0.25f;
    public float kickDuration = 0.5f;
    [Range(0.05f, 0.5f)]
    public float kickRise = 0.15f;
    public float kickPitch = 1.5f;

    private float yaw;
    private float pitch = 15f;

    private float kickProgress = 1f;
    private float activeKickDistance = 0.25f;
    private float activeKickDuration = 0.5f;
    private float kickAmount;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 startAngles = transform.eulerAngles;
        yaw = startAngles.y;
        pitch = Mathf.DeltaAngle(0f, startAngles.x);
    }

    public void Kick()
    {
        Kick(kickDistance, kickDuration);
    }

    public void Kick(float distance, float duration)
    {
        activeKickDistance = distance;
        activeKickDuration = duration;
        kickProgress = kickAmount * kickRise;
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool gameOver = GameManagger.Instance != null && GameManagger.Instance.IsGameOver;
        if (!gameOver && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 look = Vector2.zero;

            Mouse mouse = Mouse.current;
            if (mouse != null) look += mouse.delta.ReadValue() * mouseScale;

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null) look += gamepad.rightStick.ReadValue() * 100f * Time.deltaTime;

            yaw += look.x * sensitivityX;
            pitch -= look.y * sensitivityY;
        }

        if (!gameOver && Cursor.lockState != CursorLockMode.Locked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        float minP = firstPerson ? fpsMinPitch : minPitch;
        float maxP = firstPerson ? fpsMaxPitch : maxPitch;
        pitch = Mathf.Clamp(pitch, minP, maxP);

        UpdateKick();

        Quaternion rotation = Quaternion.Euler(pitch - kickPitch * kickAmount, yaw, 0f);
        Vector3 pivot = target.position + targetOffset;

        float baseDistance = firstPerson ? 0f : distance;
        float wantedDistance = baseDistance + activeKickDistance * kickAmount;

        Vector3 desiredPosition = pivot - (rotation * Vector3.forward * wantedDistance);

        float finalDistance = wantedDistance;
        if (wantedDistance > 0.01f && Physics.Linecast(pivot, desiredPosition, out RaycastHit hit, collisionMask))
        {
            float minDistance = firstPerson ? 0f : 0.2f;
            finalDistance = Mathf.Max(hit.distance - collisionBuffer, minDistance);
            finalDistance = Mathf.Min(finalDistance, wantedDistance);
        }

        transform.position = pivot - (rotation * Vector3.forward * finalDistance);
        transform.rotation = rotation;
    }

    private void UpdateKick()
    {
        if (kickProgress >= 1f)
        {
            kickAmount = 0f;
            return;
        }

        kickProgress = Mathf.Min(kickProgress + Time.deltaTime / Mathf.Max(activeKickDuration, 0.01f), 1f);

        if (kickProgress < kickRise)
        {
            kickAmount = kickProgress / kickRise;
        }
        else
        {
            float t = (kickProgress - kickRise) / (1f - kickRise);
            kickAmount = 1f - Mathf.SmoothStep(0f, 1f, t);
        }
    }
}
