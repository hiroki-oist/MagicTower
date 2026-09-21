#!/usr/bin/env python3
"""原典の属性表から「物体ID → どの絵か」を書き出す。

原典の地図描画（MTE.EXE 0x00ac2c〜）はこう引いている:
    bx = 物体ID * 6
    種別   = 属性表[bx + 0x282]   （1バイト）
    絵の位置 = 属性表[bx + 0x286]  （ワード。絵をまとめて読んだ buffer 内の変位）
buffer のどこに何を読むかは **DS:0x240 のファイル表**（1件6バイト = 名前の位置 /
名前のセグメント / 置き場）に書いてある。読み込みの繰り返しは 0x6e70〜0x6e97。
  LCG=0（別の buffer）/ GROUND=0 / OBJECT=8000 / ITEM=18000 / WEAPON=24500
  / PLAYER1=32000 / PLAYER2=32400 / MONSTER=34000 / PICTURE=64500
表の値と各ファイルの絵の開始位置が全件一致することも確かめてある
（GROUND 14/14・OBJECT 14/14・ITEM 22/22・WEAPON 10/10・MONSTER 98/98）。
"""
import os, sys, struct
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from p58 import decode

ROOT = os.path.expanduser("~/Code/MagicTower")
DISK = f"{ROOT}/docs/original/disk"
def file_table():
    """DS:0x240 のファイル表を読む。→ {ファイル名: 置き場}"""
    d = open(f"{DISK}/MTE.EXE", 'rb').read()
    b = 4608 + 0x12340
    body = d[b:]
    out = {}
    for i in range(12):
        o = 0x240 + 6 * i
        noff, nseg, boff = struct.unpack('<HHH', d[b + o:b + o + 6])
        if boff == 0xffff or nseg == 0:
            continue
        e = body.index(b'\0', noff)
        name = body[noff:e].decode('ascii', 'replace')
        if name.endswith(".P58"):
            out[name[:-4]] = boff
    return out


BASE = {k: v for k, v in file_table().items()
        if k in ("GROUND", "OBJECT", "ITEM", "WEAPON", "MONSTER")}
LAST_ID = 158          # これより先は表が壊れている（種別が範囲外）


def attr_table():
    d = open(f"{DISK}/MTE.EXE", 'rb').read()
    b = 4608 + 0x12340
    out = []
    for i in range(LAST_ID + 1):
        kind = d[b + 0x282 + 6 * i]
        off = struct.unpack('<H', d[b + 0x286 + 6 * i:b + 0x286 + 6 * i + 2])[0]
        out.append((kind, off))
    return out


def image_starts():
    s = {}
    for n in BASE:
        data = open(f"{DISK}/{n}.P58", 'rb').read()
        i = 0; lst = []
        while i + 6 <= len(data) and data[i] == data[i + 1]:
            lst.append(i)
            _, _, _, i = decode(data, i)
        s[n] = lst
    return s


def build():
    tab = attr_table(); starts = image_starts()
    rows = []
    for oid, (kind, off) in enumerate(tab):
        hit = None
        for n, base in BASE.items():
            if off - base in starts[n]:
                hit = (n, starts[n].index(off - base)); break
        rows.append((oid, kind, hit))
    return rows


if __name__ == "__main__":
    rows = build()
    out = [
        "# 物体ID → 原典の絵  （MTE.EXE の属性表 DS:0x282 から機械的に作った）",
        "#",
        "#   種別 … 原典の属性。1=鍵の扉 / 2=門 / 3=通れる / 4=壁 / 5=特別 / 7=人や物 など",
        "#   絵   … *.P58 の何枚目か。同じ絵を指すIDがある（ID7は床、ID9/10は壁と同じ）",
        "#   人や敵は **IDが2つで1体**（2コマのアニメーション）",
        "#",
        "# 形式: 物体ID | 種別 | 絵",
    ]
    for oid, kind, hit in rows:
        where = f"{hit[0]}_{hit[1]:03d}" if hit else "-"
        out.append(f"{oid:3d} | {kind:2d} | {where}")
    open(f"{ROOT}/docs/original/p58_map.txt", "w", encoding="utf-8").write("\n".join(out) + "\n")
    print(f"{len(rows)} 件書き出し（絵の付かないID {sum(1 for _,_,h in rows if not h)} 件）")
