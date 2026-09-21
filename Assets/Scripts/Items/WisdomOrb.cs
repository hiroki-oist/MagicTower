using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 知恵のオーブ。**持っているアイテムの能力**と、**それまでに聞いた情報の読み返し**。
/// 原典の説明（セリフ61）:
///   「所有しているアイテムの持つ能力を知る事ができる。
///     他にそれまでに聞いた情報を記憶しており、必要な時に聞き出す事ができる。」
///
/// 読み返しの表は原典 MTE.EXE 0x8a46 が抱えている **(フラグ, セリフ番号) 24件**で、
/// データは Resources/WisdomInfo.txt（`docs/make_wisdom_info.py` が取り出す）。
/// 原典はフラグが立っているものだけを「情報番号：n」付きで並べる。
/// </summary>
public class WisdomOrb
{
    public struct Info
    {
        /// <summary>原典が出す「情報番号」。表の並び順（1から）。</summary>
        public int Number;
        /// <summary>この情報を聞いたかどうかの一度きりフラグ。</summary>
        public int Flag;
        /// <summary>読み返すセリフ。</summary>
        public int Record;
    }

    readonly List<Info> all = new List<Info>();

    static WisdomOrb _instance;
    public static WisdomOrb Instance
    {
        get { if (_instance == null) _instance = LoadDefault(); return _instance; }
    }

    static WisdomOrb LoadDefault()
    {
        var o = new WisdomOrb();
        var asset = Resources.Load<TextAsset>("WisdomInfo");
        if (asset == null) Debug.LogError("WisdomOrb: Resources/WisdomInfo.txt が見つかりません");
        else o.Parse(asset.text);
        return o;
    }

    void Parse(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line.Substring(0, hash);
            var p = line.Split('|');
            if (p.Length < 3) continue;
            int n, f, r;
            if (!int.TryParse(p[0].Trim(), out n)) continue;
            if (!int.TryParse(p[1].Trim(), out f)) continue;
            if (!int.TryParse(p[2].Trim(), out r)) continue;
            all.Add(new Info { Number = n, Flag = f, Record = r });
        }
    }

    public int Count { get { return all.Count; } }
    public IEnumerable<Info> All { get { return all; } }

    /// <summary>その人が聞いた情報だけ、原典と同じ並びで返す。</summary>
    public List<Info> Heard(PlayerState p)
    {
        var list = new List<Info>();
        if (p == null) return list;
        foreach (var i in all) if (p.EventFlags.Contains(i.Flag)) list.Add(i);
        return list;
    }

    /// <summary>情報番号から引く。聞いていなければ -1。</summary>
    public int RecordOf(PlayerState p, int number)
    {
        foreach (var i in Heard(p)) if (i.Number == number) return i.Record;
        return -1;
    }

    /// <summary>
    /// 窓に出す文字。原典と同じく「持っているアイテムの能力」のあとに
    /// 「情報番号：」の並びが来る。
    /// </summary>
    public static string Text(PlayerState p)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var s in TowerMenu.ItemKnowledge(p))
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(s);
        }
        var heard = Instance.Heard(p);
        foreach (var i in heard)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(GameText.Instance.Message("orb_info_no")).Append(i.Number);
        }
        return sb.ToString();
    }
}
