using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Muerte : MonoBehaviour
{
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        PLayer player = other.GetComponentInParent<PLayer>();
        if (player != null)
        {
            player.Die();
        }
    }
}
