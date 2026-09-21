using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 盤面の状態と規則が実際につながっているかを Unity 上で確かめる。
///
/// 原典の一手は 移動 → 隣接マスの魔法攻撃 → 解錠判定 の順に進む。
/// ここではその流れを GameRules 越しに動かす。
/// </summary>
public class GameRulesTests
{
    GameObject host;
    GameRules rules;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("GameRulesTestHost");
        rules = host.AddComponent<GameRules>();
    }

    [TearDown]
    public void TearDown()
    {
        if (host != null) Object.DestroyImmediate(host);
    }

    [Test]
    public void 階に入ると盤面ができる()
    {
        rules.EnterFloor(FloorData.Load(17));
        Assert.IsNotNull(rules.Floor);
        Assert.AreEqual(17, rules.Floor.FloorNumber);
        Assert.AreEqual("s", rules.Floor.At(new Vector2Int(-4, 2)), "F17 の左の門はまだ閉じている");
    }

    [Test]
    public void 守護者を倒すと門が開く()
    {
        rules.EnterFloor(FloorData.Load(17));
        var gate = new Vector2Int(-4, 2);
        var guardA = new Vector2Int(-5, 1);
        var guardB = new Vector2Int(-3, 1);

        Assert.AreEqual("s", rules.Floor.At(gate));

        rules.OnEnemyDefeated(guardA);
        Assert.AreEqual("s", rules.Floor.At(gate), "片方だけでは開かない");

        rules.OnEnemyDefeated(guardB);
        Assert.AreEqual(FloorState.Floor, rules.Floor.At(gate), "両方倒すと門が消える");

        Assert.AreEqual("s", rules.Floor.At(new Vector2Int(4, 2)), "別の門は閉じたまま");
    }

    [Test]
    public void 解錠が起きたら知らせが飛ぶ()
    {
        int fired = 0;
        rules.OnUnlocked += _ => fired++;
        rules.EnterFloor(FloorData.Load(17));
        rules.OnEnemyDefeated(new Vector2Int(-5, 1));
        rules.OnEnemyDefeated(new Vector2Int(-3, 1));
        Assert.AreEqual(1, fired, "解錠は1回だけ通知される");
    }

    [Test]
    public void 世界座標を盤面のマスに直せる()
    {
        Assert.AreEqual(new Vector2Int(0, 0),  GameRules.ToCell(new Vector3(0.02f, -0.01f, 0f)));
        Assert.AreEqual(new Vector2Int(-4, 2), GameRules.ToCell(new Vector3(-4f, 2f, 0f)));
    }

    [Test]
    public void 魔導師に挟まれると生命力が半分になる()
    {
        // F49 は魔導師(ID93)が並ぶ階。ここでは盤面を直接作って確かめる
        rules.EnterFloor(FloorData.Load(49));
        var at = new Vector2Int(0, 0);
        rules.Floor.Put(at + new Vector2Int(-1, 0), "Enemy_ID93");
        rules.Floor.Put(at + new Vector2Int(1, 0),  "Enemy_ID93");

        int after = rules.OnPlayerMoved(at, 1000, 0);
        Assert.AreEqual(500, after, "挟まれると半分");

        int guarded = rules.OnPlayerMoved(at, 1000, 5);
        Assert.AreEqual(1000, guarded, "神盾(5)なら無効");
    }
}
