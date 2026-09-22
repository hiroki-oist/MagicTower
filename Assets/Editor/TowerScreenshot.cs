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

    [MenuItem("MagicTower/画面の写しを撮る")]
    public static void Capture()
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

        var canvas = Object.FindObjectOfType<Canvas>();
        var cam = Object.FindObjectOfType<Camera>();
        var layout = Object.FindObjectOfType<PhoneLayoutView>();

        // 画布はふだん画面に直に描く（Overlay）ので、そのままでは写せない。
        // カメラ越しに描く形へ一時的に切り替える。
        var mode = canvas.renderMode;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;

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
        Object.DestroyImmediate(hero);
        Debug.Log($"画面の写しを書き出した → {OutPath}  ({Width}x{Height})");
        EditorSceneManager.CloseScene(scene, false);
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
