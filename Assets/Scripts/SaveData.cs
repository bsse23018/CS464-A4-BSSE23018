using UnityEngine;

// The only script that spells a PlayerPrefs key. Every read has a default.
public static class SaveData
{
    public const int LevelCount = 5;
    const string UnlockedKey = "unlocked";
    const string MusicKey = "MusicVolume";
    static string BestKey(int level) => "best" + level;

    public static int Unlocked => PlayerPrefs.GetInt(UnlockedKey, 1);

    public static void Unlock(int level)
    {
        if (level > LevelCount || level <= Unlocked) return;
        PlayerPrefs.SetInt(UnlockedKey, level);
        PlayerPrefs.Save();
    }

    public static bool HasBest(int level) => PlayerPrefs.HasKey(BestKey(level));
    public static float Best(int level) => PlayerPrefs.GetFloat(BestKey(level), 0f);

    // returns true when this time is a new best (the first time always is)
    public static bool Record(int level, float time)
    {
        bool best = !HasBest(level) || time < Best(level);
        if (best) { PlayerPrefs.SetFloat(BestKey(level), time); PlayerPrefs.Save(); }
        return best;
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicKey, 1f);
        set { PlayerPrefs.SetFloat(MusicKey, value); PlayerPrefs.Save(); }
    }
}
