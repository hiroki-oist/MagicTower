#!/usr/bin/env python3
"""動画フレーム -> Floor_N.txt 生成（F35-F50）"""
import os,sys
SP=sys.argv[1]; FD=os.path.expanduser("~/Code/MagicTower/Assets/Resources/Floors")
def cls(p):
    r,g,b=p; mx,mn=max(p),min(p)
    if 88<=r<=112 and 88<=g<=112 and 112<=b<=136 and b-r>12: return "X"  # 未踏プレビュー
    if 96<=mn and mx<=112 and mx-mn<=6:                      return "D"  # ダイアログ
    if 62<=mn and mx<=80 and mx-mn<=8:                       return "*"  # 星空(F50)
    if r>115 and 70<=g<=120 and 40<=b<=85 and r-b>55:        return "#"
    if 70<=mn and mx<=100 and mx-mn<=10:                     return "."
    if r>185 and g>140 and 100<b<160 and r-b>55:             return "y"
    if b>185 and r>140 and b-r>25 and b-g>25:                return "b"
    if r>200 and 85<=g<=125 and 85<=b<=125 and r-g>85:       return "~"
    if r>140 and g<90 and b<90 and r-g>70:                   return "r"
    return "O"
d=open(os.path.join(SP,"all_p2.raw"),'rb').read()
FR={35:114,36:120,37:130,38:142,39:207,40:211,41:259,42:363,
    43:273,44:339,45:285,46:286,47:294,48:624,49:637,50:882}
HDR=("terrain:  . 床  # 壁  % 隠し壁  ~ マグマ  > 上り  < 下り  * 星空\n"
     "          y/b/r 黄青赤の扉  s 封印扉  x 特殊扉  O 物体(要同定)\n")
for n,idx in sorted(FR.items()):
    b=d[idx*363:(idx+1)*363]
    g=[[cls(tuple(b[(r*11+c)*3:(r*11+c)*3+3])) for c in range(11)] for r in range(11)]
    bad=sum(row.count("X")+row.count("D") for row in g)
    objs=[(c-5,5-r) for r in range(11) for c in range(11) if g[r][c]=="O"]
    L=[f"# Floor {n}",
       f"grid: 11x11  x:-5..5  y:-5..5",
       f"# 出典: 動画 Part2 t={idx*2}s (index {idx}) より自動生成",
       f"# 注意: 地形のみ確定。O は物体の位置で、種別は未同定。",
       "", HDR.rstrip(), ""]
    for r in range(11):
        L.append(f"  {5-r:+d} | " + "".join(g[r]))
    L += ["", "objects:  (x, y, prefab, instance-name)"]
    for x,y in objs:
        L.append(f"  {x:+d} {y:+d}  UNKNOWN                    [動画より検出 要同定]")
    open(os.path.join(FD,f"Floor_{n}.txt"),"w").write("\n".join(L)+"\n")
    flag = f"  ⚠ 異常タイル{bad}" if bad else ""
    print(f"F{n:<3} t={idx*2:>5}s  物体{len(objs):>3}箇所{flag}")
