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
        // 原典の画面は縦12.5マス（<see cref="ScreenLayout.CameraSize"/>）だが、
        // 縦画面の並べ方（作者の指定）では**正方形の枠に11マスちょうど**にする。
        // 実際の値は PhoneLayoutView が枠の大きさに合わせて毎コマ入れ直す。
        cam.orthographicSize = PhoneScreen.CameraSize;
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
        // 2コマ目（PLAYER2.P58）。左右に動くときだけ入れ替える（原典 0xa276）
        var sprite2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Effects/Player_1.png");
        if (sprite2 != null) cso.FindProperty("playerSprite2").objectReferenceValue = sprite2;
        cso.ApplyModifiedPropertiesWithoutUndo();

        BuildHud(tower, controller, cam);

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
    static void BuildHud(GameObject tower, TowerController controller, Camera cam)
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
        // **スマホの縦画面**（作者の指定。並べ方は PhoneScreen）。
        // 盤面が画面の幅いっぱいなので、**幅を基準**に合わせる。
        scaler.referenceResolution =
            new Vector2(PhoneScreen.ReferenceWidth, PhoneScreen.ReferenceHeight);
        // **入りきる側に合わせる**（Expand＝拡大率は縦横の小さいほう）。
        // 窓が横長でも縦長でも、スマホの画面（1080x1920）がそのままの形で入る。
        // ⚠ Shrink は逆（大きいほう＝はみ出す）。実機のシミュレータ（1206x2622）で
        //   左右が切れて気づいた。
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
        scaler.matchWidthOrHeight = 0f;

        // スマホの画面ぶんの枠。中の並べ方はこの枠の中だけで決める（PhoneLayoutView）。
        var frameGo = new GameObject("PhoneFrame", typeof(RectTransform));
        frameGo.transform.SetParent(canvasGo.transform, false);
        var frame = frameGo.GetComponent<RectTransform>();
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
        frame.anchoredPosition = Vector2.zero;
        frame.sizeDelta = new Vector2(PhoneScreen.ReferenceWidth, PhoneScreen.ReferenceHeight);

        // 盤面の枠。中身はカメラが写すので、ここは場所を決めるためだけの空の枠。
        var mapGo = new GameObject("MapArea", typeof(RectTransform));
        mapGo.transform.SetParent(frame, false);
        var mapArea = mapGo.GetComponent<RectTransform>();
        mapArea.anchorMin = mapArea.anchorMax = mapArea.pivot = new Vector2(0.5f, 0.5f);

        // 原典の座標で置く入れ物（会話とメニューの窓・戦闘の帯・1枚絵）。
        // 大きさと場所は PhoneLayoutView が盤面に合わせる。
        var screenGo = new GameObject("Screen", typeof(RectTransform));
        screenGo.transform.SetParent(frame, false);
        var screen = screenGo.GetComponent<RectTransform>();
        screen.anchorMin = screen.anchorMax = screen.pivot = new Vector2(0.5f, 0.5f);
        screen.anchoredPosition = Vector2.zero;
        screen.sizeDelta = new Vector2(DialogueWindow.CanvasWidth, DialogueWindow.CanvasHeight);

        // 上下の帯。地は原典の枠の左右の欄と同じ石積み（作者の指定）。
        var top = Band(frame, "TopPanel", true, PhoneScreen.TopHeight);
        var bottom = Band(frame, "BottomPanel", false, PhoneScreen.BottomHeight);

        var view = canvasGo.AddComponent<TowerHudView>();
        var vso = new SerializedObject(view);
        vso.FindProperty("controller").objectReferenceValue = controller;

        // --- 上の帯（3段）。並べ方は PhoneScreen（作者の指定） ---
        float w = PhoneScreen.ReferenceWidth, pad = PhoneScreen.Pad * 1.5f;
        float rh = PhoneScreen.RowHeight;

        // 1段目: 金 / 黄 / 紫 / 赤
        float cell = (w - pad * 5f) / 4f;
        var goldBox = Pill(top, font, "Gold", pad, 0f, cell, rh, PhoneScreen.GoldIconPath);
        var yKeyBox = Pill(top, font, "KeyYellow", pad * 2 + cell, 0f, cell, rh,
                           "Assets/Sprites/Hud/Key_Yellow.png");
        var pKeyBox = Pill(top, font, "KeyPurple", pad * 3 + cell * 2, 0f, cell, rh,
                           "Assets/Sprites/Hud/Key_Purple.png");
        var rKeyBox = Pill(top, font, "KeyRed", pad * 4 + cell * 3, 0f, cell, rh,
                           "Assets/Sprites/Hud/Key_Red.png");
        vso.FindProperty("gold").objectReferenceValue = goldBox;
        vso.FindProperty("yellowKeys").objectReferenceValue = yKeyBox;
        vso.FindProperty("purpleKeys").objectReferenceValue = pKeyBox;
        vso.FindProperty("redKeys").objectReferenceValue = rKeyBox;

        // 2段目: 階の旗 / 生命 / 攻撃 / 防御
        float flagW = 210f;
        var floorBox = Pill(top, font, "FloorLabel", pad, rh, flagW, rh, null,
                            PhoneScreen.FloorFill);
        float sw = (w - pad * 5f - flagW) / 3f;
        var hpBox  = Pill(top, font, "Hp",      pad * 2 + flagW, rh, sw, rh,
                          PhoneScreen.HpIconPath);
        var atkBox = Pill(top, font, "Attack",  pad * 3 + flagW + sw, rh, sw, rh,
                          PhoneScreen.AtkIconPath);
        var defBox = Pill(top, font, "Defense", pad * 4 + flagW + sw * 2, rh, sw, rh,
                          PhoneScreen.DefIconPath);
        vso.FindProperty("floorLabel").objectReferenceValue = floorBox;
        vso.FindProperty("hp").objectReferenceValue = hpBox;
        vso.FindProperty("attack").objectReferenceValue = atkBox;
        vso.FindProperty("defense").objectReferenceValue = defBox;

        // 3段目: 装備（剣と盾）と、押しても何も起きない飾りのボタン2つ
        float eqW = (w - pad * 4f - rh * 2f) / 2f;
        UnityEngine.UI.Image wIcon, sIcon;
        var weaponBox = Equip(top, font, "Weapon", pad, rh * 2, eqW, rh, out wIcon);
        var shieldBox = Equip(top, font, "Shield", pad * 2 + eqW, rh * 2, eqW, rh, out sIcon);
        vso.FindProperty("weapon").objectReferenceValue = weaponBox;
        vso.FindProperty("shield").objectReferenceValue = shieldBox;
        vso.FindProperty("weaponIcon").objectReferenceValue = wIcon;
        vso.FindProperty("shieldIcon").objectReferenceValue = sIcon;
        Placeholder(top, font, "Trophy", pad * 3 + eqW * 2, rh * 2, rh, rh, "実績",
                    PhoneScreen.RightButton);
        Placeholder(top, font, "Help", pad * 3 + eqW * 2 + rh, rh * 2, rh, rh, "？",
                    PhoneScreen.RightButton);

        for (int rank = 1; rank <= 5; rank++)
        {
            vso.FindProperty("swordIcons").GetArrayElementAtIndex(rank - 1)
               .objectReferenceValue = TileById(50 + rank);
            vso.FindProperty("shieldIcons").GetArrayElementAtIndex(rank - 1)
               .objectReferenceValue = TileById(55 + rank);
        }

        // --- 下の帯: 持ち物の帯（黒い地・左右に送れる）とボタン ---
        // 地
        var barGo = new GameObject("ItemBar", typeof(RectTransform),
                                   typeof(UnityEngine.UI.Image));
        barGo.transform.SetParent(bottom, false);
        var barRt = barGo.GetComponent<RectTransform>();
        barRt.anchorMin = barRt.anchorMax = barRt.pivot = new Vector2(0f, 1f);
        barRt.anchoredPosition = Vector2.zero;
        barRt.sizeDelta = new Vector2(w, PhoneScreen.ItemBarHeight);
        barGo.GetComponent<UnityEngine.UI.Image>().color = PhoneScreen.ItemBarFill;

        // 左右の矢印（押すと1画面ぶん送る）
        var leftArrow = Arrow(barRt, font, "ArrowLeft", true);
        var rightArrow = Arrow(barRt, font, "ArrowRight", false);

        // 見える所（ここからはみ出た絵は切り取る）
        var viewGo = new GameObject("ItemViewport", typeof(RectTransform),
                                    typeof(UnityEngine.UI.RectMask2D));
        viewGo.transform.SetParent(barRt, false);
        var viewRt = viewGo.GetComponent<RectTransform>();
        viewRt.anchorMin = new Vector2(0f, 0f);
        viewRt.anchorMax = new Vector2(1f, 1f);
        viewRt.offsetMin = new Vector2(PhoneScreen.ArrowWidth, 0f);
        viewRt.offsetMax = new Vector2(-PhoneScreen.ArrowWidth, 0f);

        float islot = (w - PhoneScreen.ArrowWidth * 2f) / PhoneScreen.ItemsPerPage;
        var contentGo = new GameObject("ItemContent", typeof(RectTransform));
        contentGo.transform.SetParent(viewRt, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 0f);
        contentRt.anchorMax = new Vector2(0f, 1f);
        contentRt.pivot = new Vector2(0f, 0.5f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(islot * ScreenLayout.ItemPanelSlots, 0f);

        var slots = vso.FindProperty("itemSlots");
        slots.arraySize = ScreenLayout.ItemPanelSlots;
        var slotRects = new List<Object>();
        for (int i = 0; i < ScreenLayout.ItemPanelSlots; i++)
        {
            var id = ScreenLayout.ItemPanelObjectId(i);
            var img = Slot(contentRt, $"Item{id}", islot * i, 0f,
                           islot, PhoneScreen.ItemBarHeight);
            var sprite = TileById(id);
            if (sprite != null) img.sprite = sprite;
            img.enabled = false;                      // 持っていない間は出さない
            slots.GetArrayElementAtIndex(i).objectReferenceValue = img;
            slotRects.Add(img.transform.parent);      // 触り判定は枠のほう
        }

        // ボタンはどれも**まだ繋がっていない**（作者の指定。移動もキーボードの矢印のまま）
        float bw = PhoneScreen.SideButtonWidth, bh = 84f, bgap = 12f;
        int nb = PhoneScreen.LeftButtons.Length;
        float stack = bh * nb + bgap * (nb - 1);
        float btop = PhoneScreen.ItemBarHeight + (PhoneScreen.ButtonsHeight - stack) / 2f;
        RectTransform damageButton = null;
        var pressable = new List<Object>
        {
            leftArrow.GetComponent<UnityEngine.UI.Image>(),
            rightArrow.GetComponent<UnityEngine.UI.Image>(),
        };
        for (int i = 0; i < nb; i++)
        {
            float by = btop + (bh + bgap) * i;
            var left = Placeholder(bottom, font, "Left" + i, pad, by, bw, bh,
                                   PhoneScreen.LeftButtons[i], PhoneScreen.LeftButton);
            var right = Placeholder(bottom, font, "Right" + i, w - pad - bw, by, bw, bh,
                                    PhoneScreen.RightButtons[i], PhoneScreen.RightButton);
            pressable.Add(left.GetComponent<UnityEngine.UI.Image>());
            pressable.Add(right.GetComponent<UnityEngine.UI.Image>());
            if (i == PhoneScreen.DamageButtonIndex) damageButton = right;
        }
        // 中央の十字。**押すと歩く**（作者が選んだ絵。DPadView が触りを受ける）
        float dpad = PhoneScreen.DPadSize;
        var padRt = Placeholder(bottom, font, "DPad", (w - dpad) / 2f,
                                PhoneScreen.ItemBarHeight
                                + (PhoneScreen.ButtonsHeight - dpad) / 2f,
                                dpad, dpad, "", new Color(0f, 0f, 0f, 0f));
        // **5枚に分けて重ねる**（上・右・下・左の扇と、真ん中の決定ボタン）。
        // 押している所だけ色を変えられるようにするため。
        var padParts = new List<Object>();
        foreach (var path in PhoneScreen.DPadPartPaths)
        {
            var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path),
                                    typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(padRt, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            img.preserveAspect = true;
            img.raycastTarget = false;
            padParts.Add(img);
        }

        // 知らせは**画面の中央に出して薄くなって消える**（作者の指定。MessageToastView）。
        // 原典の右下の欄（TowerHudView.messageBox）は使わない。
        var toastGo = new GameObject("Toast", typeof(RectTransform),
                                     typeof(UnityEngine.UI.Image));
        toastGo.transform.SetParent(frame, false);
        var toastRt = toastGo.GetComponent<RectTransform>();
        toastRt.anchorMin = new Vector2(0f, 0.5f);
        toastRt.anchorMax = new Vector2(1f, 0.5f);
        toastRt.pivot = new Vector2(0.5f, 0.5f);
        toastRt.sizeDelta = new Vector2(0f, PhoneScreen.ToastHeight);
        toastRt.anchoredPosition = new Vector2(0f, mapArea.anchoredPosition.y);
        var toastImg = toastGo.GetComponent<UnityEngine.UI.Image>();
        toastImg.color = new Color(0f, 0f, 0f, 0.72f);
        toastImg.raycastTarget = false;

        var toastTextGo = new GameObject("Text", typeof(RectTransform));
        toastTextGo.transform.SetParent(toastRt, false);
        var ttRt = toastTextGo.GetComponent<RectTransform>();
        ttRt.anchorMin = Vector2.zero; ttRt.anchorMax = Vector2.one;
        ttRt.offsetMin = new Vector2(24f, 0f); ttRt.offsetMax = new Vector2(-24f, 0f);
        var toastText = toastTextGo.AddComponent<TextMeshProUGUI>();
        if (font != null) toastText.font = font;
        toastText.fontSize = PhoneScreen.ToastFontSize;
        toastText.enableAutoSizing = true;
        toastText.fontSizeMin = 20f;
        toastText.fontSizeMax = PhoneScreen.ToastFontSize;
        toastText.color = Color.white;
        toastText.alignment = TextAlignmentOptions.Center;
        toastText.enableWordWrapping = false;
        toastText.raycastTarget = false;

        var toastView = canvasGo.AddComponent<MessageToastView>();
        var toso = new SerializedObject(toastView);
        toso.FindProperty("controller").objectReferenceValue = controller;
        toso.FindProperty("panel").objectReferenceValue = toastRt;
        toso.FindProperty("background").objectReferenceValue = toastImg;
        toso.FindProperty("text").objectReferenceValue = toastText;
        toso.ApplyModifiedPropertiesWithoutUndo();
        toastGo.SetActive(false);

        // 戦闘中は同じ枠に相手の絵と能力値を出す（原典 0x9626）。
        var battleView = BuildBattleView(controller);
        vso.FindProperty("battleView").objectReferenceValue = battleView;
        // 戦闘の演出が終わるまで入力を止めるので、入力の側からも見えるようにする
        var bso = new SerializedObject(controller);
        bso.FindProperty("battle").objectReferenceValue = battleView;
        // 主人公の絵（Java版の 上/下/左/右 × 2コマ）。無ければ原典の2枚のまま。
        var heroProp = bso.FindProperty("heroSprites");
        heroProp.arraySize = AuthorStyle.HeroSpriteNames.Length;
        var atlas = AssetDatabase.LoadAllAssetsAtPath(AuthorStyle.HeroAtlasPath);
        for (int i = 0; i < AuthorStyle.HeroSpriteNames.Length; i++)
        {
            Sprite found = null;
            foreach (var a in atlas)
            {
                var sp = a as Sprite;
                if (sp != null && sp.name == AuthorStyle.HeroSpriteNames[i]) { found = sp; break; }
            }
            if (found == null)
                Debug.LogWarning($"主人公の絵が見つからない: {AuthorStyle.HeroSpriteNames[i]}");
            heroProp.GetArrayElementAtIndex(i).objectReferenceValue = found;
        }
        bso.ApplyModifiedPropertiesWithoutUndo();
        vso.FindProperty("registry").objectReferenceValue =
            Resources.Load<FloorPrefabRegistry>("FloorPrefabRegistry");
        BuildMonsterPanel(screen, font, vso);
        vso.ApplyModifiedPropertiesWithoutUndo();

        // 作者の旧版の見せ方（黒い帯）。AuthorStyle.Enabled が false なら出ない。
        var bandView = BuildBattleBand(screen, font, controller, battleView);

        // 鍵で扉を開けたときの9コマの動き（原典 0x7be6）。絵は LCG.P58 の #2〜#5。
        var doorGo = new GameObject("DoorOpenView");
        doorGo.transform.SetParent(controller.transform, false);
        var doorView = doorGo.AddComponent<DoorOpenView>();
        var dso = new SerializedObject(doorView);
        dso.FindProperty("controller").objectReferenceValue = controller;
        var doorColors = new List<string> { "Yellow", "Purple", "Red", "Sealed" };
        var doorFrames = new List<Sprite>();
        foreach (var c in doorColors)
            for (int f = 0; f < DoorAnimation.Frames; f++)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(DoorAnimation.SpritePath(c, f));
                if (sp == null) Debug.LogWarning($"TowerSceneBuilder: 扉の絵が無い {c}_{f}");
                doorFrames.Add(sp);
            }
        Fill(dso.FindProperty("colors"), doorColors);
        Fill(dso.FindProperty("frames"), doorFrames);
        dso.ApplyModifiedPropertiesWithoutUndo();

        // 扉が開き切るまで／戦闘の帯が消えるまで、次の一手を受けないようにする
        var wso2 = new SerializedObject(controller);
        wso2.FindProperty("doorView").objectReferenceValue = doorView;
        wso2.FindProperty("band").objectReferenceValue = bandView;
        wso2.ApplyModifiedPropertiesWithoutUndo();

        // 20階のボスが現れる10コマ（原典 0x3044）。絵は LCG.P58 の #6。
        var bossGo = new GameObject("BossAppearView");
        bossGo.transform.SetParent(controller.transform, false);
        var bossView = bossGo.AddComponent<BossAppearView>();
        var boso = new SerializedObject(bossView);
        boso.FindProperty("controller").objectReferenceValue = controller;
        var bossFrames = new List<Sprite>();
        for (int f = 0; f < BossAppear.Frames; f++)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(BossAppear.SpritePath(f));
            if (sp == null) Debug.LogWarning($"TowerSceneBuilder: ボス登場の絵が無い {f}");
            bossFrames.Add(sp);
        }
        Fill(boso.FindProperty("frames"), bossFrames);
        boso.ApplyModifiedPropertiesWithoutUndo();

        // 場面でものが現れるときの点滅（原典 0x6ae0）。会話の窓はこれが終わってから出す。
        var blinkGo = new GameObject("BlinkView");
        blinkGo.transform.SetParent(controller.transform, false);
        var blinkView = blinkGo.AddComponent<BlinkView>();
        var blso = new SerializedObject(blinkView);
        blso.FindProperty("controller").objectReferenceValue = controller;
        blso.FindProperty("loader").objectReferenceValue =
            controller.GetComponent<FloorLoader>();
        blso.ApplyModifiedPropertiesWithoutUndo();

        // 場面の中で人が歩いていく動き（原典 0x070dc の盗賊）。
        var walkGo = new GameObject("NpcWalkView");
        walkGo.transform.SetParent(controller.transform, false);
        var walkView = walkGo.AddComponent<NpcWalkView>();
        var wkso = new SerializedObject(walkView);
        wkso.FindProperty("controller").objectReferenceValue = controller;
        wkso.FindProperty("loader").objectReferenceValue =
            controller.GetComponent<FloorLoader>();
        wkso.FindProperty("registry").objectReferenceValue =
            Resources.Load<FloorPrefabRegistry>("FloorPrefabRegistry");
        wkso.ApplyModifiedPropertiesWithoutUndo();

        // **メニューの窓を先に置く**。あとに置いたものが前に出るので、
        // メニューを開いたまま開く会話の窓（飛翔のオーブの断り）が隠れないようにする。
        BuildMenuWindow(screen, font, controller);
        BuildDialogueWindow(screen, font, controller, blinkView, bossView);
        // 一覧は画面いっぱいに出すので、原典の座標の入れ物ではなく画布に直に置く
        BuildHeroOrbPanel(frame, font, controller,
                          Resources.Load<FloorPrefabRegistry>("FloorPrefabRegistry"));
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
        // 終幕の塔の絵は上へ流れていく（原典 0x0ce83 の26回の輪）
        var towerScreen = BuildFullScreen(screen, font, controller, TowerGame.PageKind.EndingTower,
                        "EndingTowerScreen", "Assets/Sprites/Screens/Ending_Tower2.png",
                        "", Ending.TitleDot, "", Ending.AnyKeyDot);
        if (towerScreen != null)
        {
            // 白へ寄せるぶんの板。原典はパレットで色を白へ寄せる（0x0c4d8 の表）。
            var veilGo = new GameObject("WhiteVeil", typeof(RectTransform),
                                        typeof(UnityEngine.UI.Image));
            veilGo.transform.SetParent(towerScreen.transform, false);
            var vrt = veilGo.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = vrt.offsetMax = Vector2.zero;
            var veil = veilGo.GetComponent<UnityEngine.UI.Image>();
            veil.color = new Color(1f, 1f, 1f, 0f);
            veil.raycastTarget = false;

            var tso = new SerializedObject(towerScreen);
            tso.FindProperty("scrollTower").boolValue = true;
            tso.FindProperty("whiteVeil").objectReferenceValue = veil;
            tso.ApplyModifiedPropertiesWithoutUndo();
        }
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

        // 十字キーの触りを受ける役（押しっぱなしで歩き続ける）
        var padView = canvasGo.AddComponent<DPadView>();
        var padso = new SerializedObject(padView);
        padso.FindProperty("controller").objectReferenceValue = controller;
        padso.FindProperty("pad").objectReferenceValue = padRt;
        FillObjects(padso.FindProperty("parts"), padParts);
        padso.FindProperty("topBand").objectReferenceValue = top;
        padso.FindProperty("bottomBand").objectReferenceValue = bottom;
        padso.ApplyModifiedPropertiesWithoutUndo();

        // 持ち物の帯と「ダメージ表示」の触りを受ける役（作者の指定）
        var barView = canvasGo.AddComponent<ItemBarView>();
        var barso = new SerializedObject(barView);
        barso.FindProperty("controller").objectReferenceValue = controller;
        barso.FindProperty("viewport").objectReferenceValue = viewRt;
        barso.FindProperty("content").objectReferenceValue = contentRt;
        barso.FindProperty("leftArrow").objectReferenceValue = leftArrow;
        barso.FindProperty("rightArrow").objectReferenceValue = rightArrow;
        barso.FindProperty("damageButton").objectReferenceValue = damageButton;
        FillObjects(barso.FindProperty("slots"), slotRects);
        FillObjects(barso.FindProperty("buttons"), pressable);
        barso.ApplyModifiedPropertiesWithoutUndo();

        // 盤面の枠へカメラを合わせ、窓の入れ物も盤面の大きさに合わせる役（縦画面）
        var layout = canvasGo.AddComponent<PhoneLayoutView>();
        var lso = new SerializedObject(layout);
        lso.FindProperty("boardCamera").objectReferenceValue = cam;
        lso.FindProperty("frame").objectReferenceValue = frame;
        lso.FindProperty("mapArea").objectReferenceValue = mapArea;
        lso.FindProperty("topBand").objectReferenceValue = top;
        lso.FindProperty("bottomBand").objectReferenceValue = bottom;
        lso.FindProperty("screen").objectReferenceValue = screen;
        lso.ApplyModifiedPropertiesWithoutUndo();
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

        // 選んでいる行に置く印（原典 PICTURE.P58 の4枚目。0x9e78 が -0x337 から貼る）
        var curGo = new GameObject("Cursor", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        curGo.transform.SetParent(panelGo.transform, false);
        var cur = curGo.GetComponent<UnityEngine.UI.Image>();
        cur.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hud/Cursor_0.png");
        cur.raycastTarget = false;

        var win = screen.gameObject.AddComponent<MenuWindowView>();
        var wso = new SerializedObject(win);
        wso.FindProperty("controller").objectReferenceValue = controller;
        wso.FindProperty("panel").objectReferenceValue = rt;
        wso.FindProperty("background").objectReferenceValue = img;
        wso.FindProperty("body").objectReferenceValue = tmp;
        wso.FindProperty("cursor").objectReferenceValue = cur;
        wso.FindProperty("cursorFrame2").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hud/Cursor_1.png");
        wso.ApplyModifiedPropertiesWithoutUndo();

        panelGo.SetActive(false);
    }

    /// <summary>会話ウィンドウ。位置と大きさは原典（MTE.EXE 0x94ba）由来。</summary>
    static void BuildDialogueWindow(RectTransform screen, TMP_FontAsset font,
                                    TowerController controller, BlinkView blink,
                                    BossAppearView boss)
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
        wso.FindProperty("blink").objectReferenceValue = blink;
        wso.FindProperty("boss").objectReferenceValue = boss;
        // 選んでいる行に置く印（メニューの窓と同じ、原典 PICTURE.P58 の4枚目）
        var dcurGo = new GameObject("Cursor", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        dcurGo.transform.SetParent(panelGo.transform, false);
        var dcur = dcurGo.GetComponent<UnityEngine.UI.Image>();
        dcur.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hud/Cursor_0.png");
        dcur.raycastTarget = false;
        dcur.enabled = false;
        wso.FindProperty("cursor").objectReferenceValue = dcur;
        wso.FindProperty("cursorFrame2").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hud/Cursor_1.png");
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
    static BattleBandView BuildBattleBand(RectTransform screen, TMP_FontAsset font,
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
        return view;
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
    static FullScreenView BuildFullScreen(RectTransform screen, TMP_FontAsset font,
                                TowerController controller,
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
        return view;
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

    /// <summary>
    /// 勇者のオーブの一覧（作者の指定。並べ方は <see cref="HeroOrbPanel"/>）。
    /// 9行ぶんの枠をあらかじめ作っておき、出すときに中身だけ入れ替える。
    /// </summary>
    static void BuildHeroOrbPanel(RectTransform screen, TMP_FontAsset font,
                                  TowerController controller, FloorPrefabRegistry registry)
    {
        var panelGo = new GameObject("HeroOrbPanel", typeof(RectTransform),
                                     typeof(UnityEngine.UI.Image));
        panelGo.transform.SetParent(screen, false);
        var rt = panelGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(HeroOrbPanel.MarginX, HeroOrbPanel.MarginY);
        rt.anchorMax = new Vector2(1f - HeroOrbPanel.MarginX, 1f - HeroOrbPanel.MarginY);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img = panelGo.GetComponent<UnityEngine.UI.Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AuthorStyle.PanelSpritePath);
        img.type = UnityEngine.UI.Image.Type.Sliced;
        img.color = DialogueWindowView.AuthorFill;

        var icons = new List<UnityEngine.UI.Image>();
        var names = new List<TextMeshProUGUI>();
        var golds = new List<TextMeshProUGUI>();
        var turns = new List<TextMeshProUGUI>();
        var stats = new List<TextMeshProUGUI>();
        var damages = new List<TextMeshProUGUI>();

        for (int i = 0; i < HeroOrb.MaxRows; i++)
        {
            float top = 1f - HeroOrbPanel.RowHeight * i;
            float bottom = top - HeroOrbPanel.RowHeight;
            var rowGo = new GameObject($"Row{i}", typeof(RectTransform));
            rowGo.transform.SetParent(panelGo.transform, false);
            var row = rowGo.GetComponent<RectTransform>();
            row.anchorMin = new Vector2(0f, bottom);
            row.anchorMax = new Vector2(1f, top);
            row.offsetMin = new Vector2(12f, 2f);
            row.offsetMax = new Vector2(-12f, -2f);

            var iconGo = new GameObject("Icon", typeof(RectTransform),
                                        typeof(UnityEngine.UI.Image));
            iconGo.transform.SetParent(rowGo.transform, false);
            var irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(HeroOrbPanel.IconLeft, 0.05f);
            irt.anchorMax = new Vector2(HeroOrbPanel.IconLeft, 0.95f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.sizeDelta = new Vector2(88f, 0f);
            irt.anchoredPosition = Vector2.zero;
            var iconImg = iconGo.GetComponent<UnityEngine.UI.Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            icons.Add(iconImg);

            names.Add(RowText(rowGo, font, "Name", HeroOrbPanel.NameLeft,
                              HeroOrbPanel.MiddleLeft, true, TextAlignmentOptions.Left));
            golds.Add(RowText(rowGo, font, "Gold", HeroOrbPanel.NameLeft,
                              HeroOrbPanel.MiddleLeft, false, TextAlignmentOptions.Left));
            turns.Add(RowText(rowGo, font, "Turns", HeroOrbPanel.MiddleLeft,
                              0.72f, true, TextAlignmentOptions.Left));
            stats.Add(RowText(rowGo, font, "Stats", HeroOrbPanel.MiddleLeft,
                              HeroOrbPanel.RightEdge, false, TextAlignmentOptions.Left));
            damages.Add(RowText(rowGo, font, "Damage", 0.72f,
                                HeroOrbPanel.RightEdge, true, TextAlignmentOptions.Right));
        }

        var view = screen.gameObject.AddComponent<HeroOrbView>();
        var vso = new SerializedObject(view);
        vso.FindProperty("controller").objectReferenceValue = controller;
        vso.FindProperty("registry").objectReferenceValue = registry;
        vso.FindProperty("panel").objectReferenceValue = rt;
        FillObjects(vso.FindProperty("icons"), icons.ConvertAll(x => (Object)x));
        FillObjects(vso.FindProperty("names"), names.ConvertAll(x => (Object)x));
        FillObjects(vso.FindProperty("golds"), golds.ConvertAll(x => (Object)x));
        FillObjects(vso.FindProperty("turns"), turns.ConvertAll(x => (Object)x));
        FillObjects(vso.FindProperty("stats"), stats.ConvertAll(x => (Object)x));
        FillObjects(vso.FindProperty("damages"), damages.ConvertAll(x => (Object)x));
        vso.ApplyModifiedPropertiesWithoutUndo();

        panelGo.SetActive(false);
    }

    /// <summary>一覧の1行の中の文字ひとつ。上半分／下半分に置き分ける。</summary>
    static TextMeshProUGUI RowText(GameObject row, TMP_FontAsset font, string name,
                                   float left, float right, bool upper,
                                   TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(row.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(left, upper ? 0.5f : 0.05f);
        rt.anchorMax = new Vector2(right, upper ? 0.95f : 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = 22f;
        t.enableAutoSizing = true;
        t.fontSizeMin = 10f;
        t.fontSizeMax = 26f;
        t.color = HeroOrbPanel.Text;
        t.alignment = align;    // TMP の Left / Right は縦は中央
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    // ---------------------------------------------------------------- 縦画面の部品
    // 並べ方は PhoneScreen（作者の指定）。位置は帯の**左上からのずれ**で書く。

    /// <summary>上下の帯。地は原典の枠と同じ石積みを敷き詰める。</summary>
    static RectTransform Band(Transform parent, string name, bool atTop, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, atTop ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, atTop ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, atTop ? 1f : 0f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<UnityEngine.UI.Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PhoneScreen.BrickPath);
        img.type = UnityEngine.UI.Image.Type.Tiled;
        img.raycastTarget = false;
        return rt;
    }

    /// <summary>持ち物の帯の左右の矢印。</summary>
    static RectTransform Arrow(RectTransform bar, TMP_FontAsset font, string name, bool left)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(bar, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(left ? 0f : 1f, 0f);
        rt.anchorMax = new Vector2(left ? 0f : 1f, 1f);
        rt.pivot = new Vector2(left ? 0f : 1f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(PhoneScreen.ArrowWidth, 0f);
        go.GetComponent<UnityEngine.UI.Image>().color = new Color(1f, 1f, 1f, 0.08f);
        var t = CellText(rt, font, 0f, TextAlignmentOptions.Center);
        t.text = left ? "←" : "→";   // PixelMplus に ◀▶ は無い
        t.fontSizeMax = 40f;
        return rt;
    }

    /// <summary>帯の中の枠ひとつ。左上からのずれで置く。</summary>
    static RectTransform Cell(Transform parent, string name, float x, float y,
                              float w, float h, Color fill)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y - PhoneScreen.Pad);
        rt.sizeDelta = new Vector2(w, h - PhoneScreen.Pad * 2f);
        var img = go.GetComponent<UnityEngine.UI.Image>();
        img.color = fill;
        img.raycastTarget = false;
        return rt;
    }

    /// <summary>絵ひとつぶんの小さな枠（持ち物の帯に並べる）。</summary>
    static UnityEngine.UI.Image Slot(Transform parent, string name, float x, float y,
                                     float w, float h)
    {
        var rt = Cell(parent, name, x, y, w, h, new Color(0f, 0f, 0f, 0f));
        var go = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(rt, false);
        var irt = go.GetComponent<RectTransform>();
        irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(4f, 4f); irt.offsetMax = new Vector2(-4f, -4f);
        var img = go.GetComponent<UnityEngine.UI.Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>絵ひとつと数字の枠（金・鍵・生命・攻撃・防御）。返すのは数字の欄。</summary>
    static TextMeshProUGUI Pill(Transform parent, TMP_FontAsset font, string name,
                                float x, float y, float w, float h,
                                string iconPath, Color fill = default(Color))
    {
        var rt = Cell(parent, name, x, y, w, h,
                      fill == default(Color) ? PhoneScreen.BoxFill : fill);
        float left = 8f;
        if (!string.IsNullOrEmpty(iconPath))
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            var go = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(rt, false);
            var irt = go.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(8f, 0f);
            irt.sizeDelta = new Vector2(h - 28f, h - 28f);
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.sprite = sp;
            img.preserveAspect = true;
            img.raycastTarget = false;
            left += h - 20f;
        }
        return CellText(rt, font, left, TextAlignmentOptions.Center);
    }

    /// <summary>装備の枠（絵と名前）。</summary>
    static TextMeshProUGUI Equip(Transform parent, TMP_FontAsset font, string name,
                                 float x, float y, float w, float h,
                                 out UnityEngine.UI.Image icon)
    {
        var rt = Cell(parent, name, x, y, w, h, PhoneScreen.BoxFill);
        var go = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(rt, false);
        var irt = go.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(0f, 0.5f);
        irt.pivot = new Vector2(0f, 0.5f);
        irt.anchoredPosition = new Vector2(10f, 0f);
        irt.sizeDelta = new Vector2(h - 28f, h - 28f);
        icon = go.GetComponent<UnityEngine.UI.Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;
        return CellText(rt, font, 10f + h - 20f, TextAlignmentOptions.Left);
    }

    /// <summary>
    /// **押しても何も起きない**飾りのボタン（作者の指定）。
    /// 下の4つ＋4つと十字、上の段の丸2つがこれ。移動は当面キーボードの矢印のまま。
    /// </summary>
    static RectTransform Placeholder(Transform parent, TMP_FontAsset font, string name,
                                     float x, float y, float w, float h,
                                     string label, Color fill)
    {
        // Cell は上下に Pad を取るので、その分を戻して**指定した位置・大きさそのもの**にする
        var rt = Cell(parent, name, x, y - PhoneScreen.Pad, w, h + PhoneScreen.Pad * 2f, fill);
        var t = CellText(rt, font, 0f, TextAlignmentOptions.Center);
        t.text = label;
        t.fontSizeMax = 34f;
        return rt;
    }

    /// <summary>枠の中の文字。左のずれだけ指定する。</summary>
    static TextMeshProUGUI CellText(RectTransform box, TMP_FontAsset font,
                                    float left, TextAlignmentOptions align)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(box, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, 0f);
        rt.offsetMax = new Vector2(-8f, 0f);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = 34f;
        t.enableAutoSizing = true;
        t.fontSizeMin = 12f;
        t.fontSizeMax = 40f;
        t.color = Color.white;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    static void FillObjects(SerializedProperty prop, List<Object> values)
    {
        prop.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

}
