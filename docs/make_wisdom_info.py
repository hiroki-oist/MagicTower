#!/usr/bin/env python3
"""知恵のオーブが読み返せる情報の表を MTE.EXE から取り出す。

原典の表示関数 0x8a46 は、スタックの上に **(フラグ, セリフ番号) の組を24件**
並べてから順に見て、**フラグが立っているものだけ**を「情報番号」付きで並べる。
フラグは一度きりフラグの配列を `es:[bx + 0x1e]` で引くので、
**実際のフラグ番号は表の値 + 0x1e（30）**。

  フラグ 69〜86 … 仙人18人（セリフ 80〜97）
  フラグ 50〜68 … 商人（セリフ 12〜31）

  使い方: python3 docs/make_wisdom_info.py > Assets/Resources/WisdomInfo.txt
"""
import os, re

DIS = os.path.join(os.path.dirname(__file__), "MTE_disasm.txt")
LINES = open(DIS, encoding="utf-8").read().split("\n")
START, END = 0x008a46, 0x008b66
FLAG_BASE = 0x1e

ST_IMM = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +mov +word ptr \[bp - (0x[0-9a-f]+|\d+)\], (-?(?:0x[0-9a-f]+|\d+))\s*(?:;.*)?$')
ST_REG = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +mov +word ptr \[bp - (0x[0-9a-f]+|\d+)\], (ax|bx|cx|dx)\s*(?:;.*)?$')
MOV    = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +mov +(ax|bx|cx|dx), (0x[0-9a-f]+|\d+)\s*(?:;.*)?$')

def table():
    lo = next(i for i, l in enumerate(LINES) if l.startswith("  %06x  " % START))
    hi = next(i for i, l in enumerate(LINES) if l.startswith("  %06x  " % END))
    frame, reg = {}, {}
    for l in LINES[lo:hi]:
        m = MOV.match(l)
        if m: reg[m.group(1)] = int(m.group(2), 0); continue
        m = ST_IMM.match(l)
        if m: frame[int(m.group(1), 0)] = int(m.group(2), 0); continue
        m = ST_REG.match(l)
        if m and reg.get(m.group(2)) is not None:
            frame[int(m.group(1), 0)] = reg[m.group(2)]
    out, off = [], 0x7c
    while off > 0:
        a, b = frame.get(off), frame.get(off - 2)
        if a is None or a == 0xffff: break
        out.append((a + FLAG_BASE, b))
        off -= 4
    return out

def main():
    rows = table()
    print("# 知恵のオーブが読み返せる情報 — 原典 MTE.EXE 0x8a46 の表から取り出したもの")
    print("#")
    print("# 原典は (フラグ, セリフ番号) の組を24件スタックに並べ、上から順に見て")
    print("# **フラグが立っているものだけ**を「情報番号：n」付きで並べる。")
    print("# フラグは `es:[bx + 0x1e]` で引くので、実際の番号は表の値 + 30。")
    print("#   フラグ69〜86 … 仙人18人（セリフ80〜97） / フラグ50〜68 … 商人（セリフ12〜31）")
    print("#")
    print("# 情報番号は表の並び順（原典は添字+1を出す）。")
    print("#")
    print("# 形式: 情報番号 | フラグ | セリフ番号")
    for i, (flag, rec) in enumerate(rows, 1):
        who = "仙人" if flag >= 69 else "商人"
        print(f"{i:2d} | {flag:2d} | {rec:2d}   # {who}")

if __name__ == "__main__":
    main()
