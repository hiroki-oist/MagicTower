#!/usr/bin/env python3
"""Java版（2001年の公式移植）の終幕の絵を Unity 用の PNG に直す。

作者の指定「エンディングは全てJava準拠にしたい」。

Java版 `MTower.class` の `eventMT` は、最上階で会話が終わると
`OpenningZeno(4)` → `OpenningZeno(5)` を呼ぶ。その中で読むのが
`data2/edata01.gif` 〜 `edata18.gif` で:

  edata01〜15 … **塔が地面へ沈んでいく15枚**（1枚ずつ描いてある）
  edata16     … 終わりの言葉（英語）    ※言語の旗 mapObject[76][121] が 0 のとき
  edata17     … 終わりの言葉（日本語）  ※旗が 0 でないとき
  edata18     … 「The End」（(C)1998-2000 Oz & Kenichi / (C)1996 N.W / See you again!）

1枚ごとの切り替えは **40x40 のマス目のモザイク**で溶かし、`twait(100)` ずつ待つ。

Unity は .gif をスプライトにできないので、ここで PNG（RGBA）に開く。
"""
import os, sys, zlib, struct, hashlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gif
from p58_to_tiles import TILE_META

ROOT = os.path.expanduser("~/Code/MagicTower")
SRC = f"{ROOT}/Assets/Images/MagicTower/data2"
OUT = f"{ROOT}/Assets/Sprites/Screens"

# 出す名前 → もとの GIF。日本語の言葉（17）を使い、英語（16）は取り込まない。
FRAMES = [(f"EndJava_{i:02d}.png", f"edata{i:02d}.gif") for i in range(1, 16)]
FRAMES += [("EndJava_Words.png", "edata17.gif"), ("EndJava_End.png", "edata18.gif")]


def write_png(path, w, h, pal, px, transparent):
    rows = []
    for row in px:
        line = bytearray()
        for v in row:
            r, g, b = pal[v]
            line += bytes((r, g, b, 0 if v == transparent else 255))
        rows.append(bytes(line))
    raw = b''.join(b'\x00' + r for r in rows)

    def chunk(kind, body):
        return (struct.pack('>I', len(body)) + kind + body
                + struct.pack('>I', zlib.crc32(kind + body) & 0xffffffff))

    out = b'\x89PNG\r\n\x1a\n'
    out += chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0))
    out += chunk(b'IDAT', zlib.compress(raw, 9))
    out += chunk(b'IEND', b'')
    open(path, 'wb').write(out)


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, src in FRAMES:
        w, h, pal, px, tr = gif.read(os.path.join(SRC, src))
        # 終幕の絵はどれも全画面の不透明な絵。透明色の指定は使わない
        # （使うと塔の輪郭が抜けてしまう）。
        path = os.path.join(OUT, name)
        write_png(path, w, h, pal, px, None)
        if not os.path.exists(path + ".meta"):
            guid = hashlib.md5(f"MagicTower/screen/{name}".encode()).hexdigest()
            open(path + ".meta", "w").write(TILE_META.format(guid=guid))
        print(f"  {name:22} ← {src}  {w}x{h}")
    print(f"{len(FRAMES)} 枚を書き出した → {OUT}")


if __name__ == "__main__":
    main()
