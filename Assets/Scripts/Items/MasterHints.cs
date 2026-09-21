using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 仙人（Master, ID25）のヒント表。データは Resources/MasterHints.txt。
///
/// 原典（PC98版 MTE.EXE の関数 0x0000）はこう書かれている:
/// <code>
///   int jii[18][3] = { {2,39,93}, {3,40,80}, ... };
///   for (i = 0; i &lt; 50; i++) if (jii[i][0] == 現在階) break;   // ←18件しか無いのに50まで回す
///   if (i == 50) 「WARNING !!! ｼﾞｲｻﾝ ﾊﾞﾝｺﾞｳ ｶﾞﾐﾂｶﾘﾏｾﾝ !!!」
///   if (現在階 == 16 &amp;&amp; X &gt; 8) i++;
///   if (フラグ[jii[i][1] + 0x1e]) 「もう、話は終わりじゃ。」
///   else { セリフ(jii[i][2]); フラグ = 1; ごほうび; }
/// </code>
///
/// 【設計方針・全体に適用】
///   原典の「表引き＋一度きりフラグ」の形をそのまま持つ。
///   ごほうびだけは階を見て分けている原典の書き方に合わせ、ここに直書きする
///   （3件しか無く、表に一般化して持つと かえって原典から離れるため）。
/// </summary>
public class MasterHints
{
    /// <summary>仙人ひとり分。</summary>
    public struct Hint
    {
        public int Floor;
        /// <summary>原典の仙人通し番号（39〜56）。</summary>
        public int MasterId;
        /// <summary>一度きりフラグの番号（= MasterId + 0x1e）。</summary>
        public int FlagId;
        /// <summary>Dialogue.txt のレコード番号。</summary>
        public int Record;
        /// <summary>この行を選ぶのに要る x の下限。無ければ int.MinValue。</summary>
        public int MinX;
    }

    readonly List<Hint> hints = new List<Hint>();
    public IEnumerable<Hint> All { get { return hints; } }
    public int Count { get { return hints.Count; } }

    static MasterHints _instance;
    public static MasterHints Instance
    {
        get
        {
            if (_instance == null) _instance = LoadDefault();
            return _instance;
        }
    }

    static MasterHints LoadDefault()
    {
        var m = new MasterHints();
        var asset = Resources.Load<TextAsset>("MasterHints");
        if (asset == null) Debug.LogError("MasterHints: Resources/MasterHints.txt が見つかりません");
        else m.Parse(asset.text);
        return m;
    }

    public static MasterHints FromText(string text)
    {
        var m = new MasterHints();
        m.Parse(text);
        return m;
    }

    void Parse(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split('|');
            if (p.Length < 5) continue;

            int floor, id, flag, rec;
            if (!int.TryParse(p[0].Trim(), out floor)) continue;
            if (!int.TryParse(p[1].Trim(), out id)) continue;
            if (!int.TryParse(p[2].Trim(), out flag)) continue;
            if (!int.TryParse(p[3].Trim(), out rec)) continue;

            hints.Add(new Hint
            {
                Floor = floor, MasterId = id, FlagId = flag,
                Record = rec, MinX = ParseCond(p[4].Trim())
            });
        }
    }

    /// <summary>"x&gt;=4" のような条件を読む。"-" なら条件なし。</summary>
    static int ParseCond(string s)
    {
        if (s.StartsWith("x>="))
        {
            int v;
            if (int.TryParse(s.Substring(3), out v)) return v;
        }
        return int.MinValue;
    }

    /// <summary>
    /// その階・その場所の仙人を引く。居なければ null（原典の WARNING にあたる）。
    /// x は Floor_N.txt と同じ -5〜+5。
    /// </summary>
    public Hint? For(int floor, int x)
    {
        int found = -1;
        for (int i = 0; i < hints.Count; i++)
            if (hints[i].Floor == floor) { found = i; break; }
        if (found < 0) return null;

        // 原典: 16階だけ2人いるので X で後ろの行に送る
        int next = found + 1;
        if (next < hints.Count && hints[next].Floor == floor
            && hints[next].MinX != int.MinValue && x >= hints[next].MinX)
            found = next;

        return hints[found];
    }
}
