#!/usr/bin/env python3
"""動画の盤面と .floor データを全50階で突き合わせる。

タイルの平均色データ docs/original/all_p1.raw / all_p2.raw は
動画から11x11マスの色を2秒ごとに抜いたもの（1フレーム363バイト）。
各階について最も一致するフレームを選び、食い違いを3種類に分けて数える:
  構造差異  … 地形そのものが違う。これが真に直すべき箇所
  開封済扉  … データでは扉、動画では床。プレーヤーが既に開けただけ
  未登録物体… データでは床、動画では何かある。プレーヤー自身など
"""
import os,re,sys,json
ROOT=os.path.expanduser("~/Code/MagicTower"); FD=os.path.join(ROOT,"Assets/Resources/Floors")
SP=sys.argv[1] if len(sys.argv)>1 else os.path.join(ROOT,"docs/original")
def classify(p):
    r,g,b=p; mx,mn=max(p),min(p)
    if r>115 and 70<=g<=120 and 40<=b<=85 and r-b>55: return "WALL"
    if 70<=mn and mx<=100 and mx-mn<=10:              return "FLOOR"
    if r>185 and g>140 and 100<b<160 and r-b>55:      return "DOOR_Y"
    if b>185 and r>140 and b-r>25 and b-g>25:         return "DOOR_B"
    if r>200 and 85<=g<=125 and 85<=b<=125 and r-g>85:return "MAGMA"
    if r>140 and g<90 and b<90 and r-g>70:            return "DOOR_R"
    return "OBJ"
def load(n):
    t=open(os.path.join(FD,f"Floor_{n}.txt"),encoding="utf-8").read()
    g=[m.group(2) for m in (re.match(r"\s*([+-]\d+) \| (.{11})$",l) for l in t.splitlines()) if m]
    o={}
    for l in t.splitlines():
        m=re.match(r"\s*([+-]\d+) ([+-]\d+)\s+(\S+)\s+(.*)$",l)
        if m: o[(int(m.group(1)),int(m.group(2)))]=m.group(3)
    return g,o
frames=[]
for tag in (1,2):
    d=open(os.path.join(SP,f"all_p{tag}.raw"),'rb').read()
    for i in range(len(d)//363):
        b=d[i*363:(i+1)*363]
        frames.append((tag,i*2,[[classify(tuple(b[(r*11+c)*3:(r*11+c)*3+3])) for c in range(11)] for r in range(11)]))
# 赤扉とマグマは同色のため両方許容。階段/隠し壁/封印扉/特殊扉は判定保留。
OK={"#":{"WALL"},".":{"FLOOR"},"y":{"DOOR_Y"},"b":{"DOOR_B"},
    "r":{"DOOR_R","MAGMA"},"~":{"MAGMA","DOOR_R"},
    # F50 の星空。暗い一様色なので FLOOR/OBJ のどちらにも転びうる
    "*":{"FLOOR","OBJ","WALL"}}
SPEC=set("%sx><")
def analyze(g,o,tl):
    hard=[]; door=[]; objx=[]
    for r in range(11):
        for c in range(11):
            gx,gy=c-5,5-r; e=g[r][c]; ob=tl[r][c]
            if e in SPEC: continue
            if (gx,gy) in o: continue
            if ob in OK[e]: continue
            if e in "ybr" and ob=="FLOOR": door.append((gx,gy,e,ob))   # 開封済み
            elif e=="." and ob=="OBJ":     objx.append((gx,gy,e,ob))   # プレイヤー or 未登録物
            else:                          hard.append((gx,gy,e,ob))   # 構造的差異
    return hard,door,objx
print(f"{'階':<5}{'構造差異':>8}{'開封済扉':>10}{'物体差':>8}   最良フレーム")
tot_h=tot_d=tot_o=0; det={}
for n in range(1,51):
    p=os.path.join(FD,f"Floor_{n}.txt")
    if not os.path.exists(p): continue
    g,o=load(n)
    if all(ch=="." for row in g for ch in row) and not o: continue
    best=None
    for tag,t,tl in frames:
        h,d_,ox=analyze(g,o,tl)
        # プレイヤー1体は不問
        sc=len(h)*10+len(d_)*3+max(0,len(ox)-1)
        if best is None or sc<best[0]: best=(sc,tag,t,h,d_,ox)
    sc,tag,t,h,d_,ox=best
    op=max(0,len(ox)-1)
    tot_h+=len(h); tot_d+=len(d_); tot_o+=op
    det[n]=dict(tag=tag,t=t,hard=h,door=d_,obj=ox)
    mark="✓" if (len(h)+len(d_)+op)==0 else ("△" if len(h)==0 else "✗")
    print(f"F{n:<4}{len(h):>8}{len(d_):>10}{op:>8}   {mark} Part{tag} {t}s")
print(f"\n合計: 構造差異 {tot_h} / 開封済扉 {tot_d} / 未登録物体 {tot_o}")
print("\n=== 構造差異の明細（これが真に直すべき箇所） ===")
for n,v in sorted(det.items()):
    if v["hard"]:
        print(f"F{n} (Part{v['tag']} t={v['t']}s): " + "  ".join(f"({x:+d},{y:+d}) データ={e}→動画={ob}" for x,y,e,ob in v["hard"]))
json.dump({str(k):{"tag":v["tag"],"t":v["t"]} for k,v in det.items()}, open(os.path.join(SP,"best2.json"),"w"))
