#!/usr/bin/env python3
"""金騎士の突撃の道順を MTE.EXE から取り出す。

40階の場面（原典 0x325e）は、**向きを1バイトずつ並べた表**を持っていて、
それを順に読みながら金騎士の絵を動かす。表は関数の前置きで
`mov al, 値` ＋ `mov byte ptr [bp - off], al` の羅列として作られ、
読むときは `[bp + si - 0x92]` を si を増やしながら引く。

向きの意味（0x35ac〜0x35d5 と 0x3b94〜0x3be5 の分岐より）:

    1 = X+1（右） / 2 = X-1（左） / 3 = Y+1（画面の下） / 4 = Y-1（上） / 5 = 区切り

原典の Y は下に向かって増えるので、盤面（y は上が＋）では 3 が y-1、4 が y+1 になる。

  使い方: python3 docs/make_knight_charge.py > Assets/Resources/KnightCharge.txt
"""
import os, re

DIS = os.path.join(os.path.dirname(__file__), "MTE_disasm.txt")
LINES = open(DIS, encoding="utf-8").read().split("\n")
START, END, BASE = 0x00325e, 0x003e05, 0x92

MOVAL = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +mov +al, (0x[0-9a-f]+|\d+)\s*(?:;.*)?$')
STB   = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +mov +byte ptr \[bp - (0x[0-9a-f]+|\d+)\], (al|0x[0-9a-f]+|\d+)\s*(?:;.*)?$')

def path():
    lo = next(i for i, l in enumerate(LINES) if l.startswith("  %06x  " % START))
    hi = next(i for i, l in enumerate(LINES) if l.startswith("  %06x  " % END))
    al, frame = None, {}
    for l in LINES[lo:hi]:
        m = MOVAL.match(l)
        if m: al = int(m.group(1), 0); continue
        m = STB.match(l)
        if m:
            v = al if m.group(2) == "al" else int(m.group(2), 0)
            if v is not None: frame.setdefault(int(m.group(1), 0), v)
    out, off = [], BASE
    while off > 0 and off in frame:
        out.append(frame[off]); off -= 1
    return out

NAME = {1: "右", 2: "左", 3: "下", 4: "上", 5: "区切り"}

def main():
    p = path()
    print("# 金騎士の突撃の道順 — 原典 MTE.EXE 0x325e が持っている向きの表")
    print("#")
    print("# 1バイト＝1歩。`[bp + si - 0x92]` を si を増やしながら読む。")
    print("#   1 = X+1（右） / 2 = X-1（左） / 3 = Y+1（画面の下） / 4 = Y-1（上） / 5 = 区切り")
    print("# 原典の Y は下へ増えるので、盤面（y は上が＋）では 3 が y-1、4 が y+1。")
    print("#")
    print(f"# 全{len(p)}歩。区切りで区切ると{p.count(5)}まとまりで、")
    print("# ジグザグに降りていって、最後にまっすぐ下りてから上へ戻る。")
    print("#")
    print("# 形式: 1行が1まとまり（区切りまで）。数字は上の向き。")
    line = []
    for v in p:
        if v == 5:
            print(" ".join(str(x) for x in line) + "   # " + "".join(NAME[x] for x in line))
            line = []
        else:
            line.append(v)
    if line:
        print(" ".join(str(x) for x in line) + "   # " + "".join(NAME[x] for x in line))

if __name__ == "__main__":
    main()
