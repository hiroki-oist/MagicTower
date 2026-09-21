#!/usr/bin/env python3
"""門の解錠を、実装と同じ手順で最初から最後まで動かして確かめる。

FloorState / SealedDoors の C# 実装と同じことを Python で行う:
  盤面を .floor から作る → 守護マスを床にする（＝敵を倒す）→ 解錠判定 → 結果を盤面に適用。
Unity を立ち上げずに、データと規則がつながっていることを確認できる。
"""
import re, sys

ROOT = "/Users/hirokisawada/Code/MagicTower"

def load_floor(fl):
    t = open(f"{ROOT}/Assets/Resources/Floors/Floor_{fl}.txt", encoding="utf-8").read()
    g = {}
    for l in t.splitlines():
        m = re.match(r"\s*([+-]\d+) \| (.{11})$", l)
        if m:
            y = int(m.group(1))
            for c, ch in enumerate(m.group(2)): g[(c - 5, y)] = ch
        m = re.match(r"\s*([+-]\d+) ([+-]\d+)\s+(\S+)", l)
        if m: g[(int(m.group(1)), int(m.group(2)))] = m.group(3)
    return g

def unlocks(fl):
    out = []
    for l in open(f"{ROOT}/Assets/Resources/SealedDoors.txt", encoding="utf-8"):
        if not l[:1].isdigit(): continue
        p = [x.strip() for x in l.split("|")]
        if int(p[0]) != fl: continue
        gs = [tuple(map(int, q.split(","))) for q in p[2].split(";")]
        res = re.findall(r"\(([+-]\d+),([+-]\d+)\)=(\S+)", p[3])
        out.append((int(p[1], 16), gs, [((int(a), int(b)), c) for a, b, c in res]))
    return out

def apply_unlocks(g, fl, fired):
    changed = []
    for flag, gs, res in unlocks(fl):
        if flag in fired: continue
        if not all(g.get(p) == "." for p in gs): continue
        fired.add(flag)
        for pos, kind in res:
            g[pos] = "." if kind == "床" else kind
        changed.append(flag)
    return changed

def run():
    bad = []
    def chk(c, label):
        if not c: bad.append(label)

    # F17: 左の門は守護マス2つの敵を倒すと開く。右の門は別条件なのでまだ閉じたまま
    g, fired = load_floor(17), set()
    chk(g[(-4, 2)] == "s", "F17 の (-4,+2) が封印扉")
    chk(g[(-5, 1)].startswith("Enemy_") and g[(-3, 1)].startswith("Enemy_"), "F17 の守護マスに敵")
    chk(apply_unlocks(g, 17, fired) == [], "倒す前は開かない")
    g[(-5, 1)] = "."
    chk(apply_unlocks(g, 17, fired) == [], "片方だけでは開かない")
    g[(-3, 1)] = "."
    chk(0x62 in apply_unlocks(g, 17, fired), "両方倒すと発火")
    chk(g[(-4, 2)] == ".", "門が開いた")
    chk(g[(4, 2)] == "s", "別の門は閉じたまま")

    # F48: 守護マスは床と同じ見た目の隠れマス（原典ヒント236）
    g, fired = load_floor(48), set()
    chk(g[(-5, 5)] == "Marker_ID7", "F48 の守護マスは隠れた敵")
    g[(-5, 5)] = "."
    apply_unlocks(g, 48, fired)
    chk(g[(2, -2)] == ".", "F48 の門が開いた")

    # F41: 隠し壁の敵を倒すと降臨の翼が出る（原典ヒント216）
    g, fired = load_floor(41), set()
    chk(g[(4, 4)] == "Marker_ID10", "F41 の守護マスは隠れている者のマス")
    g[(4, 4)] = "."
    apply_unlocks(g, 41, fired)
    chk(g[(0, 1)] == "ID45", "降臨の翼が出現")
    return bad

if __name__ == "__main__":
    bad = run()
    print(f"門の解錠の通し確認: 不合格 {len(bad)}件" + ("" if not bad else " " + ", ".join(bad)))
    sys.exit(1 if bad else 0)
