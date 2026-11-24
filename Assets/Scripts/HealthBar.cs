using UnityEngine;
using UnityEngine.UI;

// Two filled Images: the fill drops at once, the paler lag bar waits 0.4 s and then drains at 0.8 per second.
public class HealthBar : MonoBehaviour
{
    public Image fill;
    public Image lag;
    public float lagDelay = 0.4f;
    public float lagSpeed = 0.8f;

    Health health;
    float target, lagValue, holdUntil;

    void OnEnable()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null || !player.TryGetComponent(out health)) return;
        health.Damaged += OnChanged;
        health.Healed += OnChanged;
        Set(health.current, health.max);
        lagValue = target;
        lag.fillAmount = lagValue;
    }

    void OnDisable()
    {
        if (health == null) return;
        health.Damaged -= OnChanged;
        health.Healed -= OnChanged;
        health = null;
    }

    void OnChanged(int current, int max) { Set(current, max); }

    public void Set(int current, int max)
    {
        float f = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        if (f < target) holdUntil = Time.unscaledTime + lagDelay;
        target = f;
        fill.fillAmount = f;
        if (f >= lagValue) lagValue = f;
    }

    void Update()
    {
        if (lagValue > target && Time.unscaledTime >= holdUntil)
            lagValue = Mathf.MoveTowards(lagValue, target, lagSpeed * Time.unscaledDeltaTime);
        lag.fillAmount = lagValue;
    }
}
