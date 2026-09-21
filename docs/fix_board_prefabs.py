#!/usr/bin/env python3
"""盤面に置くプレハブを **1マス = 原典のタイル1枚** に揃える。Unity不要。

作者の手で作られた古いプレハブは、旧アトラス（`Assets/Images/MTower.gif`、
1枚 0.4ユニット）を前提に **`m_LocalScale: 2.5` / `m_Size: 0.4`** で組まれていた
（0.4 x 2.5 = 1マス）。原典から書き出したタイル（32px・PPU32 = 1ユニット）へ
絵だけ差し替えると **1 x 2.5 = 2.5マス** になり、黄色の鍵・赤いクリスタル・檻が
「バカデカい」状態になる。

ここで直すのは3つ:
  * 絵を原典のタイル（`Assets/Sprites/Tiles/NNN_*.png`）に向ける
  * `m_LocalScale` と `m_Size` を 1 にする（32px / PPU32 = ちょうど1マス）
  * **Animator を外す**

Animator は敵と人の36個に付いていて、`Assets/Images/CharMoveAnim/*.anim` が
SpriteRenderer の `m_Sprite` を毎コマ書き換える。差し替え先は**旧アトラス**の切り出しで、
そちらは **PPU 100**（32px なら 0.32ユニット）。旧尺度 2.5 のときは 0.8マスで
それらしく見えていたが、尺度を1に直した途端に **0.32マス＝極端に小さく**なった。
2コマの動きは原典どおりの仕掛け（`MonsterAnimator`、原典 0x92a4）が受け持っているので、
このクリップは要らない。外すことで**旧アトラスへの参照も完全に無くなる**。

3x3のボスと祭壇、および `docs/make_prefabs.py` が作るプレハブは触らない
（あちらは初めから 尺度1 で作ってある）。
"""
import os,re,glob,hashlib

ROOT=os.path.expanduser("~/Code/MagicTower")
TILES=os.path.join(ROOT,"Assets/Sprites/Tiles")

# 地形のプレハブ名 → 原典の物体ID（ObjectIds.txt に載らないもの）
TERRAIN_ID={
 "Lock_Yellow":1, "Lock_Blue":2, "Lock_Red":3, "Lock_Sealed":4, "Lock_Special":5,
 "road":6, "Wall_Field":8, "Wall_Secret":9,
 "Stairs_Up":11, "Stairs_Down":12, "Magma":13,
}

def object_ids():
    out={}
    for l in open(os.path.join(ROOT,"Assets/Resources/ObjectIds.txt"),encoding="utf-8"):
        m=re.match(r"^(\S+)\s*\|\s*(\d+)\s*$",l.strip())
        if m: out.setdefault(m.group(1),int(m.group(2)))
    return out

def tile_guids():
    out={}
    for f in sorted(os.listdir(TILES)):
        if not f.endswith(".png.meta"): continue
        name=f[:-len(".png.meta")]
        if name.endswith("_b"): continue          # 2コマ目（大文字小文字は区別する）
        m=re.match(r"^(\d+)_",name)
        if not m: continue
        g=re.search(r"guid: ([0-9a-f]{32})",open(os.path.join(TILES,f)).read())
        if g: out.setdefault(int(m.group(1)),(name,g.group(1)))
    return out

# 生成器（docs/make_prefabs.py）が作るぶんは触らない。あちらは初めから尺度1。
GENERATED=set(re.findall(r'\("(?:Item|Wall|Enemy|FieldObstacle)","([^"]+)"',
                         open(os.path.join(ROOT,"docs/make_prefabs.py"),
                              encoding="utf-8").read()))

def strip_animator(text):
    """Animator の部品と、GameObject 側の参照を取り除く。"""
    m=re.search(r"^--- !u!95 &(\d+)\nAnimator:\n(?:.*\n)*?(?=^--- !u!|\Z)",text,re.M)
    if not m: return text
    fid=m.group(1)
    text=text[:m.start()]+text[m.end():]
    text=re.sub(r"^  - component: \{fileID: "+fid+r"\}\n","",text,flags=re.M)
    return text

def main():
    ids=object_ids(); tiles=tile_guids()
    changed=[]; skipped=[]
    for p in sorted(glob.glob(os.path.join(ROOT,"Assets/Prefab/**/*.prefab"),recursive=True)):
        name=os.path.basename(p)[:-len(".prefab")]
        if name in GENERATED: continue
        oid=TERRAIN_ID.get(name, ids.get(name))
        if oid is None or oid not in tiles:
            skipped.append(name); continue
        tname,tguid=tiles[oid]
        t=open(p,encoding="utf-8").read(); before=t
        t=strip_animator(t)
        t=re.sub(r"m_Sprite: \{fileID: -?\d+, guid: [0-9a-f]{32}, type: 3\}",
                 f"m_Sprite: {{fileID: 21300000, guid: {tguid}, type: 3}}", t)
        t=re.sub(r"m_LocalScale: \{x: [\d.-]+, y: [\d.-]+, z: [\d.-]+\}",
                 "m_LocalScale: {x: 1, y: 1, z: 1}", t)
        for k in ("m_Size","oldSize","newSize"):
            t=re.sub(k+r": \{x: [\d.-]+, y: [\d.-]+\}", k+": {x: 1, y: 1}", t)
        if t!=before:
            open(p,"w",encoding="utf-8").write(t); changed.append((name,tname))
    print(f"{len(changed)} 個を原典のタイル・尺度1・Animator無しに揃えた")
    for n,t in changed: print(f"  {n:26s} → {t}")
    if skipped: print(f"\n対応するタイルが無くて触らなかったもの {len(skipped)}件: {sorted(skipped)}")

if __name__=="__main__": main()
