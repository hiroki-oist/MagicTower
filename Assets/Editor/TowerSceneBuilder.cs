using System.Collections.Generic;
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
        // 盤面の下地。原典は階を描く前に盤面を**索引0（68,68,68）**で塗る
        // （階を移った直後のコマで実測。会話の窓の中と同じ色）。
        // 渦巻きの描き出しの最中や、絵の無いマスでここが見える。
        cam.backgroundColor = DialogueWindowView.Fill;
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

        // 盤面の敵と人は2コマで動く（原典 0x92a4）。
        BuildMonsterAnimator(tower, controller, loader);
        // 移動の演出はコマごとにその動きを進めるので、入力の側から参照を持たせる
        var mso = new SerializedObject(controller);
        mso.FindProperty("monsters").objectReferenceValue =
            tower.GetComponent<MonsterAnimator>();
        mso.ApplyModifiedPropertiesWithoutUndo();

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"TowerSceneBuilder: {ScenePath} を作成した");
    }

    const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/PixelMplus12-Regular.asset";

    /// <summary>
    /// 盤面の敵と人を2コマで動かす役（原典 0x92a4）。
    /// 2コマ目の絵は `Assets/Sprites/Tiles/*_b.png`。名前は1コマ目と揃えてある
    /// （`061_Enemy_SlimeG.png` ↔ `062_Enemy_SlimeG_b.png`）ので、それで対応を取る。
    /// </summary>
    static void BuildMonsterAnimator(GameObject tower, TowerController controller,
                                     FloorLoader loader)
    {
        // 絵は `061_Enemy_SlimeG.png`（1コマ目）と `062_Enemy_SlimeG_b.png`（2コマ目）で
        // 名前を揃えてあるので、名札（頭3桁の後ろ）で対応を取る。
        var firstOf = new Dictionary<string, string>();
        var secondOf = new Dictionary<string, string>();
        foreach (var path in System.IO.Directory.GetFiles("Assets/Sprites/Tiles", "*.png"))
        {
            var file = System.IO.Path.GetFileNameWithoutExtension(path);
            if (file.Length < 5) continue;
            // macOS の照合は大文字小文字を区別しないので、`*_b.png` で引くと
            // `010_Wall_Secret_B.png` のような別物まで拾ってしまう。ここで厳密に見る。
            if (file.EndsWith("_b", System.StringComparison.Ordinal))
                secondOf[file.Substring(4, file.Length - 4 - 2)] = path;
            else
                firstOf[file.Substring(4)] = path;
        }

        var keys = new List<string>();
        var first = new List<Sprite>();
        var second = new List<Sprite>();
        foreach (var pair in secondOf)
        {
            string firstPath;
            if (!firstOf.TryGetValue(pair.Key, out firstPath))
            {
                Debug.LogWarning($"TowerSceneBuilder: {pair.Key} の1コマ目の絵が無い");
                continue;
            }
            var a = LoadSprite(firstPath);
            var b = LoadSprite(pair.Value);
            if (a == null || b == null)
            {
                Debug.LogWarning($"TowerSceneBuilder: {pair.Key} の絵が読めない");
                continue;
            }
            keys.Add(pair.Key); first.Add(a); second.Add(b);
        }

        var anim = tower.AddComponent<MonsterAnimator>();
        var so = new SerializedObject(anim);
        so.FindProperty("controller").objectReferenceValue = controller;
        so.FindProperty("loader").objectReferenceValue = loader;
        Fill(so.FindProperty("keys"), keys);
        Fill(so.FindProperty("firstFrames"), first);
        Fill(so.FindProperty("secondFrames"), second);
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"TowerSceneBuilder: 2コマで動くもの {keys.Count} 種");
    }

    /// <summary>
    /// 絵を1枚読む。切り分けの設定によっては本体ではなく下にぶら下がっているので、
    /// <c>LoadAssetAtPath&lt;Sprite&gt;</c> では取れないことがある。全部見て最初の1枚を返す。
    /// </summary>
    static Sprite LoadSprite(string path)
    {
        path = path.Replace('\\', '/');
        var one = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (one != null) return one;
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite) return (Sprite)o;
        return null;
    }

    static void Fill(SerializedProperty prop, List<string> values)
    {
        prop.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            prop.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    static void Fill(SerializedProperty prop, List<Sprite> values)
    {
        prop.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

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
            Box(screen, font, "FloorLabel", ScreenLayout.FloorBox, ScreenLayout.TextSizeOnCanvas, TextAlignmentOptions.Left);
        vso.FindProperty("hp").objectReferenceValue =
            Box(screen, font, "Hp", ScreenLayout.HpBox, ScreenLayout.TextSizeOnCanvas, TextAlignmentOptions.Right);
        vso.FindProperty("attack").objectReferenceValue =
            Box(screen, font, "Attack", ScreenLayout.AttackBox, ScreenLayout.TextSizeOnCanvas, TextAlignmentOptions.Right);
        vso.FindProperty("defense").objectReferenceValue =
            Box(screen, font, "Defense", ScreenLayout.DefenseBox, ScreenLayout.TextSizeOnCanvas, TextAlignmentOptions.Right);
        vso.FindProperty("gold").objectReferenceValue =
            Box(screen, font, "Gold", ScreenLayout.GoldBox, ScreenLayout.TextSizeOnCanvas, TextAlignmentOptions.Right);

        // WEAPON / SHIELD の見出しは枠の絵に入っているので、欄には名前だけを出す。
        vso.FindProperty("weapon").objectReferenceValue =
            Box(screen, font, "Weapon", ScreenLayout.WeaponBox, ScreenLayout.TextSizeOnCanvas, TextAlignmentOptions.Left);
        vso.FindProperty("shield").objectReferenceValue =
            Box(screen, font, "Shield", ScreenLayout.ShieldBox, ScreenLayout.TextSizeOnCanvas, TextAlignmentOptions.Left);

        // 装備の絵。原典は 剣=物体ID 50+段位 / 盾=55+段位 の絵をそのまま貼る（0x9b80 付近）。
        vso.FindProperty("weaponIcon").objectReferenceValue =
            Icon(screen, "WeaponIcon", ScreenLayout.WeaponIcon);
        vso.FindProperty("shieldIcon").objectReferenceValue =
            Icon(screen, "ShieldIcon", ScreenLayout.ShieldIcon);
        for (int rank = 1; rank <= 5; rank++)
        {
            vso.FindProperty("swordIcons").GetArrayElementAtIndex(rank - 1)
               .objectReferenceValue = TileById(50 + rank);
            vso.FindProperty("shieldIcons").GetArrayElementAtIndex(rank - 1)
               .objectReferenceValue = TileById(55 + rank);
        }

        // 持ち物の欄（原典 0x2f8c）。物体ID 36〜50 の絵を3列で並べる。
        var slots = vso.FindProperty("itemSlots");
        slots.arraySize = ScreenLayout.ItemPanelSlots;
        for (int i = 0; i < ScreenLayout.ItemPanelSlots; i++)
        {
            var id = ScreenLayout.ItemPanelObjectId(i);
            var img = Icon(screen, $"Item{id}", ScreenLayout.ItemPanelSlot(i));
            var sprite = TileById(id);
            if (sprite != null) img.sprite = sprite;
            img.enabled = false;                      // 持っていない間は出さない
            slots.GetArrayElementAtIndex(i).objectReferenceValue = img;
        }

        // 鍵は絵を本数ぶん並べる（原典 0x9ff8）。入れ物を画面の左上に合わせておけば、
        // 中の絵は原典の座標をそのまま使える。
        var keyGo = new GameObject("Keys", typeof(RectTransform));
        keyGo.transform.SetParent(screen, false);
        var keyRt = keyGo.GetComponent<RectTransform>();
        keyRt.anchorMin = keyRt.anchorMax = keyRt.pivot = new Vector2(0f, 1f);
        keyRt.anchoredPosition = Vector2.zero;
        keyRt.sizeDelta = Vector2.zero;
        vso.FindProperty("keyIcons").objectReferenceValue = keyRt;
        vso.FindProperty("keyRed").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hud/Key_Red.png");
        vso.FindProperty("keyPurple").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hud/Key_Purple.png");
        vso.FindProperty("keyYellow").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hud/Key_Yellow.png");
        vso.FindProperty("messageBox").objectReferenceValue =
            Box(screen, font, "Message", ScreenLayout.MessageBox, ScreenLayout.TextSizeOnCanvas, TextAlignmentOptions.TopLeft);

        // 戦闘中は同じ枠に相手の絵と能力値を出す（原典 0x9626）。
        var battleView = BuildBattleView(controller);
        vso.FindProperty("battleView").objectReferenceValue = battleView;
        // 戦闘の演出が終わるまで入力を止めるので、入力の側からも見えるようにする
        var bso = new SerializedObject(controller);
        bso.FindProperty("battle").objectReferenceValue = battleView;
        bso.ApplyModifiedPropertiesWithoutUndo();
        vso.FindProperty("registry").objectReferenceValue =
            Resources.Load<FloorPrefabRegistry>("FloorPrefabRegistry");
        BuildMonsterPanel(screen, font, vso);
        vso.ApplyModifiedPropertiesWithoutUndo();

        // 作者の旧版の見せ方（黒い帯）。AuthorStyle.Enabled が false なら出ない。
        BuildBattleBand(screen, font, controller, battleView);

        BuildDialogueWindow(screen, font, controller);
        BuildMenuWindow(screen, font, controller);
        BuildFullScreen(screen, font, controller, TowerGame.PageKind.Title,
                        // 題名は**絵を貼らない**。動画の序章（Part1 t≈31）では
                        // 真っ黒な画面に
                        //   「Ｔｏｗｅｒ　ｏｆ　ｔｈｅ　Ｓｏｒｃｅｒｅｒ」(144,160)
                        //   「（Ｃ）１９９６　Ｎ．Ｗ」(144,190)
                        // の2行だけが出る（原典 0x6db2 / 0x6dcd）。
                        // MTOPSTR（渦巻き）はそのあとの**序章の1枚絵**、
                        // MTMOJI（「魔法の塔」）はこの場面には出てこない。
                        "TitleScreen", "",
                        TitleScreen.Title,   TitleScreen.TitleDot,
                        TitleScreen.Copyright, TitleScreen.CopyrightDot);
        // 序章の1枚絵（MTOPSTR）。行が1つずつ現れるので4枚を差し替える。
        BuildFullScreen(screen, font, controller, TowerGame.PageKind.Prologue,
                        "PrologueScreen", "Assets/Sprites/Screens/Prologue_0.png",
                        "", TitleScreen.TitleDot, "", TitleScreen.CopyrightDot,
                        new[] { "Assets/Sprites/Screens/Prologue_0.png",
                                "Assets/Sprites/Screens/Prologue_1.png",
                                "Assets/Sprites/Screens/Prologue_2.png",
                                "Assets/Sprites/Screens/Prologue_3.png" });
        // 終幕の塔の絵（MTOWER2）と、終わりの言葉（MTENDMJ、段落が1つずつ）
        BuildFullScreen(screen, font, controller, TowerGame.PageKind.EndingTower,
                        "EndingTowerScreen", "Assets/Sprites/Screens/Ending_Tower2.png",
                        "", Ending.TitleDot, "", Ending.AnyKeyDot);
        BuildFullScreen(screen, font, controller, TowerGame.PageKind.EndingText,
                        "EndingTextScreen", "Assets/Sprites/Screens/Ending_Text_0.png",
                        "", Ending.TitleDot, "", Ending.AnyKeyDot,
                        new[] { "Assets/Sprites/Screens/Ending_Text_0.png",
                                "Assets/Sprites/Screens/Ending_Text_1.png",
                                "Assets/Sprites/Screens/Ending_Text_2.png",
                                "Assets/Sprites/Screens/Ending_Text_3.png",
                                "Assets/Sprites/Screens/Ending_Text_4.png" });
        // ゲームオーバー（原典 0x097ac）。真っ黒に1行だけ。
        BuildFullScreen(screen, font, controller, TowerGame.PageKind.GameOver,
                        "GameOverScreen", "",
                        GameOverScreen.Line, GameOverScreen.Dot,
                        "", GameOverScreen.Dot);
        BuildFullScreen(screen, font, controller, TowerGame.PageKind.FinalScreen,
                        "EndingScreen", "Assets/Sprites/Screens/Ending_Art.png",
                        Ending.TitleLine,  Ending.TitleDot,
                        Ending.AnyKeyLine, Ending.AnyKeyDot);
    }

    /// <summary>
    /// 窓の中の文字。原典は窓の左上から (16,12) の内側に、行送り20ドットで書く
    /// （x=0x16 バイト＝176、y=上端+0xc、行送りは lcall 0x107e,0 の 0x14）。
    /// </summary>
    static TextMeshProUGUI WindowBody(GameObject panelGo, TMP_FontAsset font, bool author = false)
    {
        int left = author ? AuthorStyle.DialogueTextLeft : DialogueWindow.TextLeft;
        int top  = author ? AuthorStyle.DialogueTextTop  : DialogueWindow.TextTop;
        var textGo = new GameObject("Body", typeof(RectTransform));
        textGo.transform.SetParent(panelGo.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(left * DialogueWindow.CanvasScale, 0f);
        trt.offsetMax = new Vector2(0f, -top * DialogueWindow.CanvasScale);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = author ? AuthorStyle.DialogueTextOnCanvas : ScreenLayout.TextSizeOnCanvas;
        tmp.lineSpacing = author ? AuthorStyle.DialogueLineSpacing : TowerHud.InfoLineSpacing;
        tmp.color = DialogueWindowView.Text;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = false;
        return tmp;
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

        var tmp = WindowBody(panelGo, font);

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

        // はじめは6行ぶんの大きさ（作者の見せ方のときは広い窓）
        var r = AuthorStyle.Enabled ? AuthorStyle.DialogueFor(6) : DialogueWindow.For(6);
        rt.anchoredPosition = DialogueWindow.ToCanvas(r);
        rt.sizeDelta = DialogueWindow.SizeOnCanvas(r);

        var img = panelGo.GetComponent<UnityEngine.UI.Image>();
        if (AuthorStyle.Enabled)
        {
            // 旧版のセリフの枠はこの絵そのもの（9スライス・中身は黒）
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AuthorStyle.PanelSpritePath);
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = DialogueWindowView.AuthorFill;
        }
        else img.color = DialogueWindowView.Fill;

        var tmp = WindowBody(panelGo, font, AuthorStyle.Enabled);

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
    /// <summary>
    /// 戦闘中の相手の欄（原典 0x9626）。位置は原典そのまま:
    /// 絵 (560,232)、名前 (528,272)、命/攻/防 の見出し (536,296/312/328)、
    /// 数値は右詰めで右端 616。
    /// </summary>
    static void BuildMonsterPanel(RectTransform screen, TMP_FontAsset font, SerializedObject vso)
    {
        var panelGo = new GameObject("MonsterPanel", typeof(RectTransform));
        panelGo.transform.SetParent(screen, false);
        var panel = panelGo.GetComponent<RectTransform>();
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = Vector2.zero;

        vso.FindProperty("monsterPanel").objectReferenceValue = panel;
        vso.FindProperty("monsterIcon").objectReferenceValue =
            Icon(panel, "MonsterIcon", ScreenLayout.MonsterIcon);
        vso.FindProperty("monsterName").objectReferenceValue =
            Box(panel, font, "MonsterName", ScreenLayout.MonsterName,
                ScreenLayout.TextSizeOnCanvas, TextAlignmentOptions.Left);

        // 見出しは原典が毎回書く文字（命／攻／防）。
        Label(panel, font, "HpLabel",      ScreenLayout.MonsterHpLabel,      "命");
        Label(panel, font, "AttackLabel",  ScreenLayout.MonsterAttackLabel,  "攻");
        Label(panel, font, "DefenseLabel", ScreenLayout.MonsterDefenseLabel, "防");

        vso.FindProperty("monsterHp").objectReferenceValue =
            Box(panel, font, "MonsterHp", ScreenLayout.MonsterHpBox,
                ScreenLayout.TextSizeOnCanvas, TextAlignmentOptions.Right);
        vso.FindProperty("monsterAttack").objectReferenceValue =
            Box(panel, font, "MonsterAttack", ScreenLayout.MonsterAttackBox,
                ScreenLayout.TextSizeOnCanvas, TextAlignmentOptions.Right);
        vso.FindProperty("monsterDefense").objectReferenceValue =
            Box(panel, font, "MonsterDefense", ScreenLayout.MonsterDefenseBox,
                ScreenLayout.TextSizeOnCanvas, TextAlignmentOptions.Right);

        panelGo.SetActive(false);
    }

    static void Label(RectTransform parent, TMP_FontAsset font, string name,
                      RectInt r, string text)
    {
        var t = Box(parent, font, name, r, ScreenLayout.TextSizeOnCanvas,
                    TextAlignmentOptions.Left);
        t.text = text;
    }

    /// <summary>
    /// 戦闘中の黒い帯（**作者の旧版の見せ方**、<see cref="AuthorStyle"/>）。
    /// 旧版 `Assets/Scenes/Floor1.unity`（commit a58a72a）の BattleUI と同じ並びで、
    /// 左が自分・右が相手、上に「第 N ターン」。原典どおりの見せ方に戻すなら
    /// <c>AuthorStyle.Enabled</c> を false にする（この入れ物は出なくなる）。
    /// </summary>
    static void BuildBattleBand(RectTransform screen, TMP_FontAsset font,
                                TowerController controller, BattleView battleView)
    {
        // 入れ物は画面の左上に合わせておく。中の欄は原典の画面座標をそのまま使える。
        var holderGo = new GameObject("BattleBand", typeof(RectTransform));
        holderGo.transform.SetParent(screen, false);
        var holder = holderGo.GetComponent<RectTransform>();
        holder.anchorMin = holder.anchorMax = holder.pivot = new Vector2(0f, 1f);
        holder.anchoredPosition = Vector2.zero;
        holder.sizeDelta = Vector2.zero;

        // 旧版と同じ枠の絵を、旧版と同じ並び（第Nターン・左右の絵と名前・左右の能力値）で貼る。
        var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AuthorStyle.PanelSpritePath);
        var band = Panel(holder, "TurnPanel", AuthorStyle.TurnPanel, panelSprite);
        Panel(holder, "PlayerPanel", AuthorStyle.SidePanel(AuthorStyle.PlayerCenterX), panelSprite);
        Panel(holder, "EnemyPanel",  AuthorStyle.SidePanel(AuthorStyle.EnemyCenterX),  panelSprite);
        Panel(holder, "PlayerStatsPanel", AuthorStyle.StatPanel(AuthorStyle.PlayerCenterX), panelSprite);
        Panel(holder, "EnemyStatsPanel",  AuthorStyle.StatPanel(AuthorStyle.EnemyCenterX),  panelSprite);

        var view = screen.gameObject.AddComponent<BattleBandView>();
        var so = new SerializedObject(view);
        so.FindProperty("controller").objectReferenceValue = controller;
        so.FindProperty("battleView").objectReferenceValue = battleView;
        so.FindProperty("registry").objectReferenceValue =
            Resources.Load<FloorPrefabRegistry>("FloorPrefabRegistry");
        so.FindProperty("panel").objectReferenceValue = holder;
        so.FindProperty("background").objectReferenceValue = band;
        so.FindProperty("turnText").objectReferenceValue =
            Box(holder, font, "BattleTurn", AuthorStyle.TurnBox,
                AuthorStyle.DialogueTextOnCanvas, TextAlignmentOptions.Center);

        BuildBandSide(holder, font, so, "Player", AuthorStyle.PlayerCenterX);
        BuildBandSide(holder, font, so, "Enemy",  AuthorStyle.EnemyCenterX);
        so.ApplyModifiedPropertiesWithoutUndo();

        holderGo.SetActive(false);
    }

    /// <summary>旧版の枠の絵（9スライス・中身は黒）を1枚貼る。</summary>
    static UnityEngine.UI.Image Panel(RectTransform parent, string name, RectInt r, Sprite sprite)
    {
        var img = Icon(parent, name, r);
        img.sprite = sprite;
        img.type = UnityEngine.UI.Image.Type.Sliced;
        img.color = AuthorStyle.BandFill;
        return img;
    }

    /// <summary>帯の片側（絵・名前・HP/ATK/DEF の3行）。</summary>
    static void BuildBandSide(RectTransform holder, TMP_FontAsset font,
                              SerializedObject so, string side, int centerX)
    {
        var lower = char.ToLowerInvariant(side[0]) + side.Substring(1);
        so.FindProperty(lower + "Icon").objectReferenceValue =
            Icon(holder, side + "Icon", AuthorStyle.Icon(centerX));
        so.FindProperty(lower + "Name").objectReferenceValue =
            Box(holder, font, side + "Name", AuthorStyle.Name(centerX),
                ScreenLayout.TextSizeOnCanvas, TextAlignmentOptions.Center);
        var stats = so.FindProperty(lower + "Stats");
        stats.arraySize = AuthorStyle.StatTops.Length;
        for (int i = 0; i < AuthorStyle.StatTops.Length; i++)
            stats.GetArrayElementAtIndex(i).objectReferenceValue =
                Box(holder, font, side + "Stat" + i, AuthorStyle.Stat(centerX, i),
                    ScreenLayout.TextSizeOnCanvas, TextAlignmentOptions.Left);
    }

    static BattleView BuildBattleView(TowerController controller)
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
        return view;
    }

    /// <summary>
    /// 画面いっぱいの1枚絵と2行の文字（題名・終幕）。ふだんは隠しておく。
    /// 文字の位置は原典が渡す値そのままで、x はバイト（＝8ドット）、y はドット。
    /// </summary>
    static void BuildFullScreen(RectTransform screen, TMP_FontAsset font, TowerController controller,
                                TowerGame.PageKind showOn, string name, string artPath,
                                string first, Vector2Int firstAt,
                                string second, Vector2Int secondAt,
                                string[] stepArt = null)
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
        // 絵が無い場面（題名）は**真っ黒**。原典も何も貼らずに文字だけ書く。
        art.sprite = string.IsNullOrEmpty(artPath)
                     ? null : AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
        art.color = art.sprite == null ? Color.black : Color.white;
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
        if (stepArt != null)
        {
            var steps = so.FindProperty("steps");
            steps.arraySize = stepArt.Length;
            for (int i = 0; i < stepArt.Length; i++)
                steps.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite(stepArt[i]);
        }
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
    /// <summary>絵を貼るだけの欄。位置は原典の座標そのまま。</summary>
    static UnityEngine.UI.Image Icon(RectTransform parent, string name, RectInt r)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = ScreenLayout.ToCanvas(r);
        rt.sizeDelta = ScreenLayout.SizeOnCanvas(r);
        return go.GetComponent<UnityEngine.UI.Image>();
    }

    /// <summary>
    /// 物体IDのタイル絵。書き出した絵は `051_Sword_1.png` のように頭にIDが付いている。
    /// </summary>
    static Sprite TileById(int id)
    {
        foreach (var path in System.IO.Directory.GetFiles("Assets/Sprites/Tiles", "*.png"))
        {
            var file = System.IO.Path.GetFileNameWithoutExtension(path);
            if (file.EndsWith("_b", System.StringComparison.Ordinal)) continue;   // 2コマ目
            if (file.StartsWith($"{id:000}_"))
                return LoadSprite(path);
        }
        Debug.LogWarning($"TowerSceneBuilder: 物体ID {id} の絵が見つからない");
        return null;
    }

    static TextMeshProUGUI Box(RectTransform parent, TMP_FontAsset font, string name,
                               RectInt r, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = ScreenLayout.ToCanvas(r);
        rt.sizeDelta = ScreenLayout.SizeOnCanvas(r);

        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = align;
        // 折り返しは原典の規則で自前でやる（TowerHud.InfoBox）ので、TMP 側では折らない。
        t.enableWordWrapping = false;
        // 原典の行送りは文字の高さと同じ16ドット。TMP の既定より詰める。
        t.lineSpacing = TowerHud.InfoLineSpacing;
        return t;
    }

}
