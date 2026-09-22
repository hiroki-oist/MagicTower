#!/usr/bin/env python3
"""40階「金騎士の突撃」の台本を MTE.EXE から取り出す。

原典 PC98版 MTE.EXE の関数 **0x0325e**（位置イベント 0x1588 が 40階・X=5,Y=6 で呼ぶ）。
**斬る演出ではなく、部下が1体ずつ突撃してきて、そのたびに戦う**場面だった。

  0x350a  セリフ135
  0x3540  マス(0,-2) に門(4) → 0x1276 で閉まる          ; 退路を断つ
  0x356a  突撃してくる者の絵を登録（0x40d6）
  0x3584  向きの表どおりに1マスずつ滑らせる（0x39c6）   ; 出発は表から
  0x35ed  0x45be(5,5)                                    ; 着いた (0,0) を描き直す
  0x3605  **0x6082 で戦闘**（相手は [bp-0x94] のID）
  0x363c  数え上げ。3・6・9・12 の切れ目でセリフと絵を替える
  0x3cb1  最後の1本（金騎士が引き上げる。戦闘なし）
  0x3da3  三つ組の表で (0,-2) を床に戻して門を開く
  0x3dd1  マス(0,0) を床にして終わり

出発のマスは `[bp - 0x38]`（X）と `[bp - 0x36]`（Y）から4バイトおきに12組。
道順は `[bp + si - 0x92]` の向きの表で、区切り(5)で14本に分かれている
（`docs/make_knight_charge.py` が同じ表を Resources/KnightCharge.txt に書き出している）。

12組の出発マスは Floor_40.txt に並んでいる12体の位置とぴったり一致し、
戦う相手のID（141→139→137→135→133）もそのマスに居るものと一致した。

  使い方: python3 docs/make_knight_rush.py > Assets/Resources/KnightRush.txt
"""
import os, re

HERE = os.path.dirname(os.path.abspath(__file__))
LINES = open(os.path.join(HERE, "MTE_disasm.txt"), encoding="utf-8").read().split("\n")
START, END = 0x0325e, 0x03e0a
TABLE, XS, YS = 0x92, 0x38, 0x36

MOVR = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +mov +(a[lx]|b[lx]|c[lx]|d[lx]), (0x[0-9a-f]+|\d+)\s*(?:;.*)?$')
SUBR = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +sub +(ax|bx|cx|dx), (ax|bx|cx|dx)\s*(?:;.*)?$')
STB  = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +mov +byte ptr \[bp - (0x[0-9a-f]+|\d+)\], (al|bl|cl|dl|0x[0-9a-f]+|\d+)\s*(?:;.*)?$')
STW  = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +mov +word ptr \[bp - (0x[0-9a-f]+|\d+)\], (ax|bx|cx|dx|0x[0-9a-f]+|\d+)\s*(?:;.*)?$')

def scan():
    lo = next(i for i, l in enumerate(LINES) if l.startswith("  %06x  " % START))
    hi = next(i for i, l in enumerate(LINES) if l.startswith("  %06x  " % END))
    reg, byte, word = {}, {}, {}
    for l in LINES[lo:hi]:
        m = MOVR.match(l)
        if m:
            reg[m.group(1)[0] + "x"] = int(m.group(2), 0); continue
        m = SUBR.match(l)
        if m and m.group(1) == m.group(2): reg[m.group(1)] = 0
        m = STB.match(l)
        if m:
            v = reg.get(m.group(2)[0] + "x") if m.group(2).isalpha() else int(m.group(2), 0)
            if v is not None: byte.setdefault(int(m.group(1), 0), v & 0xff)
            continue
        m = STW.match(l)
        if m:
            v = reg.get(m.group(2)) if m.group(2).isalpha() else int(m.group(2), 0)
            if v is not None: word.setdefault(int(m.group(1), 0), v)
    return byte, word

def paths(byte):
    out, cur, off = [], [], TABLE
    while off > 0 and off in byte:
        v = byte[off]
        if v == 5: out.append(cur); cur = []
        else: cur.append(v)
        off -= 1
    return out

def objects():
    """Floor_40.txt の (x,y) → プレハブ名。"""
    out = {}
    path = os.path.join(HERE, "..", "Assets", "Resources", "Floors", "Floor_40.txt")
    body = open(path, encoding="utf-8").read().split("objects:")[1]
    for line in body.split("\n"):
        m = re.match(r'\s*([+-]\d+)\s+([+-]\d+)\s+(\S+)', line)
        if m: out[(int(m.group(1)), int(m.group(2)))] = m.group(3)
    return out

STEP = {1: (1, 0), 2: (-1, 0), 3: (0, -1), 4: (0, 1)}

def walk(start, dirs):
    x, y = start
    for d in dirs: dx, dy = STEP[d]; x, y = x + dx, y + dy
    return x, y

def pos(p): return "(%+d,%+d)" % p

def main():
    byte, word = scan()
    ps = paths(byte)
    if len(ps) != 14: raise SystemExit("道順が %d 本（14本のはず）" % len(ps))
    obj = objects()
    starts = []
    for i in range(12):
        x, y = word[XS - 4 * i], word[YS - 4 * i]
        starts.append((x - 5, 5 - y))
    starts.append((0, 4))          # 12本目は金騎士自身（0x39a0 あたりで別に置く）
    # セリフの切れ目（0x363c/0x36f3/0x3964/0x39db の数え上げ）
    says = {0: 135, 3: 136, 6: 137, 9: 138, 12: 139, 13: 140}

    print("# 40階「金騎士の突撃」の台本 — 原典 MTE.EXE 0x0325e から起こしたもの")
    print("#")
    print("# **部下が1体ずつ突撃してきて、そのたびに戦う**場面。")
    print("# 出発のマスは [bp-0x38]/[bp-0x36] の12組、道順は [bp-0x92] の表14本、")
    print("# 相手のIDは戦闘（0x6082）に渡す [bp-0x94]。どれも原典から取り出している。")
    print("# 出発の12マスは Floor_40.txt の12体の位置と、相手のIDもそこに居るものと一致する。")
    print("#")
    print("# 退路を断つ（0x3540〜0x3557）")
    print("地図 (+0,-2)=門")
    print("閉まる (+0,-2)")
    for i in range(13):
        if i in says:
            print()
            print("# セリフ%d（数え上げ %d の切れ目）" % (says[i], i))
            print(says[i])
        s = starts[i]
        who = obj.get(s)
        if who is None: raise SystemExit("%s に居るものが Floor_40.txt に無い" % (s,))
        goal = walk(s, ps[i])
        if goal != (0, 0): raise SystemExit("%d本目の行き先が (0,0) でない: %s" % (i, goal))
        print("地図 %s=床" % pos(s))
        print("歩き %s %s %s" % (who, pos(s), ",".join(str(d) for d in ps[i])))
        print("戦闘 %s (+0,+0)" % who)
        print("消す (+0,+0)")          # 原典 0x45be(5,5)（倒した相手の絵を消す）
    print()
    print("# 引き上げる（0x3cb1。最後の1本だけ戦闘が無い）")
    print(says[13])
    print("歩き %s (+0,+0) %s" % (obj[(0, 4)], ",".join(str(d) for d in ps[13])))
    print("消す %s" % pos(walk((0, 0), ps[13])))
    print()
    print("# 門を開けて中央を空ける（0x3da3〜0x3dd1）。三つ組を当てるたびに音6（0x3d18）")
    print("音 場面のできごと")
    print("地図 (+0,-2)=床")
    print("開く (+0,-2)")
    print("地図 (+0,+0)=床")

if __name__ == "__main__":
    main()
