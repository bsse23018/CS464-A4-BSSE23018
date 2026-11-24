using UnityEngine;

// A sound that has a place in the level: fully 3D, with a rolloff and a Min/Max distance (see README).
[RequireComponent(typeof(AudioSource))]
public class WorldSound : MonoBehaviour
{
    public AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;
    public float minDistance = 2f;
    public float maxDistance = 30f;

    void Awake()
    {
        var s = GetComponent<AudioSource>();
        s.spatialBlend = 1f;
        s.rolloffMode = rolloff;
        s.minDistance = minDistance;
        s.maxDistance = maxDistance;
        s.loop = true;
        if (!s.isPlaying) s.Play();
    }
}
