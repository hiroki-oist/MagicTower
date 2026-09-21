#!/usr/bin/env python3
"""Assets/Prefab 以下のプレハブGUIDを集めて
Assets/Resources/FloorPrefabRegistry.asset を生成する。Unity不要。"""
import os,re,glob,hashlib,collections

def root_fileid(prefab_path):
    """プレハブの根本 GameObject の fileID を返す。

    Unity 2018.3 以降のプレハブでは、GameObject 型のフィールドから参照するとき
    **根本 GameObject の fileID** を書く必要がある。
    旧形式の 100100000 を書くと実行時に InvalidCastException になる。
    根本は「親を持たない Transform（m_Father が 0）」が指す GameObject。
    """
    text = open(prefab_path, encoding="utf-8", errors="replace").read()
    blocks = re.split(r"^--- !u!(\d+) &(\d+)", text, flags=re.M)
    # blocks = [前置き, 型, id, 本文, 型, id, 本文, ...]
    gos, roots = {}, []
    for i in range(1, len(blocks), 3):
        cls, fid, body = blocks[i], blocks[i+1], blocks[i+2]
        if cls == "1":
            gos[fid] = body
        elif cls in ("4", "224"):          # Transform / RectTransform
            m = re.search(r"m_Father: \{fileID: (-?\d+)\}", body)
            g = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", body)
            if m and g and m.group(1) == "0":
                roots.append(g.group(1))
    if roots:
        return roots[0]
    return next(iter(gos), None)
ROOT=os.path.expanduser("~/Code/MagicTower")
SCRIPT_GUID="8df5d129f2c0ae791cd291eb279f1d35"   # FloorPrefabRegistry.cs
# プレハブ名 -> (guid, fileID)
pref={}; guid2root={}
for meta in glob.glob(os.path.join(ROOT,"Assets/Prefab/**/*.prefab.meta"),recursive=True):
    g=re.search(r"guid: ([0-9a-f]{32})",open(meta).read()).group(1)
    name=os.path.basename(meta)[:-len(".prefab.meta")]
    pref[name]=g
    guid2root[g]=root_fileid(meta[:-len(".meta")]) or "100100000"
# .floor で使われている名前を収集
used=collections.Counter()
for p in glob.glob(os.path.join(ROOT,"Assets/Resources/Floors/Floor_*.txt")):
    t=open(p,encoding="utf-8").read(); inobj=False
    for l in t.splitlines():
        if l.startswith("objects:"): inobj=True; continue
        if inobj:
            m=re.match(r"\s*[+-]\d+ [+-]\d+\s+(\S+)",l)
            if m: used[m.group(1)]+=1
# .floor には出てこないが、できごとで盤面に現れる物体も登録する
#   Cross(ID39)     … 19階の隠れマスを壊すと現れる十字架（HiddenOccupants.txt）
#   Wing_Down(ID45) … 41階の解錠で現れる降臨の翼（SealedDoors.txt）
for _n in ("Cross", "Wing_Down"):
    used[_n]+=1

# 場面（Cutscenes.txt）の「地図 (x,y)=種別」で盤面に書き込まれる物体も登録する。
# ここを見ていなかったため **ゼノ(ID91)とバンパイア(ID95)が登録簿から漏れて**
# 3階のゼノとの初対面・20階のボス登場で姿が出なかった。
_ids={}
for _l in open(os.path.join(ROOT,"Assets/Resources/ObjectIds.txt"),encoding="utf-8"):
    _m=re.match(r"^(\S+)\s*\|\s*(\d+)\s*$",_l.strip())
    if _m: _ids["ID"+_m.group(2)]=_m.group(1)
_TERRAIN_WORDS={"床","壁","隠し壁","門","上り","下り","マグマ"}
for _l in open(os.path.join(ROOT,"Assets/Resources/Cutscenes.txt"),encoding="utf-8"):
    if _l.lstrip().startswith("#"): continue
    for _k in re.findall(r"地図\s*\([^)]*\)\s*=\s*(\S+)",_l):
        _k=_ids.get(_k,_k)
        if _k not in _TERRAIN_WORDS: used[_k]+=1
# 地形用のプレハブも登録
TERRAIN={"Wall":"Wall_Field","SecretWall":"Wall_Secret","Magma":"Magma","Floor":"road",
         "StairsUp":"Stairs_Up","StairsDown":"Stairs_Down",
         "LockYellow":"Lock_Yellow","LockPurple":"Lock_Blue","LockRed":"Lock_Red",
         "LockSealed":"Lock_Sealed","LockSpecial":"Lock_Special",
         "Void":"Tile_Void"}      # F50 の星空
entries=[]; missing=[]
for key,pname in TERRAIN.items():
    if pname in pref: entries.append((key,pref[pname]))
    else: missing.append(f"{key}->{pname}")
for key in sorted(used):
    if key in pref: entries.append((key,pref[key]))
    else: missing.append(key)
body=["%YAML 1.1","%TAG !u! tag:unity3d.com,2011:","--- !u!114 &11400000","MonoBehaviour:",
      "  m_ObjectHideFlags: 0","  m_CorrespondingSourceObject: {fileID: 0}",
      "  m_PrefabInstance: {fileID: 0}","  m_PrefabAsset: {fileID: 0}",
      "  m_GameObject: {fileID: 0}","  m_Enabled: 1","  m_EditorHideFlags: 0",
      f"  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID}, type: 3}}",
      "  m_Name: FloorPrefabRegistry","  m_EditorClassIdentifier: ","  entries:"]
for k,g in entries:
    body.append(f"  - key: {k}")
    body.append(f"    prefab: {{fileID: {guid2root.get(g, '100100000')}, guid: {g}, type: 3}}")
out=os.path.join(ROOT,"Assets/Resources/FloorPrefabRegistry.asset")
open(out,"w").write("\n".join(body)+"\n")
mg=hashlib.md5(b"MagicTower/FloorPrefabRegistry.asset").hexdigest()
open(out+".meta","w").write(f"""fileFormatVersion: 2
guid: {mg}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")
print(f"登録 {len(entries)} 件 → {out}")
if missing: print(f"★ プレハブが存在しない名前 {len(missing)}件: {sorted(missing)}")
