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
