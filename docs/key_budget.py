#!/usr/bin/env python3
"""鍵の出入りを数える — 「何本落ちていて、何枚の扉があるか」。

通しの自動プレイが10階の関門で鍵切れになるので、**そもそも全部の扉を開けられるのか**を
数で押さえるために作った。原典のデータ（`Assets/Resources/Floors/Floor_*.txt` と
`ShopOffers.txt`）を数えるだけで、実装は見ない。

分かること:
  * **落ちている鍵より扉の方が多い**。つまり「開ける扉を選ぶ」ことが遊びの骨で、
    手当たりしだいに開けると必ず詰まる。
  * 買える鍵は商人ごとに一度きり。**いくらでも買えるのは12階の鍵屋だけ**
    （1本 = 20 x 到達最高階 GOLD）なので、そこへ着くまでが本当の勝負。
"""
import re, os, sys

ROOT = os.path.expanduser("~/Code/MagicTower")
FLOORS = f"{ROOT}/Assets/Resources/Floors"

# 盤面の記号 → 鍵の色
DOOR = {"y": "yellow", "b": "purple", "r": "red"}
ITEM = {"Key_Yellow": "yellow", "Key_Blue": "purple", "Key_Red": "red"}
COLORS = ("yellow", "purple", "red")


def floor_counts(n):
    keys = {c: 0 for c in COLORS}
    doors = {c: 0 for c in COLORS}
    inobj = False
    for line in open(f"{FLOORS}/Floor_{n}.txt", encoding="utf-8"):
        if line.startswith("objects:"):
            inobj = True
            continue
        if inobj:
            m = re.match(r"\s*([+-]\d+) ([+-]\d+)\s+(\S+)", line)
            if m and m.group(3) in ITEM:
                keys[ITEM[m.group(3)]] += 1
            continue
        m = re.match(r"\s*([+-]\d+) \| (.{11})$", line.rstrip("\n"))
        if m:
            for ch in m.group(2):
                if ch in DOOR:
                    doors[DOOR[ch]] += 1
    return keys, doors


def shop_supply():
    """商人から買える／もらえる鍵。階 → {色: 本数}。無制限のものは None を入れる。"""
    out = {}
    for line in open(f"{ROOT}/Assets/Resources/ShopOffers.txt", encoding="utf-8"):
        if line.startswith("#") or not line.strip():
            continue
        c = [x.strip() for x in line.split("|")]
        floor, kind, body = int(c[0]), c[2], c[3]
        for part in body.split():
            if ":" not in part:
                continue
            what, num = part.split(":")
            if what not in COLORS:
                continue
            n = None if kind == "buy_var" else int(num)
            slot = out.setdefault(floor, {})
            if kind == "sell":
                n = -int(num)          # 売ると減る
            slot[what] = n if slot.get(what) is None else (
                None if n is None else slot.get(what, 0) + n)
    return out


def main():
    shops = shop_supply()
    print("階 | 黄 落/扉 | 紫 落/扉 | 赤 落/扉 | 商人")
    run = {c: 0 for c in COLORS}
    for n in range(1, 51):
        keys, doors = floor_counts(n)
        for c in COLORS:
            run[c] += keys[c] - doors[c]
        s = shops.get(n, {})
        note = " ".join(f"{k}{'無制限' if v is None else v:+}" if v is not None
                        else f"{k}いくらでも" for k, v in s.items())
        print(f"{n:2d} | {keys['yellow']:2d}/{doors['yellow']:2d}    "
              f"| {keys['purple']:2d}/{doors['purple']:2d}    "
              f"| {keys['red']:2d}/{doors['red']:2d}    | {note}")
    print()
    tot_k = {c: 0 for c in COLORS}
    tot_d = {c: 0 for c in COLORS}
    for n in range(1, 51):
        k, d = floor_counts(n)
        for c in COLORS:
            tot_k[c] += k[c]
            tot_d[c] += d[c]
    for c in COLORS:
        print(f"{c:7s}: 落ちている {tot_k[c]:3d} 本 / 扉 {tot_d[c]:3d} 枚 "
              f"→ {tot_k[c] - tot_d[c]:+d}")
    print("\n※ 足りないぶんは商人で買う。ただし商人は品ごとに一度きりで、")
    print("   いくらでも買えるのは12階の鍵屋（黄・1本 = 20 x 到達最高階 GOLD）だけ。")


if __name__ == "__main__":
    main()
