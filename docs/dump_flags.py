#!/usr/bin/env python3
"""セーブに入る「フラグ」120個を数え上げる。

セーブ処理 0x9b44 が `rep movsw cx=0x3c` で **60ワード＝120バイト**まとめて写している
ところがフラグの置き場で、番号はその中のバイト位置そのもの:

    フラグ番号 0x1e(30) 〜 0x95(149)

原典は `es:[0xNN]`（es = [0x554a] など）の形で1バイトずつ見ている。
商人（50〜68）と仙人（69〜86）は番号を計算して引くので `es:[bx]` になり、
この数え上げには出てこない（表の値 + 0x1e が番号）。

直前に `現在階 == N` を見ている場合は、その階も一緒に書き出す。
「一度きり」の旗（87〜132 の46個）は**どの関数が立てるか**で持ち主が決まる:

    0x4762 … 門の解錠（28個、SealedDoors.txt）
    0x1588 … 座標のできごと（14個、PositionEvents.txt）
    0x003e0 … 盗賊（3個、ThiefEvents.txt）※0x529 はその続き
    0x023ee … 階段で着いたとき（1個、ArrivalEvents.txt）

  使い方: python3 docs/dump_flags.py   → docs/original/flags.txt
"""
import os, re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LINES = open(f"{ROOT}/docs/MTE_disasm.txt", encoding="utf-8").read().split("\n")

LOW, HIGH = 0x1e, 0x95

# 番号で直に引いているのに注記が付かないもの。読んで分かった意味を手で足す。
EXTRA = {0x1f: "高速モード（鍵0x70で入り切り、0xa922〜0xa95f）",
         0x32: "2階で一度だけ 1000GOLD もらう贈り物（0x14e4〜0x1524、セリフ29）"}

def floor_before(i):
    """直前の 40 行に `現在階 == N` があればその N。"""
    for j in range(i - 1, max(0, i - 40), -1):
        m = re.search(r"cmp\s+word ptr es:\[0x9e\], (0x[0-9a-f]+|\d+)", LINES[j])
        if m:
            v = m.group(1)
            return int(v, 16) if v.startswith("0x") else int(v)
        if "=====" in LINES[j]:
            return None
    return None

OWNER = {0x4762: "門の解錠", 0x1588: "座標のできごと",
         0x3e0: "盗賊", 0x529: "盗賊", 0x23ee: "階段で着いたとき"}

FUNCS = []
for i, l in enumerate(LINES):
    m = re.match(r"===== 関数 (0x[0-9a-f]+) =====", l)
    if m:
        FUNCS.append((i, int(m.group(1), 16)))

def func_of(i):
    lo = None
    for j, a in FUNCS:
        if j < i:
            lo = a
        else:
            break
    return lo

found = {}
for i, l in enumerate(LINES):
    m = re.search(r"byte ptr es:\[(0x[0-9a-f]+)\]", l)
    if not m:
        continue
    off = int(m.group(1), 16)
    if not (LOW <= off <= HIGH):
        continue
    d = found.setdefault(off, {"読": [], "書": [], "note": set(), "階": set()})
    addr = l.split()[0]
    (d["書"] if re.search(r"mov\s+byte ptr es:\[", l) else d["読"]).append(addr)
    if ";" in l:
        d["note"].add(l.split(";")[1].strip())
    f = floor_before(i)
    if f is not None:
        d["階"].add(f)
    if re.search(r"mov\s+byte ptr es:\[", l):
        d.setdefault("持ち主", set()).add(OWNER.get(func_of(i), f"関数 {func_of(i):#x}"))

out = ["# セーブに入るフラグ（docs/dump_flags.py が生成）",
       "# 番号 = 置き場の中のバイト位置。0x1e(30) 〜 0x95(149) の120個。",
       "# 商人(50〜68)と仙人(69〜86)は番号を計算して引くのでここには出ない。",
       "#",
       "# 番号 | 読 | 書 | 階 | 持ち主 | 意味"]
for off in sorted(found):
    d = found[off]
    fl = ",".join(str(x) for x in sorted(d["階"])) or "-"
    note = " / ".join(sorted(d["note"])) or EXTRA.get(off, "")
    own = " / ".join(sorted(d.get("持ち主", set()))) or "-"
    out.append(f"0x{off:02x} {off:3d} | {len(d['読']):2d} | {len(d['書']):2d} | {fl:>10}"
               f" | {own:<16} | {note}")
open(f"{ROOT}/docs/original/flags.txt", "w", encoding="utf-8").write("\n".join(out) + "\n")
print("\n".join(out))
