using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menu: CS464 > Build Assignment 04. Safe to run again: it rebuilds the audio, effects, HUD prefab and the two new scenes,
// and adds the music player and SfxPlayer to every level.
public static class A4Builder
{
    // ---- palette: triadic, base hue 262 (violet), plus orange (22) and green (142)
    static readonly Color Panel = Hex("#1B112B"), PanelLight = Hex("#34244F"), ButtonC = Hex("#A47EE6"), TextC = Hex("#EEE9F7"),
        ScoreC = Hex("#FAA370"), PickupC = Hex("#5CE68E"), Ink = Hex("#100A1A"), LagPale = Hex("#C9F7DA"), Danger = Hex("#D2465A");
    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

    static AudioMixer mixer;
    static AudioMixerGroup musicGroup, sfxGroup;
    static Sprite white;
    static GameObject sfxPrefab, musicPrefab, pickupFx, hitFx;

    [MenuItem("CS464/Build Assignment 04")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        Folder("Assets/Audio"); Folder("Assets/Prefabs"); Folder("Assets/Scenes"); Folder("Assets/Art");
        MakeMixer(); white = MakeWhiteSprite(); Recolour();
        MakeEffects(); MakeAudioPrefabs(); UpdateGameplayPrefabs(); BuildHudPrefab();
        UpdateLevels(); BuildMainMenu(); BuildCredits(); SetBuildList();
        AssetDatabase.SaveAssets();
        Debug.Log("A4Builder: done");
    }

    // ================================================================ helpers
    static void Folder(string p) { if (!AssetDatabase.IsValidFolder(p)) AssetDatabase.CreateFolder(Path.GetDirectoryName(p).Replace('\\', '/'), Path.GetFileName(p)); }
    static AudioClip Clip(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + n + ".wav");

    static RectTransform R(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
        var rt = (RectTransform)go.transform; rt.SetParent(parent, false); return rt;
    }
    static RectTransform Place(RectTransform rt, Vector2 a, Vector2 pos, Vector2 size)
    { rt.anchorMin = rt.anchorMax = rt.pivot = a; rt.anchoredPosition = pos; rt.sizeDelta = size; return rt; }
    static void Stretch(RectTransform rt, float pad = 0)
    { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad); }
    static Image Img(Transform p, string name, Color c)
    { var rt = R(name, p); var i = rt.gameObject.AddComponent<Image>(); i.sprite = white; i.color = c; i.raycastTarget = false; return i; }
    static TMP_Text Txt(Transform p, string name, string s, float size, Color c, TextAlignmentOptions al = TextAlignmentOptions.Center, bool bold = false)
    {
        var rt = R(name, p); var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = s; t.fontSize = size; t.color = c; t.alignment = al; t.raycastTarget = false;
        if (bold) t.fontStyle = FontStyles.Bold; return t;
    }
    // a solid panel with its text on top (text is never straight over the level)
    static TMP_Text TextPanel(Transform p, string name, Vector2 anchor, Vector2 pos, Vector2 size, string s, float fs, Color textColor, TextAlignmentOptions al = TextAlignmentOptions.Center)
    {
        var panel = Img(p, name + "Panel", Color.Lerp(Panel, Color.black, 0.0f)); Place(panel.rectTransform, anchor, pos, size);
        var t = Txt(panel.transform, name, s, fs, textColor, al); Stretch(t.rectTransform, 8); return t;
    }
    static Button Btn(Transform p, string name, string label, Vector2 size)
    {
        var img = Img(p, name, ButtonC); img.raycastTarget = true; img.rectTransform.sizeDelta = size;
        var b = img.gameObject.AddComponent<Button>(); b.targetGraphic = img;
        var cb = b.colors; cb.normalColor = new Color(.8f, .8f, .8f); cb.highlightedColor = Color.white; cb.selectedColor = Color.white;
        cb.pressedColor = new Color(.6f, .6f, .6f); cb.disabledColor = new Color(.35f, .35f, .35f); b.colors = cb;
        var t = Txt(img.transform, "Label", label, 34, Ink, TextAlignmentOptions.Center, true); Stretch(t.rectTransform);
        img.gameObject.AddComponent<ButtonSound>(); return b;
    }
    static void Wire(Button b, UnityAction a) { UnityEventTools.AddPersistentListener(b.onClick, a); }
    static GameObject Canvas(string name, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); go.layer = 5;
        if (parent) go.transform.SetParent(parent, false);
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; sc.matchWidthOrHeight = 0.5f;
        return go;
    }
    static GameObject EventSys() => new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

    // ================================================================ audio
    static void MakeMixer()
    {
        const string path = "Assets/Audio/MainMixer.mixer";
        mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
        if (mixer == null)
        {
            var asm = typeof(Editor).Assembly;
            var ct = asm.GetType("UnityEditor.Audio.AudioMixerController");
            var gt = asm.GetType("UnityEditor.Audio.AudioMixerGroupController");
            const BindingFlags all = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var ctrl = ct.GetMethod("CreateMixerControllerAtPath", all).Invoke(null, new object[] { path });
            var master = ct.GetProperty("masterGroup", all).GetValue(ctrl);
            object NewGroup(string n)
            {
                var g = ct.GetMethod("CreateNewGroup", all).Invoke(ctrl, new object[] { n, false });
                ct.GetMethod("AddChildToParent", all).Invoke(ctrl, new[] { g, master }); return g;
            }
            var music = NewGroup("Music"); var sfx = NewGroup("SFX");
            var ept = asm.GetType("UnityEditor.Audio.ExposedAudioParameter");
            var arr = System.Array.CreateInstance(ept, 2); string[] names = { "MusicVolume", "SfxVolume" }; var groups = new[] { music, sfx };
            for (int i = 0; i < 2; i++)
            {
                object e = System.Activator.CreateInstance(ept);
                object guid = gt.GetMethod("GetGUIDForVolume", all).Invoke(groups[i], null);
                var fg = ept.GetField("guid", all); if (fg != null) fg.SetValue(e, guid); else ept.GetProperty("guid", all).SetValue(e, guid);
                var fn = ept.GetField("name", all); if (fn != null) fn.SetValue(e, names[i]); else ept.GetProperty("name", all).SetValue(e, names[i]);
                arr.SetValue(e, i);
            }
            ct.GetProperty("exposedParameters", all).SetValue(ctrl, arr);
            EditorUtility.SetDirty((Object)ctrl);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
        }
        musicGroup = mixer.FindMatchingGroups("Music")[0];
        sfxGroup = mixer.FindMatchingGroups("SFX")[0];
    }

    static Sprite MakeWhiteSprite()
    {
        const string p = "Assets/Art/white.png";
        if (!File.Exists(p))
        {
            var t = new Texture2D(4, 4); for (int x = 0; x < 4; x++) for (int y = 0; y < 4; y++) t.SetPixel(x, y, Color.white);
            File.WriteAllBytes(p, t.EncodeToPNG()); AssetDatabase.ImportAsset(p);
            var imp = (TextureImporter)AssetImporter.GetAtPath(p); imp.textureType = TextureImporterType.Sprite; imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(p);
    }

    static void Recolour()
    {
        // palette on value: ground darkest, walls, hazards, player, pickups lightest (reads in grayscale)
        void C(string n, string hex) { var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + n + ".mat"); if (m) { m.SetColor("_BaseColor", Hex(hex)); EditorUtility.SetDirty(m); } }
        C("Ground", "#2A1F40"); C("Stone", "#4B3D6B"); C("Red", "#D2465A"); C("GuardBlue", "#D2465A");
        C("Blue", "#A47EE6"); C("White", "#EEE9F7"); C("Gold", "#FAA370");
    }

    static Material ParticleMat()
    {
        const string p = "Assets/Materials/Particle.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, p); }
        return m;
    }

    static GameObject MakeFx(string name, Color c, int count, float life, float speed, float size)
    {
        var go = new GameObject(name); var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main; main.duration = life; main.loop = false; main.startLifetime = life; main.startSpeed = speed; main.startSize = size;
        main.startColor = c; main.playOnAwake = true; main.stopAction = ParticleSystemStopAction.Destroy; main.gravityModifier = 0.5f;
        var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });   // one burst, no loop
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.15f;
        var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }); col.color = g;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
        string path = "Assets/Prefabs/" + name + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path); Object.DestroyImmediate(go); return prefab;
    }

    static void MakeEffects()
    {
        pickupFx = MakeFx("PickupFX", PickupC, 16, 0.6f, 3.5f, 0.2f);
        hitFx = MakeFx("HitSparks", ScoreC, 22, 0.45f, 5f, 0.14f);
    }

    static void MakeAudioPrefabs()
    {
        var sfx = new GameObject("SfxPlayer"); var comp = sfx.AddComponent<SfxPlayer>(); comp.sources = new AudioSource[4];
        for (int i = 0; i < 4; i++)
        {
            var s = new GameObject("Source" + i).AddComponent<AudioSource>(); s.transform.SetParent(sfx.transform);
            s.playOnAwake = false; s.outputAudioMixerGroup = sfxGroup; s.ignoreListenerPause = true; comp.sources[i] = s;
        }
        comp.pickup = Clip("pickup"); comp.hit = Clip("hit"); comp.click = Clip("click"); comp.pause = Clip("pause"); comp.win = Clip("win"); comp.lose = Clip("lose");
        sfxPrefab = PrefabUtility.SaveAsPrefabAsset(sfx, "Assets/Prefabs/SfxPlayer.prefab"); Object.DestroyImmediate(sfx);

        var mus = new GameObject("PersistentMusic"); var pm = mus.AddComponent<PersistentMusic>(); pm.mixer = mixer;
        var a = mus.AddComponent<AudioSource>(); a.clip = Clip("music"); a.loop = true; a.playOnAwake = true; a.outputAudioMixerGroup = musicGroup;
        musicPrefab = PrefabUtility.SaveAsPrefabAsset(mus, "Assets/Prefabs/PersistentMusic.prefab"); Object.DestroyImmediate(mus);
    }

    static void EditPrefab(string path, System.Action<GameObject> edit)
    {
        var root = PrefabUtility.LoadPrefabContents(path); edit(root);
        PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
    }

    static void AddWorldSound(GameObject root, string clip, AudioRolloffMode mode, float min, float max)
    {
        var h = root.transform.Find("Hum"); if (h) Object.DestroyImmediate(h.gameObject);
        var go = new GameObject("Hum"); go.transform.SetParent(root.transform, false);
        var s = go.AddComponent<AudioSource>(); s.clip = Clip(clip); s.loop = true; s.playOnAwake = true; s.spatialBlend = 1f;
        s.outputAudioMixerGroup = sfxGroup; s.rolloffMode = mode; s.minDistance = min; s.maxDistance = max;
        var w = go.AddComponent<WorldSound>(); w.rolloff = mode; w.minDistance = min; w.maxDistance = max;
    }

    static void UpdateGameplayPrefabs()
    {
        EditPrefab("Assets/Prefabs/Coin.prefab", r => r.GetComponent<Coin>().pickupFx = pickupFx);
        EditPrefab("Assets/Prefabs/PowerUp.prefab", r => r.GetComponent<PowerUp>().pickupFx = pickupFx);
        EditPrefab("Assets/Prefabs/Player.prefab", r =>
        {
            if (!r.GetComponent<HitSfx>()) r.AddComponent<HitSfx>();
            if (!r.GetComponent<CameraShake>()) r.AddComponent<CameraShake>();
            var hs = r.GetComponent<HitSparks>(); if (!hs) hs = r.AddComponent<HitSparks>(); hs.sparks = hitFx;
            if (!r.GetComponent<AudioListener>()) r.AddComponent<AudioListener>();    // the camera is ~12 m away, so the ears ride on the player
        });
        // C1: goal hum is heard across the whole level (logarithmic); guard hum is a short-range warning (linear)
        EditPrefab("Assets/Prefabs/Goal.prefab", r => AddWorldSound(r, "hum_goal", AudioRolloffMode.Logarithmic, 3f, 40f));
        EditPrefab("Assets/Prefabs/Guard.prefab", r => AddWorldSound(r, "hum_guard", AudioRolloffMode.Linear, 1.5f, 16f));
    }

    // ================================================================ HUD prefab (one prefab in every level)
    static void BuildHudPrefab()
    {
        EditPrefab("Assets/Prefabs/GameManager.prefab", root =>
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            var gm = root.GetComponent<GameManager>();
            var canvas = Canvas("HUD", root.transform); var c = canvas.transform;
            EventSys().transform.SetParent(root.transform, false);

            // health bar: back + paler lag bar + fill (both Filled, Horizontal, from the left)
            var hp = Img(c, "HealthPanel", Panel); Place(hp.rectTransform, new Vector2(0, 1), new Vector2(30, -30), new Vector2(520, 90));
            Txt(hp.transform, "Label", "HEALTH", 22, TextC, TextAlignmentOptions.TopLeft, true).rectTransform.anchoredPosition = Vector2.zero;
            var lbl = hp.transform.Find("Label").GetComponent<TMP_Text>(); Place(lbl.rectTransform, new Vector2(0, 1), new Vector2(16, -8), new Vector2(200, 26)); lbl.alignment = TextAlignmentOptions.TopLeft;
            var back = Img(hp.transform, "Back", PanelLight); Place(back.rectTransform, new Vector2(.5f, 0), new Vector2(0, 14), new Vector2(488, 34));
            Image Filled(string n, Color col) { var i = Img(back.transform, n, col); Stretch(i.rectTransform); i.type = Image.Type.Filled; i.fillMethod = Image.FillMethod.Horizontal; i.fillOrigin = 0; i.fillAmount = 1; return i; }
            var lag = Filled("Lag", LagPale); var fill = Filled("Fill", PickupC);
            var bar = hp.gameObject.AddComponent<HealthBar>(); bar.fill = fill; bar.lag = lag;

            gm.titleText = TextPanel(c, "Title", new Vector2(.5f, 1), new Vector2(0, -30), new Vector2(480, 56), "Level", 30, TextC);
            gm.timeText = TextPanel(c, "Time", new Vector2(.5f, 1), new Vector2(0, -96), new Vector2(240, 48), "Time 0.0", 26, TextC);
            gm.scoreText = TextPanel(c, "Score", new Vector2(1, 1), new Vector2(-30, -30), new Vector2(360, 56), "Score: 0", 30, ScoreC);
            gm.livesText = TextPanel(c, "Lives", new Vector2(1, 1), new Vector2(-30, -96), new Vector2(360, 48), "Lives: 3", 26, TextC);
            gm.powerText = TextPanel(c, "Power", new Vector2(0, 1), new Vector2(30, -134), new Vector2(520, 48), "", 26, PickupC);
            gm.messageText = TextPanel(c, "Message", new Vector2(.5f, 0), new Vector2(0, 110), new Vector2(900, 64), "", 30, TextC);
            gm.objectiveText = TextPanel(c, "Objective", new Vector2(0, 0), new Vector2(30, 30), new Vector2(760, 52), "", 24, TextC, TextAlignmentOptions.Left);
            TextPanel(c, "Controls", new Vector2(1, 0), new Vector2(-30, 30), new Vector2(760, 52), "Move WASD / arrows   Jump Space   Pause Esc   Restart R", 22, TextC);

            // pause menu
            var pm = canvas.AddComponent<PauseMenu>();
            var pausePanel = Dim(c, "PausePanel"); var card = Card(pausePanel.transform, new Vector2(620, 520), "PAUSED");
            var resume = MenuButton(card, "Resume", 0); var restart = MenuButton(card, "Restart", 1); var quit = MenuButton(card, "Quit to menu", 2);
            pm.panel = pausePanel.gameObject; pm.firstButton = resume;

            // win and game-over screens
            var winP = Dim(c, "WinPanel"); var wc = Card(winP.transform, new Vector2(760, 640), "LEVEL COMPLETE");
            gm.winInfoText = Txt(wc, "Info", "", 30, TextC); Place(gm.winInfoText.rectTransform, new Vector2(.5f, 1), new Vector2(0, -150), new Vector2(680, 150));
            var next = MenuButton(wc, "Next level", 0, 300); var wRestart = MenuButton(wc, "Restart", 1, 300); var wMenu = MenuButton(wc, "Main menu", 2, 300);
            gm.nextButtonLabel = next.GetComponentInChildren<TMP_Text>();
            var loseP = Dim(c, "LosePanel"); var lc = Card(loseP.transform, new Vector2(700, 560), "GAME OVER");
            gm.loseInfoText = Txt(lc, "Info", "", 30, TextC); Place(gm.loseInfoText.rectTransform, new Vector2(.5f, 1), new Vector2(0, -150), new Vector2(620, 110));
            var lRestart = MenuButton(lc, "Restart", 0, 300); var lMenu = MenuButton(lc, "Main menu", 1, 300);
            gm.winPanel = winP.gameObject; gm.losePanel = loseP.gameObject; gm.winFirstButton = next; gm.loseFirstButton = lRestart;

            var loader = MakeLoader(canvas);   // below
            gm.loader = loader; pm.loader = loader;

            Wire(resume, pm.Resume); Wire(restart, pm.Restart); Wire(quit, pm.QuitToMenu);
            Wire(next, gm.NextLevel); Wire(wRestart, gm.Restart); Wire(wMenu, gm.MainMenu);
            Wire(lRestart, gm.Restart); Wire(lMenu, gm.MainMenu);
            pausePanel.gameObject.SetActive(false); winP.gameObject.SetActive(false); loseP.gameObject.SetActive(false);
            LogContrast();
        });
    }

    static LevelLoader MakeLoader(GameObject canvas)
    {
        var loader = canvas.AddComponent<LevelLoader>();
        var rt = R("LoadingScreen", canvas.transform); Stretch(rt);
        var bg = rt.gameObject.AddComponent<Image>(); bg.sprite = white; bg.color = Panel;
        var cg = rt.gameObject.AddComponent<CanvasGroup>();
        var t = Txt(rt, "Text", "Loading", 56, TextC, TextAlignmentOptions.Center, true); Place(t.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 70), new Vector2(800, 90));
        var track = Img(rt, "Track", PanelLight); Place(track.rectTransform, new Vector2(.5f, .5f), new Vector2(0, -10), new Vector2(800, 36));
        var fill = Img(track.transform, "Fill", ButtonC); Stretch(fill.rectTransform);
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = 0f;
        var pct = Txt(rt, "Percent", "0%", 36, TextC, TextAlignmentOptions.Center); Place(pct.rectTransform, new Vector2(.5f, .5f), new Vector2(0, -80), new Vector2(300, 56));
        loader.loadingScreen = cg; loader.bar = fill; loader.percent = pct; cg.alpha = 0f; cg.blocksRaycasts = false;
        return loader;
    }

    static Image Dim(Transform p, string name)
    { var d = Img(p, name, new Color(0, 0, 0, 0.7f)); Stretch(d.rectTransform); d.raycastTarget = true; return d; }

    static RectTransform Card(Transform p, Vector2 size, string title)
    {
        var card = Img(p, "Card", Panel); Place(card.rectTransform, new Vector2(.5f, .5f), Vector2.zero, size);
        var t = Txt(card.transform, "Title", title, 54, TextC, TextAlignmentOptions.Center, true); Place(t.rectTransform, new Vector2(.5f, 1), new Vector2(0, -30), new Vector2(size.x - 40, 80));
        return card.rectTransform;
    }

    static Button MenuButton(Transform card, string label, int index, float top = 150)
    {
        var b = Btn(card, label.Replace(" ", ""), label, new Vector2(420, 76));
        Place(b.GetComponent<RectTransform>(), new Vector2(.5f, 1), new Vector2(0, -top - index * 100), new Vector2(420, 76)); return b;
    }

    static void LogContrast()
    {
        string L(string n, Color a, Color b) => n + " " + ColourUtil.Contrast(a, b).ToString("0.00");
        Debug.Log("Contrast: " + L("text/panel", TextC, Panel) + " | " + L("score/panel", ScoreC, Panel) + " | " + L("pickup/panel", PickupC, Panel) + " | " + L("ink/button", Ink, ButtonC));
    }

    // ================================================================ levels
    static void UpdateLevels()
    {
        for (int i = 1; i <= 5; i++)
        {
            string path = "Assets/Scenes/Level0" + i + ".unity";
            var sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var l in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (l.GetComponent<PlayerController>() == null) Object.DestroyImmediate(l);   // the player carries the one AudioListener
            AddOnce<PersistentMusic>(musicPrefab); AddOnce<SfxPlayer>(sfxPrefab);
            EditorSceneManager.SaveScene(sc);
        }
    }

    static void AddOnce<T>(GameObject prefab) where T : Component
    {
        if (Object.FindFirstObjectByType<T>() == null) PrefabUtility.InstantiatePrefab(prefab);
    }

    // ================================================================ main menu and credits
    static Camera MenuCamera()
    {
        var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); go.tag = "MainCamera";
        var cam = go.GetComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Panel; return cam;
    }

    static void BuildMainMenu()
    {
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MenuCamera(); EventSys();
        PrefabUtility.InstantiatePrefab(musicPrefab); PrefabUtility.InstantiatePrefab(sfxPrefab);
        var canvas = Canvas("Canvas"); var c = canvas.transform;
        var menu = canvas.AddComponent<MainMenu>(); var vs = canvas.AddComponent<VolumeSlider>(); vs.mixer = mixer;
        var loader = MakeLoader(canvas); menu.loader = loader;

        var title = Txt(c, "Title", "ARENA RUN", 110, TextC, TextAlignmentOptions.Center, true); Place(title.rectTransform, new Vector2(.5f, 1), new Vector2(0, -90), new Vector2(1200, 150));
        var sub = Txt(c, "Sub", "five levels - collect, dodge, reach the goal", 34, ScoreC); Place(sub.rectTransform, new Vector2(.5f, 1), new Vector2(0, -230), new Vector2(1200, 50));

        RectTransform PanelRT(string n) { var p = R(n, c); Place(p, new Vector2(.5f, .5f), new Vector2(0, -80), new Vector2(700, 760)); return p; }
        var main = PanelRT("MainPanel"); var levels = PanelRT("LevelPanel"); var options = PanelRT("OptionsPanel");
        Button Row(RectTransform parent, string label, int i, UnityAction a) { var b = Btn(parent, label.Replace(" ", ""), label, new Vector2(480, 84)); Place(b.GetComponent<RectTransform>(), new Vector2(.5f, 1), new Vector2(0, -i * 108), new Vector2(480, 84)); Wire(b, a); return b; }

        menu.playButton = Row(main, "Play", 0, menu.Play);
        Row(main, "Levels", 1, menu.OpenLevels); Row(main, "Options", 2, menu.OpenOptions); Row(main, "Credits", 3, menu.OpenCredits); Row(main, "Quit", 4, menu.Quit);

        var list = R("List", levels); Place(list, new Vector2(.5f, 1), Vector2.zero, new Vector2(620, 560));
        var vl = list.gameObject.AddComponent<VerticalLayoutGroup>(); vl.spacing = 16; vl.childControlHeight = false; vl.childControlWidth = false; vl.childAlignment = TextAnchor.UpperCenter;
        var tpl = Btn(list, "LevelTemplate", "Level", new Vector2(560, 76)); tpl.gameObject.SetActive(false);
        menu.levelTemplate = tpl; menu.levelList = list;
        menu.levelsBack = Btn(levels, "Back", "Back", new Vector2(480, 76)); Place(menu.levelsBack.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(0, 10), new Vector2(480, 76)); Wire(menu.levelsBack, menu.BackToMain);

        var ol = Txt(options, "Label", "Music volume", 40, TextC); Place(ol.rectTransform, new Vector2(.5f, 1), new Vector2(0, -20), new Vector2(600, 60));
        var sl = DefaultControls.CreateSlider(new DefaultControls.Resources()); sl.transform.SetParent(options, false);
        var slRt = (RectTransform)sl.transform; Place(slRt, new Vector2(.5f, 1), new Vector2(0, -130), new Vector2(560, 40));
        var slider = sl.GetComponent<Slider>(); vs.slider = slider;
        foreach (var im in sl.GetComponentsInChildren<Image>()) { im.sprite = white; }
        sl.transform.Find("Background").GetComponent<Image>().color = PanelLight;
        sl.transform.Find("Fill Area/Fill").GetComponent<Image>().color = ButtonC;
        sl.transform.Find("Handle Slide Area/Handle").GetComponent<Image>().color = TextC;
        sl.transform.Find("Handle Slide Area/Handle").GetComponent<RectTransform>().sizeDelta = new Vector2(36, 0);
        var cbs = slider.colors; cbs.normalColor = new Color(.85f, .85f, .85f); cbs.selectedColor = Color.white; slider.colors = cbs;
        menu.optionsBack = Btn(options, "Back", "Back", new Vector2(480, 76)); Place(menu.optionsBack.GetComponent<RectTransform>(), new Vector2(.5f, 1), new Vector2(0, -260), new Vector2(480, 76)); Wire(menu.optionsBack, menu.BackToMain);
        // slider and Back button navigate with the arrow keys
        var nav = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = menu.optionsBack }; slider.navigation = nav;
        var nav2 = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = slider }; menu.optionsBack.navigation = nav2;

        menu.mainPanel = main.gameObject; menu.levelPanel = levels.gameObject; menu.optionsPanel = options.gameObject;
        EditorSceneManager.SaveScene(sc, "Assets/Scenes/MainMenu.unity");
    }

    static void BuildCredits()
    {
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MenuCamera(); EventSys();
        PrefabUtility.InstantiatePrefab(musicPrefab); PrefabUtility.InstantiatePrefab(sfxPrefab);
        var canvas = Canvas("Canvas"); var c = canvas.transform; var cs = canvas.AddComponent<CreditsScreen>();
        var loader = MakeLoader(canvas); cs.loader = loader;
        var t = Txt(c, "Title", "CREDITS", 90, TextC, TextAlignmentOptions.Center, true); Place(t.rectTransform, new Vector2(.5f, 1), new Vector2(0, -60), new Vector2(1200, 120));
        string body =
            "<b>Sounds and music</b>\nEvery sound and the music loop were generated for this project by tools/make_audio.py\n(sine and noise synthesis, no samples). Own work - free to use (CC0).\n\n" +
            "<b>Font</b>\nLiberation Sans - SIL Open Font Licence 1.1 (TextMesh Pro default font).\n\n" +
            "<b>Levels, prefabs and scripts</b>\nOwn work for CS464 Assignments 03 and 04. Structure follows the CS464 lecture labs 08 to 10.\n\n" +
            "<b>Made with</b>\nUnity 6.3 - BSSE23018";
        var tp = Img(c, "BodyPanel", Panel); Place(tp.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 0), new Vector2(1400, 600));
        var bt = Txt(tp.transform, "Body", body, 30, TextC, TextAlignmentOptions.Center); Stretch(bt.rectTransform, 24);
        var back = Btn(c, "Back", "Back to menu", new Vector2(480, 84)); Place(back.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(0, 60), new Vector2(480, 84));
        cs.backButton = back; Wire(back, cs.Back);
        EditorSceneManager.SaveScene(sc, "Assets/Scenes/Credits.unity");
    }

    static void SetBuildList()
    {
        string[] order = { "MainMenu", "Level01", "Level02", "Level03", "Level04", "Level05", "Credits" };
        var list = new EditorBuildSettingsScene[order.Length];
        for (int i = 0; i < order.Length; i++) list[i] = new EditorBuildSettingsScene("Assets/Scenes/" + order[i] + ".unity", true);
        EditorBuildSettings.scenes = list;
    }
}
