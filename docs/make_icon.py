#!/usr/bin/env python3
"""アプリのアイコンの候補を作る（1024x1024）。外部の道具は使わない。

作者の指定「塔の絵に魔法の塔って書いてくれればいい」。素材はどれも原典のもの:
  Title_Logo.png    … MTMOJI.P58 から切り出した「魔法の塔」の看板
  Ending_Tower1.png … MTOWER1.P58（終幕の塔）
  EndJava_01.png    … Java版の終幕の1枚目（夜の荒野に立つ塔）
  Hud/Brick.png     … LCG.P58 の枠の石積み

書き出し先: docs/icon_candidates/Icon_1..4.png
選んだら docs/make_icon.py の PICK を変えて `--install` で
Assets/Sprites/Icon.png に入れ、Unity の Player Settings へ差す。
"""
import os, sys, zlib, struct

ROOT = os.path.expanduser("~/Code/MagicTower")
SRC = f"{ROOT}/Assets/Sprites/Screens"
HUD = f"{ROOT}/Assets/Sprites/Hud"
OUT = f"{ROOT}/docs/icon_candidates"
N = 1024


def read_rgba(path):
    d = open(path, 'rb').read()
    pos, w, h, idat = 8, None, None, b''
    while pos < len(d):
        ln = struct.unpack('>I', d[pos:pos + 4])[0]
        typ = d[pos + 4:pos + 8]
        body = d[pos + 8:pos + 8 + ln]
        if typ == b'IHDR':
            w, h, bd, ct, _, _, il = struct.unpack('>IIBBBBB', body)
            assert bd == 8 and ct == 6 and il == 0, path
        elif typ == b'IDAT':
            idat += body
        pos += 12 + ln
    raw = zlib.decompress(idat)
    stride = w * 4
    out = []
    prev = bytearray(stride)
    p = 0
    for _ in range(h):
        f = raw[p]; p += 1
        line = bytearray(raw[p:p + stride]); p += stride
        if f == 1:
            for i in range(4, stride): line[i] = (line[i] + line[i - 4]) & 255
        elif f == 2:
            for i in range(stride): line[i] = (line[i] + prev[i]) & 255
        elif f == 3:
            for i in range(stride):
                a = line[i - 4] if i >= 4 else 0
                line[i] = (line[i] + ((a + prev[i]) >> 1)) & 255
        elif f == 4:
            for i in range(stride):
                a = line[i - 4] if i >= 4 else 0
                b = prev[i]
                c = prev[i - 4] if i >= 4 else 0
                pp = a + b - c
                pa, pb, pc = abs(pp - a), abs(pp - b), abs(pp - c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[i] = (line[i] + pr) & 255
        out.append(bytes(line))
        prev = line
    return w, h, out


def write_rgba(path, w, h, rows):
    raw = b''.join(b'\x00' + r for r in rows)

    def chunk(kind, body):
        return (struct.pack('>I', len(body)) + kind + body
                + struct.pack('>I', zlib.crc32(kind + body) & 0xffffffff))

    out = b'\x89PNG\r\n\x1a\n'
    out += chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0))
    out += chunk(b'IDAT', zlib.compress(raw, 9))
    out += chunk(b'IEND', b'')
    open(path, 'wb').write(out)


def blank(color):
    r, g, b = color
    row = bytes((r, g, b, 255)) * N
    return [bytearray(row) for _ in range(N)]


def blit(dst, src, sw, sh, srows, x0, y0, scale, tile=False):
    """近傍で整数倍に拡大して貼る（点の絵なので滑らかにしない）。"""
    for y in range(int(sh * scale)):
        ty = y0 + y
        if not tile and not (0 <= ty < N): continue
        sy = int(y / scale)
        srow = srows[sy]
        drow = dst[ty % N]
        for x in range(int(sw * scale)):
            tx = x0 + x
            if not tile and not (0 <= tx < N): continue
            sx = int(x / scale) * 4
            a = srow[sx + 3]
            if a == 0: continue
            d = (tx % N) * 4
            drow[d] = srow[sx]; drow[d + 1] = srow[sx + 1]; drow[d + 2] = srow[sx + 2]
            drow[d + 3] = 255


def tile_bg(dst, path, scale):
    w, h, rows = read_rgba(path)
    step = int(w * scale)
    for y in range(0, N, int(h * scale)):
        for x in range(0, N, step):
            blit(dst, None, w, h, rows, x, y, scale, tile=True)


def crop(w, h, rows, x0, y0, cw, ch):
    return cw, ch, [bytearray(r[x0 * 4:(x0 + cw) * 4]) for r in rows[y0:y0 + ch]]


def cut_letters(img):
    """
    看板（Title_Logo.png）は**文字が透明で地が不透明**（#444444）に作ってある
    （題名画面では石積みを文字から透かすため）。アイコンでは逆に**文字だけ**が
    要るので、透明・不透明を入れ替える。
    """
    w, h, rows = img
    out = []
    for r in rows:
        line = bytearray(r)
        for i in range(0, len(line), 4):
            line[i + 3] = 0 if line[i + 3] else 255
        out.append(line)
    return w, h, out


# 看板のうち「魔法の塔」の4文字だけの高さ（下の英字の行を落とす）。
KANJI_H = 80

NIGHT = (0, 0, 34)          # 終幕のパレットの索引0
STONE = (0x44, 0x44, 0x44)  # 原典の地の灰


def candidates():
    logo = cut_letters(read_rgba(f"{SRC}/Title_Logo.png"))   # 300x104（文字だけ）
    tower = read_rgba(f"{SRC}/Ending_Tower1.png")      # 320x400（周りは透明）
    java = read_rgba(f"{SRC}/EndJava_01.png")          # 640x400

    made = []

    # ① 石積みの地に看板だけ（題名画面と同じ組み方）
    a = blank(STONE)
    tile_bg(a, f"{HUD}/Brick.png", 4)
    blit(a, None, logo[0], logo[1], logo[2], (N - 300 * 3) // 2, (N - 104 * 3) // 2, 3)
    made.append(("Icon_1_看板.png", a))

    # ② 夜の荒野に立つ塔（Java版の終幕の1枚目）を正方に切って、下に看板
    b = blank(NIGHT)
    jw, jh, jr = java
    # 640x400 を 1024 幅に伸ばして上に置く（塔の頭から地面まで入る）
    blit(b, None, jw, jh, jr, 0, 40, N / float(jw))
    blit(b, None, logo[0], logo[1], logo[2], (N - 300 * 3) // 2, N - 104 * 3 - 70, 3)
    made.append(("Icon_2_夜の塔.png", b))

    # ③ 原典の塔（MTOWER1）を大きく置いて、下に看板
    c = blank(NIGHT)
    tw, th, tr = tower
    blit(c, None, tw, th, tr, (N - 320 * 2) // 2, -40, 2)
    blit(c, None, logo[0], logo[1], logo[2], (N - 300 * 3) // 2, N - 104 * 3 - 40, 3)
    made.append(("Icon_3_塔と看板.png", c))

    # ④ 石積みの地に塔と看板（①と③の合わせ）
    d = blank(STONE)
    tile_bg(d, f"{HUD}/Brick.png", 4)
    blit(d, None, tw, th, tr, (N - 320 * 2) // 2, 40, 2)
    blit(d, None, logo[0], logo[1], logo[2], (N - 300 * 3) // 2, N - 104 * 3 - 40, 3)
    made.append(("Icon_4_石積みと塔.png", d))

    # ⑤⑥ 英字の行を落として「魔法の塔」の4文字だけ大きく使う。
    #     アイコンは 60 ドットまで縮むので、小さい英字は必ず潰れる。
    kw, kh, kr = crop(logo[0], logo[1], logo[2], 0, 0, 300, KANJI_H)

    e = blank(NIGHT)
    blit(e, None, jw, jh, jr, 0, 0, N / float(jw))
    blit(e, None, kw, kh, kr, (N - 300 * 3) // 2, N - KANJI_H * 3 - 90, 3)
    made.append(("Icon_5_夜の塔_文字大.png", e))

    f = blank(STONE)
    tile_bg(f, f"{HUD}/Brick.png", 4)
    blit(f, None, kw, kh, kr, (N - 300 * 3) // 2, (N - KANJI_H * 3) // 2, 3)
    made.append(("Icon_6_看板_文字大.png", f))

    return made


# ★作者が選んだ案（2026-09-24）。`--install` でここの1枚を Assets へ入れる。
PICK = "Icon_5_夜の塔_文字大.png"
INSTALL = f"{ROOT}/Assets/Sprites/Icon.png"

ICON_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
  textureType: 0
  textureShape: 1
  spriteMode: 0
  alphaIsTransparency: 0
  npotScale: 0
  isReadable: 1
  streamingMipmaps: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def main():
    os.makedirs(OUT, exist_ok=True)
    made = candidates()
    for name, rows in made:
        write_rgba(os.path.join(OUT, name), N, N, rows)
        print(" ", name)
    print(f"{OUT} に書き出した")
    if "--install" in sys.argv:
        import hashlib
        rows = dict(made)[PICK]
        write_rgba(INSTALL, N, N, rows)
        meta = INSTALL + ".meta"
        if not os.path.exists(meta):
            guid = hashlib.md5(b"MagicTower/Icon.png").hexdigest()
            open(meta, "w").write(ICON_META.format(guid=guid))
        print(f"{PICK} → {INSTALL}")


if __name__ == "__main__":
    main()
