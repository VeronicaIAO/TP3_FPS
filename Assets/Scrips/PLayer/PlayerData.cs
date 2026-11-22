using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "FPS/Player Data")]
public class PlayerData : ScriptableObject
{
    public float moveSpeed = 5f;
    public float jumpForce = 6f;

    public float maxLive = 100f;

    public AudioClip hurtSound;
}
