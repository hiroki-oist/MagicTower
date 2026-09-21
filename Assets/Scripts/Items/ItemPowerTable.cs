using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回復薬・クリスタルの効果量。原典では階によって段階が変わる。
/// データは Resources/ItemPower.txt（根拠はそのファイルのコメントに記載）。
/// </summary>
public class ItemPowerTable
{
    public struct Band
    {
        public int MinFloor, MaxFloor;
        public int PotionRed, PotionBlue, CrystalAttack, CrystalDefense;
    }

    readonly List<Band> bands = new List<Band>();

    static ItemPowerTable _instance;
    public static ItemPowerTable Instance
    {
        get
        {
            if (_instance == null) _instance = LoadDefault();
            return _instance;
        }
    }

    static ItemPowerTable LoadDefault()
    {
        var asset = Resources.Load<TextAsset>("ItemPower");
        if (asset == null)
        {
            Debug.LogError("ItemPowerTable: Resources/ItemPower.txt が見つかりません");
            return new ItemPowerTable();
        }
        return Parse(asset.text);
    }

    public static ItemPowerTable Parse(string text)
    {
        var t = new ItemPowerTable();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split('|');
            if (p.Length < 5) continue;
            var range = p[0].Trim().Split('-');
            t.bands.Add(new Band
            {
                MinFloor = int.Parse(range[0]),
                MaxFloor = int.Parse(range[1]),
                PotionRed = int.Parse(p[1].Trim()),
                PotionBlue = int.Parse(p[2].Trim()),
                CrystalAttack = int.Parse(p[3].Trim()),
                CrystalDefense = int.Parse(p[4].Trim()),
            });
        }
        return t;
    }

    public Band ForFloor(int floor)
    {
        foreach (var b in bands)
            if (floor >= b.MinFloor && floor <= b.MaxFloor) return b;
        Debug.LogWarning($"ItemPowerTable: {floor}階の段階が定義されていません");
        return bands.Count > 0 ? bands[0] : new Band();
    }
}
