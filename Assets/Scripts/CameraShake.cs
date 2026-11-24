using System.Collections;
using UnityEngine;

// Listener 4: a hit shakes the camera for a moment (CameraFollow adds CameraFollow.Shake to its position).
public class CameraShake : MonoBehaviour
{
    public float duration = 0.25f;
    public float strength = 0.35f;
    Health health;
    Coroutine routine;

    void OnEnable() { health = GetComponent<Health>(); health.Damaged += OnHit; }
    void OnDisable()
    {
        if (health != null) health.Damaged -= OnHit;
        CameraFollow.Shake = Vector3.zero;
    }

    void OnHit(int current, int max)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Shake());
    }

    IEnumerator Shake()
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / duration;
            CameraFollow.Shake = Random.insideUnitSphere * strength * k;
            yield return null;
        }
        CameraFollow.Shake = Vector3.zero;
    }
}
