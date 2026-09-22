using NUnit.Framework;

/// <summary>
/// **鳴らす所の無い音が無いこと**。原典の音は21の合図に仕分けてあるが、
/// 鳴らす側が繋がっていないと、その音は一度も出ない
/// （題名の曲・門が開く音・遠ざかる音などが実際そうなっていた）。
/// </summary>
public class SoundCoverageTests
{
    /// <summary>鳴らす所が無くてよいもの。理由つきで並べる。</summary>
    static readonly string[] MayBeSilent =
    {
        // 原典は起動時にテンポを130へ戻すだけで、音そのものは出ない
        "起動時のテンポ",
    };

    [Test]
    public void 合図はどれも鳴らす所がある()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerGame.cs")
                + System.IO.File.ReadAllText("Assets/Scripts/TowerController.cs")
                + System.IO.File.ReadAllText("Assets/Scripts/UI/FullScreenView.cs")
                + System.IO.File.ReadAllText("Assets/Scripts/UI/BattleView.cs");
        var consts = System.IO.File.ReadAllText("Assets/Scripts/Items/SoundCues.cs");
        var fromTable = System.IO.File.ReadAllText("Assets/Resources/PositionEvents.txt");
        var missing = new System.Collections.Generic.List<string>();

        foreach (var name in SoundCues.Instance.Names)
        {
            if (System.Array.IndexOf(MayBeSilent, name) >= 0) continue;
            // 表（PositionEvents.txt）から名前で鳴らすものは定数を持たない
            if (fromTable.Contains("音 " + name)) continue;
            // 合図の名前 → 定数名
            var m = System.Text.RegularExpressions.Regex.Match(
                consts, @"public const string (\w+)\s*=\s*""" + name + @"""");
            Assert.IsTrue(m.Success, $"『{name}』の定数が無い");
            var key = "SoundCues." + m.Groups[1].Value;
            if (!src.Contains(key)) missing.Add($"{name}（{key}）");
        }
        CollectionAssert.IsEmpty(missing, "鳴らす所が無い: " + string.Join(" / ", missing.ToArray()));
    }

    [Test]
    public void 番号は原典の表どおり()
    {
        Assert.AreEqual(11, SoundCues.Instance.Number(SoundCues.OpenGate), "門が開く（0x7a68）");
        Assert.AreEqual(11, SoundCues.Instance.Number(SoundCues.WallAppear), "壁が現れる（0x226b）");
        Assert.AreEqual(13, SoundCues.Instance.Number(SoundCues.Receding), "遠ざかる（0xcd20）");
        Assert.AreEqual(2, SoundCues.Instance.Number(SoundCues.TitleMusic), "題名の曲（0xbf5b）");
        Assert.AreEqual(6, SoundCues.Instance.Number(SoundCues.SceneBeat2), "場面のできごと（0x3d1c）");
    }
}

/// <summary>
/// 書き出してあるのに誰も使っていない絵が無いか。
/// 主人公の2コマ目（PLAYER2.P58）が実際そうなっていて、**歩いても絵が変わらなかった**。
/// </summary>
public class SpriteCoverageTests
{
    [Test]
    public void 主人公は二コマ持っていて左右に歩くと入れ替わる()
    {
        Assert.IsTrue(System.IO.File.Exists("Assets/Sprites/Effects/Player_0.png"));
        Assert.IsTrue(System.IO.File.Exists("Assets/Sprites/Effects/Player_1.png"));
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerController.cs");
        StringAssert.Contains("playerSprite2", src);
        // 原典 0xa276 は向き1/2（左右）だけ 0x100 → 0x120 と行を変える
        StringAssert.Contains("sideways", src);
        StringAssert.Contains("from.y == to.y && from.x != to.x", src);
    }

    [Test]
    public void シーンに二コマとも繋がっている()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            var c = UnityEngine.Object.FindObjectOfType<TowerController>();
            Assert.IsNotNull(c);
            var so = new UnityEditor.SerializedObject(c);
            foreach (var n in new[] { "playerSprite", "playerSprite2" })
                Assert.IsNotNull(so.FindProperty(n).objectReferenceValue, $"{n} が未設定");
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }
}

/// <summary>
/// 選んでいる行に置く印。原典は **PICTURE.P58 の4枚目**（16x16）を
/// `0x9e78` が変位 -0x337 から貼る。鍵3枚（-0x40c / -0x3cb / -0x381）の次に並んでいて、
/// 間隔（65 / 74バイト）も合う。
/// </summary>
public class CursorSpriteTests
{
    [Test]
    public void 印の絵が書き出してある()
    {
        Assert.IsTrue(System.IO.File.Exists("Assets/Sprites/Hud/Cursor_0.png"),
                      "PICTURE #3（印）");
        Assert.IsTrue(System.IO.File.Exists("Assets/Sprites/Hud/Cursor_1.png"),
                      "PICTURE #4（印の2コマ目）");
        var t = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(
            "Assets/Sprites/Hud/Cursor_0.png");
        Assert.IsNotNull(t);
        Assert.AreEqual(16, t.width);
        Assert.AreEqual(16, t.height);
    }

    [Test]
    public void 変位の並びが鍵の次になっている()
    {
        // -0x40c(黄) → +65 → -0x3cb(赤) → +74 → -0x381(紫) → +74 → -0x337(印)
        Assert.AreEqual(-0x3cb, -0x40c + 65);
        Assert.AreEqual(-0x381, -0x3cb + 74);
        Assert.AreEqual(-0x337, -0x381 + 74);
    }

    [Test]
    public void メニューの窓に印の絵が繋がっている()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            var v = UnityEngine.Object.FindObjectOfType<MenuWindowView>(true);
            Assert.IsNotNull(v);
            var so = new UnityEditor.SerializedObject(v);
            var cur = so.FindProperty("cursor").objectReferenceValue;
            Assert.IsNotNull(cur, "印の絵が未設定");
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }
}

/// <summary>セリフの窓の印も、メニューと同じ原典の絵を使う。</summary>
public class DialogueCursorTests
{
    [Test]
    public void セリフの窓にも印の絵が繋がっている()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            var v = UnityEngine.Object.FindObjectOfType<DialogueWindowView>(true);
            Assert.IsNotNull(v);
            var so = new UnityEditor.SerializedObject(v);
            var cur = so.FindProperty("cursor").objectReferenceValue as UnityEngine.UI.Image;
            Assert.IsNotNull(cur, "印の絵が未設定");
            Assert.IsNotNull(cur.sprite, "絵が入っていない");
            StringAssert.Contains("Cursor", cur.sprite.name, "メニューと同じ絵");
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void 選んでいる行を本文から数える()
    {
        // 選ばせている行（「◎」付き、または全角空白2つで字下げ）を数えて、
        // ChoiceIndex 番目に印を置く
        var src = System.IO.File.ReadAllText("Assets/Scripts/UI/DialogueWindowView.cs");
        StringAssert.Contains("static int ChoiceLine(", src);
        StringAssert.Contains("IsChoiceLine(", src);
        Assert.IsTrue(DialogueWindowView.IsChoiceLine("　　◎はい"));
        Assert.IsTrue(DialogueWindowView.IsChoiceLine("　　生命力を１００ポイント回復"));
        Assert.IsFalse(DialogueWindowView.IsChoiceLine("「どの望みをかなえて欲しいのだ？」"));
        // 行送りは見せ方によって違う（作者の見せ方は26、原典は20）
        StringAssert.Contains("AuthorStyle.DialogueLineAdvance", src);
        StringAssert.Contains("ScreenLayout.LineAdvance", src);
    }
}

/// <summary>
/// 印は2枚で切り替わる。原典 `0x9e78` の数え（0〜0x14 で PICTURE #3、
/// 0x15〜0x28 で #4、そのあと0へ戻る）。
/// </summary>
public class ChoiceCursorTests
{
    [Test]
    public void 変位が五枚ぴったり並ぶ()
    {
        // 実際の大きさ 65 / 74 / 74 / 111 / 112 バイトを足すと原典の変位に一致する
        int[] size = { 65, 74, 74, 111, 112 };
        int at = -0x40c;
        var want = new[] { -0x40c, -0x3cb, -0x381, -0x337, -0x2c8 };
        for (int i = 0; i < size.Length; i++)
        {
            Assert.AreEqual(want[i], at, $"PICTURE #{i} の変位");
            at += size[i];
        }
    }

    [Test]
    public void 二枚が順に出る()
    {
        Assert.AreEqual(0x15, ChoiceCursor.StepsPerFrame);
        Assert.AreEqual(11f / 60f, ChoiceCursor.StepSeconds, 1e-6f);
        Assert.AreEqual(0, ChoiceCursor.FrameAt(0f));
        Assert.AreEqual(0, ChoiceCursor.FrameAt(ChoiceCursor.FrameSeconds * 0.9f));
        Assert.AreEqual(1, ChoiceCursor.FrameAt(ChoiceCursor.FrameSeconds * 1.1f));
        Assert.AreEqual(0, ChoiceCursor.FrameAt(ChoiceCursor.FrameSeconds * 2.1f));
    }

    [Test]
    public void 速さが読み切れていないことを明記してある()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/UI/ChoiceCursor.cs");
        StringAssert.Contains("読み切れていない", src);
    }
}
