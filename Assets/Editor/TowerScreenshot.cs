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
    const int Width = 1080, Height = 1920;

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

        FillSample();
        ShowDpad();
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
        Set(so, "messageBox", "黄色の扉が開いた");

        Icon(so, "weaponIcon", "swordIcons", p.SwordRank);
        Icon(so, "shieldIcon", "shieldIcons", p.ShieldRank);

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
