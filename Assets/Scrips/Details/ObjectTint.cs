using UnityEngine;

public class ObjectTint : MonoBehaviour
{
    public Color color = Color.white;
    public float emission = 0f;

    void Awake()
    {
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    public void Apply()
    {
        Renderer target = GetComponent<Renderer>();
        if (target == null) return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        target.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        if (emission > 0f) block.SetColor("_EmissionColor", color * emission);
        target.SetPropertyBlock(block);
    }
}
