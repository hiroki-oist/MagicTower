using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// **組み立てた画面の写しを1枚 PNG に撮る**道具。
///
/// 遊ばずに（再生せずに）撮るので、盤面は階のデータをその場で敷き、
/// 欄の数字は見本の値を入れている。並び（どこに何がどれだけの大きさで出るか）を
/// 確かめるためのもので、動きの確認には使えない。
///
///   Unity -batchmode -quit -projectPath . -executeMethod TowerScreenshot.Capture
///   （**-nographics は付けない**。描けなくなる）
/// </summary>
public static class TowerScreenshot
{
    public const string OutPath = "/tmp/mt_screen.png";
    /// <summary>
    /// 写しの大きさ。ふだんは 1080x1920 だが、**App Store Connect が受け取る寸法は
    /// 決まっている**（課金の審査用スクリーンショットは 1242x2688 / 1284x2778 など）ので、
    /// 環境変数 MT_SHOT_W / MT_SHOT_H で差し替えられるようにしてある。
    /// </summary>
    static int Width { get { return EnvSize("MT_SHOT_W", 1080); } }
    static int Height { get { return EnvSize("MT_SHOT_H", 1920); } }

    static int EnvSize(string name, int fallback)
    {
        int v;
        return int.TryParse(System.Environment.GetEnvironmentVariable(name), out v) && v > 0
             ? v : fallback;
    }

    /// <summary>十字キーの候補を1つずつ差し替えて撮る（作者に選んでもらうため）。</summary>
    [MenuItem("MagicTower/十字キーの候補を撮る")]
    public static void CaptureDpads()
    {
        foreach (var name in new[] { "A", "B", "C", "D", "E", "F" })
        {
            dpad = name;
            Capture(false);
            System.IO.File.Copy(OutPath, $"/tmp/mt_dpad_{name}.png", true);
        }
        dpad = null;
    }

    static string dpad;

    /// <summary>十字キーの枠に候補の絵を貼る。</summary>
    static void ShowDpad()
    {
        var go = GameObject.Find("HUD/PhoneFrame/BottomPanel/DPad");
        if (go == null) return;
        var img = go.GetComponent<UnityEngine.UI.Image>();
        var text = go.transform.Find("Text");
        if (string.IsNullOrEmpty(dpad))
        {
            if (text != null) text.gameObject.SetActive(true);
            return;
        }
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(
            $"Assets/Sprites/Hud/DPad_{dpad}.png");
        if (img != null && sp != null)
        {
            img.sprite = sp;
            img.color = Color.white;
            img.preserveAspect = true;
        }
        if (text != null) text.gameObject.SetActive(false);
    }

    [MenuItem("MagicTower/画面の写しを撮る（ダメージ表示）")]
    public static void CaptureDamage() { Capture(true); }

    [MenuItem("MagicTower/画面の写しを撮る")]
    public static void Capture() { Capture(false); }

    /// <summary>1枚絵（題名・序章の絵）が帯に隠れていないかを見るための写し。</summary>
    public static void CaptureTitle()    { fullScreen = "TitleScreen";    Capture(false); }
    public static void CapturePrologue() { fullScreen = "PrologueScreen"; Capture(false); }

    static string fullScreen;

    /// <summary>
    /// **課金の窓の写しを撮る**。本線と同じ道（DialogueWindowView.PreviewBoxed）を通すので、
    /// ここで1行に収まっていれば実機でも収まる。
    ///
    ///   Unity -batchmode -quit -projectPath . -executeMethod TowerScreenshot.CaptureShop
    /// </summary>
    public static void CaptureShop() { shop = true; Capture(false); }

    /// <summary>
    /// **メニューの窓の写しを撮る**。本線と同じ道（MenuWindowView.Preview →
    /// Display）を通すので、ここで窓に収まっていれば実機でも収まる。
    ///
    ///   Unity -batchmode -quit -projectPath . -executeMethod TowerScreenshot.CaptureMenu
    /// </summary>
    public static void CaptureMenu() { menu = true; Capture(false); }

    /// <summary>プライバシーポリシーの頁の写し（題名から出るもの）。</summary>
    public static void CapturePolicy() { policy = true; Capture(false); }

    /// <summary>題名そのものの写し（下の「プライバシーポリシー」の字を見る）。</summary>
    public static void CaptureTitleView() { policy = true; policyOnly = true; Capture(false); }

    static bool policy, policyOnly;

    static void ShowPolicy()
    {
        if (!policy) return;
        var title = Object.FindObjectOfType<TitleView>(true);
        if (title == null) { Debug.LogWarning("題名が見つからない"); return; }
        title.gameObject.SetActive(true);
        title.PreviewPolicy(!policyOnly);
        Debug.Log(policyOnly ? "■題名を出した（プライバシーの字つき）"
                             : "■プライバシーの頁を出した");
    }

    static bool shop, menu;

    static void ShowMenu()
    {
        if (!menu) return;
        var title = Object.FindObjectOfType<TitleView>(true);
        if (title != null) title.Hide();
        var view = Object.FindObjectOfType<MenuWindowView>(true);
        if (view == null) { Debug.LogWarning("メニューの窓が見つからない"); return; }
        var g = new TowerGame(new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100,
            Floor = 3, Position = new Vector2Int(0, -3),
        });
        g.OpenMenu();
        view.Preview(g);
        view.transform.SetAsLastSibling();

        var body = view.Body;
        if (body == null) { Debug.LogWarning("本文が繋がっていない"); return; }
        body.ForceMeshUpdate();
        var win = TowerMenu.Window;
        Debug.Log($"■メニュー: 行数 = {body.textInfo.lineCount}"
                + $" / 窓 = {win.width}x{win.height}ドット（{TowerMenu.PhoneWindowLines}行）"
                + $" / 組んだ高さ = {body.preferredHeight:0.0}"
                + $" / 枠の高さ = {body.rectTransform.rect.height:0.0}");
        for (int i = 0; i < body.textInfo.lineCount; i++)
        {
            var li = body.textInfo.lineInfo[i];
            var sb = new System.Text.StringBuilder();
            for (int c = li.firstCharacterIndex; c <= li.lastCharacterIndex; c++)
                sb.Append(body.textInfo.characterInfo[c].character);
            Debug.Log($"■{i}: {sb}");
        }
    }

    static void ShowShop()
    {
        if (!shop) return;
        // 題名が前に出ているので退ける。**題名の部品は名前でも消しておく** —
        // 遊んでいないときは Awake が走っておらず、部品の enabled が
        // シーンに保存されたまま（＝出たまま）のことがある。
        var title = Object.FindObjectOfType<TitleView>(true);
        if (title != null) title.Hide();
        foreach (var t in Object.FindObjectsOfType<TMPro.TextMeshProUGUI>(true))
            if (t.name.StartsWith("Title")) t.enabled = false;
        foreach (var im in Object.FindObjectsOfType<UnityEngine.UI.Image>(true))
            if (im.name.StartsWith("Title")) im.enabled = false;
        var win = Object.FindObjectOfType<DialogueWindowView>(true);
        if (win == null) { Debug.LogWarning("会話の窓が見つからない"); return; }
        // ★**実機と同じ値段の字**で組ませる（米国ストアの表記がいちばん長い）。
        //   環境変数 MT_SHOP_PRICES で差し替えられる（「,」区切りで3つ）。
        var env = System.Environment.GetEnvironmentVariable("MT_SHOP_PRICES");
        var prices = string.IsNullOrEmpty(env)
                   ? new[] { "$0.99", "$1.99", "$5.99" } : env.Split(',');
        AppleStore.PreviewLive = true;
        var sb2 = new System.Text.StringBuilder(
            "{\"ready\":true,\"canPay\":true,\"busy\":false,\"msg\":\"\",\"items\":[");
        for (int i = 0; i < AppleStore.ProductIds.Length; i++)
        {
            if (i > 0) sb2.Append(',');
            sb2.Append("{\"id\":\"").Append(AppleStore.ProductIds[i])
               .Append("\",\"price\":\"")
               .Append(prices[System.Math.Min(i, prices.Length - 1)].Trim())
               .Append("\",\"owned\":false}");
        }
        sb2.Append("]}");
        AppleStore.Parse(sb2.ToString());
        // ★**本線とまったく同じ道**を通す。TowerGame.Say は本文を
        //   DialogueBox.Paginate に通してから窓へ渡すので、ここも同じにする。
        //   （ここを素通りさせていたせいで「写しでは1行、実機では2行」を見逃した）
        var paged = DialogueBox.Paginate(GoldShop.Text());
        // どの品目に印を置くか（審査用スクリーンショットを品目ごとに撮るため）
        int pick = 0;
        int.TryParse(System.Environment.GetEnvironmentVariable("MT_SHOP_PICK"), out pick);
        Debug.Log($"■課金の窓: ページ数 = {paged.Count} / 印 = {pick}");
        win.PreviewBoxed(paged[0], pick);
        win.transform.SetAsLastSibling();
        // 何行で組まれたかを書き出す（1品目が1行に収まっているかの手がかり）
        var body = win.Body;
        if (body == null) { Debug.LogWarning("本文が繋がっていない"); return; }
        body.ForceMeshUpdate();
        if (body.textInfo == null) { Debug.LogWarning("本文がまだ組まれていない"); return; }
        Debug.Log($"■課金の窓: 組まれた行数 = {body.textInfo.lineCount}"
                + $" / 本文の行数 = {paged[0].Split('\n').Length}"
                + $" / 窓の幅 = {AuthorStyle.WidthFor(GoldShop.Text())}ドット");
        for (int i = 0; i < body.textInfo.lineCount; i++)
        {
            var li = body.textInfo.lineInfo[i];
            var sb = new System.Text.StringBuilder();
            for (int c = li.firstCharacterIndex; c <= li.lastCharacterIndex; c++)
                sb.Append(body.textInfo.characterInfo[c].character);
            Debug.Log($"■{i}: {sb}");
        }
    }

    /// <summary>名前の1枚絵と、その後ろの黒地を出す。</summary>
    static void ShowFullScreen()
    {
        if (string.IsNullOrEmpty(fullScreen)) return;
        foreach (var rt in Object.FindObjectsOfType<RectTransform>(true))
        {
            if (rt.name == "Backdrop") rt.gameObject.SetActive(true);
            if (rt.name == fullScreen) rt.gameObject.SetActive(true);
        }
    }

    static void Capture(bool damagePanel)
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Tower.unity");

        // 盤面を敷く（遊んでいないので自分で組む）。見本は3階。
        var loader = Object.FindObjectOfType<FloorLoader>();
        if (loader != null) loader.Build(FloorData.Load(3));

        // 主人公（遊び始めてから作られるものなので、ここでは自分で置く）
        var hero = new GameObject("ShotPlayer", typeof(SpriteRenderer));
        var hsr = hero.GetComponent<SpriteRenderer>();
        hsr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Effects/Player_0.png");
        hsr.sortingOrder = 100;
        hero.transform.position = new Vector3(0f, -3f, -1f);
        // Java版の絵を使うときは、正面の1コマ目を見本の階（3階＝奇数＝原寸）で出す
        if (AuthorStyle.JavaHeroSprites)
        {
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(AuthorStyle.HeroAtlasPath))
            {
                var sp = a as Sprite;
                if (sp == null || sp.name != AuthorStyle.HeroSpriteNames[2]) continue;
                hsr.sprite = sp;
                float k = AuthorStyle.FullSizeHeroOn(3)
                        ? AuthorStyle.HeroScaleFull : AuthorStyle.HeroScaleFit;
                hero.transform.localScale = new Vector3(k, k, 1f);
                hero.transform.position = new Vector3(
                    0f, -3f + (AuthorStyle.FullSizeHeroOn(3)
                               ? AuthorStyle.HeroFullOffsetY : 0f), -1f);
                break;
            }
        }

        FillSample();
        ShowDpad();
        ShowFullScreen();
        ShowShop();
        ShowMenu();
        ShowPolicy();
        if (damagePanel) FillDamagePanel();

        var canvas = Object.FindObjectOfType<Canvas>();
        var cam = Object.FindObjectOfType<Camera>();
        var layout = Object.FindObjectOfType<PhoneLayoutView>();

        // 画布はふだん画面に直に描く（Overlay）ので、そのままでは写せない。
        // カメラ越しに描く形へ一時的に切り替える。
        var mode = canvas.renderMode;
        var order = canvas.sortingOrder;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
        // カメラ越しに描くと、画布は**盤面の絵と同じ並び順**で混ざる（盤面の物は
        // 描画順2〜3なので、そのままだと窓の上に出てしまう）。撮る間だけ前に出す。
        // 遊んでいるときは画布は画面に直に描く（Overlay）ので、この心配は無い。
        canvas.sortingOrder = 1000;

        var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        if (layout != null) layout.Apply();
        Canvas.ForceUpdateCanvases();
        if (layout != null) layout.Apply();
        cam.Render();

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        System.IO.File.WriteAllBytes(OutPath, tex.EncodeToPNG());

        cam.targetTexture = null;
        canvas.renderMode = mode;
        canvas.sortingOrder = order;
        Object.DestroyImmediate(hero);
        Debug.Log($"画面の写しを書き出した → {OutPath}  ({Width}x{Height})");
        EditorSceneManager.CloseScene(scene, false);
    }

    /// <summary>
    /// 勇者のオーブの一覧（ダメージ表示）を出した形にする。
    /// 遊んでいないと <see cref="HeroOrbView"/> が動かないので、中身は自分で入れる。
    /// </summary>
    static void FillDamagePanel()
    {
        var panel = GameObject.Find("HUD/PhoneFrame/HeroOrbPanel");
        if (panel == null) { Debug.LogWarning("HeroOrbPanel が無い"); return; }
        panel.SetActive(true);

        var player = new PlayerState { Hp = 1000, Attack = 100, Defense = 100, Floor = 3 };
        var floor = new FloorState(FloorData.Load(3));
        var rows = HeroOrb.Rows(floor, player);
        var registry = Resources.Load<FloorPrefabRegistry>("FloorPrefabRegistry");

        for (int i = 0; i < HeroOrb.MaxRows; i++)
        {
            var row = panel.transform.Find($"Row{i}");
            if (row == null) continue;
            bool used = i < rows.Count;
            var r = used ? rows[i] : default(HeroOrb.Row);
            Text(row, "Name", used ? r.Name : "");
            Text(row, "Gold", used ? HeroOrbPanel.GoldLine(r.Gold) : "");
            Text(row, "Turns", used && r.CanAttack ? HeroOrbPanel.TurnLine(r.Turns) : "");
            Text(row, "Stats", used ? HeroOrbPanel.StatLine(r.Hp, r.Attack, r.Defense) : "");
            Text(row, "Damage", used ? HeroOrbPanel.DamageLine(r.CanAttack, r.Damage) : "");
            var icon = row.Find("Icon");
            var img = icon != null ? icon.GetComponent<UnityEngine.UI.Image>() : null;
            if (img == null) continue;
            Sprite sp = null;
            if (used && registry != null)
            {
                var prefab = registry.Get(r.Prefab);
                var sr = prefab != null ? prefab.GetComponent<SpriteRenderer>() : null;
                sp = sr != null ? sr.sprite : null;
            }
            img.enabled = sp != null;
            if (sp != null) img.sprite = sp;
        }
    }

    static void Text(Transform row, string name, string s)
    {
        var t = row.Find(name);
        var tmp = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        if (tmp != null) tmp.text = s;
    }

    /// <summary>欄に見本の値を入れる（遊んでいないと空のままなので）。</summary>
    static void FillSample()
    {
        var view = Object.FindObjectOfType<TowerHudView>();
        if (view == null) return;
        var so = new SerializedObject(view);
        var p = new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100, Gold = 29,
            YellowKeys = 2, PurpleKeys = 1, RedKeys = 0,
            SwordRank = 2, ShieldRank = 1, Floor = 3,
        };
        Set(so, "floorLabel", TowerHud.FloorLabel(p.Floor));
        Set(so, "hp", TowerHud.Hp(p));
        Set(so, "attack", TowerHud.Attack(p));
        Set(so, "defense", TowerHud.Defense(p));
        Set(so, "gold", TowerHud.Gold(p));
        Set(so, "weapon", TowerHud.WeaponName(p));
        Set(so, "shield", TowerHud.ShieldName(p));
        Set(so, "yellowKeys", GameText.Num(p.YellowKeys));
        Set(so, "purpleKeys", GameText.Num(p.PurpleKeys));
        Set(so, "redKeys", GameText.Num(p.RedKeys));


        Icon(so, "weaponIcon", "swordIcons", p.SwordRank);
        Icon(so, "shieldIcon", "shieldIcons", p.ShieldRank);

        // 知らせ（画面の中央に出て薄くなって消えるもの）も見本を出しておく
        var toast = GameObject.Find("HUD/PhoneFrame/Toast");
        if (toast != null)
        {
            toast.SetActive(true);
            var t = toast.transform.Find("Text");
            var tmp = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
            if (tmp != null) tmp.text = "黄色の鍵を手に入れた！";
        }

        // 持ち物はいくつか持っている形にしておく
        var slots = so.FindProperty("itemSlots");
        for (int i = 0; i < slots.arraySize; i++)
        {
            var img = slots.GetArrayElementAtIndex(i).objectReferenceValue
                      as UnityEngine.UI.Image;
            if (img != null) img.enabled = i % 3 == 0;
        }
    }

    static void Set(SerializedObject so, string name, string text)
    {
        var t = so.FindProperty(name).objectReferenceValue as TextMeshProUGUI;
        if (t != null) t.text = text;
    }

    static void Icon(SerializedObject so, string name, string list, int rank)
    {
        var img = so.FindProperty(name).objectReferenceValue as UnityEngine.UI.Image;
        var arr = so.FindProperty(list);
        if (img == null || rank < 1 || rank > arr.arraySize) return;
        img.sprite = arr.GetArrayElementAtIndex(rank - 1).objectReferenceValue as Sprite;
        img.enabled = img.sprite != null;
    }
}
