using UnityEngine;
using UnityEngine.EventSystems;

// Click sound for a button, from the mouse or the keyboard.
public class ButtonSound : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    public void OnPointerClick(PointerEventData e) { Play(); }
    public void OnSubmit(BaseEventData e) { Play(); }
    void Play() { if (SfxPlayer.Instance != null) SfxPlayer.Instance.Play(SfxPlayer.Instance.click); }
}
