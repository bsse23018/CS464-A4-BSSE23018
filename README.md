# CS464 Assignment 04 · Muhammad Tayyab · BSSE23018

## Demo Video
- [Watch / Download Demo Video (docs/demo.mp4)](docs/demo.mp4)

<video src="docs/demo.mp4" width="360" controls></video>

## Screens and flow
<p>
<img src="Docs/main_menu.png" width="320" alt="">
<img src="Docs/level1.png" width="240" alt="">
<img src="Docs/guard.png" width="320" alt="">
</p>

Unity 6.3 (6000.3.24f1). Open the project and press Play from `Assets/Scenes/MainMenu.unity`. The Scene List is saved in
`ProjectSettings/EditorBuildSettings.asset`: MainMenu 0, Level01–Level05 at 1–5, Credits 6.
Built on my Assignment 03 levels.

## Settings

| Part | Setting | Value | Why |
|---|---|---|---|
| A | Harmony · base hue | triadic · 262° (violet) | Orange (22°) and green (142°) sit 120° either side, so the score and the pickups pop against the violet |
| A | Dark · mid · light | #1B112B · #A47EE6 · #EEE9F7 | Grays 23 · 149 · 236, three clearly separate values |
| A | Accents | #FAA370 · #5CE68E | Orange for score and coins, green for health, pickups and the speed boost |
| A | HUD text on its panel | #EEE9F7 on #1B112B · #FAA370 on #1B112B · #5CE68E on #1B112B | 15.18 : 1 · 9.07 : 1 · 11.33 : 1 (measured with `ColourUtil.Contrast`, logged by the builder) |
| A | Button label on the button | #100A1A on #A47EE6 | 6.21 : 1 |
| B | Music · SFX groups, as mixed | 0 dB · 0 dB | Mixer `Assets/Audio/MainMixer.mixer`, volumes exposed as `MusicVolume` and `SfxVolume`; the music level comes from the saved slider |
| B | Slider at 1 · 0.5 · 0.0001 | 0 dB · −6.02 dB · −80 dB | `Log10(v) × 20`, slider minimum 0.0001 |
| C | Goal hum, in Levels 1–5 | Logarithmic · Min 3 m · Max 40 m | Spatial Blend 1. You hear the goal across the level, louder as you close in, and the log curve drops about 6 dB per doubling |
| C | Guard hum, in Levels 3, 4 and 5 | Linear · Min 1.5 m · Max 16 m | Spatial Blend 1. A short-range warning that is silent at 16 m, so the guard only hums when it is close |
| C | AudioListener | on the player | The follow camera is about 12 m behind and above the player, so the listener rides on the player (the camera's listener is removed) |
| D | unlocked | int · default 1 | Highest unlocked level |
| D | best1 … best5 | float · default none, `HasKey` first | Best time in seconds, written only when it improves |
| D | MusicVolume | float · default 1 | The menu slider |

## Answers

1. **What `Time.timeScale = 0` stops in my game, and what it doesn't:** It stops everything that moves by `Time.deltaTime` or physics: the guards, the crushers, the spinning coins, the level clock and the respawn timer. It does not stop `Update` itself, so `PlayerController.Update` returns early when `PauseMenu.IsPaused`. Sound ignores the time scale too, so pausing also sets `AudioListener.pause`; the `SfxPlayer` sources ignore that pause so menu clicks still play. The health bar's lag bar and the loading bar use unscaled time.
2. **What survives a LoadScene in my game, and how:** The `SfxPlayer` and `PersistentMusic` objects, through `DontDestroyOnLoad` and a singleton check in `Awake`, so a second copy destroys itself. PlayerPrefs survive on disk. `Time.timeScale`, `AudioListener.pause` and the static `PauseMenu.IsPaused` also survive, so `LevelLoader.Load` calls `PauseMenu.Unfreeze()` before every load.
3. **Why every PlayerPrefs read in my game has a default:** On the first run, or after Edit › Clear All PlayerPrefs, no key exists. Without a default, `unlocked` would read 0 and lock Level 1, and the volume would read 0, which is silence. `SaveData` is the only script that spells a key, and each read there has a default: 1 for `unlocked` and `MusicVolume`, and `HasKey` first for the best times.
4. **An exception a forgotten unsubscribe would throw, and why mine can't:** `Health.Damaged` is a plain C# event. If a listener such as `HitSparks` or `CameraShake` was destroyed with its scene but stayed subscribed, the next hit would call a dead object and throw a `MissingReferenceException`. Mine all do `+=` in `OnEnable` and `-=` in `OnDisable`, which also runs on destroy and unload.

## What is where

- `Assets/Scripts/` — `SaveData`, `LevelLoader`, `PauseMenu`, `SfxPlayer`, `PersistentMusic`, `VolumeSlider`, `HealthBar`, listeners (`HitSfx`, `HitSparks`, `CameraShake`, plus the bar), `WorldSound`, `MainMenu`, `CreditsScreen`, and the Assignment 03 gameplay scripts.
- `Assets/Prefabs/GameManager.prefab` — the one HUD, pause menu, win and game-over screens and loading bar, used by all five levels. A fix there reaches every level.
- `Assets/Scripts/Editor/A4Builder.cs` — menu **CS464 › Build Assignment 04** rebuilds the mixer, effects, HUD prefab and the two new scenes.
- `tools/make_audio.py` — generates every sound in `Assets/Audio`.
- Keyboard: arrow keys and Enter in every menu, Esc pauses, R restarts.

