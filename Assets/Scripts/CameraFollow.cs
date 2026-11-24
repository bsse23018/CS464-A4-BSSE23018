using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 9f, -8f);
    public float smooth = 6f;
    public static Vector3 Shake;     // set by CameraShake when the player is hit

    void LateUpdate()
    {
        var p = PlayerController.I;
        if (p == null) return;
        Vector3 target = p.transform.position;
        transform.position = Vector3.Lerp(transform.position, target + offset, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        transform.position += Shake;
        transform.rotation = Quaternion.LookRotation(target + Vector3.up * 0.5f - transform.position);
    }
}
