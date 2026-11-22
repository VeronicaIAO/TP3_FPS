using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Victory : MonoBehaviour
{
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PLayer>() == null) return;
        if (GameManagger.Instance != null) GameManagger.Instance.Victory();
    }
}
