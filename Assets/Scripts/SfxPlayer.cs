using UnityEngine;

// One player for every sound. Lives through every scene load (singleton check in Awake).
public class SfxPlayer : MonoBehaviour
{
    public static SfxPlayer Instance { get; private set; }

    public AudioSource[] sources;
    public AudioClip pickup, hit, click, pause, win, lose;
    int next;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sources.Length == 0) return;
        AudioSource s = sources[next];
        next = (next + 1) % sources.Length;
        s.pitch = Mathf.Pow(2f, Random.Range(-2f, 2f) / 12f);   // +-2 semitones
        s.PlayOneShot(clip, volume);
    }
}
