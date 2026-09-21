#!/usr/bin/env python3
"""Floor_N.unity から PrefabInstance を抽出して JSON 化する。Unity 不要。"""
import re, json, os, sys, glob

ROOT = os.path.expanduser("~/Code/MagicTower")
PREFAB_DIR = os.path.join(ROOT, "Assets/Prefab")
SCENE_DIR  = os.path.join(ROOT, "Assets/Scenes")

# guid -> プレハブ名
guid2name = {}
for meta in glob.glob(os.path.join(PREFAB_DIR, "**/*.prefab.meta"), recursive=True):
    m = re.search(r"guid:\s*([0-9a-f]{32})", open(meta).read())
    if m:
        guid2name[m.group(1)] = os.path.basename(meta)[:-len(".prefab.meta")]

MOD_RE = re.compile(
    r"- target: \{fileID: (-?\d+), guid: ([0-9a-f]{32}), type: \d+\}\s*\n"
    r"\s*propertyPath: (\S+)\s*\n"
    r"\s*value: (.*?)\s*\n", re.M)
SRC_RE = re.compile(r"m_SourcePrefab: \{fileID: \d+, guid: ([0-9a-f]{32})")

WANTED = {"m_Name", "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z",
          "m_Size.x", "m_Size.y", "m_SpriteTilingProperty.newSize.x",
          "m_SpriteTilingProperty.newSize.y", "m_IsActive"}

def parse_scene(path):
    text = open(path, encoding="utf-8", errors="replace").read()
    # PrefabInstance ブロックで分割
    blocks = re.split(r"^--- !u!1001 &\d+\s*$", text, flags=re.M)[1:]
    out = []
    for b in blocks:
        # 次のドキュメント区切りまで
        b = re.split(r"^--- !u!", b, flags=re.M)[0]
        src = SRC_RE.search(b)
        if not src:
            continue
        prefab = guid2name.get(src.group(1), "UNKNOWN:" + src.group(1)[:8])
        props = {}
        for _fid, _g, pp, val in MOD_RE.findall(b):
            if pp in WANTED:
                props.setdefault(pp, val)
        def num(k, d=None):
            v = props.get(k)
            if v is None: return d
            try: return float(v)
            except ValueError: return d
        rec = {
            "prefab": prefab,
            "name": props.get("m_Name", prefab),
            "x": num("m_LocalPosition.x"),
            "y": num("m_LocalPosition.y"),
            "z": num("m_LocalPosition.z"),
        }
        sx = num("m_Size.x") or num("m_SpriteTilingProperty.newSize.x")
        sy = num("m_Size.y") or num("m_SpriteTilingProperty.newSize.y")
        if sx is not None: rec["w"] = sx
        if sy is not None: rec["h"] = sy
        if props.get("m_IsActive") == "0": rec["inactive"] = True
        out.append(rec)
    return out

if __name__ == "__main__":
    os.makedirs(os.path.join(SP := sys.argv[1], "floors"), exist_ok=True)
    summary = []
    for n in range(1, 51):
        p = os.path.join(SCENE_DIR, f"Floor_{n}.unity")
        if not os.path.exists(p): continue
        objs = parse_scene(p)
        json.dump(objs, open(os.path.join(SP, "floors", f"Floor_{n}.json"), "w"),
                  ensure_ascii=False, indent=1)
        kinds = {}
        for o in objs: kinds[o["prefab"]] = kinds.get(o["prefab"], 0) + 1
        summary.append((n, len(objs), kinds))
    for n, cnt, kinds in summary:
        top = ", ".join(f"{k}:{v}" for k, v in sorted(kinds.items(), key=lambda x: -x[1])[:5])
        print(f"F{n:<3} {cnt:4d} objs  | {top}")
