using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// Lives on the Canvas of the main menu (not on a hidden panel, whose scripts would not start until it opens).
public class VolumeSlider : MonoBehaviour
{
    public Slider slider;
    public AudioMixer mixer;

    void Start()
    {
        slider.minValue = 0.0001f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(SaveData.MusicVolume);
        PersistentMusic.ApplyMusicVolume(mixer, slider.value);
        slider.onValueChanged.AddListener(OnChanged);
    }

    void OnChanged(float v)
    {
        PersistentMusic.ApplyMusicVolume(mixer, v);
        SaveData.MusicVolume = v;
    }
}
