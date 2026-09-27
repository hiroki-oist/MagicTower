using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スペシャル・アイテムを使ったときの効果。
/// 原典 MTE.EXE の「どのアイテムを使いますか？」の処理（0x7d50〜0x8a46）より。
///
/// 原典のメニューの並びもこの順:
///   エリクサー / マトック / 破壊の玉 / 空間転移の秘宝 / 昇華の翼 / 降臨の翼 /
///   魔法の鍵 / スーパーマトック / やっぱりやめる
///
/// 使うと所持フラグが 0 に戻るものは**使い切り**（下の <see cref="IsConsumedOnUse"/>）。
/// </summary>
public static class SpecialItemEffects
{
    /// <summary>地図のマスの値。原典の物体IDと同じ。</summary>
    public const int CellFloor = 6;
    public const int CellWall  = 8;
    public const int CellLockYellow = 1;

    /// <summary>破壊の玉が効かない相手（原典が明示的に除外しているID）。</summary>
    public static readonly int[] DestructionImmuneIds = { 91, 104, 122, 145 };
    // 91 魔導師ゼノ / 104 大ダコ / 122 ドラゴン / 145 大魔導師

    /// <summary>空間転移の秘宝を使うと支払う生命力（原典 0x320）。</summary>
    public const int TeleportHpCost = 800;

    /// <summary>
    /// 空間転移の秘宝の飛び先。原典 0x8722〜0x873b は
    /// <c>X' = 10 - X ／ Y' = 10 - Y</c> を書き込む＝**その階の中心について点対称**。
    /// 盤面の座標では (x,y) → (-x,-y)。
    /// </summary>
    public static Vector2Int TeleportTarget(Vector2Int at)
    {
        return new Vector2Int(-at.x, -at.y);
    }

    /// <summary>
    /// **「使う」道具か**（原典のメニューの special_1〜8 に並ぶもの）。
    ///
    /// ⚠ ここを <see cref="IsConsumedOnUse"/> で代用してはいけない。
    /// **空間転移の秘宝は使っても無くならない**（原典 0x86f7 は生命力800を払うだけで
    /// 所持フラグを落とさない）ので、「無くなるか」で判ると持ち物の帯から
    /// 一生使えないことになる（作者の報告「タップしても効果音が鳴るだけ」）。
    ///
    /// 雪の結晶はここに入らない。原典はマグマに踏み込んだときに使うもので、
    /// メニューの一覧（special_1〜8）にも無い。
    /// </summary>
    public static bool IsUsable(SpecialItems.Kind k)
    {
        switch (k)
        {
            case SpecialItems.Kind.Elixir:         // special_1
            case SpecialItems.Kind.Matock:         // special_2
            case SpecialItems.Kind.DestructionOrb: // special_3
            case SpecialItems.Kind.TeleportRelic:  // special_4
            case SpecialItems.Kind.WingUp:         // special_5
            case SpecialItems.Kind.WingDown:       // special_6
            case SpecialItems.Kind.MagicKey:       // special_7
            case SpecialItems.Kind.SuperMatock:    // special_8
                return true;
            // ★**雪の結晶も「使う道具」にする**（作者の指定。原典に無い）。
            //   原典 0x1fd5 は**持っているだけ**で、マグマの手前に立った瞬間に溶ける。
            //   作者の指定で「目の前で使ったときに溶ける」形へ変えた。
            //   理由は <see cref="AuthorStyle.SnowCrystalIsUsed"/>。
            case SpecialItems.Kind.SnowCrystal:
                return AuthorStyle.SnowCrystalIsUsed;
            default:
                return false;
        }
    }

    /// <summary>使うと無くなるアイテムか。</summary>
    public static bool IsConsumedOnUse(SpecialItems.Kind k)
    {
        switch (k)
        {
            case SpecialItems.Kind.Elixir:
            case SpecialItems.Kind.Matock:
            case SpecialItems.Kind.SuperMatock:
            case SpecialItems.Kind.DestructionOrb:
            case SpecialItems.Kind.WingUp:
            case SpecialItems.Kind.WingDown:
            case SpecialItems.Kind.MagicKey:
            case SpecialItems.Kind.SnowCrystal:
                return true;
            default:
                return false;   // オーブ3種・十字架・幸運の金貨・ドラゴンスレイヤーは持ち続ける
        }
    }

    /// <summary>
    /// 使ったときの知らせの文言の鍵（Resources/Messages.txt）。
    /// メニューから使うものは <see cref="TowerGame.UseItem"/> が直に書いているが、
    /// 立ち入って使うもの（13階の雪の結晶）はここを通す。
    /// </summary>
    public static string UseMessageKey(SpecialItems.Kind k)
    {
        switch (k)
        {
            case SpecialItems.Kind.Elixir:         return "elixir_use";
            case SpecialItems.Kind.Matock:         return "matock_use";
            case SpecialItems.Kind.SuperMatock:    return "super_matock_use";
            case SpecialItems.Kind.DestructionOrb: return "destruction_use";
            case SpecialItems.Kind.TeleportRelic:  return "teleport_use";
            case SpecialItems.Kind.WingUp:         return "wing_up_use";
            case SpecialItems.Kind.WingDown:       return "wing_down_use";
            case SpecialItems.Kind.MagicKey:       return "magic_key_use";
            case SpecialItems.Kind.SnowCrystal:    return "snow_use";
            default:                               return "";
        }
    }

    /// <summary>
    /// エリクサーの回復量。原典は
    /// <c>ax = 攻撃力×2 + 防御力 ; 回復量 = ax×5</c>。
    /// 原典の説明「その時の攻撃力と防御力に応じた値だけ生命力が回復する。
    /// じゃから、当然、後の方で使う方が効果は大きいぞ。」に対応する。
    /// </summary>
    public static int ElixirHeal(int attack, int defense)
    {
        return 5 * (attack * 2 + defense);
    }

    /// <summary>マトック: 上下左右のマスが壁なら床にする。</summary>
    public static List<Vector2Int> MatockTargets(Vector2Int at, Func<Vector2Int, int> cellAt)
    {
        var list = new List<Vector2Int>();
        if (cellAt == null) return list;
        foreach (var d in Around)
        {
            var p = at + d;
            if (cellAt(p) == CellWall) list.Add(p);
        }
        return list;
    }

    /// <summary>
    /// スーパーマトック: **その階の壁をすべて**床にする。
    /// 原典は 11×11 を二重ループで走査して 8 のマスを 6 にしている。
    /// </summary>
    public static List<Vector2Int> SuperMatockTargets(Func<Vector2Int, int> cellAt)
    {
        return ScanFloor(cellAt, v => v == CellWall);
    }

    /// <summary>
    /// 魔法の鍵: **その階の黄色い扉をすべて**開ける。
    /// 原典は 11×11 を走査して 1 のマスを 6 にしている。
    /// </summary>
    public static List<Vector2Int> MagicKeyTargets(Func<Vector2Int, int> cellAt)
    {
        return ScanFloor(cellAt, v => v == CellLockYellow);
    }

    /// <summary>
    /// 破壊の玉: 上下左右の敵を消す。
    /// ただし <see cref="DestructionImmuneIds"/> の4体は消せない。
    /// </summary>
    public static List<Vector2Int> DestructionTargets(Vector2Int at,
                                                      Func<Vector2Int, int> cellAt,
                                                      Func<int, bool> isEnemy)
    {
        var list = new List<Vector2Int>();
        if (cellAt == null || isEnemy == null) return list;
        foreach (var d in Around)
        {
            var p = at + d;
            int id = cellAt(p);
            if (!isEnemy(id)) continue;
            if (Array.IndexOf(DestructionImmuneIds, id) >= 0) continue;
            list.Add(p);
        }
        return list;
    }

    /// <summary>
    /// 翼で移動したあとの階。
    /// **階段と違い「44階を飛ばす」補正を通らない**ので、
    /// これが幻の階 F44 へ行く唯一の方法になる
    /// （原典ヒント「我々のいる４４階に行くには、２種類のスペシャルアイテムのうち、
    ///   どちらかを使うしかない」）。
    /// </summary>
    public static int WingFloor(int floor, bool up)
    {
        return up ? floor + 1 : floor - 1;
    }

    static readonly Vector2Int[] Around =
    {
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(-1, 0), new Vector2Int(1, 0)
    };

    static List<Vector2Int> ScanFloor(Func<Vector2Int, int> cellAt, Func<int, bool> match)
    {
        var list = new List<Vector2Int>();
        if (cellAt == null) return list;
        for (int y = 5; y >= -5; y--)
            for (int x = -5; x <= 5; x++)
            {
                var p = new Vector2Int(x, y);
                if (match(cellAt(p))) list.Add(p);
            }
        return list;
    }
}
