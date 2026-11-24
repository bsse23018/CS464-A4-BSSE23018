using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The only script that loads a scene. Loads in the background behind a loading bar.
public class LevelLoader : MonoBehaviour
{
    public CanvasGroup loadingScreen;
    public Image bar;
    public TMP_Text percent;
    bool loading;

    void Awake()
    {
        if (loadingScreen != null) { loadingScreen.alpha = 0f; loadingScreen.blocksRaycasts = false; }
    }

    public void LoadNext()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings) next = 0;   // after Credits, back to the menu
        Load(next);
    }

    public void Reload() => Load(SceneManager.GetActiveScene().buildIndex);
    public void LoadMenu() => Load(0);

    public void Load(int index)
    {
        if (loading) return;
        loading = true;
        PauseMenu.Unfreeze();       // time, audio and IsPaused all survive a load
        StartCoroutine(Run(index));
    }

    IEnumerator Run(int index)
    {
        if (loadingScreen != null) { loadingScreen.transform.SetAsLastSibling(); loadingScreen.alpha = 1f; loadingScreen.blocksRaycasts = true; }
        AsyncOperation op = SceneManager.LoadSceneAsync(index);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
        {
            Show(Mathf.Clamp01(op.progress / 0.9f));
            yield return null;
        }
        Show(1f);                                   // the bar reaches 100%
        yield return new WaitForSecondsRealtime(0.25f);
        op.allowSceneActivation = true;
    }

    void Show(float t)
    {
        if (bar != null) bar.fillAmount = t;
        if (percent != null) percent.text = Mathf.RoundToInt(t * 100f) + "%";
    }
}
