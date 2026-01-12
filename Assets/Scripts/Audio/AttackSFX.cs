using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AttackSFX : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip deathClip;

    [Header("Volumes")]
    [SerializeField] private float attackVolume = 1f;
    [SerializeField] private float deathVolume = 1f;

    private AudioSource src;

    private void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f; // 2D
    }

    public void PlayAttack()
    {
        if (attackClip == null) return;
        src.PlayOneShot(attackClip, attackVolume);
    }

    public void PlayDeath()
    {
        if (deathClip == null) return;
        src.PlayOneShot(deathClip, deathVolume);
    }
}
