#!/usr/bin/env python3
"""PC98版 INITIAL.DAT から全50階＋0階の正典データを生成する。
  1階の開始 = word offset 199 / ストライド = 124ワード（121マップ + 3メタ）

**0階**（word offset 75、1階の1つ手前）も原典のデータとして実在する。
中身は床だけの部屋で、左上 (-5,+5) に上り階段（ID11）、真ん中 (0,0) に
**幸運の金貨（ID50）**が1つ置いてあるだけ。

ただし原典はここに行けない: 始まりは 現在階=1（INITIAL.DAT の先頭 …,1,1,5,10）で、
**1階に下り階段が無く**（全50階で下りが無いのは 1・44・50 階）、
現在階に0を書く道筋もコードに無い。
つまり幸運の金貨は、ヒント26が「この塔のどこかにある」と言っているのに
**この版では手に入らない**（絵だけ残ったモンスター2体と同じ、作りかけの名残）。
正典なのでデータとしては出しておき、行ける道は足さない。"""
import struct,os,sys
SRC=sys.argv[1]; FD=os.path.expanduser("~/Code/MagicTower/Assets/Resources/Floors")
d=open(SRC,'rb').read()
w=[struct.unpack('<H',d[i:i+2])[0] for i in range(0,len(d)-1,2)]
OFF,ST=199,124
# ID7 は ID6(床) と見た目が完全に同じ。F48(-5,+6) に1つだけ存在する論理マーカー。
# 地形としては床として扱い、位置は下の SPECIAL_MARKERS に記録する。
#
# ID10 は ID9(隠し壁) と見た目が同じだが、全50階でたった4マスしか無く、
# その4マスは原典が「そこに何かが隠れている」と名指ししている場所と一致する:
#   F12(+5,+5)=隠れた鍵屋 / F16(+5,-5)=エリクサーをくれる仙人
#   F19(0,+3)=十字架（原典ヒント210「十字架は19階にある」）
#   F41(+4,+4)=隠れた敵（原典ヒント216「41階の右上に私が隠れている」＝門の守護マス）
# いずれも隣の ID9 を壊して隣接マスに立ってから話しかける形になっている。
SPECIAL_MARKERS={7:"床と同じ見た目の特殊マス（F48 の左上隅に1つ）",
                 10:"隠し壁と見た目が同じ「何かが隠れている」マス"}
TERRAIN={6:".",7:".",8:"#",9:"%",10:"%",11:">",12:"<",13:"~",14:"*",
         1:"y",2:"b",3:"r",4:"s",5:"x"}
# 祭壇は3マスで1つの絵。**マスごとに絵が違う**（015 左・016 中・017 右）ので、
# 3マスとも別の名前にして1マスぶんずつ置く。
# （以前は16も17も "Shop_Altar" で、3マス幅の絵を2回重ねて置いていた）
OBJ={15:"Vendor",16:"Shop_Altar",17:"Shop_Altar_R",19:"Merchant",21:"Mikata",23:"Fairy",
     25:"Master",27:"Princess",29:"Key_Yellow",30:"Key_Red",31:"Key_Blue",
     32:"Potion_Red",33:"Potion_Blue",34:"Gem_Attack",35:"Gem_Defense",
     # --- 装備。段階は **物体IDの昇順** に一致する（動画で4例を実測して確定）---
     #   剣  51 長剣(F5) / 52 銀の剣(F17) / 53 騎士の剣(F33) / 54 聖剣(F48) / 55 神剣(F13)
     #   盾  56 鉄の盾(F9) / 57 銀の盾(F11) / 58 騎士の盾(F38) / 59 聖盾(F43) / 60 神盾(F44)
     #   ※ 以前は「階の順に段階が上がる」と仮定していたが、動画で
     #      F17 で長剣→銀の剣、F33 で騎士の剣、F48 で聖剣になるのを確認したため誤りだった。
     #      神盾(60) が F44（公式ヒントの「幻の階」）にあることとも整合する。
     51:"Sword_1",52:"Sword_2",53:"Sword_3",54:"Sword_4",55:"Sword_5",
     56:"Shield_1",57:"Shield_2",58:"Shield_3",59:"Shield_4",60:"Shield_5",
     # --- 特殊アイテム。原典 MTE.EXE のアイテム取得分岐から確定（ID = 36..50）---
     #   36 勇者のオーブ / 37 知恵のオーブ / 38 飛翔のオーブ / 39 十字架 / 40 エリクサー
     #   41 マトック / 42 破壊の玉 / 43 空間転移の秘宝 / 44 昇華の翼 / 45 降臨の翼
     #   46 ドラゴンスレイヤー / 47 雪の結晶 / 48 魔法の鍵 / 49 スーパーマトック / 50 幸運の金貨
     #   初期配置にあるのはこの6つだけで、残りはイベントで手に入る。
     37:"Orb_Wisdom",38:"Orb_Flight",42:"Ball_Destruction",
     44:"Wing_Up",48:"Key_Magic",50:"Coin_Lucky",
     61:"Enemy_SlimeG",63:"Enemy_SlimeR",65:"Enemy_SlimeB",67:"Enemy_Bat",
     69:"Enemy_Apprentice",71:"Enemy_Magician",73:"Enemy_Guardian",
     75:"Enemy_GreatGuardian",77:"Enemy_Skeleton",79:"Enemy_SkeletonWorrior",
     81:"Enemy_BossSkeleton",83:"Enemy_GreatBat",85:"Enemy_Zombie",
     87:"Enemy_ZombieWorrior",89:"Enemy_Rock",133:"Enemy_Worrior",
     135:"Enemy_GreatWorrior",137:"Enemy_Samurai",139:"Enemy_GreatZombieSoldier",
     141:"Enemy_GreatZombie",143:"Enemy_MozaikuMan",145:"Enemy_KingMagician",
     91:"Boss_Zeno",93:"Enemy_ID93",95:"Enemy_ID95",
     147:"Enemy_ID147",149:"Enemy_ID149",151:"Enemy_ID151",
     153:"Enemy_ID153",155:"Enemy_ID155",157:"Enemy_ID157"}
BOSS3x3={**{i:"Boss_GiantOctopus" for i in range(97,106)},
         **{i:"Boss_Dragon" for i in range(115,124)}}
# 3x3ボスの代表ID: 大ダコ=104(HP1200/攻180/防20), ドラゴン=122(HP1500/攻600/防250)
HDR=("terrain:  . 床  # 壁  % 隠し壁  ~ マグマ  > 上り  < 下り  * 星空\n"
     "          y/b/r 黄紫赤の扉  s 封印扉  x 特殊扉  O 物体")
unknown=set()
for n in range(0,51):
    # 1ブロックは **[3ワードのメタ][121マス]** の並び。
    #   メタ = [階の番号, 下から登ってきたときの着地マス, 上から降りてきたときの着地マス]
    # 原典のセーブ処理 0x9b4e が地図を **DS:0x1ae8 から 0x1930 ワード**（＝52ブロック）
    # 書き出していて、マスを引く式は `0x1aee + 248*階`。0x1aee は 0x1ae8 の3ワード先なので、
    # 各ブロックは メタが先・マスが後。ファイルでも 先頭72ワード（12の値＋60のフラグ）の
    # あとに 72+124*階 からメタ、75+124*階 からマスが並ぶ。
    b=OFF+(n-1)*ST; cells=w[b:b+121]; meta=w[b-3:b]
    grid=[]; objs=[]
    for r in range(11):
        row=""
        for c in range(11):
            v=cells[r*11+c]; x,y=c-5,5-r
            if v in TERRAIN:
                row+=TERRAIN[v]
                if v in SPECIAL_MARKERS: objs.append((x,y,f"Marker_ID{v}",SPECIAL_MARKERS[v]))
            else:
                row+="O"
                if v in BOSS3x3: objs.append((x,y,BOSS3x3[v],f"ID{v} 3x3ボスの一部"))
                elif v in OBJ:   objs.append((x,y,OBJ[v],f"ID{v}"))
                else:            objs.append((x,y,f"UNKNOWN_{v}","要同定")); unknown.add(v)
        grid.append(row)
    L=[f"# Floor {n}",
       "grid: 11x11  x:-5..5  y:-5..5",
       f"# 出典: PC98版 INITIAL.DAT（原典）word offset {b} / meta={list(meta)}",
       "", HDR, ""]
    L += [f"  {5-r:+d} | {grid[r]}" for r in range(11)]
    L += ["", "objects:  (x, y, prefab, note)"]
    L += [f"  {x:+d} {y:+d}  {p:<26} {note}" for x,y,p,note in objs]
    open(os.path.join(FD,f"Floor_{n}.txt"),"w").write("\n".join(L)+"\n")
print(f"0階＋全50階を生成。未同定ID: {sorted(unknown)}")
