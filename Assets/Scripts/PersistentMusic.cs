using UnityEngine;
using UnityEngine.Audio;

// One music player for the whole game: the first one stays, any later copy destroys itself.
public class PersistentMusic : MonoBehaviour
{
    static PersistentMusic instance;
    public AudioMixer mixer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        ApplyMusicVolume(mixer, SaveData.MusicVolume);
    }

    // slider value 0.0001..1 to decibels: 1 -> 0 dB, 0.5 -> -6 dB, 0.0001 -> -80 dB
    public static void ApplyMusicVolume(AudioMixer mixer, float value)
    {
        if (mixer == null) return;
        mixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
    }
}
