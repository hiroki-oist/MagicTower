using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// **階段で着いたマスが立ち入りのできごとのマスと重なる**組み合わせ。
/// 原典 0x23ee は階段で X,Y を書き換えたあと位置イベント（0x1588）を呼ぶので、
/// 着地しただけでも起きる。こちらで抜けていて、作者から
/// 「32階のイベントが初到達時に発生せず、そのまま戦闘になった」と報告された。
/// </summary>
public class ArriveSweepTests
{
    /// <summary>重なる組み合わせ（階段の階, 上りか, できごとの階, マス）。</summary>
    static List<KeyValuePair<int, PositionEvents.Event>> Overlaps()
    {
        var list = new List<KeyValuePair<int, PositionEvents.Event>>();
        foreach (var ev in PositionEvents.Instance.All)
            foreach (var spot in ev.Spots)
                for (int f = 0; f <= 50; f++)
                {
                    var up = FloorTransition.GoUp(f);
                    if (up.Ok && up.Floor == ev.Floor && up.Arrive == spot)
                        list.Add(new KeyValuePair<int, PositionEvents.Event>(f, ev));
                }
        return list;
    }

    [Test]
    public void 重なるのは三十二階と四十二階の金騎士だけ()
    {
        var o = Overlaps();
        Assert.AreEqual(2, o.Count, "31→32 と 41→42 の2件");
        Assert.AreEqual(31, o[0].Key);
        Assert.AreEqual("金騎士敗走", o[0].Value.Scene);
        Assert.AreEqual(41, o[1].Key);
        Assert.AreEqual("金騎士処刑とゼノ登場", o[1].Value.Scene);
    }

    [Test]
    public void 階段で着いた拍子にその場面が始まる()
    {
        foreach (var pair in Overlaps())
        {
            var p = new PlayerState
            {
                Hp = 99990, Attack = 9990, Defense = 9990,
                Floor = pair.Key, Position = Vector2Int.zero
            };
            var g = new TowerGame(p);
            var up = FloorData.Load(pair.Key).Find(FloorData.Terrain.StairsUp);
            Vector2Int stairs = Vector2Int.zero;
            foreach (var s in up) stairs = s;
            // 階段の隣に立ってから踏み込む（原典も踏み込んだ先が階段なら昇る）
            g.EnterFloor(pair.Key, stairs + Vector2Int.down);
            g.Step(Vector2Int.up);
            Assert.AreEqual(pair.Value.Floor, g.Player.Floor, $"{pair.Key}階から上がる");
            Assert.AreEqual(pair.Value.Scene, g.PendingScene,
                            $"{pair.Value.Floor}階に着いた拍子に始まる");
        }
    }
}
