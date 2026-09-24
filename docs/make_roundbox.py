#!/usr/bin/env python3
"""角の丸い四角（9スライス用）を書き出す。外部の道具は使わない。

課金の窓の選択肢を**角丸**で囲むのに要る（作者の指定）。
白一色で書き出して、色は Unity 側で掛ける（枠と地で色を変えるため）。

  Assets/Sprites/Hud/RoundBox.png   32x32・角の半径8・9スライスの縁8
"""
import os, zlib, struct, hashlib

ROOT = os.path.expanduser("~/Code/MagicTower")
OUT = f"{ROOT}/Assets/Sprites/Hud/RoundBox.png"
N, R = 32, 8

META = """fileFormatVersion: 2
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
  textureType: 8
  textureShape: 1
  spriteMode: 1
  spritePixelsToUnits: 32
  spriteBorder: {{x: {r}, y: {r}, z: {r}, w: {r}}}
  spritePivot: {{x: 0.5, y: 0.5}}
  alphaIsTransparency: 1
  npotScale: 0
  isReadable: 0
  streamingMipmaps: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def coverage(x, y):
    """その点が角丸の内側にどれだけ入っているか（0〜1）。端は少しなめらかに。"""
    cx = R - 0.5 if x < R else (N - R - 0.5 if x >= N - R else x)
    cy = R - 0.5 if y < R else (N - R - 0.5 if y >= N - R else y)
    dx, dy = x - cx, y - cy
    d = (dx * dx + dy * dy) ** 0.5
    if d <= R - 1.0: return 1.0
    if d >= R: return 0.0
    return R - d


def main():
    rows = []
    for y in range(N):
        line = bytearray()
        for x in range(N):
            a = int(round(255 * coverage(x, y)))
            line += bytes((255, 255, 255, a))
        rows.append(bytes(line))
    raw = b''.join(b'\x00' + r for r in rows)

    def chunk(kind, body):
        return (struct.pack('>I', len(body)) + kind + body
                + struct.pack('>I', zlib.crc32(kind + body) & 0xffffffff))

    png = b'\x89PNG\r\n\x1a\n'
    png += chunk(b'IHDR', struct.pack('>IIBBBBB', N, N, 8, 6, 0, 0, 0))
    png += chunk(b'IDAT', zlib.compress(raw, 9))
    png += chunk(b'IEND', b'')
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    open(OUT, 'wb').write(png)
    guid = hashlib.md5(b"MagicTower/hud/RoundBox.png").hexdigest()
    open(OUT + ".meta", "w").write(META.format(guid=guid, r=R))
    print(f"{OUT}  {N}x{N} 角の半径{R}")


if __name__ == "__main__":
    main()
