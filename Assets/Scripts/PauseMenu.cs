using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    public GameObject panel;
    public Button firstButton;
    public LevelLoader loader;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { IsPaused = false; }

    // Everything that survives a scene load, put back in one place.
    public static void Unfreeze()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
    }

    void Start() { if (panel != null) panel.SetActive(false); }

    void Update()
    {
        if (!GameInput.Pause()) return;
        if (IsPaused) Resume();
        else if (GameManager.Instance != null && GameManager.Instance.CanPause) Pause();
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        panel.SetActive(true);
        if (SfxPlayer.Instance != null) SfxPlayer.Instance.Play(SfxPlayer.Instance.pause);
        EventSystem.current.SetSelectedGameObject(firstButton.gameObject);   // keyboard works at once
    }

    public void Resume()
    {
        Unfreeze();
        if (panel != null) panel.SetActive(false);
    }

    public void Restart()
    {
        Resume();
        loader.Reload();
    }

    public void QuitToMenu()
    {
        Resume();
        loader.LoadMenu();
    }
}
