#!/usr/bin/env python3
"""プロジェクトの整合性を監査する。Unity不要。
 1. 敵プレハブのタグに対応する Battlers/<タグ>.asset があるか
    （CreateBattler() が Resources.Load("Battlers/" + tag) で引くため必須）
 2. .floor が参照する名前が FloorPrefabRegistry に全て登録されているか
 3. モンスターの数値が原典(docs/original/monsters.json)と一致するか"""
import os,re,glob,json,sys
os.chdir(os.path.expanduser("~/Code/MagicTower"))
ng=0
prefab_tag={}
for p in glob.glob("Assets/Prefab/**/*.prefab",recursive=True):
    t=open(p,encoding="utf-8",errors="replace").read()
    m=re.search(r"^  m_TagString: (.+)$",t,re.M)
    if m: prefab_tag[os.path.basename(p)[:-7]]=m.group(1).strip()
assets={os.path.basename(p)[:-6] for p in glob.glob("Assets/Resources/Battlers/*.asset")}
# .floor から実際に使われている名前だけを対象にする
used_by_data=set()
for p in glob.glob("Assets/Resources/Floors/Floor_*.txt"):
    inobj=False
    for l in open(p,encoding="utf-8"):
        if l.startswith("objects:"): inobj=True; continue
        if inobj:
            m=re.match(r"\s*[+-]\d+ [+-]\d+\s+(\S+)",l)
            if m: used_by_data.add(m.group(1))
bad=[(n,t) for n,t in sorted(prefab_tag.items())
     if t.startswith("Enemy_") and t not in assets and n in used_by_data]
legacy=[(n,t) for n,t in sorted(prefab_tag.items())
        if t.startswith("Enemy_") and t not in assets and n not in used_by_data]
print(f"[1] 敵プレハブのタグ ↔ Battlersアセット: 不一致 {len(bad)}件")
for n,t in bad: print(f"      {n:<26} タグ={t} → Battlers/{t}.asset が無い")
if legacy:
    print(f"    （参考）.floor から参照されない旧プレハブ {len(legacy)}件: "
          + ", ".join(n for n,_ in legacy))
ng+=len(bad)
reg=open("Assets/Resources/FloorPrefabRegistry.asset").read()
keys=set(re.findall(r"^  - key: (.+)$",reg,re.M))
used=set()
for p in glob.glob("Assets/Resources/Floors/Floor_*.txt"):
    inobj=False
    for l in open(p,encoding="utf-8"):
        if l.startswith("objects:"): inobj=True; continue
        if inobj:
            m=re.match(r"\s*[+-]\d+ [+-]\d+\s+(\S+)",l)
            if m: used.add(m.group(1))
miss=sorted(used-keys)
print(f"[2] .floor の参照 ↔ プレハブ登録簿: 未登録 {len(miss)}件 {miss}")
ng+=len(miss)
ID2={61:"Enemy_SlimeG",63:"Enemy_SlimeR",65:"Enemy_SlimeB",67:"Enemy_Bat",69:"Enemy_Apprentice",
 71:"Enemy_Magician",73:"Enemy_Guardian",75:"Enemy_GreatGuardian",77:"Enemy_Skeleton",
 79:"Enemy_SkeletonWorrior",81:"Enemy_BossSkeleton",83:"Enemy_GreatBat",85:"Enemy_Zombie",
 87:"Enemy_ZombieWorrior",89:"Enemy_Rock",91:"Enemy_Zeno",104:"Enemy_GiantOctopus",
 122:"Enemy_Dragon",133:"Enemy_Worrior",135:"Enemy_GreatWorrior",137:"Enemy_Samurai",
 139:"Enemy_GreatZombieSoldier",141:"Enemy_GreatZombie",143:"Enemy_MozaikuMan",145:"Enemy_KingMagician"}
mon={m["id"]:m for m in json.load(open("docs/original/monsters.json"))}
diff=0
for i,nm in sorted(ID2.items()):
    p=f"Assets/Resources/Battlers/{nm}.asset"
    if not os.path.exists(p): print(f"      {nm}: アセット無し"); diff+=1; continue
    t=open(p,encoding="utf-8").read()
    g=lambda k:int(re.search(rf"^  {k}: (-?\d+)$",t,re.M).group(1))
    o=mon[i]
    if (g("maxHP"),g("attack"),g("defense"),g("coin"))!=(o["hp"],o["atk"],o["dfn"],o["gold"]):
        print(f"      {nm}: 原典 {o['hp']}/{o['atk']}/{o['dfn']}/{o['gold']} ≠ "
              f"{g('maxHP')}/{g('attack')}/{g('defense')}/{g('coin')}"); diff+=1
print(f"[3] モンスター数値 ↔ 原典: 不一致 {diff}件")
ng+=diff
print(f"\n合計 {ng} 件の要対応")
sys.exit(1 if ng else 0)
