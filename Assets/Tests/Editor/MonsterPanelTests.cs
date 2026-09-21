using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 戦闘中に右下の枠に出る相手の欄（原典 0x9626）。
/// </summary>
public class MonsterPanelTests
{
    [Test]
    public void 欄の位置が原典どおり()
    {
        // 絵の貼り先 0x48c6 = 行232・バイト列70 → (560,232)
        Assert.AreEqual(new RectInt(560, 232, 32, 32), ScreenLayout.MonsterIcon);
        // 名前 x=0x42(528)・y=0x110(272)
        Assert.AreEqual(528, ScreenLayout.MonsterName.xMin);
        Assert.AreEqual(272, ScreenLayout.MonsterName.yMin);
        // 見出し x=0x43(536)、y=0x128/0x138/0x148
        Assert.AreEqual(new RectInt(536, 296, 32, 16), ScreenLayout.MonsterHpLabel);
        Assert.AreEqual(new RectInt(536, 312, 32, 16), ScreenLayout.MonsterAttackLabel);
        Assert.AreEqual(new RectInt(536, 328, 32, 16), ScreenLayout.MonsterDefenseLabel);
        // 数値 x=0x47(568)・幅3 → 右端 616
        Assert.AreEqual(616, ScreenLayout.MonsterAttackBox.xMax);
        Assert.AreEqual(616, ScreenLayout.MonsterDefenseBox.xMax);
        Assert.AreEqual(48, ScreenLayout.MonsterAttackBox.width);
    }

    [Test]
    public void 欄はぜんぶメッセージの枠の中にある()
    {
        foreach (var r in new[] { ScreenLayout.MonsterIcon, ScreenLayout.MonsterName,
                                  ScreenLayout.MonsterHpLabel, ScreenLayout.MonsterHpBox,
                                  ScreenLayout.MonsterDefenseBox })
        {
            Assert.GreaterOrEqual(r.xMin, ScreenLayout.MessageBox.xMin, $"{r} の左");
            Assert.LessOrEqual(r.xMax, ScreenLayout.MessageBox.xMax, $"{r} の右");
            Assert.GreaterOrEqual(r.yMin, ScreenLayout.MessageBox.yMin, $"{r} の上");
            Assert.LessOrEqual(r.yMax, ScreenLayout.MessageBox.yMax, $"{r} の下");
        }
    }

    [Test]
    public void 命は一撃ごとに減って零で止まる()
    {
        var info = new TowerHud.MonsterInfo { MaxHp = 50, DamagePerHit = 20 };
        Assert.AreEqual(50, TowerHud.MonsterHpAfter(info, 0));
        Assert.AreEqual(30, TowerHud.MonsterHpAfter(info, 1));
        Assert.AreEqual(10, TowerHud.MonsterHpAfter(info, 2));
        Assert.AreEqual(0,  TowerHud.MonsterHpAfter(info, 3), "下は0で止まる");
    }

    [Test]
    public void 倒せない相手は命が減らない()
    {
        var info = new TowerHud.MonsterInfo { MaxHp = 50, DamagePerHit = 0 };
        Assert.AreEqual(50, TowerHud.MonsterHpAfter(info, 5));
    }

    [Test]
    public void 相手の能力値は原典の表から取れる()
    {
        var p = new PlayerState { Attack = 100, Defense = 100 };
        var info = TowerHud.Inspect(p, "Enemy_SlimeG");
        Assert.IsTrue(info.Valid);
        Assert.Greater(info.MaxHp, 0);
        Assert.Greater(info.DamagePerHit, 0, "攻撃100なら緑スライムは倒せる");
    }

    [Test]
    public void シーンに相手の欄があって最初は隠してある()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Tower.unity");
        // 縦画面（作者の指定）では、原典の座標の入れ物は枠 PhoneFrame の下にある
        var panel = GameObject.Find("HUD/PhoneFrame/Screen/MonsterPanel");
        Assert.IsNotNull(panel, "MonsterPanel が無い");
        Assert.IsFalse(panel.activeSelf, "戦闘していない間は隠す");
        foreach (var n in new[] { "MonsterIcon", "MonsterName", "HpLabel",
                                  "AttackLabel", "DefenseLabel",
                                  "MonsterHp", "MonsterAttack", "MonsterDefense" })
            Assert.IsNotNull(panel.transform.Find(n), $"{n} が無い");
    }
}
