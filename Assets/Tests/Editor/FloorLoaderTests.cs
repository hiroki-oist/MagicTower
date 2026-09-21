using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// FloorLoader が `.floor` データから全50階を実際に組み立てられるかを Unity 上で確かめる。
///
/// これまでは「登録簿に名前が揃っているか」を Python で静的に見ていただけだった。
/// ここでは本当に Instantiate まで通し、プレハブ未登録の警告が出ないことを確認する。
/// </summary>
public class FloorLoaderTests
{
    FloorPrefabRegistry registry;
    GameObject host;
    FloorLoader loader;

    [SetUp]
    public void SetUp()
    {
        registry = Resources.Load<FloorPrefabRegistry>("FloorPrefabRegistry");
        host = new GameObject("FloorLoaderTestHost");
        loader = host.AddComponent<FloorLoader>();
        loader.Configure(registry, host.transform);
    }

    [TearDown]
    public void TearDown()
    {
        if (loader != null) loader.Clear();
        if (host != null) Object.DestroyImmediate(host);
    }

    [Test]
    public void 登録簿が読める()
    {
        Assert.IsNotNull(registry, "Resources/FloorPrefabRegistry.asset が読めない");
    }

    [Test]
    public void 全50階を組み立てられる()
    {
        var problems = new List<string>();
        for (int n = 1; n <= 50; n++)
        {
            var data = FloorData.Load(n);
            if (data == null) { problems.Add($"F{n}: データが読めない"); continue; }

            loader.Build(data);
            int built = loader.Container.childCount;
            // 地形121マスぶんの床は必ず敷かれるので、最低でも121個は生成される
            if (built < 121) problems.Add($"F{n}: 生成数が {built} 個しかない");
            loader.Clear();
        }
        Assert.IsEmpty(problems, string.Join(" / ", problems));
    }

    /// <summary>
    /// 組み立ての警告（プレハブ未登録）を拾う。**警告はテストを落とさない**ので、
    /// 自分で受け取って確かめないと「見えない物が置かれている」のに気づけない。
    /// </summary>
    [Test]
    public void 全50階を組み立てても未登録のプレハブが出ない()
    {
        var warnings = new List<string>();
        Application.LogCallback catcher = (msg, stack, type) =>
        {
            if (type == LogType.Warning && msg.Contains("プレハブ未登録")) warnings.Add(msg);
        };
        Application.logMessageReceived += catcher;
        try
        {
            for (int n = 1; n <= 50; n++)
            {
                var data = FloorData.Load(n);
                if (data == null) continue;
                loader.Build(data);
                loader.Clear();
            }
        }
        finally { Application.logMessageReceived -= catcher; }

        Assert.IsEmpty(warnings, string.Join(" / ", warnings.ToArray()));
    }

    /// <summary>
    /// マス1つの作り直し。盤面の記号（"#" や ">"）は
    /// <see cref="FloorState.ViewKeyOf"/> で見た目の鍵に直してから渡す約束。
    /// </summary>
    [Test]
    public void マスの作り直しは地形の記号でも通る()
    {
        loader.Build(FloorData.Load(1));
        var at = new Vector2Int(0, 0);

        var warnings = new List<string>();
        Application.LogCallback catcher = (msg, stack, type) =>
        {
            if (type == LogType.Warning && msg.Contains("プレハブ未登録")) warnings.Add(msg);
        };
        Application.logMessageReceived += catcher;
        try
        {
            foreach (var cell in new[] { "#", "%", ">", "<", "y", "b", "r", "s", "x", "*", "~" })
                loader.Refresh(at, FloorState.ViewKeyOf(cell));
            loader.Refresh(at, FloorState.ViewKeyOf(FloorState.Floor));   // 床に戻す
        }
        finally { Application.logMessageReceived -= catcher; }

        Assert.IsEmpty(warnings, "地形の記号は全部プレハブに直せる: "
                                 + string.Join(" / ", warnings.ToArray()));
    }

    [Test]
    public void 三マス占有のボスは中心に1体だけ出る()
    {
        // F35 のドラゴンはデータ上9マスを占めるが、生成されるのは1体
        loader.Build(FloorData.Load(35));
        int dragons = 0;
        foreach (Transform c in loader.Container)
            if (c.name.StartsWith("Boss_Dragon")) dragons++;
        Assert.AreEqual(1, dragons, "3x3のボスは中心の1体だけ生成されるはず");
    }
}
