using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CreditsScreen : MonoBehaviour
{
    public LevelLoader loader;
    public Button backButton;

    void Start()
    {
        PauseMenu.Unfreeze();
        EventSystem.current.SetSelectedGameObject(backButton.gameObject);
    }

    public void Back() { loader.LoadNext(); }      // Credits is the last scene: LoadNext wraps to the menu
    void Update() { if (GameInput.Pause()) Back(); }
}
