using UnityEngine;

// Listener 2: a hit makes a sound.
public class HitSfx : MonoBehaviour
{
    Health health;
    void OnEnable() { health = GetComponent<Health>(); health.Damaged += OnHit; }
    void OnDisable() { if (health != null) health.Damaged -= OnHit; }
    void OnHit(int current, int max)
    {
        if (SfxPlayer.Instance != null) SfxPlayer.Instance.Play(SfxPlayer.Instance.hit);
    }
}
