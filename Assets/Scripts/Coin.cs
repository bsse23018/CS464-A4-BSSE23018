using UnityEngine;

// A1: Rotating collectible coin.
// Uses a trigger collider and tags to award score to the player on contact.
// The player's attached Rigidbody ensures OnTriggerEnter triggers reliably.
public class Coin : MonoBehaviour
{
    [Tooltip("Score value awarded to the player when collected.")]
    [SerializeField] int value = 1;

    [Tooltip("Rotation speed around the Y axis in degrees per second.")]
    [SerializeField] float spinSpeed = 120f;

    public GameObject pickupFx;      // particles, destroys itself when finished

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;   // tags are case-sensitive
        GameManager.Instance.AddScore(value);
        if (pickupFx != null) Instantiate(pickupFx, transform.position, Quaternion.identity);
        if (SfxPlayer.Instance != null) SfxPlayer.Instance.Play(SfxPlayer.Instance.pickup);
        Destroy(gameObject);
    }
}
