#!/usr/bin/env python3
"""扉が開く動きの絵を原典 LCG.P58 から書き出す。Unity不要。

原典 `0x07be6`（鍵で扉を開けたとき `0x023ee` から呼ばれる）は
**9コマの動き**を扉のマスに1コマずつ貼る。その絵が LCG.P58 の #2〜#5 で、
どれも 32x288 ＝ **32x32 が縦に9つ**。色を盤面のタイルと突き合わせて

    #2 … 001_Lock_Yellow（黄色の扉）
    #3 … 002_Lock_Purple（紫の扉）
    #4 … 003_Lock_Red   （赤の扉）
    #5 … 004_Lock_Sealed（門・封印扉）

と確定した（原典が渡す色の番号 1/2/3 が 黄/紫/赤）。

書き出し先は `Assets/Sprites/Doors/<色>_<コマ>.png`。
扉の絵はマスを埋めて描かれる（原典も透過なしで貼る）ので、**抜かない**。
"""
import sys, os, hashlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from p58 import decode_all, to_indices
from p58_to_tiles import write_rgba

ROOT = os.path.expanduser("~/Code/MagicTower")
OUT = os.path.join(ROOT, "Assets/Sprites/Doors")

# LCG.P58 の何枚目 → 色の名前（原典の色番号もあわせて控える）
STRIPS = [(2, "Yellow", 1), (3, "Purple", 2), (4, "Red", 3), (5, "Sealed", None)]

FRAMES = 9          # 原典 0x7be6 の輪は 0x48 から 0xa00 きざみで 0x5a48 まで＝9回
CELL = 32

META = """fileFormatVersion: 2
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


# LCG #6（480x192）は **20階のボスが現れる動き**。原典 0x03044（ボス登場）が
# 作業域 0x3c00 に敷いてから、96x96（＝3x3マス）を **横5 x 縦2 の10コマ**として
# 盤面の中央（VRAM 0x2fa2 ＝ 左上のマスが (-1,+1)）へ順に貼る。
# 1コマごとに 0x19(25) 待つので、全部で 250/60 ≒ 4.2秒。
BOSS_STRIP = 6
BOSS_CELL = 96
BOSS_COLS, BOSS_ROWS = 5, 2


def boss_frames(imgs):
    px = to_indices(*imgs[BOSS_STRIP])
    h, w = len(px), len(px[0])
    if (w, h) != (BOSS_CELL * BOSS_COLS, BOSS_CELL * BOSS_ROWS):
        raise SystemExit(f"★ LCG #{BOSS_STRIP} の大きさが {w}x{h}")
    out = []
    for r in range(BOSS_ROWS):
        for c in range(BOSS_COLS):
            out.append([row[c * BOSS_CELL:(c + 1) * BOSS_CELL]
                        for row in px[r * BOSS_CELL:(r + 1) * BOSS_CELL]])
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    imgs = decode_all(open(f"{ROOT}/docs/original/disk/LCG.P58", "rb").read())
    n = 0
    for idx, name, _color in STRIPS:
        px = to_indices(*imgs[idx])
        h, w = len(px), len(px[0])
        if (w, h) != (CELL, CELL * FRAMES):
            raise SystemExit(f"★ LCG #{idx} の大きさが {w}x{h}（{CELL}x{CELL*FRAMES} のはず）")
        for f in range(FRAMES):
            frame = [row[:] for row in px[f * CELL:(f + 1) * CELL]]
            path = os.path.join(OUT, f"{name}_{f}.png")
            write_rgba(path, frame, None)          # 扉はマスを埋めるので抜かない
            guid = hashlib.md5(f"MagicTower/door/{name}/{f}".encode()).hexdigest()
            open(path + ".meta", "w").write(META.format(guid=guid))
            n += 1
    # 20階のボスが現れる動き（LCG #6）
    eff = os.path.join(ROOT, "Assets/Sprites/Effects")
    os.makedirs(eff, exist_ok=True)
    for i, frame in enumerate(boss_frames(imgs)):
        path = os.path.join(eff, f"BossAppear_{i}.png")
        write_rgba(path, frame, None)
        guid = hashlib.md5(f"MagicTower/boss-appear/{i}".encode()).hexdigest()
        open(path + ".meta", "w").write(META.format(guid=guid))
        n += 1
    print(f"{n} 枚を書き出した（扉36枚＋ボス登場10枚） → {OUT} / {eff}")


if __name__ == "__main__":
    main()
