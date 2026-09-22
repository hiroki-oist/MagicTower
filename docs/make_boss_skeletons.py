#!/usr/bin/env python3
"""10階「骸骨に囲まれる」場面の台本を MTE.EXE から取り出す。

原典 PC98版 MTE.EXE の関数 **0x03e0a**（位置イベント 0x1588 が 10階・X=5,Y=5 で呼ぶ）。
中身は次の順番だった:

  0x3ebc  主人公を1歩上へ滑らせて Y を1つ減らす      ; 盤面 (0,0) → (0,+1)
  0x3efc  窓でセリフ100                               ; 0x94ba(…, 100)
  0x3f52  マス(0,+2) を床にする                       ; 金騎士がどく
  0x3f6b  金騎士を上へ3歩                             ; 0x39c6(4,5,3) → (4,5,2) → (4,5,1)
  0x3fd7  マス(0,+5) に金騎士(81)、マス(0,+3) に門(4)
          （台本では行き先の書き込みを歩きの前に置く。着くまで隠すのは見た目の役目）
  0x3fe5  (0,+3) の門が閉まる                         ; 0x1276
  0x4007  マス(0,-1) に門(4) → 閉まる                 ; 退路を断つ
  0x4030  マス(-2,+2) を床に → 開く                   ; 0x11b8（封印扉が開く）
  0x4056  マス(+2,+2) を床に → 開く
  0x4096  **骸骨戦士(79)が2体**、向きの表どおりに歩いてくる
  0x4174  **骸骨(77)が6体**、同じように歩いてくる
  0x424a  (-2,+2) と (+2,+2) を門にして閉める
  0x428d  主人公の絵を戻して終わり

歩く道順は関数の前置きで `mov al, 値` ＋ `mov byte ptr [bp - off], al` として
**8本ぶん**並べてあり、`[bp + si - 0x30]` を si を増やしながら読む。
出発のマスは `[bp - 0x5c]`（X）と `[bp - 0x5a]`（Y）から4バイトおき。

    1 = X+1（右） / 2 = X-1（左） / 3 = Y+1（画面の下） / 4 = Y-1（上） / 5 = 区切り

原典の Y は下に向かって増えるので、盤面（y は上が＋）では 3 が y-1、4 が y+1。
盤面の座標は x = X-5、y = 5-Y。

  使い方: python3 docs/make_boss_skeletons.py > Assets/Resources/BossSkeletons.txt
"""
import os, re

HERE = os.path.dirname(os.path.abspath(__file__))
LINES = open(os.path.join(HERE, "MTE_disasm.txt"), encoding="utf-8").read().split("\n")
START, END = 0x003e0a, 0x0042aa
TABLE, XS, YS = 0x30, 0x5c, 0x5a          # 向きの表 / 出発の X / 出発の Y
WALKERS = 8

MOVR  = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +mov +(a[lx]|b[lx]|c[lx]|d[lx]), (0x[0-9a-f]+|\d+)\s*(?:;.*)?$')
SUBR  = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +sub +(ax|bx|cx|dx), (ax|bx|cx|dx)\s*(?:;.*)?$')
STB   = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +mov +byte ptr \[bp - (0x[0-9a-f]+|\d+)\], (al|bl|cl|dl|0x[0-9a-f]+|\d+)\s*(?:;.*)?$')
STW   = re.compile(r'^  [0-9a-f]{6}  [0-9a-f]+ +mov +word ptr \[bp - (0x[0-9a-f]+|\d+)\], (ax|bx|cx|dx|0x[0-9a-f]+|\d+)\s*(?:;.*)?$')

def scan():
    """関数の中の `[bp - 変位]` への書き込みを、最初の1回だけ拾う。"""
    lo = next(i for i, l in enumerate(LINES) if l.startswith("  %06x  " % START))
    hi = next(i for i, l in enumerate(LINES) if l.startswith("  %06x  " % END))
    reg, byte, word = {}, {}, {}
    for l in LINES[lo:hi]:
        m = MOVR.match(l)
        if m:
            reg[m.group(1)[0] + "x"] = int(m.group(2), 0)
            continue
        m = SUBR.match(l)
        if m and m.group(1) == m.group(2):
            reg[m.group(1)] = 0
            continue
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
    """向きの表を区切り(5)で割って8本にする。"""
    out, cur, off = [], [], TABLE
    while off > 0 and off in byte:
        v = byte[off]
        if v == 5:
            out.append(cur); cur = []
        else:
            cur.append(v)
        off -= 1
    return out

def board(x, y):
    return x - 5, 5 - y

STEP = {1: (1, 0), 2: (-1, 0), 3: (0, -1), 4: (0, 1)}

def walk(start, dirs):
    x, y = start
    for d in dirs:
        dx, dy = STEP[d]
        x, y = x + dx, y + dy
    return x, y

def prefabs():
    """物体ID → プレハブ名（Assets/Resources/ObjectIds.txt）。"""
    out = {}
    path = os.path.join(HERE, "..", "Assets", "Resources", "ObjectIds.txt")
    for line in open(path, encoding="utf-8"):
        line = line.strip()
        if not line or line.startswith("#") or "|" not in line: continue
        name, num = line.split("|")
        out[int(num)] = name.strip()
    return out

def pos(p):
    return "(%+d,%+d)" % p

def main():
    byte, word = scan()
    ps = paths(byte)
    if len(ps) != WALKERS:
        raise SystemExit("向きの表が %d 本しか取れない（8本のはず）" % len(ps))
    name = prefabs()
    starts = [board(word[XS - 4 * i], word[YS - 4 * i]) for i in range(WALKERS)]
    kinds = [79, 79, 77, 77, 77, 77, 77, 77]     # 0x4109 が 0x4f、0x4225 が 0x4d を書く

    print("# 10階「骸骨に囲まれる」場面の台本 — 原典 MTE.EXE 0x03e0a から起こしたもの")
    print("#")
    print("# 1行が1手。Cutscenes.txt の書き方と同じで、Cutscenes が「骸骨に囲まれる」の")
    print("# 1行をこの並びに広げる。歩きの数字は原典の向きの表そのままで、")
    print("#   1 = X+1（右） / 2 = X-1（左） / 3 = Y+1（画面の下） / 4 = Y-1（上）")
    print("# 盤面の y は上が＋なので、3 が y-1、4 が y+1 にあたる。")
    print("#")
    print("# 出発のマスは [bp-0x5c]（X）と [bp-0x5a]（Y）から4バイトおきに8つ、")
    print("# 道順は [bp-0x30] から下へ、区切り(5)で8本に分かれている。")
    print("# どちらも docs/make_boss_skeletons.py が原典から取り出している。")
    print()
    print("# 金騎士(81)が奥へ下がる（0x3f52〜0x3fe5）")
    print("地図 (+0,+2)=床")
    print("地図 (+0,+5)=%s" % name[81])
    print("歩き %s (+0,+2) 4,4,4" % name[81])
    print("地図 (+0,+3)=門")
    print("閉まる (+0,+3)")
    print()
    print("# 退路の門が閉まる（0x4007〜0x4016）")
    print("地図 (+0,-1)=門")
    print("閉まる (+0,-1)")
    print()
    print("# 左右の封印扉が開く（0x4030〜0x406b）")
    print("地図 (-2,+2)=床")
    print("開く (-2,+2)")
    print("地図 (+2,+2)=床")
    print("開く (+2,+2)")
    print()
    for i in range(WALKERS):
        if i == 0: print("# 骸骨戦士(79)が2体（0x4096〜0x4155）")
        if i == 2: print()
        if i == 2: print("# 骸骨(77)が6体（0x4174〜0x4235）")
        goal = walk(starts[i], ps[i])
        print("地図 %s=床" % pos(starts[i]))
        print("地図 %s=%s" % (pos(goal), name[kinds[i]]))
        print("歩き %s %s %s" % (name[kinds[i]], pos(starts[i]),
                                 ",".join(str(d) for d in ps[i])))
    print()
    print("# 左右の封印扉が門になって閉まる（0x424a〜0x428a）")
    print("地図 (-2,+2)=門")
    print("閉まる (-2,+2)")
    print("地図 (+2,+2)=門")
    print("閉まる (+2,+2)")

if __name__ == "__main__":
    main()
