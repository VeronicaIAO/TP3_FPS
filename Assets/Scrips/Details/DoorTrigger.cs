using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorTrigger : MonoBehaviour
{
    public Interactable door;
    public Vector3 passDirection = Vector3.back;

    private Collider area;
    private bool triggered;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        area = GetComponent<Collider>();
        area.isTrigger = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (triggered || door == null) return;

        PLayer player = other.GetComponentInParent<PLayer>();
        if (player == null) return;

        Vector3 toPlayer = player.transform.position - area.bounds.center;
        if (Vector3.Dot(toPlayer, passDirection) <= 0f) return;

        triggered = true;
        door.Open();
    }
}
