using UnityEngine;

// Listener 3: a hit throws one burst of sparks (the prefab destroys itself when it ends).
public class HitSparks : MonoBehaviour
{
    public GameObject sparks;
    Health health;
    void OnEnable() { health = GetComponent<Health>(); health.Damaged += OnHit; }
    void OnDisable() { if (health != null) health.Damaged -= OnHit; }
    void OnHit(int current, int max)
    {
        if (sparks != null) Instantiate(sparks, transform.position, Quaternion.identity);
    }
}
