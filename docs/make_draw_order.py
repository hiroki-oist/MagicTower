#!/usr/bin/env python3
"""階を描く順番を MTE.EXE から取り出す。

階に入るとき、原典は 0xafa6 が **121マスを決まった順に1つずつ描く**。
順番は関数の前置きで積み上げる 121組の (x, y) の表で、読むときは

    x = [bp + si - 0xfa]   y = [bp + si - 0xf9]   （si = 番号 * 2）

原典は **3マス描くごとに縦帰線を1回待つ**（0xb519 付近、`番号 % 3 == 0` で待つ）。
高速モード（旗31）のときは 10マスごと。

  使い方: python3 docs/make_draw_order.py > Assets/Resources/FloorDrawOrder.txt
"""
import os, re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LINES = open(f"{ROOT}/docs/MTE_disasm.txt", encoding="utf-8").read().split("\n")

start = next(i for i, l in enumerate(LINES) if l.startswith("===== 関数 0x0afa6"))
end = next(i for i in range(start + 1, len(LINES)) if LINES[i].startswith("====="))

# 前置きの `mov al, 値` と `mov byte ptr [bp - 変位], al` を順に追う
frame = {}
al = None
for i in range(start, end):
    l = LINES[i]
    m = re.search(r"mov\s+al, (0x[0-9a-f]+|\d+)", l)
    if m:
        v = m.group(1)
        al = int(v, 16) if v.startswith("0x") else int(v)
        continue
    m = re.search(r"sub\s+al, al", l)
    if m:
        al = 0
        continue
    m = re.search(r"mov\s+byte ptr \[bp - (0x[0-9a-f]+)\], al", l)
    if m and al is not None:
        frame[int(m.group(1), 16)] = al

# 番号 n の x は [bp-0xfa+2n]、y は [bp-0xf9+2n]。bp からの変位は負なので
# 変位 = 0xfa - 2n（x）と 0xfa - 2n - 1（y）。
order = []
for n in range(121):
    dx = 0xfa - 2 * n
    dy = dx - 1
    if dx not in frame or dy not in frame:
        break
    order.append((frame[dx], frame[dy]))

# 最後の1マス（番号120）だけは、置き場が関数の他の変数と重なっていて
# この読み方では取れない。**121マスのうち残った1マス＝盤面の中央 (0,0)** なので、
# そこだけ差分で埋める（渦巻きの中心にあたる）。
missing = [(X, Y) for X in range(11) for Y in range(11) if (X, Y) not in set(order)]
assert len(missing) == 1 and missing[0] == (5, 5), missing
order.append(missing[0])

out = ["# 階を描く順番 — 原典 PC98版 MTE.EXE の 0xafa6 が積む表から取り出したもの",
       "#",
       "# 階に入ると 121マスをこの順に1つずつ描く。**3マスごとに縦帰線を1回待つ**ので、",
       "# 1階ぶんで 40回ぶんの待ち ≒ 0.67秒かかる。高速モード（旗31）のときは10マスごと。",
       "#",
       "# 原典の X,Y は 0〜10（上が0）。この表は Floor_N.txt と同じで x = X-5、y = 5-Y。",
       "#",
       "# 渦巻き。左上から左の列を下り、下の行、右の列、…と外から内へ回って中央 (0,0) で終わる。",
       "# 番号 | x | y"]
for n, (X, Y) in enumerate(order):
    out.append(f"{n:3d} | {X - 5:+d} | {5 - Y:+d}")
print("\n".join(out))
