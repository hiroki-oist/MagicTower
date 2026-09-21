#!/usr/bin/env python3
"""MTE.EXE の「演出つき地図書き換え表」を全部取り出す。

原典は一部のできごとで、書き換えを **(X, Y, 値) の三つ組の表** としてスタック上に
組み立て、1マスずつ音と描画を挟みながら順に適用する。表は関数の前置きで
`mov word ptr [bp - off], imm` の羅列として作られ、値 -1 で終わる。

  使い方: python3 docs/make_unlock_sequences.py
  出力  : 表ごとに「どの階のどのフラグの解錠か」と中身（盤面座標つき）。
          SealedDoors.txt / Cutscenes.txt の結果欄はこの出力から書き写している。
"""
import re, os

DIS = os.path.join(os.path.dirname(__file__), "MTE_disasm.txt")
LINES = open(DIS, encoding="utf-8").read().split("\n")

ADDR    = re.compile(r'^  ([0-9a-f]{6})  ')
MOV_IMM = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +mov +(ax|bx|cx|dx|si|di), (-?(?:0x[0-9a-f]+|\d+))\s*(?:;.*)?$')
SUB_SELF= re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +sub +(ax|bx|cx|dx), \1\s*(?:;.*)?$')
ST_REG  = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +mov +word ptr \[bp - (0x[0-9a-f]+|\d+)\], (ax|bx|cx|dx|si|di)\s*(?:;.*)?$')
ST_IMM  = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +mov +word ptr \[bp - (0x[0-9a-f]+|\d+)\], (-?(?:0x[0-9a-f]+|\d+))\s*(?:;.*)?$')
FLOOR   = re.compile(r'cmp +word ptr es:\[0x9e\], (0x[0-9a-f]+|\d+)')
FLAGSET = re.compile(r'mov +byte ptr es:\[(0x[0-9a-f]+)\], 1')
# 表の終端判定 "cmp word ptr [bp + si - 0xNNN], -1" の base は 値スロット
TERM    = re.compile(r'cmp +word ptr \[bp \+ si - (0x[0-9a-f]+)\], -1')

TERRAIN = {6:"床", 8:"壁", 9:"隠し壁", 11:"上り", 12:"下り", 4:"門", 5:"特殊扉",
           1:"黄扉", 2:"紫扉", 3:"赤扉"}

def func_range(start):
    lo = next(i for i, l in enumerate(LINES) if l.startswith("  %06x  " % start))
    hi = lo
    while "retf" not in LINES[hi]: hi += 1
    return lo, hi

def frame_of(lo, hi):
    """関数の前置きが組み立てるスタック枠の像（最初の代入が生きる）。"""
    frame, reg = {}, {}
    for l in LINES[lo:hi]:
        m = SUB_SELF.match(l)
        if m: reg[m.group(1)] = 0; continue
        m = MOV_IMM.match(l)
        if m: reg[m.group(1)] = int(m.group(2), 0); continue
        m = ST_IMM.match(l)
        if m: frame.setdefault(int(m.group(1), 0), int(m.group(2), 0)); continue
        m = ST_REG.match(l)
        if m:
            v = reg.get(m.group(2))
            if v is not None: frame.setdefault(int(m.group(1), 0), v)
    return frame

def table(frame, val_base, limit=40):
    """値スロットの base から (X, Y, 値) を 6 バイトずつ読む。-1 で終わり。"""
    out = []
    for i in range(limit):
        v = frame.get(val_base - 6*i)
        x = frame.get(val_base + 4 - 6*i)
        y = frame.get(val_base + 2 - 6*i)
        if v is None or v in (0xffff, -1): break
        out.append((x, y, v))
    return out

def context(lo, i):
    """その位置より前にある「階の判定」と「フラグを立てる」を拾う。"""
    floor = flag = None
    for l in LINES[lo:i]:
        m = FLOOR.search(l)
        if m: floor = int(m.group(1), 0)
        m = FLAGSET.search(l)
        if m: flag = m.group(1)
    return floor, flag

def dump(start, title):
    lo, hi = func_range(start)
    frame = frame_of(lo, hi)
    seen = set()
    for i in range(lo, hi + 1):
        m = TERM.search(LINES[i])
        if not m: continue
        base = int(m.group(1), 16)
        if base in seen: continue
        seen.add(base)
        floor, flag = context(lo, i)
        rows = table(frame, base)
        print(f"--- {title} 0x{start:04x} : 表 bp-0x{base:x}"
              f"（直前の階={floor} フラグ={flag}）{len(rows)}件")
        for x, y, v in rows:
            kind = TERRAIN.get(v, f"ID{v}")
            print(f"    ({x-5:+d},{5-y:+d})={kind}")
    # lea 形（0x3044 のように si を直に立てるもの）
    for i in range(lo, hi + 1):
        m = re.search(r'lea +si, \[bp - (0x[0-9a-f]+)\]', LINES[i])
        if not m: continue
        base = int(m.group(1), 16) - 2      # lea は Y スロット、値はその 2 つ先
        floor, flag = context(lo, i)
        rows = table(frame, base)
        if not rows: continue
        print(f"--- {title} 0x{start:04x} : 表 bp-0x{base:x}"
              f"（直前の階={floor} フラグ={flag}）{len(rows)}件")
        for x, y, v in rows:
            kind = TERRAIN.get(v, f"ID{v}")
            print(f"    ({x-5:+d},{5-y:+d})={kind}")

dump(0x4762, "解錠判定")
dump(0x3044, "場面 ボス登場")
dump(0x3e0a, "場面 ボススケルトン登場")
dump(0x325e, "場面 金騎士の突撃")
dump(0x7182, "場面 金騎士処刑とゼノ登場")
dump(0x6ae0, "場面 ゼノとの初対面")
dump(0x03e0, "場面 盗賊との再会")
dump(0x68b0, "場面 金騎士敗走")
