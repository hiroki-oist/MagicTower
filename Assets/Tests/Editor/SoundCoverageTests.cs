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
        var missing = new System.Collections.Generic.List<string>();

        foreach (var name in SoundCues.Instance.Names)
        {
            if (System.Array.IndexOf(MayBeSilent, name) >= 0) continue;
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
