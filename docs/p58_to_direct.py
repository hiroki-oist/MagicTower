#!/usr/bin/env python3
"""**属性表を通さず、原典が番地で直に貼っている絵**を Assets へ書き出す。

物体の絵は「物体ID → 属性表 → 絵の位置」で引かれるが、次の3枚だけは
コードが**まとめ読みした buffer の番地を直に**足して貼っている。

原典は戦闘でも魔法攻撃でも場面の節目でも、**同じ絵**を貼っている:

    mov  ax, word ptr es:[0xa]      ; まとめ読みした絵の buffer の頭
    add  ax, 0x87a                  ; ← この変位
    lcall 0xf5f, 0x147

`0x87a = 2170` は GROUND.P58 の中の **11枚目（GROUND_010）の先頭とぴったり一致**する
（各絵の詰め込み後の大きさを足し上げると 2170 になる）。この絵だけは属性表
（物体ID → 絵）のどこからも指されておらず、当たりの演出専用だと分かる。

使っている7か所: 0x1b9e / 0x1d61 / 0x1f42（魔法攻撃の3種）、
0x629c（自分の攻撃）、0x648f（相手の反撃）、0x6d42（ゼノとの初対面）、0x73c6（金騎士処刑）。

  使い方: python3 docs/p58_to_effect.py
"""
import os, sys, zlib, struct
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from p58 import decode, to_indices, GAME_PALETTE

ROOT = os.path.expanduser("~/Code/MagicTower")
SRC = f"{ROOT}/docs/original/disk/GROUND.P58"
OUT = f"{ROOT}/Assets/Sprites/Effects/Hit.png"
HIT_OFFSET = 0x87a          # 原典が足している変位

# 主人公の絵。原典は `push 0x7e90; push 0x7d00; lcall 0x68b, 0x40d6` の形で
# **2コマまとめて**渡している（5か所）。32000 / 32400 はまとめ読みの buffer 内の位置で、
# それぞれ PLAYER1.P58 / PLAYER2.P58 の先頭にあたる。
PLAYERS = [("PLAYER1", 32000, f"{ROOT}/Assets/Sprites/Effects/Player_0.png"),
           ("PLAYER2", 32400, f"{ROOT}/Assets/Sprites/Effects/Player_1.png")]

# 鍵の絵（16x16）。画面の右の欄に**本数ぶん並べて**出す（原典 0x9ff8）。
# 原典は buffer の頭から **引き算**した番地で指していて（黄 -0x40c / 赤 -0x3cb / 紫 -0x381）、
# 差が PICTURE.P58 の各絵の大きさ（65 / 74 バイト）とぴったり合う。
# つまり PICTURE は buffer の**手前**に読まれている。属性表からは一度も指されない。
KEYS = [(0, f"{ROOT}/Assets/Sprites/Hud/Key_Yellow.png"),
        (1, f"{ROOT}/Assets/Sprites/Hud/Key_Red.png"),
        (2, f"{ROOT}/Assets/Sprites/Hud/Key_Purple.png")]

def images(data):
    """(変位, 絵) の並び。"""
    out, i = [], 0
    while i + 6 <= len(data) and data[i] == data[i + 1]:
        start = i
        planes, cols, rows, i = decode(data, i)
        out.append((start, (planes, cols, rows)))
    return out

def png(path, idx, transparent):
    """色番号の配列を PNG にする。transparent の色は透過。"""
    h, w = len(idx), len(idx[0])
    raw = b""
    for y in range(h):
        row = b"\x00"
        for x in range(w):
            c = idx[y][x]
            r, g, b = GAME_PALETTE[c]
            a = 0 if c in transparent else 255
            row += bytes((r, g, b, a))
        raw += row
    def chunk(tag, body):
        c = tag + body
        return struct.pack(">I", len(body)) + c + struct.pack(">I", zlib.crc32(c))
    data = (b"\x89PNG\r\n\x1a\n"
            + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(raw))
            + chunk(b"IEND", b""))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    open(path, "wb").write(data)

def main():
    data = open(SRC, "rb").read()
    imgs = images(data)
    hit = [im for off, im in imgs if off == HIT_OFFSET]
    if not hit:
        raise SystemExit(f"変位 {HIT_OFFSET} に絵が無い: {[o for o, _ in imgs]}")
    idx = to_indices(*hit[0])
    # まわりの一色（縁から繋がっている所）を透過にする
    png(OUT, idx, {idx[0][0]})
    print(f"当たりの絵 GROUND #{[o for o, _ in imgs].index(HIT_OFFSET)}"
          f"（変位 {HIT_OFFSET} = 0x{HIT_OFFSET:x}）→ {OUT}")

    pic = images(open(f"{ROOT}/docs/original/disk/PICTURE.P58", "rb").read())
    for k, out in KEYS:
        idx = to_indices(*pic[k][1])
        png(out, idx, {idx[0][0]})      # まわりの欄の灰色は透過にする
        print(f"鍵の絵 PICTURE #{k} → {out}")

    for name, addr, out in PLAYERS:
        raw = open(f"{ROOT}/docs/original/disk/{name}.P58", "rb").read()
        im = images(raw)[0][1]
        idx = to_indices(*im)
        png(out, idx, {idx[0][0]})
        print(f"主人公の絵 {name}（変位 {addr}）→ {out}")

if __name__ == "__main__":
    main()
