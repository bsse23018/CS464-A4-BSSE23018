using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Main menu: Play, Levels (built from SaveData), Options (volume slider), Credits, Quit. Keyboard works everywhere.
public class MainMenu : MonoBehaviour
{
    public LevelLoader loader;
    public GameObject mainPanel, levelPanel, optionsPanel;
    public Button playButton, levelsBack, optionsBack;
    public Button levelTemplate;
    public Transform levelList;

    void Start()
    {
        PauseMenu.Unfreeze();
        BuildLevelButtons();
        Show(mainPanel, playButton);
    }

    void BuildLevelButtons()
    {
        for (int level = 1; level <= SaveData.LevelCount; level++)
        {
            Button b = Instantiate(levelTemplate, levelList);
            b.gameObject.SetActive(true);
            int n = level;
            bool open = n <= SaveData.Unlocked;
            string best = SaveData.HasBest(n) ? "   best " + SaveData.Best(n).ToString("0.0") + " s" : "";
            b.GetComponentInChildren<TMP_Text>().text = open ? "Level " + n + best : "Level " + n + "   (locked)";
            b.interactable = open;
            b.onClick.AddListener(() => loader.Load(n));
        }
    }

    void Show(GameObject panel, Button first)
    {
        mainPanel.SetActive(panel == mainPanel);
        levelPanel.SetActive(panel == levelPanel);
        optionsPanel.SetActive(panel == optionsPanel);
        EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    public void Play() { loader.Load(1); }
    public void OpenLevels()
    {
        Button first = levelsBack;
        foreach (Transform t in levelList)
            if (t.gameObject.activeSelf && t.GetComponent<Button>().interactable) { first = t.GetComponent<Button>(); break; }
        Show(levelPanel, first);
    }
    public void OpenOptions() { Show(optionsPanel, optionsBack); }
    public void BackToMain() { Show(mainPanel, playButton); }
    public void OpenCredits() { loader.Load(SceneManager.sceneCountInBuildSettings - 1); }
    public void Quit() { Application.Quit(); }

    void Update()
    {
        if (GameInput.Pause() && !mainPanel.activeSelf) BackToMain();    // Esc goes back a panel
    }
}
