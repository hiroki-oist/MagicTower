#!/usr/bin/env python3
"""階ごとに「上り階段へ着くのに最低いくつ鍵が要るか」を見積もる。

`docs/key_budget.py` で **鍵は最初から足りない**（黄は 169本に対して扉239枚）と
分かったので、次は「では各階でいくつ要るのか」を押さえる。

やり方は人の遊び方と同じで、**塗り広げ**:

  1. 扉を通らずに行ける所を全部ひろげ、落ちている鍵を拾う
  2. 行き止まりなら、**縁にある扉のうち「開けると鍵がいちばん多く手に入る」もの**を1枚開ける
  3. 上り階段に届くまで繰り返す

⚠ これは **見積もり**であって証明ではない（2の選び方が最善とは限らない）。
それでも「この階は最低でも何本要る」の目安になるし、
данные が壊れて通れなくなれば数が跳ねるので気づける。

出どころ: `Assets/Resources/Floors/Floor_*.txt` と `FloorConnections.txt`。
"""
import re, os, sys
from collections import deque

ROOT = os.path.expanduser("~/Code/MagicTower")
FLOORS = f"{ROOT}/Assets/Resources/Floors"
MINX, MAXX, MINY, MAXY = -5, 5, -5, 5

DOOR = {"y": "yellow", "b": "purple", "r": "red"}
KEY = {"Key_Yellow": "yellow", "Key_Blue": "purple", "Key_Red": "red"}
# 通れる地形（扉と壁以外）
WALK = set(".<>~*")


def load(n):
    """階の盤面を {(x,y): 記号} と、物体 {(x,y): プレハブ名} で返す。"""
    cells, objs = {}, {}
    inobj = False
    for line in open(f"{FLOORS}/Floor_{n}.txt", encoding="utf-8"):
        if line.startswith("objects:"):
            inobj = True
            continue
        if inobj:
            m = re.match(r"\s*([+-]\d+) ([+-]\d+)\s+(\S+)", line)
            if m:
                objs[(int(m.group(1)), int(m.group(2)))] = m.group(3)
            continue
        m = re.match(r"\s*([+-]\d+) \| (.{11})$", line.rstrip("\n"))
        if m:
            y = int(m.group(1))
            for i, ch in enumerate(m.group(2)):
                cells[(MINX + i, y)] = ch
    return cells, objs


def connections():
    out = {}
    for line in open(f"{ROOT}/Assets/Resources/FloorConnections.txt", encoding="utf-8"):
        if line.startswith("#") or not line.strip():
            continue
        c = [x.strip() for x in line.split("|")]
        def pos(t):
            if t == "-":
                return None
            a, b = t.split(",")
            return (int(a), int(b))
        out[int(c[0])] = {"up": pos(c[1]), "down": pos(c[2]),
                          "from_below": pos(c[3]), "from_above": pos(c[4])}
    return out


def spread(cells, objs, start, opened):
    """扉を通らずに行ける所と、そこで拾える鍵を返す。"""
    seen = {start}
    q = deque([start])
    keys = {"yellow": 0, "purple": 0, "red": 0}
    while q:
        at = q.popleft()
        got = objs.get(at)
        if got in KEY:
            keys[KEY[got]] += 1
        for d in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            nxt = (at[0] + d[0], at[1] + d[1])
            if nxt in seen or nxt not in cells:
                continue
            ch = cells[nxt]
            if ch in DOOR and nxt not in opened:
                continue
            if ch not in WALK and ch not in DOOR and nxt not in objs:
                continue                      # 壁
            seen.add(nxt)
            q.append(nxt)
    return seen, keys


def need(n, conn):
    """その階で上り階段に着くまでに開ける扉の数（色ごと）を見積もる。"""
    cells, objs = load(n)
    info = conn.get(n, {})
    start = info.get("from_below") or info.get("from_above")
    goal = info.get("up")
    if start is None or goal is None:
        return None
    opened, spent = set(), {"yellow": 0, "purple": 0, "red": 0}
    for _ in range(60):
        seen, keys = spread(cells, objs, start, opened)
        if goal in seen:
            return spent, keys
        # 縁の扉のうち、開けると鍵がいちばん増えるものを1枚
        best, best_gain = None, -1
        for at in seen:
            for d in ((0, 1), (0, -1), (1, 0), (-1, 0)):
                nxt = (at[0] + d[0], at[1] + d[1])
                if nxt not in cells or nxt in opened or cells[nxt] not in DOOR:
                    continue
                after, k2 = spread(cells, objs, start, opened | {nxt})
                gain = (k2["yellow"] - keys["yellow"]) * 100 + len(after - seen)
                if goal in after:
                    gain += 100000
                if gain > best_gain:
                    best, best_gain = nxt, gain
        if best is None:
            return None                       # どうやっても届かない
        opened.add(best)
        spent[DOOR[cells[best]]] += 1
    return None


def main():
    conn = connections()
    print("階 | 開ける扉 黄/紫/赤 | その道で拾える鍵 黄/紫/赤")
    run = 0
    for n in range(1, 51):
        r = need(n, conn)
        if r is None:
            print(f"{n:2d} | （上り階段が最初から無いか、届かない）")
            continue
        spent, keys = r
        run += keys["yellow"] - spent["yellow"]
        print(f"{n:2d} | {spent['yellow']:3d}/{spent['purple']:2d}/{spent['red']:2d}"
              f"        | {keys['yellow']:3d}/{keys['purple']:2d}/{keys['red']:2d}"
              f"   （黄の手持ちの目安 {run:+d}）")


if __name__ == "__main__":
    main()
