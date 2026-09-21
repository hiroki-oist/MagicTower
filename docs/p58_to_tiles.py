#!/usr/bin/env python3
"""原典の *.P58 を Unity の Assets/Sprites/Tiles/*.png へ差し替える。

ファイル名の先頭3桁が物体IDなので、docs/original/p58_map.txt の対応で絵を選ぶ。
.meta（GUID）はそのままなので、プレハブの参照を書き換える必要はない。
差し替え前の動画切り出しは docs/original/video_tiles/ に控えてある。

背景は透過にする。原典は物体の絵のまわりを「索引1（茶）」または「索引8（淡緑）」で
埋めていて、動画ではそこに下地が見えている（索引8を茶にすると一致度が上がることで確認）。
縁からつながっている同じ索引のところだけを透過にするので、絵の中の同色は残る。
"""
import sys, os, glob, zlib, struct
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from p58 import decode_all, to_indices, GAME_PALETTE

ROOT = os.path.expanduser("~/Code/MagicTower")

def sprites():
    """物体ID → 32x32 の色番号配列。対応は原典の属性表から作った p58_map.txt に従う。"""
    imgs = {}
    for name in ("GROUND", "OBJECT", "ITEM", "WEAPON", "MONSTER"):
        imgs[name] = [to_indices(*im)
                      for im in decode_all(open(f"{ROOT}/docs/original/disk/{name}.P58", 'rb').read())]
    out = {}
    for line in open(f"{ROOT}/docs/original/p58_map.txt", encoding="utf-8"):
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        oid, kind, where = [c.strip() for c in line.split("|")]
        if where == "-":
            continue
        f, k = where.rsplit("_", 1)
        out[int(oid)] = imgs[f][int(k)]
    return out


def anim_table():
    """動くものを VRAM に並べた順の表（原典 DS:0x80a、150件）。

    **2コマ目がどれかは、この表で隣に並んでいるか**で決まる。
    種別（8/9・10/11）で見分けると、種別が同じ組（137/138 のような後半の敵）を
    取りこぼす。
    """
    import struct
    exe = open(f"{ROOT}/docs/original/disk/MTE.EXE", "rb").read()
    dg = 4608 + 0x12340
    return [struct.unpack("<H", exe[dg + 0x80a + 2 * i:dg + 0x80a + 2 * i + 2])[0]
            for i in range(150)]


def kinds():
    """物体ID → 原典の種別。8/10 が敵の1コマ目、9/11 が2コマ目。"""
    out = {}
    for line in open(f"{ROOT}/docs/original/p58_map.txt", encoding="utf-8"):
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        oid, kind, _ = [c.strip() for c in line.split("|")]
        out[int(oid)] = int(kind)
    return out


def transparent_index(kind):
    """その種別の絵で「背景」として抜かれる色。

    絵を置き場に詰める側（原典 0xabe6）が重ね方として渡している値そのもの:
    種別 8/9（ふつうの敵）は `0x11`、種別 7（人や物）と 0xa/0xb（隣に届く敵）は `0x88`。
    上下の桁が同じなので色の索引 **1** と **8** を表していて、
    実際に全部の絵の縁を数えるとその通りになっている。
    """
    if kind in (8, 9):
        return 1
    if kind in (7, 10, 11):
        return 8
    return None


def transparent_border(px, bg=None):
    """縁からつながっている背景を透過にする。

    背景の色は種別から決まる（<see cref="transparent_index"/>）。
    分からないときだけ、**縁でいちばん多い色**を背景とみなす
    （左上の1点で決めると、そこに絵の一部が来ている絵で外してしまう。
      実際、赤スライム(ID63)は縁126点が索引1なのに左上だけ索引13で、
      そのせいで透過が付かず灰色の四角が残っていた）。
    """
    h, w = len(px), len(px[0])
    known = bg is not None
    if bg is None:
        edge = {}
        for x in range(w):
            for y in (0, h - 1):
                edge[px[y][x]] = edge.get(px[y][x], 0) + 1
        for y in range(h):
            for x in (0, w - 1):
                edge[px[y][x]] = edge.get(px[y][x], 0) + 1
        bg = max(edge, key=edge.get)
    if bg not in (1, 8):
        return None
    # 背景の色が当てずっぽうのときだけ、「少なすぎるなら背景ではない」で守る。
    # 種別から分かっているときは、絵が大きくて背景が少なくてもそのまま抜く
    # （3x3のボスの内側のマスや祭壇の真ん中は、縁にしか背景が無い）。
    if not known:
        n = sum(1 for r in px for v in r if v == bg)
        if n < h * w * 0.2:
            return None
    seen = [[False] * w for _ in range(h)]
    stack = [(0, x) for x in range(w)] + [(h - 1, x) for x in range(w)] \
          + [(y, 0) for y in range(h)] + [(y, w - 1) for y in range(h)]
    while stack:
        y, x = stack.pop()
        if not (0 <= y < h and 0 <= x < w) or seen[y][x] or px[y][x] != bg:
            continue
        seen[y][x] = True
        stack += [(y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)]
    return seen


def write_rgba(path, px, mask):
    rows = []
    for y, row in enumerate(px):
        line = bytearray()
        for x, v in enumerate(row):
            r, g, b = GAME_PALETTE[v]
            a = 0 if (mask and mask[y][x]) else 255
            line += bytes((r, g, b, a))
        rows.append(bytes(line))
    raw = b''.join(b'\x00' + r for r in rows)

    def chunk(kind, body):
        return (struct.pack('>I', len(body)) + kind + body
                + struct.pack('>I', zlib.crc32(kind + body) & 0xffffffff))

    out = b'\x89PNG\r\n\x1a\n'
    out += chunk(b'IHDR', struct.pack('>IIBBBBB', len(px[0]), len(px), 8, 6, 0, 0, 0))
    out += chunk(b'IDAT', zlib.compress(raw, 9))
    out += chunk(b'IEND', b'')
    open(path, 'wb').write(out)


COMPOSITE = {900: (115, 3, 3), 901: (97, 3, 3), 902: (15, 3, 1)}


def composite(sp, base, cols, rows):
    """3x3 のボスなどを1枚に組み立てる。IDは左上から横に並んでいる。"""
    out = [[0] * (32 * cols) for _ in range(32 * rows)]
    for r in range(rows):
        for c in range(cols):
            src = sp[base + r * cols + c]
            for y in range(32):
                for x in range(32):
                    out[r * 32 + y][c * 32 + x] = src[y][x]
    return out


# 書き出すタイルの一覧は `docs/tile_list.txt`（「物体ID | 名前」）。
# 動画から切り出した切り出しが手元に無くても、**この一覧と原典の *.P58 だけで
# 一式を作り直せる**ようにしてある（`docs/rebuild_assets.sh`）。


def tile_list():
    """docs/tile_list.txt を読む。→ {(ID, 名前): GUID}。2コマ目（_b）も含む。"""
    out = {}
    for line in open(f"{ROOT}/docs/tile_list.txt", encoding="utf-8"):
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        cols = [c.strip() for c in line.split("|")]
        out[(int(cols[0]), cols[1])] = cols[2] if len(cols) > 2 else ""
    return out


def guid_for(oid, name):
    """控えてある GUID。無ければ名前から作る（新しく足す絵のとき）。"""
    import hashlib
    g = tile_list().get((oid, name), "")
    return g or hashlib.md5(f"MagicTower/tile/{oid}".encode()).hexdigest()


def write_meta_if_missing(path, oid):
    """.meta が無いときだけ作る。**既にあるものは触らない**（GUIDが変わると参照が切れる）。

    GUID は `docs/tile_list.txt` に控えてあるものを使う。`.meta` はリポジトリに
    入れていないので、控えが無いと作り直したときに参照が全部切れる。
    """
    if os.path.exists(path + ".meta"):
        return
    name = os.path.basename(path)[:-4].split("_", 1)[1]
    open(path + ".meta", "w").write(TILE_META.format(guid=guid_for(oid, name)))


def write_missing(sp, ks_all):
    """一覧にあってファイルが無いものを作る（.meta も添える）。"""
    os.makedirs(f"{ROOT}/Assets/Sprites/Tiles", exist_ok=True)
    made = []
    for (oid, name) in sorted(tile_list()):
        if name.endswith("_b"):
            continue                       # 2コマ目は下の道で作る
        path = f"{ROOT}/Assets/Sprites/Tiles/{oid:03d}_{name}.png"
        if os.path.exists(path):
            continue
        if oid in COMPOSITE:
            base, cols, rows = COMPOSITE[oid]
            px = composite(sp, base, cols, rows)
            ks_all[oid] = ks_all.get(base)
        elif oid in sp:
            px = sp[oid]
        else:
            continue
        write_rgba(path, px, transparent_border(px, transparent_index(ks_all.get(oid))))
        write_meta_if_missing(path, oid)
        made.append(os.path.basename(path))
    return made


TILE_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 0
  lightmap: 0
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def main():
    sp = sprites()
    ks_all = kinds()
    made = write_missing(sp, ks_all)
    if made:
        print(f"できごと用に {len(made)} 枚を新たに書き出した: {made}")
    done = skipped = 0
    for p in sorted(glob.glob(f"{ROOT}/Assets/Sprites/Tiles/*.png")):
        name = os.path.basename(p)[:-4]
        oid = int(name[:3])
        if oid in COMPOSITE:
            base, cols, rows = COMPOSITE[oid]
            px = composite(sp, base, cols, rows)
            ks_all[oid] = ks_all.get(base)
        elif oid in sp:
            px = sp[oid]
        else:
            skipped += 1
            continue
        write_rgba(p, px, transparent_border(px, transparent_index(ks_all.get(oid))))
        done += 1
    # 2コマ目（敵と人の次のID）。**原典はこれを実際に描く** — 0x92a4 が盤面を
    # 5マスずつなめて、種別 7/8/10 のマスに「表 DS:0x80a の並びで次の絵」を貼り、
    # 121マスなめ終わるたびに 1コマ目／2コマ目を入れ替える。
    #
    # 以前ここに「原典のこのビルドは一度も描かない」と書いていたのは誤り。
    # 属性表の「絵の位置」を引く箇所だけを調べていたが、0x92a4 は属性表を使わず、
    # **表 DS:0x80a で引いた番号から VRAM の並び位置を計算して**貼るので見落としていた。
    ks = kinds()
    frames = 0
    # 3x3 のボスはマス単位では2コマ目が **+9**（大ダコ 97〜105↔106〜114、
    # ドラゴン 115〜123↔124〜132）。1マスずつの +1 は隣のマスになってしまうので、
    # 組み立てた1枚のほうだけを作る。
    boss_parts = set()
    for base, cols, rows in COMPOSITE.values():
        boss_parts.update(range(base, base + cols * rows))

    tbl = anim_table()
    tbl_index = {v: i for i, v in enumerate(tbl)}
    # 盤面に出る物（タイルがある物）は「誰かの2コマ目」ではない
    tbl_placed = {int(os.path.basename(q)[:3])
                  for q in glob.glob(f"{ROOT}/Assets/Sprites/Tiles/*.png")
                  if not os.path.basename(q)[:-4].endswith("_b")}

    for p in sorted(glob.glob(f"{ROOT}/Assets/Sprites/Tiles/*.png")):
        name = os.path.basename(p)[:-4]
        if name.endswith("_b"):
            continue
        oid = int(name[:3])
        if oid in COMPOSITE:
            # 組み立てた絵の2コマ目は、**マスごとに表の隣を引いて**作る。
            # 3x3のボスは全マスが表に載っていて隣は +9。祭壇は真ん中(ID16)だけが
            # 表に載っていて隣は ID18（＝口の開き方が違う同じ絵）。
            base, cols, rows = COMPOSITE[oid]
            parts = []
            changed = False
            for k in range(cols * rows):
                pid = base + k
                nb = pid
                if pid in tbl_index and tbl_index[pid] + 1 < len(tbl):
                    cand = tbl[tbl_index[pid] + 1]
                    if cand in sp and cand not in tbl_placed:
                        nb = cand
                        changed = True
                if nb not in sp:
                    parts = []
                    break
                parts.append(nb)
            if not parts or not changed:
                continue
            px = [[0] * (32 * cols) for _ in range(32 * rows)]
            for r in range(rows):
                for c in range(cols):
                    src = sp[parts[r * cols + c]]
                    for y in range(32):
                        for x in range(32):
                            px[r * 32 + y][c * 32 + x] = src[y][x]
            out = f"{ROOT}/Assets/Sprites/Tiles/{oid:03d}_{name[4:]}_b.png"
            write_rgba(out, px, transparent_border(px))
            write_meta_if_missing(out, oid)
            frames += 1
            continue
        if oid in boss_parts:
            continue
        # 2コマ目は「並べた表で隣」。種別では見分けられない組がある（137/138 など）。
        if oid not in tbl_index:
            continue
        i = tbl_index[oid]
        if i + 1 >= len(tbl):
            continue
        nxt = tbl[i + 1]
        if nxt not in sp or nxt in tbl_placed:
            continue          # 隣が盤面に出る物なら、それは対ではなく別の物
        px = sp[nxt]
        out = f"{ROOT}/Assets/Sprites/Tiles/{nxt:03d}_{name[4:]}_b.png"
        write_rgba(out, px, transparent_border(px))
        write_meta_if_missing(out, nxt)
        frames += 1
    print(f"差し替え {done} 枚 / 対応なし {skipped} 枚 / 2コマ目 {frames} 枚")


if __name__ == "__main__":
    main()
