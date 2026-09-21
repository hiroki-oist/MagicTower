using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 新しい本線のシーン `Assets/Scenes/Tower.unity` を組み立てる。
///
/// 旧来は階ごとに 51本のシーンを手で作っていたが、
/// `.floor` データ（全50階ぶん、原典から確定済み）を `FloorLoader` が実行時に組み立てる方式に移す。
/// このシーンはその入れ物で、中身は空のまま `TowerRuntime.Enter()` で階を出し入れする。
///
/// 手で並べる作業を無くすため、シーンはコードから生成して参照も繋ぐ。
/// バッチモードから:
///   Unity -batchmode -quit -projectPath . -executeMethod TowerSceneBuilder.Build
/// </summary>
public static class TowerSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Tower.unity";

    [MenuItem("MagicTower/本線シーンを作り直す")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- カメラ。原典の画面（640x400、1マス32ドット）と同じ見え方にする ---
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = ScreenLayout.CameraSize;   // 400/32/2 = 6.25
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;
        // 音を聴く耳。**これが無いと何も鳴らない**（既定のカメラには最初から付いているが、
        // ここは手で組み立てているので自分で足す必要がある）。
        camGo.AddComponent<AudioListener>();

        // --- 塔の本体 ---
        var tower = new GameObject("Tower");
        var loader = tower.AddComponent<FloorLoader>();
        var rules  = tower.AddComponent<GameRules>();
        var runtime = tower.AddComponent<TowerRuntime>();
        var controller = tower.AddComponent<TowerController>();

        var container = new GameObject("Floor").transform;
        container.SetParent(tower.transform, false);

        var registry = Resources.Load<FloorPrefabRegistry>("FloorPrefabRegistry");
        if (registry == null) Debug.LogError("TowerSceneBuilder: FloorPrefabRegistry が読めない");
        loader.Configure(registry, container);

        // FloorLoader の [SerializeField] を保存されるように書き戻す
        var so = new SerializedObject(loader);
        so.FindProperty("registry").objectReferenceValue = registry;
        so.FindProperty("container").objectReferenceValue = container;
        so.ApplyModifiedPropertiesWithoutUndo();

        var rso = new SerializedObject(runtime);
        rso.FindProperty("loader").objectReferenceValue = loader;
        rso.FindProperty("rules").objectReferenceValue = rules;
        rso.ApplyModifiedPropertiesWithoutUndo();

        // プレーヤーの見た目。原典は PLAYER1.P58 / PLAYER2.P58 の2コマを
        // まとめて渡して描いている（`push 0x7e90; push 0x7d00; lcall 0x68b, 0x40d6`、10か所）。
        // その1コマ目を使う。
        // 音を鳴らす役。本線が積んだ音を毎手引き取る。
        // 鳴らす口（曲用と効果音用）もここで作って繋いでおく。
        // `SoundPlayer` は自分でも用意できるが、シーンを見ただけで分かるようにする。
        var sound = tower.AddComponent<SoundPlayer>();
        var music = tower.AddComponent<AudioSource>();
        music.loop = true; music.playOnAwake = false;
        var effects = tower.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        var sso = new SerializedObject(sound);
        sso.FindProperty("musicSource").objectReferenceValue = music;
        sso.FindProperty("effectSource").objectReferenceValue = effects;
        sso.ApplyModifiedPropertiesWithoutUndo();

        var cso = new SerializedObject(controller);
        cso.FindProperty("loader").objectReferenceValue = loader;
        cso.FindProperty("sound").objectReferenceValue = sound;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Effects/Player_0.png");
        if (sprite != null) cso.FindProperty("playerSprite").objectReferenceValue = sprite;
        cso.ApplyModifiedPropertiesWithoutUndo();

        BuildHud(tower, controller);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"TowerSceneBuilder: {ScenePath} を作成した");
    }

    const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/PixelMplus12-Regular.asset";

    /// <summary>
    /// 左右の帯と文字を作る。原典の画面構成に合わせて、
    /// 左に階と能力値、右に装備・鍵・メッセージを置く。
    /// </summary>
    static void BuildHud(GameObject tower, TowerController controller)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        var canvasGo = new GameObject("HUD",
            typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler),
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1152f, 720f);   // 動画と同じ比率
        // **縦を基準に合わせる**。盤面はカメラ（縦 12.5マス＝原典の400ドット）で写すので、
        // 画布も縦で合わせないと、窓の幅が変わったときに枠と盤面がずれる。
        scaler.matchWidthOrHeight = 1f;

        // 原典の画面（640x400 を1.8倍した 1152x720）ぴったりの入れ物。
        // **画布の中央に置く**ので、窓の横幅が変わっても盤面（カメラ）とずれない。
        // 中の欄はこの入れ物の左上を原点に置くので、原典の座標がそのまま使える。
        var screenGo = new GameObject("Screen", typeof(RectTransform));
        screenGo.transform.SetParent(canvasGo.transform, false);
        var screen = screenGo.GetComponent<RectTransform>();
        screen.anchorMin = screen.anchorMax = screen.pivot = new Vector2(0.5f, 0.5f);
        screen.anchoredPosition = Vector2.zero;
        screen.sizeDelta = new Vector2(DialogueWindow.CanvasWidth, DialogueWindow.CanvasHeight);

        // 画面の枠。原典は LCG.P58 の1枚目（640x400）がそれで、中央 (144,24)-(496,376) が
        // 空いていて そこに盤面が入る。書き出す時にその穴を透明にしてある。
        BuildFrame(screen);

        var view = canvasGo.AddComponent<TowerHudView>();
        var vso = new SerializedObject(view);
        vso.FindProperty("controller").objectReferenceValue = controller;

        // 欄の位置は原典（docs/original/screen_layout.txt）そのまま。
        // 数字だけを出す欄なので、見出しの文字は原典の枠の絵側にある（まだ同定できていない）。
        vso.FindProperty("floorLabel").objectReferenceValue =
            Box(screen, font, "FloorLabel", new RectInt(0, 48, 144, 16), 26f, TextAlignmentOptions.Left);
        vso.FindProperty("hp").objectReferenceValue =
            Box(screen, font, "Hp", ScreenLayout.HpBox, 24f, TextAlignmentOptions.Right);
        vso.FindProperty("attack").objectReferenceValue =
            Box(screen, font, "Attack", ScreenLayout.AttackBox, 24f, TextAlignmentOptions.Right);
        vso.FindProperty("defense").objectReferenceValue =
            Box(screen, font, "Defense", ScreenLayout.DefenseBox, 24f, TextAlignmentOptions.Right);
        vso.FindProperty("gold").objectReferenceValue =
            Box(screen, font, "Gold", ScreenLayout.GoldBox, 24f, TextAlignmentOptions.Right);

        vso.FindProperty("weapon").objectReferenceValue =
            Box(screen, font, "Weapon", new RectInt(496, 48, 96, 32), 20f, TextAlignmentOptions.TopRight);
        vso.FindProperty("shield").objectReferenceValue =
            Box(screen, font, "Shield", new RectInt(496, 96, 96, 32), 20f, TextAlignmentOptions.TopRight);
        vso.FindProperty("keys").objectReferenceValue =
            Box(screen, font, "Keys", new RectInt(528, 168, 96, 48), 20f, TextAlignmentOptions.TopLeft);
        vso.FindProperty("messageBox").objectReferenceValue =
            Box(screen, font, "Message", ScreenLayout.MessageBox, 16f, TextAlignmentOptions.TopLeft);
        vso.ApplyModifiedPropertiesWithoutUndo();

        BuildBattleView(controller);
        BuildDialogueWindow(screen, font, controller);
        BuildMenuWindow(screen, font, controller);
        BuildFullScreen(screen, font, controller, TowerGame.PageKind.Title,
                        "TitleScreen", "Assets/Sprites/Screens/Title_Art.png",
                        TitleScreen.Title,   TitleScreen.TitleDot,
                        TitleScreen.Copyright, TitleScreen.CopyrightDot);
        BuildFullScreen(screen, font, controller, TowerGame.PageKind.FinalScreen,
                        "EndingScreen", "Assets/Sprites/Screens/Ending_Art.png",
                        Ending.TitleLine,  Ending.TitleDot,
                        Ending.AnyKeyLine, Ending.AnyKeyDot);
    }

    /// <summary>メニューの窓。11行・上端50 で固定（原典 0x0d1e）。</summary>
    static void BuildMenuWindow(RectTransform screen, TMP_FontAsset font, TowerController controller)
    {
        var r = TowerMenu.Window;
        var panelGo = new GameObject("MenuWindow", typeof(RectTransform),
                                     typeof(UnityEngine.UI.Image));
        panelGo.transform.SetParent(screen, false);
        var rt = panelGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = DialogueWindow.ToCanvas(r);
        rt.sizeDelta = DialogueWindow.SizeOnCanvas(r);

        var img = panelGo.GetComponent<UnityEngine.UI.Image>();
        img.color = DialogueWindowView.Fill;

        var textGo = new GameObject("Body", typeof(RectTransform));
        textGo.transform.SetParent(panelGo.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12f, 8f);
        trt.offsetMax = new Vector2(-12f, -8f);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 22f;
        tmp.color = DialogueWindowView.Text;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = false;

        var win = screen.gameObject.AddComponent<MenuWindowView>();
        var wso = new SerializedObject(win);
        wso.FindProperty("controller").objectReferenceValue = controller;
        wso.FindProperty("panel").objectReferenceValue = rt;
        wso.FindProperty("background").objectReferenceValue = img;
        wso.FindProperty("body").objectReferenceValue = tmp;
        wso.ApplyModifiedPropertiesWithoutUndo();

        panelGo.SetActive(false);
    }

    /// <summary>会話ウィンドウ。位置と大きさは原典（MTE.EXE 0x94ba）由来。</summary>
    static void BuildDialogueWindow(RectTransform screen, TMP_FontAsset font, TowerController controller)
    {
        var panelGo = new GameObject("DialogueWindow", typeof(RectTransform),
                                     typeof(UnityEngine.UI.Image));
        panelGo.transform.SetParent(screen, false);
        var rt = panelGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);

        var r = DialogueWindow.For(6);          // はじめは6行ぶんの大きさ
        rt.anchoredPosition = DialogueWindow.ToCanvas(r);
        rt.sizeDelta = DialogueWindow.SizeOnCanvas(r);

        var img = panelGo.GetComponent<UnityEngine.UI.Image>();
        img.color = DialogueWindowView.Fill;

        var textGo = new GameObject("Body", typeof(RectTransform));
        textGo.transform.SetParent(panelGo.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12f, 8f);
        trt.offsetMax = new Vector2(-12f, -8f);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 22f;
        tmp.color = DialogueWindowView.Text;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = false;

        var win = screen.gameObject.AddComponent<DialogueWindowView>();
        var wso = new SerializedObject(win);
        wso.FindProperty("controller").objectReferenceValue = controller;
        wso.FindProperty("panel").objectReferenceValue = rt;
        wso.FindProperty("background").objectReferenceValue = img;
        wso.FindProperty("body").objectReferenceValue = tmp;
        wso.ApplyModifiedPropertiesWithoutUndo();

        panelGo.SetActive(false);
    }

    /// <summary>戦闘の演出を再生する役。盤面と同じ座標で絵を出すので画布の外に置く。</summary>
    static void BuildBattleView(TowerController controller)
    {
        var go = new GameObject("BattleView");
        go.transform.SetParent(controller.transform, false);
        var view = go.AddComponent<BattleView>();
        var so = new SerializedObject(view);
        so.FindProperty("controller").objectReferenceValue = controller;
        // 当たりの絵は原典の GROUND.P58 の11枚目（buffer 変位 0x87a）。
        // 戦闘でも魔法攻撃でも場面の節目でも、原典は同じこの絵を貼る。
        so.FindProperty("hitSprite").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Effects/Hit.png");
        // 金騎士の絵。盤面に出る金騎士（ID133）と同じものを使う。
        so.FindProperty("knightSprite").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Tiles/133_Enemy_Worrior.png");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 画面いっぱいの1枚絵と2行の文字（題名・終幕）。ふだんは隠しておく。
    /// 文字の位置は原典が渡す値そのままで、x はバイト（＝8ドット）、y はドット。
    /// </summary>
    static void BuildFullScreen(RectTransform screen, TMP_FontAsset font, TowerController controller,
                                TowerGame.PageKind showOn, string name, string artPath,
                                string first, Vector2Int firstAt,
                                string second, Vector2Int secondAt)
    {
        var panelGo = new GameObject(name, typeof(RectTransform));
        panelGo.transform.SetParent(screen, false);
        var prt = panelGo.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0f, 1f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(DialogueWindow.CanvasWidth, DialogueWindow.CanvasHeight);

        var artGo = new GameObject("Art", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        artGo.transform.SetParent(panelGo.transform, false);
        var art = artGo.GetComponent<UnityEngine.UI.Image>();
        art.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
        art.raycastTarget = false;
        var art_rt = artGo.GetComponent<RectTransform>();
        art_rt.anchorMin = art_rt.anchorMax = art_rt.pivot = new Vector2(0f, 1f);
        art_rt.anchoredPosition = Vector2.zero;
        art_rt.sizeDelta = new Vector2(DialogueWindow.CanvasWidth, DialogueWindow.CanvasHeight);

        var t1 = Box(screen, font, name + "_Line1",
                     new RectInt(firstAt.x, firstAt.y,
                                 first.Length * ScreenLayout.TextCharWidth, 20),
                     26f, TextAlignmentOptions.TopLeft);
        var t2 = Box(screen, font, name + "_Line2",
                     new RectInt(secondAt.x, secondAt.y,
                                 second.Length * ScreenLayout.TextCharWidth, 20),
                     26f, TextAlignmentOptions.TopLeft);
        t1.text = first; t2.text = second;
        t1.transform.SetParent(panelGo.transform, false);
        t2.transform.SetParent(panelGo.transform, false);

        var view = panelGo.AddComponent<FullScreenView>();
        var so = new SerializedObject(view);
        so.FindProperty("controller").objectReferenceValue = controller;
        so.FindProperty("showOn").enumValueIndex = (int)showOn;
        so.FindProperty("panel").objectReferenceValue = panelGo;
        so.FindProperty("art").objectReferenceValue = art;
        so.FindProperty("line1").objectReferenceValue = t1;
        so.FindProperty("line2").objectReferenceValue = t2;
        so.ApplyModifiedPropertiesWithoutUndo();

        panelGo.SetActive(false);
    }

    const string FramePath = "Assets/Sprites/Screens/Frame.png";

    /// <summary>原典の画面の枠を背景に敷く。</summary>
    static void BuildFrame(RectTransform screen)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);
        if (sprite == null)
        {
            Debug.LogWarning($"TowerSceneBuilder: {FramePath} が Sprite として読めない");
            return;
        }
        var go = new GameObject("Frame", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(screen, false);
        go.transform.SetAsFirstSibling();          // 文字より後ろ

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(DialogueWindow.CanvasWidth, DialogueWindow.CanvasHeight);

        var img = go.GetComponent<UnityEngine.UI.Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
    }

    /// <summary>原典の画面座標（640x400）のまま欄を置く。</summary>    /// <summary>原典の画面座標（640x400）のまま欄を置く。</summary>
    static TextMeshProUGUI Box(RectTransform screen, TMP_FontAsset font, string name,
                               RectInt r, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(screen, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = ScreenLayout.ToCanvas(r);
        rt.sizeDelta = ScreenLayout.SizeOnCanvas(r);

        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = align;
        t.enableWordWrapping = true;
        return t;
    }

}
