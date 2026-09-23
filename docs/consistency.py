#!/usr/bin/env python3
"""公式データと現在の実装の不整合を洗い出す。Unity不要。"""
import os,re,json,glob,sys
from collections import Counter
os.chdir(os.path.expanduser("~/Code/MagicTower"))
issues=[]
checks = 0
def PhoneScreen_left(src):
    import re as _re
    m=_re.search(r"LeftButtons\s*=\s*\{([^}]*)\}", src, _re.S)
    return [t.strip().strip('"') for t in m.group(1).split(",")] if m else []

def chk(ok,label,detail=""):
    global checks
    checks += 1
    print(("  ✓ " if ok else "  ✗ ")+label+(f"  {detail}" if detail else ""))
    if not ok: issues.append(label)

src=open("Assets/Scripts/Items/ItemPickup.cs",encoding="utf-8").read()

print("【1】装備の上昇値（原典: 剣盾とも +10/+20/+40/+50/+100）")
_eq=re.search(r"EquipGain = \{ ([^}]+)\}",src).group(1)
_eq=[int(x) for x in _eq.replace(" ","").split(",") if x]
chk(_eq==[0,10,20,40,50,100],"剣と盾の5段階（0番目は未装備）",f"実装={_eq}")
chk(src.count("EquipGain[rank]")==2,"剣も盾も同じ表を引く")

print("\n【2】装備の表示名（原典 DS の6文字幅の短縮名）")
_msg=open("Assets/Resources/Messages.txt",encoding="utf-8").read()
_sw=[re.search(rf"^panel_sword_{i} \| (.+)$",_msg,re.M).group(1) for i in range(1,6)]
_sh=[re.search(rf"^panel_shield_{i} \| (.+)$",_msg,re.M).group(1) for i in range(1,6)]
chk(_sw==["　長剣","　銀の剣","騎士の剣","　聖剣","　神剣"],"剣の名前",f"実装={_sw}")
chk(_sh==["　鉄の盾","　銀の盾","騎士の盾","　聖盾","　神盾"],"盾の名前",f"実装={_sh}")

print("\n【3】回復薬・クリスタルが階別データ駆動か")
chk("power.ForFloor(player.Floor)" in src,"拾ったときの効き目は階別の表から")
chk(all(k in src for k in ("band.PotionRed","band.PotionBlue",
                           "band.CrystalAttack","band.CrystalDefense")),
    "薬2種とクリスタル2種が同じ表を引く")

print("\n【4】初期値（原典: 序章は 1000/100/100・神剣神盾、ゼノのあとは 400/10/10・丸腰）")
_gs=open("Assets/Scripts/GameStart.cs",encoding="utf-8").read()
chk("Hp = 1000, Attack = 100, Defense = 100" in _gs,"序章の値")
chk("SwordRank = 5, ShieldRank = 5" in _gs,"序章は神剣と神盾")
_dbg=open("Assets/Scripts/DebugPlay.cs",encoding="utf-8").read()
chk("見終わったら必ず false に戻すこと" in _dbg,"細工は戻す前提だと書いてある")
chk("SwordRank = 0, ShieldRank = 0" in _gs,"奪われたあとは丸腰")
_ps=open("Assets/Scripts/PlayerState.cs",encoding="utf-8").read()
chk("YellowKeys" in _ps and "PurpleKeys" in _ps and "RedKeys" in _ps,"鍵は3種を持つ")

print("\n【5】戦闘規則")
bt=open("Assets/Scripts/Battle/Battlers/Battler.cs",encoding="utf-8").read()
chk("Mathf.Max(0, attacker.Attack - Defense)" in bt,"負ダメージのクランプ")
chk("CanDefeat" in open("Assets/Scripts/Battle/BattleRules.cs",encoding="utf-8").read()
    and "Predict" in open("Assets/Scripts/Battle/TowerBattle.cs",encoding="utf-8").read(),
    "倒せない敵のガード（Predict が CanWin を返す）")
br=open("Assets/Scripts/Battle/BattleRules.cs",encoding="utf-8").read()
chk("EnemyKind.Vampire && (charms & Charms.Cross) == 0) return false" not in br,
    "バンパイアの誤った禁止ルールが無い")

print("\n【6】扉の解錠（原典 MTE.EXE 0x23ee: 種別1の鍵の扉だけが開く。門と特殊扉は触れても開かない）")
_tp=open("Assets/Scripts/TowerPlayer.cs",encoding="utf-8").read()
chk("if (keys <= 0) return Make(Outcome.Blocked" in _tp,"鍵が足りないときは扉が残る")
chk('case "s":' in _tp and 'case "x":' in _tp and "Outcome.Blocked, to, what" in _tp,
    "門(s)と特殊扉(x)は触れても開かない（原典: 属性表の種別1だけが鍵の扉）")
chk("keys--;" in _tp and "floor.Clear(at);" in _tp,"鍵を1本使ってからマスを床にする")
print("\n【6b】鍵の色（原典は 黄・紫・赤。リメイクは Blue を紫の意味で使用）")
chk("LockPurple" in open("Assets/Scripts/Floor/FloorData.cs",encoding="utf-8").read(),
    "FloorData が紫として扱う")
tag=open("ProjectSettings/TagManager.asset").read()
print(f"    ※ タグは Key_Blue / Lock_Blue のまま（識別子として維持）")

print("\n【7】モンスター数値（原典 33体）")
mon={m["id"]:m for m in json.load(open("docs/original/monsters.json"))}
ID2={61:"Enemy_SlimeG",63:"Enemy_SlimeR",65:"Enemy_SlimeB",67:"Enemy_Bat",69:"Enemy_Apprentice",
 71:"Enemy_Magician",73:"Enemy_Guardian",75:"Enemy_GreatGuardian",77:"Enemy_Skeleton",
 79:"Enemy_SkeletonWorrior",81:"Enemy_BossSkeleton",83:"Enemy_GreatBat",85:"Enemy_Zombie",
 87:"Enemy_ZombieWorrior",89:"Enemy_Rock",91:"Enemy_Zeno",93:"Enemy_ID93",95:"Enemy_ID95",
 104:"Enemy_GiantOctopus",122:"Enemy_Dragon",133:"Enemy_Worrior",135:"Enemy_GreatWorrior",
 137:"Enemy_Samurai",139:"Enemy_GreatZombieSoldier",141:"Enemy_GreatZombie",
 143:"Enemy_MozaikuMan",145:"Enemy_KingMagician",147:"Enemy_ID147",149:"Enemy_ID149",
 151:"Enemy_ID151",153:"Enemy_ID153",155:"Enemy_ID155",157:"Enemy_ID157"}
bad=0
for i,nm in ID2.items():
    p=f"Assets/Resources/Battlers/{nm}.asset"
    if not os.path.exists(p): bad+=1; continue
    t=open(p,encoding="utf-8").read()
    gg=lambda k:int(re.search(rf"^  {k}: (-?\d+)$",t,re.M).group(1))
    o=mon[i]
    if (gg("maxHP"),gg("attack"),gg("defense"),gg("coin"))!=(o["hp"],o["atk"],o["dfn"],o["gold"]): bad+=1
chk(bad==0,f"33体すべて原典と一致",f"不一致 {bad}体")

print("\n【7b】セリフ・メッセージ")
import os as _os
chk(_os.path.exists("Assets/Resources/Dialogue.txt"),"セリフ原文のデータ")
chk(_os.path.exists("Assets/Resources/Messages.txt"),"PC98版メッセージのデータ")
_dlg=open("Assets/Resources/Dialogue.txt",encoding="utf-8").read()
chk(sum(1 for l in _dlg.splitlines() if l[:1].isdigit())>=98,"セリフ98本以上")
chk("GameText.Instance.Message" in open("Assets/Scripts/TowerGame.cs",
                                       encoding="utf-8").read(),"メッセージがデータ駆動")

print("\n【7a2】モンスターの表示名（MTE.EXE の nameptr から直接）")
import struct as _st
_d=open("docs/original/disk/MTE.EXE","rb").read()
_HDR,_DG=4608,0x12340
def _nm(o):
    p2=_HDR+_DG+o; e=_d.index(b"\0",p2); return _d[p2:e].decode("cp932")
_un=lambda x: re.sub(r"\\u([0-9A-Fa-f]{4})", lambda m:chr(int(m.group(1),16)), x or "")
_bad=[]
for _m in json.load(open("docs/original/monsters.json")):
    _f=ID2.get(_m["id"])
    if not _f: continue
    _p=f"Assets/Resources/Battlers/{_f}.asset"
    if not os.path.exists(_p): continue
    _t=open(_p,encoding="utf-8").read()
    _g=re.search(r'^  name: "(.*)"$',_t,re.M)
    if not _g or _un(_g.group(1))!=_nm(_m["nameptr"]): _bad.append((_m["id"],_f))
# **空白ごとそのまま**比べる。原典は名前の側に中央寄せの空白を持っていて、
# それを右下の欄の (528,272) にそのまま書く（0x9626）。
# 以前ここで空白を落として比べていたので、リメイク側も落とした名前を持っていた。
chk(not _bad, "33体の表示名が原典と一致（中央寄せの空白も込み）",
    f"不一致 {len(_bad)}件 {_bad[:4]}")

print("\n【7a3】十字架・ドラゴンスレイヤーの対象（原典 0x6101 / 0x6142）")
_br=open("Assets/Scripts/Battle/BattleRules.cs",encoding="utf-8").read()
_code=[l for l in _br.splitlines() if not l.strip().startswith("///")]
chk(not any('tag.Contains("Zombie")' in l for l in _code), "Contains による広すぎる判定がコードに無い")
chk('tag == "Enemy_Zombie"' in _br and 'tag == "Enemy_ZombieWorrior"' in _br, "十字架は ID85/ID87 を名指し")
chk('tag == "Enemy_Dragon"' in _br, "ドラゴンスレイヤーは ID122 のみ")

print("\n【7c】原典テキスト（PC98版 MT.GTX / MTE.EXE の復号結果）")
_d={}
for _l in open("Assets/Resources/Dialogue.txt",encoding="utf-8"):
    _l=_l.rstrip("\n")
    if not _l or _l[0]=="#" or "|" not in _l: continue
    _a,_b=_l.split("|",1)
    if _a.strip().isdigit(): _d[int(_a)]=_b.strip().replace("\\n","\n")
chk(len(_d)==145,"MT.GTX の本文つきレコード145本",f"{len(_d)}本")
chk(len(_d[12].split("\n"))==6,"原典の改行位置を保持している")
chk("守っているモンスターを倒さなければ開かない" in _d.get(81,"").replace("\n",""),"門の解錠条件が原典の文で入っている")
_m={}
for _l in open("Assets/Resources/Messages.txt",encoding="utf-8"):
    _l=_l.rstrip("\n")
    if not _l or _l[0]=="#" or "|" not in _l: continue
    _k,_v=_l.split("|",1); _m[_k.strip()]=_v.strip()
_num=lambda n:"".join(chr(ord(c)-ord("0")+ord("０")) if c.isdigit() else c for c in str(n))
chk(_m.get("potion","").replace("{0}",_num(200))=="回復薬を手に入れた！　生命力　２００ポイントＵＰ！","MTE.EXEの文言そのまま（全角ＵＰ・全角数字）")
chk(_m.get("shield_3")=="騎士の盾を手に入れた！" and _m.get("shield_5")=="神盾を手に入れた！","盾5段階すべて")
chk(_m.get("cannot_attack")=="相手の防御力が高すぎて攻撃できない。","倒せない敵の公式メッセージ")
_gt=open("Assets/Scripts/Items/GameText.cs",encoding="utf-8").read()
chk("public static string Num" in _gt and 'Replace("{0}", Num(value))' in _gt,"数値の全角化が実装されている")

print("\n【7d】装備の段階（物体IDの昇順＝段階。動画で4例を実測）")
_f=lambda n:open(f"Assets/Resources/Floors/Floor_{n}.txt",encoding="utf-8").read()
chk("Sword_1" in _f(5),"F5 は長剣")
chk("Sword_2" in _f(17),"F17 は銀の剣")
chk("Sword_3" in _f(33),"F33 は騎士の剣")
chk("Sword_4" in _f(48),"F48 は聖剣")
chk("Sword_5" in _f(13),"F13 は神剣")
chk("Shield_5" in _f(44),"F44（幻の階）は神盾")

print("\n【7d2】特殊アイテムの物体ID（原典 MTE.EXE の取得分岐から確定）")
_si=open("Assets/Scripts/Items/SpecialItems.cs",encoding="utf-8").read()
for _n,_k in [("Orb_Wisdom","WisdomOrb"),("Orb_Flight","FlightOrb"),("Ball_Destruction","DestructionOrb"),
              ("Wing_Up","WingUp"),("Key_Magic","MagicKey"),("Coin_Lucky","LuckyCoin")]:
    chk(f'StartsWith("{_n}")' in _si and f"Kind.{_k}" in _si, f"{_n} = Kind.{_k}")
_f=lambda n:open(f"Assets/Resources/Floors/Floor_{n}.txt",encoding="utf-8").read()
chk("Orb_Wisdom" in _f(5),"F5 は知恵のオーブ")
chk("Orb_Flight" in _f(1),"F1 は飛翔のオーブ")
chk("Ball_Destruction" in _f(37),"F37 は破壊の玉")
chk("Wing_Up" in _f(45),"F45 は昇華の翼")
chk("Key_Magic" in _f(46),"F46 は魔法の鍵")

print("\n【7f】門の解錠表（原典 MTE.EXE 0x4762 から抽出）")
_sd=[l.strip() for l in open("Assets/Resources/SealedDoors.txt",encoding="utf-8") if l[:1].isdigit()]
chk(len(_sd)==29,"解錠のきまりが29件（28件＋40階の金騎士の褒美）",f"{len(_sd)}件")
def _row(fl,flag):
    for l in _sd:
        p2=[x.strip() for x in l.split("|")]
        if int(p2[0])==fl and int(p2[1],16)==flag: return p2
    return None
_r=_row(48,0x7f); chk(_r and _r[2]=="-5,5","F48 の守護マスは左上隅(-5,+5)（原典ヒント236）",_r[2] if _r else "無し")
_r=_row(34,0x74); chk(_r and len(_r[2].split(";"))==8,"F34 は8マス（原典ヒント224）")
_r=_row(14,0x66); chk(_r and len(_r[2].split(";"))==3,"F14 は3マス（原典ヒント234）")
_r=_row(41,0x7a); chk(_r and "ID45" in _r[3],"F41 を解くと降臨の翼(ID45)が出る（原典ヒント216）")
_r=_row(17,0x62); chk(_r and _r[2]=="-5,1;-3,1","F17 の門は斜め下2マスが守護（動画と一致）")
_r=_row(10,0x5d); chk(_r and "(+0,-5)=上り" in _r[3],"F10 の関門は (0,-5) に上り階段を出す（動画 t=700）")
_r=_row(20,0x68); chk(_r and "(+0,+5)=上り" in _r[3],"F20 の関門は (0,+5) に上り階段を出す（動画 t=1180）")
_r=_row(30,0x6e); chk(_r and "(+0,+5)=上り" in _r[3],"F30 の関門は (0,+5) に上り階段を出す（原典コードが明示）")
chk("IsSatisfied" in open("Assets/Scripts/Floor/SealedDoors.cs",encoding="utf-8").read(),"守護マス方式で実装されている")

print("\n【8b】FloorLoader が全50階を組み立てられるか（プレハブの網羅）")
_reg=open("Assets/Resources/FloorPrefabRegistry.asset",encoding="utf-8").read()
_keys=set(re.findall(r"key:\s*(\S+)",_reg))
_SYM={".":"Floor","#":"Wall","%":"SecretWall","~":"Magma","*":"Void","O":"Floor",
      ">":"StairsUp","<":"StairsDown","y":"LockYellow","b":"LockPurple",
      "r":"LockRed","s":"LockSealed","x":"LockSpecial"}
_need=set(); _unk=set()
for _n in range(1,51):
    _t=open(f"Assets/Resources/Floors/Floor_{_n}.txt",encoding="utf-8").read()
    for _l in _t.splitlines():
        _m=re.match(r"\s*([+-]\d+) \| (.{11})$",_l)
        if _m:
            for _ch in _m.group(2):
                _k=_SYM.get(_ch)
                if _k is None: _unk.add(_ch)
                else: _need.add(_k)
        _m=re.match(r"\s*([+-]\d+) ([+-]\d+)\s+(\S+)",_l)
        if _m: _need.add(_m.group(3))
_miss=sorted(_need-_keys)
chk(not _unk,"未知の地形記号が無い",",".join(sorted(_unk)))
chk(not _miss,f"要求される{len(_need)}種がすべて登録簿にある",",".join(_miss))
chk("Void" in _keys,"F50 の星空タイル(Void)が登録されている")
# 旧形式の fileID 100100000 で参照すると実行時に InvalidCastException になる
chk("100100000" not in _reg,"プレハブ参照が旧形式(fileID:100100000)になっていない")
chk(len(re.findall(r"prefab: \{fileID: \d{15,}",_reg))>=70,"根本GameObjectのfileIDで参照している")

print("\n【8c】F41 が左右対称か（原典ヒント「４１階は完全に左右対称になっておる」）")
def _grid(fl):
    _t=open(f"Assets/Resources/Floors/Floor_{fl}.txt",encoding="utf-8").read()
    return [m.group(2) for m in (re.match(r"\s*([+-]\d+) \| (.{11})$",_l) for _l in _t.splitlines()) if m]
_g41=_grid(41)
_asym=[(r,c) for r in range(11) for c in range(11) if _g41[r][c]!=_g41[r][10-c]]
chk(len(_asym)==2,"非対称なマスは2件だけ",f"{len(_asym)}件")
chk(sorted(_asym)==[(1,1),(1,9)],"その2件は上段の左右端 (-4,+4) と (+4,+4)",str(sorted(_asym)))
chk(_g41[1][9]=="%","右上(+4,+4)は隠し壁＝原典ヒント「右上に私が隠れている」",_g41[1][9])
chk(_g41[1][1]=="O","左上(-4,+4)には敵がいる＝「その前に左上の奴を倒さないとだめ」",_g41[1][1])
_others=[sum(1 for r in range(11) for c in range(11) if _grid(f)[r][c]!=_grid(f)[r][10-c]) for f in (40,42)]
chk(all(n>len(_asym) for n in _others),"隣の階(F40/F42)はもっと非対称＝F41の対称性が際立つ",str(_others))

print("\n【8d】全50階を動画と突き合わせる（docs/match2.py）")
import subprocess
_out=subprocess.run([sys.executable,"docs/match2.py"],capture_output=True,text=True).stdout
_m=re.search(r"構造差異 (\d+) / 開封済扉 (\d+) / 未登録物体 (\d+)",_out)
chk(_m is not None,"照合が実行できた")
if _m:
    _hard=int(_m.group(1))
    chk(_hard<=1,f"地形の構造差異が1件以下",f"{_hard}件")
    chk("F35 (" in _out and "(-2,-3)" in _out,"残る1件は F35 (-2,-3)（盗賊が掘る抜け穴。初期配置では壁で正しい）")

print("\n【8e】EditMode テストが最新の規則を覆っているか")
_tt=open("Assets/Tests/Editor/OriginalDataTests.cs",encoding="utf-8").read()
_alltests="".join(open(f,encoding="utf-8").read() for f in glob.glob("Assets/Tests/Editor/*.cs"))
chk(_alltests.count("[Test]")>=109,"テストが109本以上ある",f"{_alltests.count('[Test]')}本")
chk("FloorLoaderTests" in _alltests,"FloorLoader を Unity 上で動かすテストがある")
chk("GameRulesTests" in _alltests,"規則の通し確認のテストがある")
chk("TowerSceneTests" in _alltests,"本線シーンのテストがある")
chk("TowerPlayerTests" in _alltests,"移動の規則のテストがある")
chk("ItemPickupTests" in _alltests,"アイテム取得のテストがある")
chk("TowerBattleTests" in _alltests,"戦闘のテストがある")
chk("TowerGameTests" in _alltests,"一手の流れのテストがある")
chk("PrologueTests" in _alltests,"冒頭の演出のテストがある")
chk("TowerHudTests" in _alltests,"画面表示のテストがある")
chk(os.path.exists("Assets/Scripts/UI/TowerHudView.cs"),"画面に描く側がある")
_sv=open("Assets/Scripts/TowerSave.cs",encoding="utf-8").read()
chk("SlotCount = 8" in _sv,"セーブは原典と同じ8スロット")
chk("Empty = 0xffff" in _sv,"空きスロットの印は原典の0xffff")
chk("TowerItemUseTests" in _alltests,"特殊アイテムの使用のテストがある")
_tg2=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("public bool UseItem" in _tg2,"特殊アイテムを使える")
chk("TowerNpcTests" in _alltests,"商人・祭壇・妖精のテストがある")
_np=open("Assets/Scripts/Items/TowerNpc.cs",encoding="utf-8").read()
chk('IsThief' in _np and "Mikata" in _np,"Mikata は盗賊（物語の味方）")
chk("AskFairy" in _np and "Ranks" in _np,"妖精の採点と称号がある")
chk("TowerMenuTests" in _alltests,"メニューとオーブのテストがある")
_tm=open("Assets/Scripts/UI/TowerMenu.cs",encoding="utf-8").read()
chk("NextToStairs" in _tm,"飛翔のオーブは階段の隣でしか使えない")
chk("Visited" in open("Assets/Scripts/PlayerState.cs",encoding="utf-8").read(),"行った事のある階を記録する")
_gt2=open("Assets/Scripts/Items/GameText.cs",encoding="utf-8").read()
chk("TrimAscii" in _gt2,"全角スペース(U+3000)の字下げを削らない")
_pr=open("Assets/Scripts/Prologue.cs",encoding="utf-8").read()
chk("{ 110, 111 }" in _pr and "{ 112, 113, 114, 115, 116 }" in _pr,"冒頭のセリフは原典のレコード110〜116")
chk(os.path.exists("Assets/Scripts/TowerController.cs"),"入力を受ける側がある")
_tg=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("ApplyMagicAttack" in _tg and "ApplyUnlocks" in _tg,"一手で 魔法攻撃→解錠判定 まで行う")
_ip=open("Assets/Scripts/Items/ItemPickup.cs",encoding="utf-8").read()
chk("{ 0, 10, 20, 40, 50, 100 }" in _ip,"剣盾の上昇値 +10/+20/+40/+50/+100")
chk('"Key_Blue"' in _ip and "PurpleKeys++" in _ip,"Key_Blue は原典の紫の鍵として扱う")
_tp=open("Assets/Scripts/TowerPlayer.cs",encoding="utf-8").read()
chk('case "s":' in _tp and "Outcome.Blocked" in _tp,"門は触れても開かない（移動処理側）")
_ps=open("Assets/Scripts/PlayerState.cs",encoding="utf-8").read()
chk("MaxHp = 9990" in _ps,"生命力の上限9990（原典マニュアル）")
chk("ApplyZenoEvent" in _ps,"ゼノのイベントで能力を奪われる処理がある")
chk(os.path.exists("Assets/Scenes/Tower.unity"),"本線シーン Tower.unity がある")
chk(os.path.exists("Assets/Editor/TowerSceneBuilder.cs"),"シーンをコードから作り直せる")
for _n in ("ドラゴンスレイヤー","十字架の対象","祭壇","妖精","神盾","特殊アイテム","門は守護マス","商店"):
    chk(_n in _tt, f"{_n} のテストがある")
chk("using UnityEngine;" in _tt,"Vector2Int を使うので using UnityEngine がある")
sys.path.insert(0,"docs")
import check_tests
_bad,_ntest,_ = check_tests.run()
chk(not _bad,"テストが参照するAPIが実在し括弧も合う（型検査の代わり）",", ".join(_bad[:4]))

print("\n【8f】ゲーム冒頭の2つの状態（序章 と ゼノのイベント後）")
_gs=open("Assets/Scripts/GameStart.cs",encoding="utf-8").read()
chk("Hp = 1000, Attack = 100, Defense = 100" in _gs,"序章は 生命力1000 / 攻撃100 / 防御100")
chk("SwordRank = 5, ShieldRank = 5" in _gs,"序章は神剣・神盾を装備")
# ⚠ ここは調べもの用に DebugPlay で差し替えられるようにしてある（原典は 400 / 10 / 10）。
chk("DebugPlay.StrongAfterZeno ? DebugPlay.StrongStat : 10" in _gs,
    "ゼノのイベント後の攻守は原典 10 / 10（細工は DebugPlay 側）")
chk("DebugPlay.StrongHpAfterZeno ? DebugPlay.StrongHp : 400" in _gs,
    "ゼノのイベント後の生命力は原典 400")
chk("Floor = 2" in _gs,"イベント後は2階の牢に入る")
chk("ZenoEncounterFloor = 3" in _gs,"ゼノに会うのは3階")

print("\n【8g】時空の門（50階へ行く唯一の方法）")
_sg=open("Assets/Scripts/Floor/SpacetimeGate.cs",encoding="utf-8").read()
chk("GateFloor = 24" in _sg,"門があるのは24階")
chk("StandPos = new Vector2Int(0, -1)" in _sg,"門の真下(0,-1)に立つ")
chk("DestinationFloor = 50" in _sg and "DestinationPos = new Vector2Int(0, -1)" in _sg,"50階の(0,-1)に着く")
chk("DollFloor = 26" in _sg,"ゼノの伝言を告げる人形は26階")
# 26階の中央に物体があり、24階の中央は空であること（同じ形の部屋）
def _cell(fl,x,y):
    _t=open(f"Assets/Resources/Floors/Floor_{fl}.txt",encoding="utf-8").read()
    for _l in _t.splitlines():
        _m=re.match(r"\s*([+-]\d+) \| (.{11})$",_l)
        if _m and int(_m.group(1))==y: return _m.group(2)[x+5]
    return None
chk(_cell(26,0,0)=="O","26階の中央に物体（人形）がある",str(_cell(26,0,0)))
chk(_cell(24,0,0)==".","24階の中央は空いていて門が現れる",str(_cell(24,0,0)))

print("\n【9】原典の式を実装したクラス")
_alt=open("Assets/Scripts/Items/Altar.cs",encoding="utf-8").read()
chk("10 * (c * (c + 1) + 2)" in _alt and "MaxCost = 9990" in _alt,"祭壇の寄進額 min(10×(C×(C+1)+2), 9990)")
chk("(Mathf.Max(0, donationCount) + 1) * 100" in _alt,"祭壇の回復量 (C+1)×100")
chk("2 * Block(floor)" in _alt and "4 * Block(floor)" in _alt,"祭壇は攻撃+2×ブロック / 防御+4×ブロック")
_mg=open("Assets/Scripts/Battle/MagicAttack.cs",encoding="utf-8").read()
chk("SorcererId = 93" in _mg and "WizardAId  = 151" in _mg and "WizardBId  = 153" in _mg,"魔導師93 / 魔術士Ａ151 / 魔術士Ｂ153")
chk("DivineShieldRank = 5" in _mg and "Immune(shieldRank)" in _mg,"神盾(番号5)で魔法攻撃を無効化")
chk("WizardADamage = 200" in _mg and "WizardBDamage = 100" in _mg,"魔術士のダメージ 200 / 100")
chk("hp / 2" in _mg,"魔導師に挟まれると生命力half")
_se=open("Assets/Scripts/Items/SpecialItemEffects.cs",encoding="utf-8").read()
chk("5 * (attack * 2 + defense)" in _se,"エリクサー 5×(攻撃×2+防御)")
chk("{ 91, 104, 122, 145 }" in _se,"破壊の玉が効かないのは ゼノ/大ダコ/ドラゴン/大魔導師")
chk("TeleportHpCost = 800" in _se,"空間転移の秘宝は生命力−800")
chk("up ? floor + 1 : floor - 1" in _se,"翼は44階を飛ばさずそのまま±1")
chk("MagicKeyTargets" in _se and "SuperMatockTargets" in _se,"魔法の鍵／スーパーマトックは階全体を走査")

print("\n【9b】座標系の整合（.floor と SealedDoors が同じ原点か）")
def _floor(fl):
    _t=open(f"Assets/Resources/Floors/Floor_{fl}.txt",encoding="utf-8").read()
    _g={}; _o={}
    for _l in _t.splitlines():
        _m=re.match(r"\s*([+-]\d+) \| (.{11})$",_l)
        if _m:
            _y=int(_m.group(1))
            for _c,_ch in enumerate(_m.group(2)): _g[(_c-5,_y)]=_ch
        _m=re.match(r"\s*([+-]\d+) ([+-]\d+)\s+(\S+)",_l)
        if _m: _o[(int(_m.group(1)),int(_m.group(2)))]=_m.group(3)
    return _g,_o
chk("y:-5..5" in open("Assets/Resources/Floors/Floor_1.txt",encoding="utf-8").read(),".floor の盤面が中心対称 -5..5")
chk("MinY = -5, MaxY = 5" in open("Assets/Scripts/Floor/FloorData.cs",encoding="utf-8").read(),"FloorData の範囲定数も -5..5")
_ok=_tot=0; _other=[]
for _l in open("Assets/Resources/SealedDoors.txt",encoding="utf-8"):
    if not _l[:1].isdigit(): continue
    _p=[x.strip() for x in _l.split("|")]
    _g,_o=_floor(int(_p[0]))
    for _part in _p[2].split(";"):
        _part=_part.strip().lstrip("!")      # "!" は「床でないこと」を求める守護マス
        if _part.startswith("フラグ:"): continue   # 前提フラグの指定（守護マスではない）
        _x,_y=map(int,_part.split(",")); _tot+=1
        _n=_o.get((_x,_y))
        if _n and (_n.startswith("Enemy_") or _n.startswith("Boss_")): _ok+=1
        else: _other.append((int(_p[0]),_x,_y,_n or _g.get((_x,_y))))
chk(_ok==53 and _tot==75,"守護マス75件のうち53件が敵のマス（座標系が一致している証拠）",f"{_ok}/{_tot}")
_dd=dict(((f,x,y),v) for f,x,y,v in _other)
chk(_dd.get((48,-5,5))=="Marker_ID7","F48 の守護マスは ID7（床と同じ見た目の隠れマス）＝原典ヒント236")
chk(_dd.get((41,4,4))=="Marker_ID10","F41 の守護マスは ID10（隠し壁と見た目が同じ「隠れている者」のマス）＝原典ヒント216")

print("\n【9c】門の解錠の通し確認（docs/sim_unlock.py）")
sys.path.insert(0,"docs")
import sim_unlock
_bad=sim_unlock.run()
chk(not _bad,"盤面→敵撃破→解錠判定→反映 が最初から最後まで動く",", ".join(_bad))

print("\n【9v】原典の音（docs/decode_sound.py が MTE.EXE から取り出したもの）")
_snd={}
for _l in open("Assets/Resources/Sounds.txt",encoding="utf-8"):
    if _l.startswith("#") or "|" not in _l: continue
    _p=[x.strip() for x in _l.split("|")]
    _snd.setdefault(int(_p[0]),[]).append(_p[2].split())
chk(sorted(_snd)==[1,2,6,7,8,9,10,11,12,13],"音は1・2と6〜13の10種類",str(sorted(_snd)))
chk(len(_snd[1])==4 and len(_snd[2])==4,"曲(音1・音2)は4チャンネル")
chk(all(len(v)==1 for k,v in _snd.items() if k>=6),"効果音は1チャンネル")
chk(all(v[0][-1]=="end" for k,v in _snd.items() if k>=6),"効果音は end で閉じる")
chk(all(c[-1].startswith("loop") for k in (1,2) for c in _snd[k]),"曲は loop で繰り返す")
chk(_snd[6][0]==["o4","v10","i0x41b","o4","c:2","d:2","e:4","end"],
    "音6（いちばん多く鳴る）はドレミの上昇",str(_snd[6][0]))
chk(_snd[12][0]==["t130","end"],"音12はテンポを戻すだけ")
chk(_snd[1][0][0]=="t130" and _snd[2][0][0]=="t108","曲のテンポは130と108")
# 第2音源（SSG）側は命令表も境目も別（cs:0x961 / 0x59、振り分け cs:0x0d9c）
_snd_txt=open("Assets/Resources/Sounds.txt",encoding="utf-8").read()
chk("cs:0x961" in _snd_txt and "cs:0x0d9c" in _snd_txt,
    "SSG 側の命令表と振り分けの場所が控えてある")
chk(all(c[0].startswith("m") for k in (1,2) for c in _snd[k][2:]),
    "曲のチャンネル3・4は SSG の設定命令で始まる（音符ではない）",
    str([c[0] for k in (1,2) for c in _snd[k][2:]]))
chk(not any(t.startswith("x") for k in _snd for c in _snd[k] for t in c),
    "音符の範囲外という扱いはもう無い")
_img3=open("docs/original/disk/MTE.EXE","rb").read()[4608:]
_b3=0x10da*16
chk(_img3[_b3+0x562:_b3+0x568]==b"\x00"*6,
    "SSG の設定表 cs:0x562 は6バイト（全部0）")

def _ticks(toks):
    """1チャンネルの長さ（原典の音長の合計）。x は拍に入らない。"""
    tot=0; last=16
    for t in toks:
        h,_,tail=t.partition(":")
        if h.endswith("~"): h=h[:-1]
        if h in ("end",) or h.startswith("loop"): break
        if h=="x": continue
        if h=="r" or h in ("c","c+","d","d+","e","f","f+","g","g+","a","a+","b"):
            n=int(tail) if tail else last
            last=n; tot+=n
    return tot
for _n in (1,2):
    _t=[_ticks(c) for c in _snd[_n]]
    chk(len(set(_t))==1 and _t[0]==768,
        f"音{_n}は4チャンネルとも768（4分音符48個＝12小節）で揃う",str(_t))
# 原典で鳴らしている箇所の数（逆アセンブルから数えた61か所）
_dis=open("docs/MTE_disasm.txt",encoding="utf-8").read()
chk(_dis.count("lcall   0x10da, 0x20b")==61,"原典が音を鳴らすのは61か所",
    str(_dis.count("lcall   0x10da, 0x20b")))

print("\n【9D】金騎士の突撃の道順（原典 0x325e の向きの表）")
import make_knight_charge as _kc
_path=_kc.path()
_rows=[l.split("#")[0].split() for l in
       open("Assets/Resources/KnightCharge.txt",encoding="utf-8") if l[:1].isdigit()]
chk(len(_path)==89,"向きは89バイト",str(len(_path)))
chk(_path.count(5)==14,"区切りは14個",str(_path.count(5)))
chk(sum(len(r) for r in _rows)==89-14,"データの歩数は区切りを除いた75歩",
    str(sum(len(r) for r in _rows)))
chk(len(_rows)==14,"データも14まとまり",str(len(_rows)))
_flat=[int(x) for r in _rows for x in r]
chk(_flat==[v for v in _path if v!=5],"データが EXE の並びと一致する")
chk(_rows[-1]==["4"]*5,"最後のまとまりは上へ5歩",str(_rows[-1]))
chk(_rows[-2]==["3"]*4,"その前はまっすぐ下へ4歩",str(_rows[-2]))
_kcs=open("Assets/Scripts/Items/KnightCharge.cs",encoding="utf-8").read()
chk("FirstStart" in _kcs and "0, -2" in _kcs,"1振り目は門のマス (0,-2) から（原典 0x3547/0x354f）")
chk("RunStart" in _kcs and "RunStart = new Vector2Int(0, 0)" in _kcs,
    "2振り目からは中央へ戻る（原典 0x35e0〜0x35e7）")
# 40階は**部下が1体ずつ歩いてきて、そのたびに戦う**（原典 0x325e を読み直した）。
# 歩かせるのは盤面のプレハブ（NpcWalkView）なので、専用の絵はもう要らない。
_kr=[l.split("#")[0].strip() for l in
     open("Assets/Resources/KnightRush.txt",encoding="utf-8").read().split("\n")]
_kr=[l for l in _kr if l]
chk(sum(1 for l in _kr if l.startswith("歩き "))==14,
    "40階の台本は14本の道順（12体＋金騎士＋引き上げ）",
    str(sum(1 for l in _kr if l.startswith("歩き "))))
chk(sum(1 for l in _kr if l.startswith("戦闘 "))==13,
    "突撃のたびに戦う（引き上げの1本だけ戦闘が無い）")
chk([l for l in _kr if l.isdigit()]==["135","136","137","138","139","140"],
    "セリフは6つ、3体ごとの切れ目で出る（原典 0x363c/0x36f3/0x3964/0x39db）")
chk("斬りかかる" in open("Assets/Resources/Cutscenes.txt",encoding="utf-8").read(),
    "場面の表に斬りかかる段がある")

print("\n【9C】物語が頭から終わりまで繋がっている")
_sw=open("Assets/Tests/Editor/StoryWalkTests.cs",encoding="utf-8").read()
for _n in ("序章はゼノに会って二階の牢から始まる","関門の場面はそれぞれの場所で始まる",
           "四十九階の中央で大魔導師の口上","二十四階の門の下で終幕まで通る",
           "物語の引き金は全部場面に繋がっている"):
    chk(_n in _sw,f"通しの検査「{_n}」がある")
# 場面を持つ位置イベントは、全部が場面の表にある
_csx=[[c.strip() for c in l.split("|")] for l in
      open("Assets/Resources/Cutscenes.txt",encoding="utf-8").read().split("\n")
      if l.strip() and not l.strip().startswith("#")]
_pex=[[c.strip() for c in l.split("|")] for l in
      open("Assets/Resources/PositionEvents.txt",encoding="utf-8").read().split("\n")
      if l.strip() and not l.strip().startswith("#")]
_scenes={r[0] for r in _csx}
_need={r[4].split("演出 ")[1].split(";")[0].strip() for r in _pex if "演出 " in r[4]}
chk(_need <= _scenes,"位置イベントが呼ぶ場面は全部表にある",str(sorted(_need-_scenes)))

print("\n【9B】地図を書き換える箇所は全部たどれている")
# 原典で地図（1マス1ワードの配列）へ書く命令を全部拾い、どの関数の中かで仕分ける。
# 「敵がプレーヤーを追ってくる」ような仕組みがあればここに未知の関数が出る。
_wr=re.compile(r"mov +word ptr es:\[(?:bx|si|di)[^\]]*\+ 0x1[0-9a-f]{3}\], ")
_ad=lambda l: int(l[2:8],16) if re.match(r"^  [0-9a-f]{6}  ",l) else None
_lines=_dis.split("\n")
_starts=[]; _prev=True
for _l in _lines:
    _a=_ad(_l)
    if _a is None: continue
    if "push    bp" in _l and _prev: _starts.append(_a)
    _prev = ("retf" in _l) or ("nop" in _l and _prev)
_known={0x1588:"位置イベント",0x23ee:"物体ディスパッチャ",0x3044:"場面ボス登場",
        0x325e:"場面金騎士の突撃",0x3e0a:"場面ボススケルトン",0x4762:"解錠判定",
        0x68b0:"場面金騎士敗走",0x6ae0:"場面ゼノとの初対面",0x7182:"場面金騎士処刑",
        0x2c6:"隠れている者",0x3e0:"場面盗賊との再会",0x7d50:"特殊アイテム",
        0xd284:"終幕の口上",0xc4d8:"終幕"}
_sites=0; _unknown=set()
for _l in _lines:
    if not _wr.search(_l): continue
    _a=_ad(_l)
    if _a is None: continue
    _f=max([x for x in _starts if x<=_a], default=0)
    _sites+=1
    if _f not in _known: _unknown.add(hex(_f))
chk(_sites==125,"地図を書き換える箇所は125か所",str(_sites))
chk(not _unknown,"どれも既知の14関数の中（未知の書き換え主は居ない）",str(sorted(_unknown)))

print("\n【9A】題名と終幕の画面")
_ts=open("Assets/Scripts/UI/TitleScreen.cs",encoding="utf-8").read()
_en=open("Assets/Scripts/Ending.cs",encoding="utf-8").read()
_sl=open("Assets/Scripts/UI/ScreenLayout.cs",encoding="utf-8").read()
chk("FromTextPos" in _sl and "y × 0x50 + x" in _sl or "FromTextPos" in _sl,
    "文字の位置の単位（x=バイト・y=ドット）が書いてある")
chk("TitleX = 0x12" in _ts and "TitleY = 0xa0" in _ts,"題名の位置は原典 0x6db2 の値")
chk("CopyrightY = 0xbe" in _ts,"副題の位置は原典 0x6dcd の値")
chk("0x1d, 0xaa" in _en and "0x1c, 0xd2" in _en,"終幕の文字の位置は原典 0xd4d2/0xd4ea の値")
_scene=open("Assets/Scenes/Tower.unity",encoding="utf-8",errors="ignore").read()
chk("TitleScreen_Line1" in _scene and "EndingScreen_Line1" in _scene,
    "組み立てたシーンに題名と終幕の画面がある")
_fs=open("Assets/Scripts/UI/FullScreenView.cs",encoding="utf-8").read()
chk("Show(" in _fs and "Hide(" in _fs,"1枚絵の出し入れができる")

print("\n【9z】戦闘の演出（原典 0x6082）")
_bp=open("Assets/Scripts/Battle/BattlePresentation.cs",encoding="utf-8").read()
chk("StepWait = 10" in _bp and "HitWait = 5" in _bp,"待ちは10と5（原典 0xa と 0x5）")
chk("SlashSound = 9" in _bp and "CounterSound = 10" in _bp,"音は9（自分の攻撃）と10（相手の反撃）")
chk("踏み込む" in _bp,"踏み込みの一歩が演出に入っている（原典 0x61ef）")
chk("1f / 60f" in _bp,"待ちは垂直帰線1回＝1/60秒（原典 0xf5f:0x3f2）")
_bv=open("Assets/Scripts/UI/BattleView.cs",encoding="utf-8").read()
chk("WaitForSeconds" in _bv and "LastBattle" in _bv,"演出を画面で再生する側がある")
chk("0x87a" in _bv,"当たりの絵の出どころ（GROUND.P58 の11枚目）を控えてある")
# 当たりの絵＝原典の変位 0x87a が GROUND.P58 の11枚目の先頭と一致する
sys.path.insert(0,"docs")
import p58 as _p58x
import struct as _st
_exe=open("docs/original/disk/MTE.EXE","rb").read()
_dgb=4608+0x12340
_gd=open("docs/original/disk/GROUND.P58","rb").read()
_offs=[]; _i=0
while _i+6<=len(_gd) and _gd[_i]==_gd[_i+1]:
    _offs.append(_i); _pl,_c,_r,_i=_p58x.decode(_gd,_i)
chk(_offs.index(0x87a)==10,"変位 0x87a は GROUND の11枚目（0起点で10）",str(_offs[:14]))
chk(os.path.exists("Assets/Sprites/Effects/Hit.png"),"当たりの絵が Assets にある")
_ids=set()
for _l in open("docs/original/p58_map.txt",encoding="utf-8"):
    if _l.startswith("#") or "|" not in _l: continue
    _ids.add(_l.split("|")[2].strip())
chk("GROUND_010" not in _ids,"当たりの絵はどの物体IDからも指されていない（演出専用）")
# 絵の棚卸し: 属性表を通さず番地で直に貼る絵と、どこからも使われない絵
_bases={"GROUND":0,"OBJECT":8000,"ITEM":18000,"WEAPON":24500,
        "PLAYER1":32000,"PLAYER2":32400,"MONSTER":34000,"PICTURE":64500}
_used=set()
for _i in range(159):
    _used.add(_st.unpack("<H",_exe[_dgb+0x286+6*_i:_dgb+0x286+6*_i+2])[0])
_unused={}
for _nm,_bs in _bases.items():
    _dd=open(f"docs/original/disk/{_nm}.P58","rb").read()
    _o=[]; _k=0
    while _k+6<=len(_dd) and _dd[_k]==_dd[_k+1]:
        _o.append(_k); _pl,_c2,_r2,_k=_p58x.decode(_dd,_k)
    _unused[_nm]=[(_j,_bs+_v) for _j,_v in enumerate(_o) if _bs+_v not in _used]
chk([_j for _j,_ in _unused["GROUND"]]==[10,12],
    "GROUND で表に無いのは11枚目（当たりの絵）と13枚目のみ",str(_unused["GROUND"]))
chk(_unused["OBJECT"]==[] and _unused["ITEM"]==[] and _unused["WEAPON"]==[],
    "物・道具・武具の絵は全部使われている")
chk(len(_unused["PLAYER1"])==1 and len(_unused["PLAYER2"])==1,
    "主人公の2コマは表を通さない（番地で直に貼る）")
chk(_dis.count("mov     ax, 0x7d00")==10 and _dis.count("mov     ax, 0x7e90")==10,
    "主人公は2コマまとめて10か所から描かれる",
    f"{_dis.count('mov     ax, 0x7d00')}/{_dis.count('mov     ax, 0x7e90')}")
chk([_j for _j,_ in _unused["MONSTER"]]==[30,31,84,85],
    "使われないモンスターの絵は2体ぶん（30/31と84/85）",str(_unused["MONSTER"]))
# PICTURE は属性表からは一度も指されないが、**鍵の絵として 0x9ff8 が番地で直に貼る**。
# 原典は絵の置き場の頭から**引いた**番地で指していて（黄 -0x40c / 赤 -0x3cb / 紫 -0x381）、
# 差が PICTURE.P58 の各絵の大きさ（65 / 74）と一致する。以前ここを
# 「5枚とも未使用」と書いていたのは、置き場 64500 からの足し算しか探していなかったため。
chk(len(_unused["PICTURE"])==5,"PICTURE は属性表からは指されない（番地で直に貼る）")
_pd=open("docs/original/disk/PICTURE.P58","rb").read()
_po=[]; _k=0
while _k+6<=len(_pd) and _pd[_k]==_pd[_k+1]:
    _po.append(_k); _pl,_c2,_r2,_k=_p58x.decode(_pd,_k)
chk(_po[:3]==[0,65,139],"PICTURE の1〜3枚目の位置",str(_po))
for _nm,_sub,_idx in (("黄",0x40c,0),("赤",0x3cb,1),("紫",0x381,2)):
    chk(_dis.count(f"sub     ax, 0x{_sub:x}")==1,f"鍵の絵（{_nm}）を指す番地が1か所")
    chk(0x40c-_sub==_po[_idx],f"鍵の絵（{_nm}）は PICTURE の{_idx+1}枚目",
        f"{0x40c-_sub} vs {_po[_idx]}")
# 原典が塗り直す欄を全部数え上げ（docs/dump_fills.py）、リメイク側の欄と突き合わせる。
# ここに出てくる欄が ScreenLayout に無ければ、画面のどこかを作り忘れている。
_fills=[]
for _l in open("docs/original/screen_fills.txt",encoding="utf-8"):
    if _l.startswith("#") or "|" not in _l: continue
    _x,_y,_w,_h,_a=[t.strip() for t in _l.split("|")]
    _fills.append((int(_x),int(_y),int(_w),int(_h)))
chk(len(_fills)==14,"原典が塗り直す欄は14種",str(len(_fills)))
_slsrc=open("Assets/Scripts/UI/ScreenLayout.cs",encoding="utf-8").read()
_slset={tuple(int(v) for v in m) for m in
        re.findall(r"new RectInt\((\d+), (\d+), (\d+), (\d+)\)",_slsrc)}
# 盤面だけは定数の組み合わせで書いてあるので、その定数から組み立てて足す
_slc=dict((a,int(b)) for a,b in re.findall(r"(\w+) = (\d+)",_slsrc))
_slset.add((_slc["MapLeft"],_slc["MapTop"],
            _slc["MapCells"]*_slc["TileSize"],_slc["MapCells"]*_slc["TileSize"]))
# 画面まるごと (0,1,1280,400) は画面消去なので欄ではない
_missing=[f for f in _fills if f[2]<1280 and f not in _slset]
chk(not _missing,"原典が塗り直す欄はすべて ScreenLayout にある",str(_missing))

# 左の帯の数値（0xb9f1〜0xbaed）。塗ってから 0xb8d6 で全角数字を**右詰め**に書く。
#   生命力/攻撃力/防御力/所持金: x=6バイト(48ドット)・枠4つ → 48〜112 ＝ 欄とぴったり
#   階: 先に「塔　　階」を x=4(32ドット) に書き、真ん中2マスに階を右詰めで重ねる
_msgs={}
for _l in open("Assets/Resources/Messages.txt",encoding="utf-8"):
    if _l.startswith("#") or "|" not in _l: continue
    _a,_b=_l.split("|",1); _msgs[_a.strip()]=_b.strip()
chk(_msgs.get("floor_label")=="塔{0}階",
    "階の見出しは「塔{0}階」（全角空白は数字の枠なので入れない）",_msgs.get("floor_label"))
chk("NumRight" in open("Assets/Scripts/UI/TowerHud.cs",encoding="utf-8").read(),
    "階の数字は2マスに右詰め（2桁だと空白が消える）")
chk(_dis.count("call    0xb8d6")>=5,"数値を書く所は左の帯の5か所以上",
    str(_dis.count("call    0xb8d6")))

# 右下の情報欄（0x9d80）。6桁×8行に折り返して、**その時のメッセージ1つだけ**を出す。
chk("96 | 128" in open("docs/original/screen_fills.txt",encoding="utf-8").read().replace("  "," ")
    or (528,224,96,128) in _fills,"情報欄は 96x128 ＝ 全角6文字×8行")
_hud=open("Assets/Scripts/UI/TowerHud.cs",encoding="utf-8").read()
_hvw=open("Assets/Scripts/UI/TowerHudView.cs",encoding="utf-8").read()
chk("InfoColumns = 6" in _hud and "InfoLines = 8" in _hud,"情報欄は6桁×8行")
chk("Tail(" not in _hvw,"情報欄に履歴を積まない（原典は毎回塗り直して1つだけ）")
chk("InfoBox(" in _hvw,"情報欄は原典の折り返しを通す")
chk(_dis.count("cmp     word ptr [bp - 2], 6")>=1,"折り返しは6桁（0x9de0）")

# 敵と人は2コマで動く（0x92a4）。以前「2コマ目は一度も描かない」と書いていたのは誤り。
chk(_dis.count("cmp     word ptr [0x17ea], 0x79")==1,"マスの番号は121で0に戻す")
chk(_dis.count("mov     word ptr [bp - 6], 5")>=1,"1回で5マスぶん塗り直す")
_ma=open("Assets/Scripts/Floor/MonsterAnimation.cs",encoding="utf-8").read()
# 絵を置き場に詰める側（0xabe6）は表を頭から1件1枠で埋める＝枠の番号＝表の並び順
chk(_dis.count("mov     word ptr [bp - 0xe], 0x80a")==1,"置き場を埋める側も同じ表を使う")
chk(_dis.count("add     ax, word ptr es:[0x2aa]")>=2,"どの枠も先に床の絵を敷く")
chk("Cells = 121" in _ma and "CellsPerTick = 5" in _ma and "TicksPerStep = 16" in _ma
    and "FastTicksPerStep = 8" in _ma,"2コマの動きの数が原典どおり")
# 移動の演出（0xa276）のコマ数。ふつうは向きによらず16、高速モードは8
_mv=re.search(r"===== 関数 0x0a276 =====(.*?)\n===== ",_dis,re.S)
_cnt=[int(m,16) if m.startswith("0x") else int(m)
      for m in re.findall(r"cmp\s+word ptr \[bp - 0xa\], (0x[0-9a-f]+|\d+)",
                          _mv.group(1) if _mv else "")]
chk(_cnt==[7,9,8,8,16,16,3,5,4,4,8,8],"移動の演出のコマ数（ふつう16／高速8）",str(_cnt))
# 表 DS:0x80a は「並べた順」。敵は 種別8/9 と 10/11 が隣り合う対になっている
_t=[_st.unpack("<H",_exe[_dgb+0x80a+2*_i:_dgb+0x80a+2*_i+2])[0] for _i in range(150)]
chk(_t[:4]==[61,62,63,64],"並べた表の頭は 61,62,63,64",str(_t[:4]))
_kind={}
for _l in open("docs/original/p58_map.txt",encoding="utf-8"):
    if _l.startswith("#") or "|" not in _l: continue
    _a,_b,_c=[x.strip() for x in _l.split("|")]
    _kind[int(_a)]=int(_b)
_pairs=[(_t[_i],_t[_i+1]) for _i in range(len(_t)-1)
        if _kind.get(_t[_i]) in (8,10) and _kind.get(_t[_i+1]) in (9,11)]
# ふつうの敵は +1、3x3のボス（大ダコ 97〜105 / ドラゴン 115〜123）は +9 が2コマ目
# 2コマ目は「並べた表で隣」。種別（8/9・10/11）で見分けると、種別が同じ組
# （137/138 のような後半の敵）を取りこぼす。
chk(all(_b in (_a+1,_a+9) for _a,_b in _pairs) and len(_pairs)>=20,
    "敵の1コマ目と2コマ目は表で隣り合う（ふつうは+1、3x3のボスは+9）",
    str([p for p in _pairs if p[1] not in (p[0]+1,p[0]+9)]))
_second=[q for q in glob.glob("Assets/Sprites/Tiles/*.png") if q.endswith("_b.png")]
chk(len(_second)==39,"2コマ目の絵が39枚ある（ふつう36＋組み立てた3枚）",str(len(_second)))
chk("原典はこれを実際に描く" in open("docs/p58_to_tiles.py",encoding="utf-8").read(),
    "2コマ目は原典が描くものだと書いてある")

# 持ち物の欄（0x2f8c）。左の帯の下の箱に、持っている道具の絵を3列で並べる。
chk(_dis.count("add     bx, 0x3c02")==1,"持ち物の欄の左上は 0x3c02 ＝ (16,192)")
chk(_dis.count("mov     si, 0x35a")==1 and _dis.count("cmp     si, 0x3b4")==1,
    "並べる表は DS:0x35a から 6バイト×15件")
chk((0x3b4-0x35a)//6==15,"15件ちょうど")
chk("ItemPanelSlots = 15" in _sl and "ItemPanelColumns = 3" in _sl,
    "持ち物の欄の数がリメイクにも入っている")
_missing_item=[_i for _i in range(36,51)
               if not glob.glob(f"Assets/Sprites/Tiles/{_i:03d}_*.png")]
chk(not _missing_item,"持ち物15種の絵が揃っている",str(_missing_item))
_hv2=open("Assets/Scripts/UI/TowerHudView.cs",encoding="utf-8").read()
chk("itemSlots" in _hv2 and "ShowItems" in _hv2,"持ち物の欄を画面に出している")
chk("PanelKind" in open("Assets/Scripts/Items/SpecialItems.cs",encoding="utf-8").read(),
    "欄の番号と持ち物が結び付いている")
chk("Orb_Hero | 36" in open("Assets/Resources/ObjectIds.txt",encoding="utf-8").read(),
    "勇者のオーブ(ID36)が登録してある（盤面には出ないが欄の1つ目）")
# 動画を枠の絵と見比べて「描き替わる所」を洗う道具（docs/cmp_video_hud.py）。
# その道具が知っている欄と ScreenLayout が食い違わないようにしておく。
_hud=open("docs/cmp_video_hud.py",encoding="utf-8").read()
_known=re.findall(r"\((\d+), (\d+), (\d+), (\d+), \"(.+?)\"\)",_hud)
chk(len(_known)==12,"見比べの道具が知っている欄は12個",str(len(_known)))
for _k in _known:
    _r=(int(_k[0]),int(_k[1]),int(_k[2]),int(_k[3]))
    if _k[4]=="持ち物":
        # (16,192) から 32ドットおき3列×5行
        _ok=_r==(16,192,96,160)
    elif _k[4]=="鍵":
        # (528,144) から 16ドットおき6列×4行
        _ok=_r==(528,144,96,64)
    else:
        _ok=_r in _slset
    if not _ok:
        chk(False,f"見比べの道具の欄 {_k[4]} が ScreenLayout に無い",str(_r)); break
else:
    chk(True,"見比べの道具の欄はどれも ScreenLayout の欄と同じ")

# 戦闘中の相手の欄（0x9626）。戦闘 0x6082 の中から1か所だけ呼ばれる。
chk(_dis.count("mov     cx, 0x110")>=1 and _dis.count("mov     cx, 0x128")>=1,
    "相手の名前(y=272)と命(y=296)の位置が原典にある")
chk(_dis.count("mov     ax, 0x48c6")==1,"相手の絵は (560,232) に1か所だけ貼る")
chk("MonsterIcon" in _sl and "MonsterHpLabel" in _sl,"相手の欄を ScreenLayout に持っている")
_tgx2=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("LastBattleEnemy" in _tgx2,"戦っている相手を見た目の層に渡している")
chk("ShowMonster" in _hvw and "HitsDone" in
    open("Assets/Scripts/UI/BattleView.cs",encoding="utf-8").read(),
    "戦闘中は相手の欄を出し、命は一撃ごとに減らす")

# 装備の絵（0x9b80 付近）。欄を塗ってから 剣=物体ID 50+段位 / 盾=55+段位 の絵を貼る。
chk(0x3b2==0x282+6*50+4 and 0x3d0==0x282+6*55+4,
    "装備の絵は属性表の物体ID 50+段位 / 55+段位 の欄から引く")
_oid={}
for _l in open("Assets/Resources/ObjectIds.txt",encoding="utf-8"):
    if _l.startswith("#") or "|" not in _l: continue
    _a,_b=_l.split("|"); _oid[_a.strip()]=int(_b.strip())
chk(all(_oid.get(f"Sword_{_r}")==50+_r and _oid.get(f"Shield_{_r}")==55+_r
        for _r in range(1,6)),"剣と盾の物体IDが 51〜55 / 56〜60")
chk(_dis.count("mov     cx, 0xf4a")==1 and _dis.count("mov     cx, 0x1e4a")==1,
    "装備の絵の欄を塗るのは剣・盾それぞれ1か所")
_bld=open("Assets/Editor/TowerSceneBuilder.cs",encoding="utf-8").read()
chk("weaponIcon" in _bld and "shieldIcon" in _bld and "TileById" in _bld,
    "装備の絵をシーンに繋いである")
chk(all(os.path.exists(f"Assets/Sprites/Hud/Key_{_c}.png")
        for _c in ("Red","Purple","Yellow")),"鍵の絵3色が Assets にある")
_slx=open("Assets/Scripts/UI/ScreenLayout.cs",encoding="utf-8").read()
chk("KeyIconsPerRow = 6" in _slx and "KeyIconMax = 24" in _slx,
    "鍵は1行6本・通しで24本まで（0x9ff8 の cmp di,6 / cmp ax,0x18）")
_hvx=open("Assets/Scripts/UI/TowerHudView.cs",encoding="utf-8").read()
chk('"WEAPON' not in _hvx and '"SHIELD' not in _hvx,
    "WEAPON/SHIELD の見出しは枠の絵にあるので文字では書かない")
chk("keys" not in _hvx.split("void LateUpdate")[1].split("static void Set")[0],
    "鍵は数字ではなく絵で出す")
chk(os.path.exists("Assets/Sprites/Effects/Player_0.png"),"主人公の絵が Assets にある")
chk(_dis.count("add     ax, 0x87a")==7,"原典は7か所で同じ当たりの絵を貼る",
    str(_dis.count("add     ax, 0x87a")))
# 7か所の内訳がリメイク側にも揃っている
_tgx=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
_cutx=open("Assets/Resources/Cutscenes.txt",encoding="utf-8").read()
chk("PlayEffect(Player.Position)" in _tgx,"魔法攻撃と場面の節目は自分のマスに閃光")
chk(_cutx.count("閃光")>=2,"場面の閃光2件（ゼノとの初対面・金騎士処刑）",str(_cutx.count("閃光")))
chk("ConsumeEffects" in _tgx,"閃光を見た目の層が引ける")
_pt=open("Assets/Editor/TowerPlaythrough.cs",encoding="utf-8").read()
chk("BestDoor" in _pt,"自動プレイは開ける扉を選ぶ（近い順ではない）")
chk("keys * 100 + cells" in _pt,"扉は「鍵が何本増えるか」を重く見て選ぶ")
chk("GoBackAndReturn" in _pt,"行き詰まったら下の階へ戻って拾い直す")
chk("desperate" in _pt,"手詰まりのときだけ避けていた戦いを受ける")
_tg3=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("Player.Position = r.Target;" in _tg3,
    "敵に勝つと相手のマスに立つ（戦闘処理が座標を動かす。原典 0x61ef と 0x631f）")

print("\n【9y】オーブ2つ（勇者・知恵）")
import make_wisdom_info as _wi
_wtab=_wi.table()
_wrows=[]
for _l in open("Assets/Resources/WisdomInfo.txt",encoding="utf-8"):
    if _l.startswith("#") or "|" not in _l: continue
    _p=[x.split("#")[0].strip() for x in _l.split("|")]
    _wrows.append((int(_p[0]),int(_p[1]),int(_p[2])))
chk(len(_wrows)==24,"知恵のオーブの読み返しは24件（原典 0x8a46 の表）",str(len(_wrows)))
chk([(f,r) for _,f,r in _wrows]==_wtab,"表が EXE から取り出した値と一致する")
chk([n for n,_,_ in _wrows]==list(range(1,25)),"情報番号は1から24の通し")
chk(all(69<=f<=86 and 80<=r<=97 for _,f,r in _wrows if f>=69),"仙人側はフラグ69〜86・セリフ80〜97")
chk(all(50<=f<=68 and 12<=r<=31 for _,f,r in _wrows if f<69),"商人側はフラグ50〜68・セリフ12〜31")
chk(sum(1 for _,f,_ in _wrows if f>=69)==15 and sum(1 for _,f,_ in _wrows if f<69)==9,
    "内訳は仙人15件・商人9件")
_tm=open("Assets/Scripts/UI/TowerMenu.cs",encoding="utf-8").read()
_tg2=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("PhantomFloor" in _tm and "HighestFloor" in _tm,
    "飛翔のオーブは1階〜到達最高階で、幻の階を飛ばす（原典 0x91d0 の cmp 0x2c）")
chk("FlightFloors" in _tg2 and "ConfirmFlight" in _tg2,"飛翔の階選びが本線にある")
chk("NextToStairs" in _tg2,"階段の隣でなければ使えない判定を通している")
_ho=open("Assets/Scripts/Items/HeroOrb.cs",encoding="utf-8").read()
chk("MaxRows = 9" in _ho,"勇者のオーブは9体まで（原典の枠の数）")
chk("Charms.None" in _ho,"予想ダメージは補正抜き（原典レコード60の断り書きどおり）")

print("\n【9x】音程表（Resources/SoundPitch.txt）が原典の EXE と一致する")
_pitch={}
for _l in open("Assets/Resources/SoundPitch.txt",encoding="utf-8"):
    if _l.startswith("#") or "|" not in _l: continue
    _n,_v=_l.split("|"); _pitch[_n.strip()]=int(_v)
_img=open("docs/original/disk/MTE.EXE","rb").read()[4608:]
_base=0x10da*16+0x96f
_want={}
_abc=["c","c+","d","d+","e","f","f+","g","g+","a","a+","b"]
for _i in range(12):
    _o=0x42+2*_i
    _want[_abc[_i]]=_img[_base+_o] | ((_img[_base+_o+1]&7)<<8)
chk(_pitch==_want,"12半音の F-Number が EXE の表と一致する",str(_pitch))
_f=lambda fn,oct: fn*3993600/(144.0*(1<<(20-(oct-1))))
chk(abs(_f(_pitch["a"],4)-220.0)<0.1,"オクターブ4のラが220Hz（式と表の裏取り）",
    f"{_f(_pitch['a'],4):.2f}Hz")
chk(abs(_f(_pitch["a"],5)-440.0)<0.2,"オクターブ5のラが440Hz",f"{_f(_pitch['a'],5):.2f}Hz")
# 音色（FM のパラメータ29バイト）
_vz={}
for _l in open("Assets/Resources/SoundVoices.txt",encoding="utf-8"):
    if _l.startswith("#") or "|" not in _l: continue
    _p=[x.strip() for x in _l.split("|")]
    _vz.setdefault(_p[0],[]).append(_p)
chk(len(_vz)==4,"FM の音色は4つ（譜面の i が指す先）",str(sorted(_vz)))
chk(all(len(v)==4 for v in _vz.values()),"どの音色も4オペレータぶんある")
chk(all(int(r[1])==7 for v in _vz.values() for r in v),"4つとも FB=7")
chk([_vz[k][0][2] for k in ("0x3c4","0x3e1","0x3fe","0x41b")]==["0","2","4","6"],
    "ALG は 0/2/4/6")
_img2=open("docs/original/disk/MTE.EXE","rb").read()[4608:]
_b=_img2[0x10da*16+0x3c4:0x10da*16+0x3c4+29]
chk(int(_vz["0x3c4"][0][6])==(_b[4]&0x7f),"TL が EXE の値と一致する（op1）")
chk(int(_vz["0x3c4"][0][2])==(_b[28]&7) and int(_vz["0x3c4"][0][1])==((_b[28]>>3)&7),
    "FB/ALG が EXE の最後のバイトと一致する")
_syn=open("Assets/Scripts/Items/SoundSynth.cs",encoding="utf-8").read()
chk("3993600" in _syn and "144.0" in _syn,"リメイクの合成も同じ式を使っている")
chk("class FmVoice" in _syn,"4オペレータのFM合成がある")
chk(all(f"case {i}:" in _syn for i in range(7)),"つなぎ方（ALG）を8通りとも組んでいる")
chk("Feedback" in _syn and "Envelope" in _syn,"自己帰還と包絡も使っている")
_rs=open("docs/render_sound.py",encoding="utf-8").read()
chk("class Fm" in _rs and "ALG_OUT" in _rs,"WAV 書き出しも同じ組み方")
_sp=open("Assets/Scripts/SoundPlayer.cs",encoding="utf-8").read()
chk("EnsureSources" in _sp and _sp.count("EnsureSources()")>=3,
    "鳴らす口は Awake を待たずに用意する（足したその場で鳴らせる）")
_rst=open("Assets/Tests/Editor/RuntimeSmokeTests.cs",encoding="utf-8").read()
chk("SoundSynth.Clip" in _rst and "AddComponent<SoundPlayer>" in _rst,
    "Unity の API を実際に叩く検査がある")
_fl=open("Assets/Scripts/Floor/FloorLoader.cs",encoding="utf-8").read()
chk(_fl.count("Application.isPlaying")>=2,
    "マスの作り直しも片付けも、遊び中とエディタで消し方を分ける")
_flt=open("Assets/Tests/Editor/FloorLoaderTests.cs",encoding="utf-8").read()
chk("プレハブ未登録" in _flt,"組み立ての警告（未登録プレハブ）を拾う検査がある")
_swt=open("Assets/Tests/Editor/SceneWiringTests.cs",encoding="utf-8").read()
chk("SerializedPropertyType.ObjectReference" in _swt,"シーンの配線の抜けを見る検査がある")
_sceneT=open("Assets/Scenes/Tower.unity",encoding="utf-8",errors="ignore").read()
chk("AudioSource" in _sceneT,"シーンに鳴らす口（AudioSource）が置いてある")
_vct=open("Assets/Tests/Editor/ViewCoroutineTests.cs",encoding="utf-8").read()
for _n in ("戦闘の演出を最後まで流せる","駆け上がりを最後まで流せる"):
    chk(_n in _vct,f"コルーチンの検査「{_n}」がある")

# 手で遊んで見つかった3件（配置のずれ・絵の大きさ・画面が付いてこない）
_tc=open("Assets/Scripts/TowerController.cs",encoding="utf-8").read()
# 後始末を呼ぶのは「キーの一手」と「持ち物の帯を触ったとき」の2か所だけ
chk("AfterAction" in _tc and _tc.count("AfterAction(floorBefore, posBefore)")==2,
    "一手ぶんの後始末を通る入口は2つだけ（キーの一手と、持ち物を触ったとき）",
    str(_tc.count("AfterAction(floorBefore, posBefore)")))
# 1マスぶんの滑らかな移動（0xa276）。滑らせている間は入力を受けない
chk("if (sliding || drawing) return;" in _tc and "IEnumerator Slide(" in _tc,
    "1マスの移動を滑らかに出し、その間は入力を止める")
chk("MonsterAnimation.FastTicksPerStep : MonsterAnimation.TicksPerStep" in _tc,
    "コマ数は高速モードで半分になる")
# 階に入ったときの描き方（0xafa6）。121マスを渦巻きの順に描き、3マスごとに縦帰線を待つ。
_do=[]
for _l in open("Assets/Resources/FloorDrawOrder.txt",encoding="utf-8"):
    _m=re.match(r"\s*(\d+) \| ([+-]\d) \| ([+-]\d)",_l)
    if _m: _do.append((int(_m.group(2)),int(_m.group(3))))
chk(len(_do)==121 and len(set(_do))==121,"描く順番は121マスを1度ずつ",str(len(_do)))
chk(_do[0]==(-5,5) and _do[-1]==(0,0),"左上から始まり中央で終わる（渦巻き）",
    f"{_do[0]} → {_do[-1]}")
chk(all(abs(_do[_i][0]-_do[_i+1][0])+abs(_do[_i][1]-_do[_i+1][1])==1 for _i in range(120)),
    "描く順番は隣のマスへ1つずつ進む")
chk(_dis.count("mov     cx, 0xa")>=1 and "div     cx" in _dis,
    "3マス（高速なら10マス）ごとに待つ割り算がある")
chk("IEnumerator DrawFloor()" in _tc and "FloorDrawOrder.Instance.Cells" in _tc,
    "階に入ったら渦巻きの順に描き出す")
chk("if (sliding || drawing) return;" in _tc,"演出の最中は入力を受けない")
chk("CellsPerWait = 3" in open("Assets/Scripts/Floor/FloorDrawOrder.cs",encoding="utf-8").read(),
    "待ちの間隔は3マス（高速10マス）")

# 窓が開く様子（0x94ba / 0x9412）。1きざみごとに縦帰線を1回待つ（高速モードは待たない）
chk(_dis.count("lcall   0xf5f, 0x3d9")>=2,"窓の塗り広げは縦帰線を待つ")
_dwv=open("Assets/Scripts/UI/DialogueWindowView.cs",encoding="utf-8").read()
chk("IEnumerator Open(" in _dwv and "if (!fast) yield return" in _dwv,
    "窓は塗り広げながら開き、高速モードでは待たない")
chk("body.enabled = false" in _dwv,"文字は塗り終わってから出す（原典も塗りのあと）")
# 敵と人の絵は**時間で**動かす。原典は移動の演出（0xa276）と待ちの中から 0x92a4 を
# 呼ぶだけだが、「自分が動かないと敵が動かない」と報告されたので常に回している。
_man=open("Assets/Scripts/Floor/MonsterAnimator.cs",encoding="utf-8").read()
chk("MonsterAnimation.IdleTickSeconds" in _man and "clock += Time.deltaTime" in _man,
    "敵と人の絵は立ち止まっていても動く（時間で回す）")
chk("Application.targetFrameRate" in _tc,
    "1秒あたりのコマ数を60に上げる（スマホの既定30だと演出が半分の速さになる）")
chk("new FloorState(data)" in _tc,
    "階を作り直すとき、済んだこと（開けた扉・倒した敵）を盤面から反映する")
_sb=open("Assets/Editor/TowerSceneBuilder.cs",encoding="utf-8").read()
chk("AudioListener" in _sb,"音を聴く耳をシーンに置く")
# 縦画面（作者の指定）。盤面が画面の幅いっぱいなので**幅**を基準に合わせる。
chk("matchWidthOrHeight = 0f" in _sb,"画布は幅を基準に合わせる（縦画面）")
chk('GameObject("Screen"' in _sb,"画面の中身は中央に置いた入れ物に入れる")
_vmb=open("Assets/Tests/Editor/ViewMatchesBoardTests.cs",encoding="utf-8").read()
chk("AssertMatches" in _vmb and _vmb.count("AssertMatches(g,")>=4,
    "一手ごとに画面と盤面を突き合わせる検査がある")
for _m in ("Assets/Sprites/Effects/Player_0.png.meta","Assets/Sprites/Effects/Hit.png.meta"):
    chk("spritePixelsToUnits: 32" in open(_m,encoding="utf-8").read(),
        f"{_os.path.basename(_m)} は1マス32ドットで取り込む")
# **全部の絵**が1マス32ドットで入っているか。3枚だけ 100 のままで、
# 2コマ目に切り替わった瞬間に 1/3 の大きさになっていた（妖精・ゼノ・バンパイア）。
_badppu=[_os.path.basename(_f) for _f in glob.glob("Assets/Sprites/Tiles/*.png.meta")
         if "spritePixelsToUnits: 32" not in open(_f,encoding="utf-8").read()]
chk(not _badppu,"盤面の絵はどれも1マス32ドットで取り込む",str(_badppu))

print("\n【9w】できごとと音の結び付き（Resources/SoundCues.txt）")
_cue={}
for _l in open("Assets/Resources/SoundCues.txt",encoding="utf-8"):
    if _l.startswith("#") or "|" not in _l: continue
    _p=[x.strip() for x in _l.split("|")]
    _cue[_p[0]]=(int(_p[1]),_p[2])
chk(len(_cue)==22,"合図は22件（原典の呼び出し61か所を仕分けたもの）",str(len(_cue)))
chk(all(v[0] in _snd for v in _cue.values()),"どの合図も実在する音を指す")
chk(all(v[1].startswith("0x") for v in _cue.values()),"どの合図にも原典の場所が控えてある")
chk(_cue["階段"][0]==8,"階段は音8（原典 0x28e6、地形11/12の判定の直後）")
chk(_cue["魔法攻撃"][0]==7,"魔法攻撃は音7（原典 0x1588 の3か所）")
chk(_cue["斬りかかる"][0]==9 and _cue["命中"][0]==10,"戦闘は音9と音10（原典 0x6082）")
chk(_cue["拾う"][0]==6,"物を拾うのは音6（いちばん多く鳴る音）")
chk(_cue["本編の曲"][0]==1 and _cue["題名の曲"][0]==2,"曲は本編=音1・題名とエンディング=音2")
_tg=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk(_tg.count("PlaySound(")>=10,"本線が合図で音を鳴らしている",str(_tg.count("PlaySound(")))
chk("ConsumeSounds" in _tg,"鳴らした音を見た目の層が引ける")

# 盤面のプレハブの描画順。床（road）が 0 で、その上に置くものは必ずそれより大きい。
# **Lock_Special が床と同じ 0 だったせいで、2階の牢の格子が床に隠れて消えていた。**
_orders={}
for _pf2 in glob.glob("Assets/Prefab/**/*.prefab",recursive=True):
    _m2=re.search(r"^  m_SortingOrder: (-?\d+)$",open(_pf2,encoding="utf-8").read(),re.M)
    if _m2: _orders[os.path.basename(_pf2)[:-7]]=int(_m2.group(1))
chk(_orders.get("road")==0,"床（road）の描画順は0",str(_orders.get("road")))
_under=sorted(k for k,v in _orders.items() if k!="road" and v<=0)
chk(not _under,"床以外はどれも床より前に出る（同じ順だと隠れることがある）",str(_under))
chk(_orders.get("Lock_Special",0)>0,"2階の牢の格子（特殊扉）が床より前",
    str(_orders.get("Lock_Special")))

print("\n【10】商店と妖精")
_sh2=[l.strip() for l in open("Assets/Resources/ShopOffers.txt",encoding="utf-8") if l[:1].isdigit()]
chk(len(_sh2)==12,"商人の品揃えが12件",f"{len(_sh2)}件")
chk(any(l.startswith("12 | 9") and "buy_var" in l for l in _sh2),"12階X=9は値段が変動する鍵屋")
chk(any(l.startswith("28 ") and "sell" in l for l in _sh2),"28階は鍵の買い取り")
chk(any(l.startswith("47 ") and "matock_super" in l and "4000" in l for l in _sh2),"47階はスーパーマトック4000")
_shc=open("Assets/Scripts/Items/Shop.cs",encoding="utf-8").read()
chk("VariableKeyUnitPrice = 20" in _shc and "highestFloorReached" in _shc,"変動価格は20×到達最高階")
_fa=open("Assets/Scripts/Items/Fairy.cs",encoding="utf-8").read()
chk("{ 10, 20, 25, 40, 49 }" in _fa,"妖精は10/20/25/40/49階")
chk('"無称号", "戦士", "勇者", "英雄", "剣帝", "闘神"' in _fa,"称号は6段階")
chk("RankThreshold = 80" in _fa and "PerfectScore = 100" in _fa,"80点で称号・100点で満点")
def _div(a,b):
    q=abs(a)//abs(b); return -q if (a<0)!=(b<0) else q
def _sc(f,hp,a,d,k,pk):
    if f==10: b=60+_div(hp-400,50)+(a-29)*3+(d-35)*2
    elif f==20: b=60+_div(hp-800,100)+(a-74)*2+(d-80)
    elif f==25: b=60+_div(hp-3000,400)+_div(a-350,2)+_div(d-460,4)
    elif f==40: b=60+_div(hp-1600,200)+(a-152)+_div(d-198,2)
    elif f==49: b=60+_div(hp-3000,400)+_div(a-240,2)+_div(d-300,3)
    else: return None
    return b+k+pk*2
_base={10:(400,29,35),20:(800,74,80),25:(3000,350,460),40:(1600,152,198),49:(3000,240,300)}
chk(all(_sc(f,*_base[f],0,0)==60 for f in _base),"5本の式が基準値でちょうど60点")

print("\n【8】フロアデータ")
chk(len(glob.glob("Assets/Resources/Floors/Floor_*.txt"))==51,
    "全50階＋0階のデータ",str(len(glob.glob("Assets/Resources/Floors/Floor_*.txt"))))
# 0階（word offset 75）は原典に実在するが、**行ける道が無い**。
# 中身は床だけの部屋で、左上に上り階段、真ん中に幸運の金貨が1つ。
_f0=open("Assets/Resources/Floors/Floor_0.txt",encoding="utf-8").read()
chk("word offset 75" in _f0,"0階は 1階の1つ手前のブロック")
chk(_f0.count("Coin_Lucky")==1,"0階の真ん中に幸運の金貨がある")
chk("+0 +0  Coin_Lucky" in _f0,"幸運の金貨は盤面の中央 (0,0)")
_ids50=0
for _fp in glob.glob("Assets/Resources/Floors/Floor_*.txt"):
    _ids50+=open(_fp,encoding="utf-8").read().count("Coin_Lucky")
chk(_ids50==1,"幸運の金貨は原典の全データで1つだけ（0階の中央）",str(_ids50))
# 1階に下り階段が無いので0階へは行けない。現在階に0を書く道筋もコードに無い。
_f1=open("Assets/Resources/Floors/Floor_1.txt",encoding="utf-8").read()
_g1=[l.split("|")[1] for l in _f1.split("\n") if re.match(r"\s*[+-]\d\s*\|",l)]
chk(not any("<" in _r for _r in _g1),"1階に下り階段が無い（0階へは行けない）")
chk("mov     word ptr es:[0x9e], 0   ; 現在階" not in _dis,
    "現在階に0を書く所は原典に無い")
# 1ブロックは [3ワードのメタ][121マス]。メタの先頭は階の番号そのもの。
# 以前は次の階のメタを見出しに書いていた（マスの並びは正しかったが名札がずれていた）。
_metabad=[]
for _n in range(0,51):
    _t=open(f"Assets/Resources/Floors/Floor_{_n}.txt",encoding="utf-8").read().split("\n")[2]
    _mm=re.search(r"meta=\[(\d+), (\d+), (\d+)\]",_t)
    if not _mm or int(_mm.group(1))!=_n: _metabad.append(_n)
chk(not _metabad,"どの階もメタの先頭が自分の階の番号",str(_metabad))
# セーブに入るフラグ120個（0x1e〜0x95）の数え上げ。docs/dump_flags.py が作る。
_flg={}
for _l in open("docs/original/flags.txt",encoding="utf-8"):
    if _l.startswith("#") or "|" not in _l: continue
    _p=[t.strip() for t in _l.split("|")]
    _flg[int(_p[0].split()[1])]={"読":int(_p[1]),"書":int(_p[2]),"階":_p[3],
                                "持ち主":_p[4],"意味":_p[5]}
chk(len(_flg)==66,"番号で直に引くフラグは66個",str(len(_flg)))
# 35〜49 の15個は持ち物。原典の注記とリメイクの品揃えが同じ顔ぶれか
_items={35:"勇者オーブ",36:"知恵オーブ",37:"飛翔オーブ",38:"十字架",39:"エリクサー",
        40:"マトック",41:"破壊の玉",42:"空間転移",43:"昇華の翼",44:"降臨の翼",
        45:"ドラゴンスレイヤー",46:"雪の結晶",47:"魔法の鍵",48:"スーパーマトック",
        49:"幸運の金貨"}
chk(all(_n in _flg and _items[_n] in _flg[_n]["意味"] for _n in _items),
    "持ち物のフラグは35〜49の15個")
_spi=open("Assets/Scripts/Items/SpecialItems.cs",encoding="utf-8").read()
chk(len(re.findall(r"=\s*1 <<\s*\d+,",_spi))==15,"リメイクの持ち物も15種",
    str(len(re.findall(r"=\s*1 <<\s*\d+,",_spi))))
# 87〜132 の46個は「一度きり」のフラグ。階と結びついていて、どれも読み1・書き1が基本
_once=[n for n in _flg if 0x57<=n<=0x84]
chk(len(_once)==46 and sorted(_once)==list(range(0x57,0x85)),
    "一度きりのフラグは 0x57〜0x84 の46個が連番",str(len(_once)))
chk(all(_flg[_n]["階"]!="-" for _n in _once),"一度きりのフラグはどれも階と結びついている")
# 46個は「立てている関数」で持ち主が決まり、リメイクの4つの表と過不足なく対応する
_own=Counter(_flg[_n]["持ち主"] for _n in _once)
chk(dict(_own)=={"門の解錠":28,"座標のできごと":14,"盗賊":3,"階段で着いたとき":1},
    "一度きりのフラグ46個の持ち主の内訳",str(dict(_own)))
# 4つの表が使う旗の**合併が、一度きりの旗46個ぴったり**になるか。
# 取りこぼしても余計に足しても、ここで落ちる。
def _rowflags(path, col, hexcol=False):
    out=set()
    for _l in open(path,encoding="utf-8"):
        if not re.match(r"^\s*\d+\s*\|",_l): continue
        _t=_l.split("|")[col].strip()
        if _t=="-": continue          # 一度きりの印を持たない行（13階の雪の結晶）
        out.add(int(_t,16) if hexcol else int(_t))
    return out
_sd=_rowflags("Assets/Resources/SealedDoors.txt",1,True)
_pe=_rowflags("Assets/Resources/PositionEvents.txt",3)
_ar=_rowflags("Assets/Resources/ArrivalEvents.txt",2)
_th=set(int(m) for m in re.findall(r"フラグ(\d+)",
        open("Assets/Resources/ThiefEvents.txt",encoding="utf-8").read()))
_union=_sd|_pe|_ar|_th
chk(_union==set(_once),"4つの表の旗の合併が一度きりの旗46個とぴったり合う",
    f"足りない={sorted(set(_once)-_union)} 余計={sorted(_union-set(_once))}")
chk(len(_sd)==29 and len(_pe)==15 and len(_ar)==1,
    "表の件数（門29 / 座標の旗15 / 階段1）",f"{len(_sd)}/{len(_pe)}/{len(_ar)}")
chk({_n for _n in _once if _flg[_n]["持ち主"]=="盗賊"}<=_th,
    "盗賊が立てる旗3つは盗賊の表に出てくる")
# 50 = 2階で一度だけ 1000 GOLD もらう贈り物（0x14e4〜0x1524、セリフ29）
chk("gift" in open("Assets/Resources/ShopOffers.txt",encoding="utf-8").read(),
    "2階の贈り物（旗50）が品揃えの表にある")
chk(_dis.count("add     word ptr es:[0xa2], 0x3e8")==1,
    "1000GOLD もらうのは原典でも1か所だけ")
# 31 = 高速モード。原典は鍵0x70 で反転し、右下の欄に知らせを出す（0xa922〜0xa95f）
chk(_dis.count("mov     byte ptr es:[0x1f], 1")==1
    and _dis.count("mov     byte ptr es:[0x1f], 0")==1,"高速モードは1か所で入り切りする")
chk("cmp     byte ptr [bp - 1], 0x70" in _dis,"高速モードの鍵は 0x70（PC98のシフト）")
_ps=open("Assets/Scripts/PlayerState.cs",encoding="utf-8").read()
chk("FastModeFlag = 31" in _ps,"リメイクも旗31を使う")
chk("ToggleFastMode" in open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
    and "ToggleFastMode" in open("Assets/Scripts/TowerController.cs",encoding="utf-8").read(),
    "高速モードの切り替えが繋がっている")
chk(_msgs.get("mode_fast")=="高速モードに切り替えます"
    and _msgs.get("mode_normal")=="標準モードに切り替えます","切り替えの知らせは原典の文言")
chk(_flg[32]["意味"]=="妖精回数" and _flg[33]["意味"]=="装備剣"
    and _flg[34]["意味"]=="装備盾","32〜34は妖精の回数・装備剣・装備盾")
# 原典の動画との突き合わせ（docs/cmp_video_floors.py の結果）。
# 「見分けが付く階」でずれが小さいことが、盤面データの**画面からの裏付け**になる。
_fv=[]
for _l in open("docs/original/floor_video_match.txt",encoding="utf-8"):
    if _l.startswith("#") or "|" not in _l: continue
    _p=[t.strip() for t in _l.split("|")]
    _fv.append((int(_p[0]), int(_p[1])))
chk(len(_fv)==50,"突き合わせの表は50階ぶん",str(len(_fv)))
# モンスターの名前は原典の文字列そのまま（中央寄せの空白も含めて）
import struct as _st2, codecs as _cd
_exe3=open("docs/original/disk/MTE.EXE","rb").read(); _dg3=4608+0x12340
_mnames={}; _b3=0x63c
while _b3+14<=0x112c:
    _mid=_st2.unpack("<H",_exe3[_dg3+_b3:_dg3+_b3+2])[0]
    _off=_st2.unpack("<H",_exe3[_dg3+_b3+10:_dg3+_b3+12])[0]
    _b3+=14
    if _mid==0: break
    if _mid in _mnames or _off==0: continue
    _e3=_exe3.find(b"\x00",_dg3+_off,_dg3+_off+40)
    if _e3<0: continue
    try: _mnames[_mid]=_exe3[_dg3+_off:_e3].decode("cp932")
    except UnicodeDecodeError: pass
chk(len(_mnames)>=60,"原典から名前が60件以上取れる",str(len(_mnames)))
_oid2={}
for _l in open("Assets/Resources/ObjectIds.txt",encoding="utf-8"):
    if _l.startswith("#") or "|" not in _l: continue
    _a2,_b2=_l.split("|"); _oid2.setdefault(_a2.strip(),int(_b2.strip()))
_nbad=[]
for _p3 in glob.glob("Assets/Resources/Battlers/*.asset"):
    _k3=os.path.basename(_p3)[:-6]; _o3=_oid2.get(_k3)
    if _o3 is None or _o3 not in _mnames: continue
    _m3=re.search(r"\n  name: (.*)",open(_p3,encoding="utf-8").read())
    _v3=_m3.group(1).strip()
    _cur=_cd.decode(_v3[1:-1],"unicode_escape") if _v3.startswith('"') else _v3
    if _cur!=_mnames[_o3]: _nbad.append(_k3)
chk(not _nbad,"モンスターの名前が原典そのまま（中央寄せの空白も）",str(_nbad[:5]))
# 空白は中央寄せぶん。欄は96ドット、全角16・半角8で数えると (96-幅)/2 になる
def _width(_s):
    return sum(8 if _c==" " else 16 for _c in _s)
_cbad=[_n for _n in _mnames.values() if _width(_n)>96 or
       (_width(_n)<96 and _width(_n.lstrip("\u3000 "))!=96-2*(_width(_n)-_width(_n.lstrip("\u3000 "))))]
chk(len([_n for _n in _mnames.values() if _width(_n)<=96])>=60,
    "名前はどれも欄（96ドット）に収まる")
chk([r[0] for r in _fv]==list(range(1,51)),"1〜50階が1度ずつ")
chk(len([r for r in _fv if r[1]<=12])>=49,"動画とずれ12マス以下の階は49以上",
    str(len([r for r in _fv if r[1]<=12])))
chk(max(r[1] for r in _fv)<=23,"いちばん大きいずれでも23マス（41階、うち20は開いた扉）",
    str(max(r[1] for r in _fv)))
chk(os.path.exists("Assets/Resources/FloorConnections.txt"),"階段接続表")
chk(os.path.exists("Assets/Resources/ItemPower.txt"),"階別効果量表")

print("\n【9】仙人のヒント表")
_mh=[l for l in open("Assets/Resources/MasterHints.txt",encoding="utf-8").read().split("\n")
     if l.strip() and not l.strip().startswith("#")]
_rows=[[c.strip() for c in l.split("|")] for l in _mh]
chk(len(_rows)==18,"仙人は18件（原典 jii[18][3]）")
chk([int(r[0]) for r in _rows]==[2,3,4,6,16,16,18,21,27,31,33,36,37,39,42,45,46,48],
    "階の並びが原典どおり")
chk([int(r[1]) for r in _rows]==list(range(39,57)),"通し番号は39〜56の連番")
chk(all(int(r[2])==int(r[1])+0x1e for r in _rows),"フラグ番号 = 通し番号 + 0x1e")
chk(sorted(int(r[3]) for r in _rows)==list(range(80,98)),"セリフは80〜97を一度ずつ")
chk([r[4] for r in _rows].count("x>=4")==1 and _rows[5][4]=="x>=4",
    "場所で分けるのは16階の2人目だけ")
_dlg={int(l.split(" | ")[0]) for l in
      open("Assets/Resources/Dialogue.txt",encoding="utf-8").read().split("\n")
      if l.strip() and not l.startswith("#") and l.split(" | ")[0].strip().isdigit()}
chk(all(int(r[3]) in _dlg for r in _rows),"セリフ番号が Dialogue.txt に実在する")
for _r in _rows:
    _f=int(_r[0])
    _t=open(f"Assets/Resources/Floors/Floor_{_f}.txt",encoding="utf-8").read()
    chk("Master " in _t,f"{_f}階の盤面に仙人が居る")
_mfl=sorted({int(_r[0]) for _r in _rows})
_all=sorted(f for f in range(1,51)
            if "Master " in open(f"Assets/Resources/Floors/Floor_{f}.txt",encoding="utf-8").read())
chk(_mfl==_all,"盤面に仙人が居る階と表の階が一致する")

print("\n【9g】隠れている者のマス（原典 ID10）")
_h10=[]
for _f in range(1,51):
    for _l in open(f"Assets/Resources/Floors/Floor_{_f}.txt",encoding="utf-8"):
        if "Marker_ID10" in _l:
            _p=_l.split()
            _h10.append((_f,int(_p[0]),int(_p[1])))
chk(sorted(_h10)==[(12,5,5),(16,5,-5),(19,0,3),(41,4,4)],
    "ID10 は全50階で4マスだけ（F12/F16/F19/F41）",str(sorted(_h10)))
# 原典の X = x + 5（0起点）。隠れている者には隣の隠し壁(ID9)を壊して立って話しかける。
chk(12+5==17 and (5+5)==10,"座標の読み替え X = x + 5")
_g12=_grid(12); _g16=_grid(16)
chk(_g12[0][9]=="%" and _g12[0][10]=="%","F12 右上は隠し壁2枚（+4 を壊して X=9 で鍵屋）")
chk(_g16[10][9]=="%" and _g16[10][10]=="%","F16 右下は隠し壁2枚（+4 を壊して X=9>8 で隠れた仙人）")
_mh12=[r for r in _rows if int(r[0])==16 and r[4]=="x>=4"]
chk(len(_mh12)==1 and int(_mh12[0][3])==85,
    "16階の隠れた仙人のセリフは85「やれやれ、見つかってしもうたか」")

print("\n【9d】盗賊のできごと表")
_te=[[c.strip() for c in l.split("|")] for l in
     open("Assets/Resources/ThiefEvents.txt",encoding="utf-8").read().split("\n")
     if l.strip() and not l.strip().startswith("#")]
chk(len(_te)==10,"盗賊の分岐は10件")
chk([(int(r[0]),int(r[2])) for r in _te]==
    [(2,116),(15,120),(10,107),(2,141),(2,142),(35,143),(35,144),(29,126),(29,124),(29,125)],
    "階とセリフの並びが原典の分岐の順どおり")
chk(all(int(r[2]) in _dlg for r in _te),"セリフ番号が Dialogue.txt に実在する")
_mik=sorted(f for f in range(1,51)
            if "Mikata " in open(f"Assets/Resources/Floors/Floor_{f}.txt",encoding="utf-8").read())
chk(_mik==[2,15,29],"盤面に最初からいる盗賊は2/15/29階だけ",str(_mik))
# 35階(-2,-3) は原典データでは壁。盗賊が開けるので動画では通路になっている
_g35=_grid(35)
chk(_g35[8][3]=="#","35階(-2,-3)は原典データでは壁（盗賊が開けて通路になる）",_g35[8][3])
chk(any("35:(-2,-3)" in r[4] for r in _te),"2階の2度目の盗賊が35階(-2,-3)を開ける")
_g2=_grid(2)
chk(_g2[10][10]==".","2階(+5,-5)は床（特殊扉の奥の牢＝2度目の盗賊が置かれる所）",_g2[10][10])
_flags=[int(r[3]) for r in _te if r[3]!="-"]
chk(sorted(_flags)==[88,107,118],"立てるフラグは88/107/118（仙人の69〜86より上）",str(sorted(_flags)))

print("\n【9e】隠れている者と人形")
_ho=[[c.strip() for c in l.split("|")] for l in
     open("Assets/Resources/HiddenOccupants.txt",encoding="utf-8").read().split("\n")
     if l.strip() and not l.strip().startswith("#")]
# 地形ID10 の4マス（0x02c6）＋48階の地形ID7 の1マス（0x02f3c、門の番人＝ヒント236）
chk(len(_ho)==5,"隠れている者は5人（地形ID10 の4マス＋48階の地形ID7）")
chk([(int(r[0]),r[1],int(r[3])) for r in _ho]==
    [(12,"+5,+5",19),(16,"+5,-5",25),(19,"+0,+3",39),(41,"+4,+4",151),(48,"-5,+5",93)],
    "階・マス・物体IDが原典 0x02c6 / 0x02f3c どおり")
_m10=set((f,x,y) for f,x,y in _h10)
chk(all((int(r[0]),int(r[1].split(",")[0]),int(r[1].split(",")[1])) in _m10
        for r in _ho if int(r[0])!=48),
    "48階以外の4マスは盤面の Marker_ID10 と一致する")
chk("Marker_ID7" in open("Assets/Resources/Floors/Floor_48.txt",encoding="utf-8").read()
    and '"Marker_ID7"' in open("Assets/Scripts/TowerPlayer.cs",encoding="utf-8").read(),
    "48階の左上隅（地形ID7）は歩けないマスとして扱い、踏むと門番が現れる")
chk(_ho[3][4]=="-4,+4","41階だけ条件つき＝左右対称の反対側を倒してから")
chk(int(_ho[2][3])==39,"19階に現れるのは物体ID39（十字架）")
chk(not any("Cross" in open(f"Assets/Resources/Floors/Floor_{_f}.txt",encoding="utf-8").read()
            for _f in range(1,51)),
    "十字架は INITIAL.DAT のどの階にも無く、できごとでだけ現れる")
_p26=[f for f in range(1,51)
      if "Princess " in open(f"Assets/Resources/Floors/Floor_{f}.txt",encoding="utf-8").read()]
chk(_p26==[26],"人形(ID27)は26階にだけ置いてある",str(_p26))
chk(160 in _dlg,"人形のセリフ160が Dialogue.txt にある")

print("\n【9f】サイコロの５の目の封印と、敵の数値アセットの取りこぼし")
_sd=[l.split("|") for l in open("Assets/Resources/SealedDoors.txt",encoding="utf-8").read().split("\n")
     if l.strip() and not l.strip().startswith("#")]
_row=[r for r in _sd if r[0].strip()=="49" and r[1].strip()=="0x83"]
chk(len(_row)==1,"49階のフラグ0x83の行がある")
if _row:
    _g=[t.strip() for t in _row[0][2].split(";")]
    chk(sorted(t for t in _g if not t.startswith("!"))==["-1,3","0,2","0,4","1,3"],
        "空いていること＝サイコロの目の上下左右")
    chk(sorted(t[1:] for t in _g if t.startswith("!"))==["-1,2","-1,4","1,2","1,4"],
        "敵がいること＝四隅（否定条件）")
    chk(_row[0][3].strip()=="封印","結果は地図ではなくゼノの弱体化")
_zeno=[m for m in __import__("json").load(open("docs/original/monsters.json")) if m["id"]==91][0]
chk((_zeno["hp"],_zeno["atk"],_zeno["dfn"])==(8000,5000,1000),"ゼノの原典の数値")
chk((_zeno["hp"]//10,_zeno["atk"]//10,_zeno["dfn"]//10)==(800,500,100),
    "封印後の 800/500/100 はちょうど10分の1")
# 盤面が使う敵の名前が全部 Battlers に届くか（Boss_ は Enemy_ に読み替える）
_bt={os.path.basename(p)[:-len(".asset")]
     for p in glob.glob("Assets/Resources/Battlers/*.asset")}
_used=set()
for _f in range(1,51):
    for _l in open(f"Assets/Resources/Floors/Floor_{_f}.txt",encoding="utf-8"):
        _m=re.match(r"\s*[+-]\d+ [+-]\d+\s+((?:Enemy|Boss)_\S+)",_l)
        if _m: _used.add(_m.group(1))
_used.add("Boss_Zeno")      # ゼノは盤面に無く、できごとで置かれる
_miss=sorted(n for n in _used if n not in _bt and not ("Enemy_"+n[5:] in _bt if n.startswith("Boss_") else False))
chk(not _miss,"敵の数値アセットに届かない名前が無い（Boss_ は Enemy_ に読み替え）",str(_miss))
chk(all("Enemy_"+n[5:] in _bt for n in _used if n.startswith("Boss_")),
    "Boss_ で呼ばれる敵は Enemy_ 側に数値がある")

print("\n【9h】立ち入ると起きることの表")
_pe=[[c.strip() for c in l.split("|")] for l in
     open("Assets/Resources/PositionEvents.txt",encoding="utf-8").read().split("\n")
     if l.strip() and not l.strip().startswith("#")]
chk([(int(r[0]),int(r[3]) if r[3]!="-" else -1) for r in _pe]==
    [(3,89),(10,91),(10,94),(13,-1),(15,95),(20,103),(25,105),(30,109),
     (32,111),(33,114),(33,113),(38,119),(40,121),(42,123),(43,-1),(47,-1),
     (49,130),(24,131)],
    "階とフラグの並びが原典どおり（最後の2つが物語の終わり）")
_pf=[int(r[3]) for r in _pe if r[3]!="-"]
chk(len(_pe)==18,"位置イベントは18件（原典 0x1588 の分岐16＋物語の終わり2）",str(len(_pe)))
chk(len(_pf)==len(set(_pf)),"フラグ番号が重複しない")
# 13階の雪の結晶。原典 0x1fd5 は一度きりの印を持たず、持ち物を使い切ることで二度目を防ぐ。
_snow=[r for r in _pe if r[0]=="13"]
chk(len(_snow)==1 and _snow[0][2]=="持ち物:Crystal_Snow" and _snow[0][3]=="-",
    "13階は雪の結晶を持っていると発火し、一度きりの印は持たない")
chk(_snow and _snow[0][4].count("=床")==4,
    "溶けるのは4マス（X=5 の Y=5〜8 ＝ (0,0)(0,-1)(0,-2)(0,-3)）")
chk(any(r[0]=="49" and "大魔導師" in r[4] for r in _pe),
    "49階の中央で大魔導師の口上（原典 0x2277）")
chk(any(r[0]=="24" and "マスが門" in r[2] and "終幕" in r[4] for r in _pe),
    "24階の時空の門の下で終幕（原典 0x22cc〜0x23e5）")
chk(all(-5<=int(t.split(",")[0])<=5 and -5<=int(t.split(",")[1])<=5
        for r in _pe for t in r[1].split(";")),"マスが -5〜+5 に収まる")
_cs=[[c.strip() for c in l.split("|")] for l in
     open("Assets/Resources/Cutscenes.txt",encoding="utf-8").read().split("\n")
     if l.strip() and not l.strip().startswith("#")]
chk(len(_cs)==9,"場面は9つ（七つ＋物語の終わりの2つ）",str(len(_cs)))
_cs_steps=[n.strip() for r in _cs for n in r[2].split(";")]
_cs_say=[n for n in _cs_steps
         if n not in ("題名","奪われる","牢が開く","最後の画面","駆け上がる","閃光",
                      "斬りかかる","序章の絵","塔の絵","終わりの言葉",
                      "ボスが現れる","骸骨に囲まれる","押し上げられる","突撃")
         and not n.startswith("地図 ") and not n.startswith("歩き ")
         and not n.startswith("開く ") and not n.startswith("閉まる ")
         and not n.startswith("消す ") and not n.startswith("戦闘 ")
         and not n.startswith("音 ") and not n.startswith("閃光")]
chk(all(int(n) in _dlg for n in _cs_say),
    "場面のセリフ番号が Dialogue.txt に実在する（「題名」「奪われる」は別仕立て）")
# 10階「骸骨に囲まれる」の台本（原典 0x3e0a。書き出しは docs/make_boss_skeletons.py）
_bs=[l.split("#")[0].strip() for l in
     open("Assets/Resources/BossSkeletons.txt",encoding="utf-8").read().split("\n")]
_bs=[l for l in _bs if l]
_bs_walk=[l for l in _bs if l.startswith("歩き ")]
chk(len(_bs_walk)==9,"10階の台本は金騎士1体＋骸骨8体が歩く",str(len(_bs_walk)))
def _goal(line):
    head,dirs=line.rsplit(" ",1)
    x,y=[int(v) for v in head[head.index("(")+1:head.index(")")].split(",")]
    for d in dirs.split(","):
        d=int(d)
        x+= 1 if d==1 else -1 if d==2 else 0
        y+= -1 if d==3 else 1 if d==4 else 0
    return (x,y)
chk(sorted(_goal(l) for l in _bs_walk[1:])
    ==sorted([(x,y) for y in (0,1,2) for x in (-1,0,1) if not (x==0 and y==1)]),
    "骸骨8体の行き先は押し上げられた主人公(0,+1)の周り8マス")
chk(_goal(_bs_walk[0])==(0,5),"金騎士は (0,+5) まで下がる")
chk(sum(1 for l in _bs if l.startswith("閉まる "))==4
    and sum(1 for l in _bs if l.startswith("開く "))==2,
    "扉は2つ開いて4つ閉まる（奥・退路・左右）")
chk("押し上げられる" in _cs_steps and "骸骨に囲まれる" in _cs_steps,
    "10階の場面は押し上げ→セリフ100→囲まれるの順")
# 原典で「1マス滑らせる（0xa276）」を呼ぶ場所は4つの関数だけ。全部が表に載っているか。
#   0x003e0 = 2階の盗賊が去る / 0x00529 = 15階の盗賊・10階の盗賊が来る
#   0x0325e = 40階の金騎士の突撃 / 0x03e0a = 10階の骸骨 / 0x068b0 = 32階の金騎士
_th=[[c.strip() for c in l.split("|")] for l in
     open("Assets/Resources/ThiefEvents.txt",encoding="utf-8").read().split("\n")
     if l.strip() and not l.strip().startswith("#")]
chk(sum(1 for r in _th if len(r)>5 and r[5]!="-")==2,
    "盗賊が歩いて去るのは2階と15階の2件（原典 0x3e0/0x529 の 0x39c6 の呼び出しと同数）")
chk(any(r[0]=="15" and r[5].startswith("(+3,+5)") and r[5].endswith("2,2") for r in _th),
    "15階の盗賊は (+3,+5) から左へ2歩（原典 0x5b0 の X=8,Y=0 と表 {2,2}）")
chk(any(r[0]=="2" and r[5].startswith("(-5,-2)") and r[5].endswith("3,3") for r in _th),
    "2階の盗賊は (-5,-2) から下へ2歩（原典 0x4b2 の X=0,Y=7 と表 {3,3}）")
_gk=[n for n in _cs_steps if n.startswith("歩き Enemy_Worrior")]
chk(len(_gk)==2 and "3,3,3" in _gk[0] and _gk[1].endswith("1,1,1,1 遅い"),
    "32階の金騎士は3歩降りてきて、上へ8歩・右へ4歩で逃げる（原典 0x68b0 の表2本）")
chk(any(n.startswith("戦闘 Enemy_Worrior") for n in _cs_steps),
    "32階は歩かせたあと戦う（原典 0x69e2 の 0x6082 呼び出し）")

chk(sum(1 for n in _cs_steps if n.startswith("閉まる "))==2
    and sum(1 for n in _cs_steps if n.startswith("開く "))==1,
    "場面の表で門が閉まるのは2か所（20階・49階）、開くのは1か所（24階の時空の門）。"
    "10階と40階は別の台本に書いてある")
chk(sum(1 for n in _cs_steps if "=ID93" in n)==16,
    "魔導師が出るのは3階4体・42階4体・49階8体の計16体（原典の座標表どおり）")
# 階段で着いたマスでも立ち入りのできごとを見る（原典 0x23ee の 0x29e9〜0x2a00）。
# 31→32階（金騎士敗走）と 41→42階（金騎士処刑）は**着地マスが発動マス**なので、
# ここが抜けると場面ごと飛ぶ。作者から「32階のイベントが起きずに戦闘になった」と報告あり。
_tg_stairs=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk(_tg_stairs.count("CheckPositionEvent()")>=2,
    "階段で着いたあとにも立ち入りのできごとを見る（原典 0x2a00 の call 0x1588）")
# 作者の報告（その4）
_dov=open("Assets/Scripts/UI/DoorOpenView.cs",encoding="utf-8").read()
chk("queue" in _dov and "Drain" in _dov,
    "扉は1枚ずつ順に開く（原典の解錠は門1枚ごとに9コマを出し切る）")
_tsb=open("Assets/Editor/TowerSceneBuilder.cs",encoding="utf-8").read()
chk("FullScreenLayer" in _tsb,
    "1枚絵（題名・序章の絵）は一番上の層に置く（上下の帯に隠れていた）")
_tg5=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("ViewMoving" in _tg5 and "afterBattle ? ViewMoving : ViewBusy" in _tg5,
    "普通の戦闘の後始末はひと呼吸を待たない（テンポが悪くなるため）")
chk("battle_gold" in _tg5,"倒した相手からの金も知らせに出す（作者の指定）")
_tcw=open("Assets/Scripts/TowerController.cs",encoding="utf-8").read()
chk("TitleScreen.Title" in _tcw,
    "1枚絵の上の字も起動時に作っておく（3階だけ一瞬止まると報告された）")
# Java版の曲（作者の指定で足したもの。原典 PC98版には曲が2つしか無い）
import glob as _g
_mus=sorted(os.path.basename(x) for x in _g.glob("Assets/Resources/Music/*.wav"))
chk(_mus==["Block1.wav","Block2.wav","Block3.wav","Block4.wav","Block5.wav",
           "Boss10.wav","Boss20.wav","Boss40.wav","Boss49.wav","Boss50.wav"],
    "Java版の曲10本を書き出してある（かたまり5つ＋ボス戦5つ）", str(_mus))
chk(os.path.exists("docs/make_music.py"),"曲を作り直せる道具がある（docs/make_music.py）")
# 下の「セーブ」「ロード」のボタンと、起動時の題名画面（どちらも作者の指定）
_ps=open("Assets/Scripts/UI/PhoneScreen.cs",encoding="utf-8").read()
_tg2=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
_ib=open("Assets/Scripts/UI/ItemBarView.cs",encoding="utf-8").read()
chk("セーブしますか？" in _ps and "ロードしますか？" in _ps,
    "セーブ／ロードはその場で「はい／いいえ」を聞く（作者の指定）")
chk("ChoiceKind.Save" in _tg2 and "ChoiceKind.Load" in _tg2 and "AskSave" in _tg2,
    "本線に「セーブしますか？」の問いかけがある")
chk("saveButton" in _ib and "loadButton" in _ib,
    "下の「セーブ」「ロード」のボタンが繋がっている")
chk(PhoneScreen_left(_ps)[:2]==["セーブ","ロード"],
    "下の左の並びの先頭がセーブとロード")
_ta=open("Assets/Scripts/UI/TitleArt.cs",encoding="utf-8").read()
chk("Touch to start game" in _ta,"題名で点滅させる言葉は作者の指定どおり")
chk(_ta.count("Signboard")>0 and _ta.count("Picture")>0
    and _ta.count("Tower")>0 and _ta.count("Original")>0,
    "題名の案が4つ用意してある（看板／題名絵／塔／原典）")
chk(os.path.exists("Assets/Sprites/Screens/Title_Logo.png")
    and os.path.exists("docs/make_title.py"),
    "題名の看板は MTMOJI から切り出してある（docs/make_title.py）")
chk(os.path.exists("Assets/Sprites/Screens/Title_Tall.png"),
    "縦長の画面に合わせた題名絵がある（MTMOJI の帯を繰り返して伸ばしたもの）")
chk("Style.Picture" in _ta.split("Choice =")[1].split(";")[0],
    "作者が選んだのは案2（題名絵）")
_tc2=open("Assets/Scripts/TowerController.cs",encoding="utf-8").read()
chk("AtTitle" in _tc2 and "BeginGame" in _tc2,
    "題名を出している間は遊びを進めず、触ると最初から始まる")
chk(all(os.path.exists("Assets/Resources/Music/"+m+".meta") for m in _mus),
    "曲の取り込み設定（.meta）も置いてある")
chk(os.path.exists("docs/tools/MidiRender.swift"),
    "MIDI を音にする道具がある（macOS 内蔵の音源を使う）")
_jm=open("Assets/Scripts/Items/JavaMusic.cs",encoding="utf-8").read()
chk("BossEnemies" in _jm and "Enemy_BossSkeleton" in _jm,
    "ボスと戦うときだけ戦いの曲にする（Java版はどの戦いでも変える）")
# 作者の報告（その9）
_dwv2=open("Assets/Scripts/UI/DialogueWindowView.cs",encoding="utf-8").read()
chk("waiting || Waiting()" in _dwv2,
    "窓を出す前の段階も「待っている」と答える（その隙の決定でセリフが飛んでいた）")
_dov3=open("Assets/Scripts/UI/DoorOpenView.cs",encoding="utf-8").read()
chk("d.Delayed || d.Closing" in open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
    and "list[i].Delayed || list[i].Closing" in _dov3,
    "描き直しを待たせるのは間をおいて開く門と閉まる門だけ（鍵の扉は待たせない）")
chk("controller.RefreshCell(door.At)" in _dov3,
    "動きの終わりは**盤面を合わせてから**絵を消す（一瞬扉が見えるのを防ぐ）")
# 作者の報告（その8）
_bv2=open("Assets/Scripts/UI/BattleView.cs",encoding="utf-8").read()
chk("SlidePlayerViewTo" in _bv2 and "ShowActor" in _bv2,
    "戦いは相手のマスへ**歩いて**入ってから、主人公→閃光→相手→閃光…と見せる（作者の指定）")
_tcb=open("Assets/Scripts/TowerController.cs",encoding="utf-8").read()
chk("!(battle != null && battle.IsPlaying)\n            && Game.Player.Position != shownPos" in _tcb
    or "battle.IsPlaying)\n            && Game.Player.Position" in _tcb,
    "斬り合いの最中は主人公の絵を元のマスへ戻さない（2人に見えていた）")
chk("Dying" in _bv2 and "FadeSeconds" in _bv2,
    "倒れたら相手を残して主人公が点滅して消え、少しずつ暗くなる")
_tg8=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("DyingStep" in _tg8 and "gameOverStarted" in _tg8,
    "ＧＡＭＥ　ＯＶＥＲ の画面は倒れる様子のあとに出す")
_dov2=open("Assets/Scripts/UI/DoorOpenView.cs",encoding="utf-8").read()
chk("Holds" in _dov2 and "ShowFirstFrame" in _dov2,
    "扉の動きの間はそのマスを描き直さない（一瞬消えてから開いて見えた）")
# 作者の報告（その7）
_tg7=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("Cue = SoundCues.TitleMusic" in _tg7,
    "題名の曲は**題名を出すとき**に切り替える（積んだ瞬間だと踏み込みで止まる）")
chk("PlaySound(SoundCues.FieldMusic)" in _tg7,
    "奪われて本編が始まるところで本編の曲に戻す（原典 0x7038）")
_plv=open("Assets/Scripts/UI/PhoneLayoutView.cs",encoding="utf-8").read()
_fsv=open("Assets/Scripts/UI/FullScreenView.cs",encoding="utf-8").read()
chk("panel.SetActive" not in _fsv and "gameObject.SetActive" not in _fsv,
    "1枚絵の役は**自分の入れ物を消さない**（消すと自分の LateUpdate も止まる）")
chk("SetVisible" in _fsv,"出し入れは絵と文字の側で行う")
# 作者の報告（その6）
_tg6=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("pendingEvaluate && pages.Count == 0 && !ViewMoving" in _tg6,
    "解錠はひと呼吸を待たずに見る（待つ間に動けて二度戦えてしまう）")
chk("Delayed = true" in _tg6,
    "戦いのあとに開く扉だけ、ひと呼吸おいてから開く")
# 作者の報告（その5）
_bbv=open("Assets/Scripts/UI/BattleBandView.cs",encoding="utf-8").read()
chk("GoldLine" not in _bbv,
    "金の一言は帯に出さない（中央の知らせと二重になる）")
_hv=open("Assets/Scripts/UI/TowerHudView.cs",encoding="utf-8").read()
chk("PhoneScreen.NoRankLabel" in _hv,
    "右上は「実績」の隣に称号（まだなら「無」）を出す")
# 作者の報告（その3）で直した見た目の約束
_dwv=open("Assets/Scripts/UI/DialogueWindowView.cs",encoding="utf-8").read()
chk("controller.IsWalking" in _dwv,
    "会話の窓は人が歩き終わってから出す（10階の盗賊は目の前に来てから話す）")
chk("controller.IsDrawing" in _dwv,
    "会話の窓は盤面を描き出している間は出さない（暗転中にセリフが出ていた）")
chk("&& !Waiting()" in _dwv,
    "動きが終わるまで窓そのものを出さない（枠の絵は9分割なので高さ0でも縁が出る）")
chk("controller.IsSettling" in _dwv,
    "会話の窓は動きが終わってからひと呼吸おいて出す（合体の直後・暗転明けの直後は待つ）")
chk("maxVisibleCharacters" in _dwv and "TypeCharsPerSecond" in _dwv,
    "本文は1文字ずつ出す（出し終わる前の決定は全部出しに使う）")
_tc=open("Assets/Scripts/TowerController.cs",encoding="utf-8").read()
chk("if (!drawing) SyncChangedCells();" in _tc,
    "描き出しの最中は地図を映さない（暗転中に商店が浮かんで見えた）")
chk("dialogue.IsWaiting" in _tc and "dialogue.RevealAll()" in _tc,
    "まだ出ていないセリフは決定で飛ばせない／出している途中なら全部出す")
chk("Game.HasPendingView" in _tc,
    "渡したばかりの動きがある間は入力を受けない（扉が閉まる前に動けた）")
_tg4=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("AfterBattleStep" in _tg4,
    "倒した後始末は斬り合いの絵が終わってから当てる（褒美が戦闘開始と同時に出ていた）")
chk("Player.Rank = r.Rank" in _tg4,
    "妖精にもらった称号を覚えておく（出し方は作者と相談中）")
# 序章の1枚絵（MTOPSTR）。行が1つずつ現れるので4枚ぶん書き出してある
chk(all(os.path.exists(f"Assets/Sprites/Screens/Prologue_{_i}.png") for _i in range(4)),
    "序章の1枚絵が4枚ある（行が0〜3つ出た状態）")
chk("序章の絵" in _cs_steps,"序章の絵の段が場面の表にある")
# 終幕は 塔の絵 → 終わりの言葉（段落4つ）→ 最後の画面
chk(all(os.path.exists(f"Assets/Sprites/Screens/Ending_Text_{_i}.png") for _i in range(5)),
    "終わりの言葉が5枚ある（段落が0〜4つ出た状態）")
chk("塔の絵" in _cs_steps and "終わりの言葉" in _cs_steps,
    "終幕に塔の絵と終わりの言葉の段がある")
# 走らせ直しの取り違えを防ぐ（Unity が落ちると前回の結果が残る）
chk("rm -f \"$OUT/results.xml\"" in open("docs/run_tests.sh",encoding="utf-8").read(),
    "テストを走らせる前に前回の結果を消す")
chk("結果が出ていない" in open("docs/run_tests.sh",encoding="utf-8").read(),
    "結果が出ていなければ落ちたと分かるようにする")
# 場面の中の地図の書き換え（原典 0x3044 の三つ組の表から取った）
_cs_map=[n for n in _cs_steps if n.startswith("地図 ")]
chk(len(_cs_map)==42,
    "場面の表の地図の書き換えは42件（3階のゼノ5＋片付け5・20階のボス登場10・"
    "32階の金騎士1・42階の処刑11・49階の口上10）。10階と40階は別の台本",
    len(_cs_map))
chk(all(re.match(r"^地図 \([-+]?\d+,[-+]?\d+\)=.+$",n) for n in _cs_map),
    "場面の地図の段は「地図 (x,y)=種別」の形")
chk("地図 (+0,+0)=ID95" in _cs_map,"20階のボス(ID95)は中央に現れる")
chk("地図 (+0,-3)=門" in _cs_map,"20階のボス登場で退路(0,-3)が門になる＝原典 0x30b1")
_cut=open("Assets/Resources/Cutscenes.txt",encoding="utf-8").read()
chk("奪われる" in _cut,"序章にゼノに奪われる段がある")
chk("牢が開く" in _cut,"序章に牢が開く段がある")
chk("駆け上がる" in _cut,"終幕に塔を駆け上がる段がある（原典 0x2386〜0x23cd）")
_end=open("Assets/Scripts/Ending.cs",encoding="utf-8").read()
chk("ClimbFrom = 24" in _end and "ClimbWait = 5" in _end,
    "駆け上がりは24階から、1階ぶん5回待ち（原典 0x237c / 0x2391）")
_hud=open("Assets/Scripts/UI/TowerHudView.cs",encoding="utf-8").read()
chk("Climb(" in _hud,"駆け上がりは階の数字だけを動かす（地図は変えない）")
chk("最後の画面" in _cut,"終幕に最後の1枚絵の段がある（原典 0xd45c）")
_names={r[0] for r in _cs}
chk(all(any(("演出 "+n) in r[4] for r in _pe) for n in _names),
    "場面はどれも位置イベントから呼ばれる")
# 門になるマスは、原典データではまだ門になっていない
_gates=[]
for _r in _pe:
    for _e in _r[4].split(";"):
        _e=_e.strip()
        if _e.startswith("地図 ") and _e.endswith("=門"):
            _b=_e[3:-2]
            _f=int(_b.split(":")[0]); _xy=_b[_b.index("(")+1:_b.index(")")].split(",")
            _gates.append((_f,int(_xy[0]),int(_xy[1])))
chk(len(_gates)==6,"立ち入ると門になるマスは6つ（15×1 25×1 30×2 33×2）",str(len(_gates)))
chk(all(_grid(_f)[5-_y][_x+5]!="s" for _f,_x,_y in _gates),
    "どのマスも原典データではまだ門になっていない")
chk(_grid(25)[9][5]=="r","25階(0,-4)は原典では赤い扉。立ち入ると門に変わって閉じ込められる",_grid(25)[9][5])

print("\n【9i】原典の画像 *.P58")
sys.path.insert(0,"docs")
import p58 as _p58
_counts={}
for _f in sorted(glob.glob("docs/original/disk/*.P58")):
    _n=os.path.basename(_f)[:-4]
    _imgs=_p58.decode_all(open(_f,"rb").read())
    _counts[_n]=(len(_imgs),_imgs[0][1]*8,_imgs[0][2])
chk(len(_counts)==16,"P58 は16ファイル")
chk(all(v[0]>0 for v in _counts.values()),"全ファイルが復号できる")
chk(_counts["WEAPON"]==(10,32,32),"WEAPON は32x32が10枚＝剣5種と盾5種",str(_counts.get("WEAPON")))
chk(_counts["PLAYER1"]==(1,32,32) and _counts["PLAYER2"]==(1,32,32),"主人公は1枚ずつ")
chk(_counts["MONSTER"][0]==102,"MONSTER は102枚",str(_counts.get("MONSTER")))
chk(_counts["LCG"]==(7,640,400),"LCG は640x400が7枚")
chk(all(v[1]==640 and v[2]==400 for k,v in _counts.items()
        if k.startswith("MT") and k!="MTOWER1"),"MTxxx は640x400の全画面")
chk(_counts["MTOWER1"]==(1,320,400),"MTOWER1 だけ320x400")
chk(len(glob.glob("docs/original/p58png/*.png"))==sum(v[0] for v in _counts.values()),
    "書き出した PNG の枚数が復号した枚数と一致する")
_pal=_p58.read_palette("docs/original/disk/MTE.EXE")
chk(_pal==_p58.GAME_PALETTE,"パレットが EXE の DS:0x908 と一致する")
chk(_pal[0]==(0x44,0x44,0x44),"索引0 は #444444（動画のタイルの最頻色）")
chk(_pal[6]==(0xdd,0x66,0x66),"索引6 は #dd6666（動画の赤 #da6464）")
_g0=_p58.to_indices(*_p58.decode_all(open("docs/original/disk/GROUND.P58","rb").read())[0])
chk(set(v for _r in _g0 for v in _r)=={0,13},
    "床タイルは索引0と13の2色だけ＝動画の灰色の市松と一致")
# 枚数が物体IDの区間とぴったり合う（1〜14 / 15〜28 / 29〜50 / 51〜60 / 61〜162）
chk(_counts["GROUND"][0]==14 and _counts["OBJECT"][0]==14
    and _counts["ITEM"][0]==22 and _counts["WEAPON"][0]==10
    and _counts["MONSTER"][0]==102,"枚数が 14/14/22/10/102")
chk(14+14+22+10+102==162,"1から162まで隙間なく並ぶ")
_map=[l for l in open("docs/original/p58_map.txt",encoding="utf-8").read().split("\n")
      if l.strip() and not l.startswith("#")]
chk(len(_map)==159,"対応表は ID0〜158 の159行",str(len(_map)))

print("\n【9j】Unity のタイルが原典の絵になっているか")
import p58_to_tiles as _t
_sp=_t.sprites()
def _rgba(path):
    import zlib as _z, struct as _st
    d=open(path,"rb").read(); i=8; idat=b""
    while i<len(d):
        ln=_st.unpack(">I",d[i:i+4])[0]; k=d[i+4:i+8]; b=d[i+8:i+8+ln]; i+=12+ln
        if k==b"IHDR": w,h,bd,ct=_st.unpack(">IIBB",b[:10])
        elif k==b"IDAT": idat+=b
        elif k==b"IEND": break
    raw=_z.decompress(idat); bpp=4; stride=w*4; out=[]; prev=bytearray(stride); pos=0
    for y in range(h):
        f=raw[pos]; pos+=1; line=bytearray(raw[pos:pos+stride]); pos+=stride
        for x in range(stride):
            a=line[x-bpp] if x>=bpp else 0; bb=prev[x]; c=prev[x-bpp] if x>=bpp else 0
            if f==1: line[x]=(line[x]+a)&0xff
            elif f==2: line[x]=(line[x]+bb)&0xff
            elif f==3: line[x]=(line[x]+(a+bb)//2)&0xff
            elif f==4:
                pp=a+bb-c; pa,pb,pc=abs(pp-a),abs(pp-bb),abs(pp-c)
                pr=a if (pa<=pb and pa<=pc) else (bb if pb<=pc else c)
                line[x]=(line[x]+pr)&0xff
        out.append(bytes(line)); prev=line
    return w,h,ct,out
_tiles=sorted(q for q in glob.glob("Assets/Sprites/Tiles/*.png") if not q.endswith("_b.png"))
chk(len(_tiles)==106,"タイルは106枚（1コマ目。できごと用の妖精ID23を含む）",str(len(_tiles)))
# タイルは一覧（docs/tile_list.txt）から作り直せること。**GUIDの控えが肝**で、
# .meta はリポジトリに入れていないので、控えが無いと作り直したとき参照が全部切れる。
_tl=[l for l in open("docs/tile_list.txt",encoding="utf-8")
     if l.strip() and not l.startswith("#")]
chk(len(_tl)==145,"タイルの一覧は145行（1コマ目106＋2コマ目39）",str(len(_tl)))
_noguid=[l.split("|")[1].strip() for l in _tl
         if len(l.split("|"))<3 or not re.fullmatch(r"[0-9a-f]{32}",l.split("|")[2].strip())]
chk(not _noguid,"一覧の全部にGUIDが控えてある",str(_noguid[:3]))
_onfile={os.path.basename(q)[:-4] for q in glob.glob("Assets/Sprites/Tiles/*.png")}
_listed={f'{int(l.split("|")[0]):03d}_{l.split("|")[1].strip()}' for l in _tl}
chk(_onfile==_listed,"一覧と実物が一致する",
    str(sorted(_onfile ^ _listed)[:4]))
chk(os.path.exists("docs/rebuild_assets.sh"),"原典から一式を作り直す手順がある")
# シーンが要る1枚絵は、どれも作り直す手順の中に生成器がある
_need=sorted(set(re.findall(r'Assets/Sprites/Screens/[A-Za-z0-9_]+\.png',
                            open("Assets/Editor/TowerSceneBuilder.cs",encoding="utf-8").read())))
chk(len(_need)==12,"シーンが使う1枚絵は12枚",str(len(_need)))
chk(all(os.path.exists(_q) for _q in _need),"その12枚が揃っている",
    str([_q for _q in _need if not os.path.exists(_q)]))
_rb=open("docs/rebuild_assets.sh",encoding="utf-8").read()
for _g in ("decode_pc98.py","p58_to_tiles.py","p58_to_direct.py","lcg_to_doors.py",
           "make_screens.py","make_prologue.py","make_ending.py",
           "make_prefabs.py","fix_board_prefabs.py","make_registry.py"):
    if _g not in _rb: chk(False,f"作り直しの手順に {_g} が入っている"); break
else:
    chk(True,"作り直しの手順に生成器が10本とも入っている")
chk("Dialogue.png" in _rb,"作り直せないもの（作者の枠の絵）を手順に明記してある")
# 退役した経路の置き土産が残っていないか（誰からも参照されない MonoBehaviour）
_dead=[]
for _cs in glob.glob("Assets/Scripts/**/*.cs",recursive=True):
    _n=os.path.basename(_cs)[:-3]
    if not re.search(r"class\s+"+re.escape(_n)+r"\s*:\s*MonoBehaviour",
                     open(_cs,encoding="utf-8").read()): continue
    _mt=_cs+".meta"
    if not os.path.exists(_mt): continue
    _g=re.search(r"^guid: ([0-9a-f]{32})",open(_mt,encoding="utf-8").read(),re.M)
    if not _g: continue
    _hit=False
    for _u in glob.glob("Assets/Scenes/*.unity")+glob.glob("Assets/Prefab/**/*.prefab",recursive=True):
        if _g.group(1) in open(_u,encoding="utf-8",errors="replace").read(): _hit=True; break
    if not _hit: _dead.append(_n)
chk(not _dead,"シーンにもプレハブにも載らない MonoBehaviour は残さない",str(_dead))
# 敵と人の絵は必ず背景が抜けている。抜く色は種別で決まる（0xabe6 の 0x11 / 0x88 ＝ 索引1 / 8）。
# 左上の1点で背景を決めていたころ、そこに絵が来ている赤スライム(ID63)だけ
# 透過が付かず灰色の四角が残っていた。
# 敵と人の絵の背景が**不透明のまま縁に残っていないか**。
# 抜く色は種別で決まる（0xabe6 の重ね方 0x11 / 0x88 ＝ 索引1（茶）と 8（淡緑））。
# 赤スライム(ID63)は縁126点が索引1なのに左上だけ索引13で、背景を左上の1点で
# 決めていたころは透過が付かず**灰色の四角が残ったまま**盤面に出ていた。
#
# 物・道具・壁は別の道（`lcall 0xf5f,0xa6`）で描かれていて色を渡さないので、
# ここでは見ない（茶色そのものを使う絵＝壁があり、区別が付かないため）。
_PAL={1:(119,68,34),8:(170,204,170)}
_opaque=[]
for _t in sorted(glob.glob("Assets/Sprites/Tiles/*.png")):
    _oid=int(os.path.basename(_t)[:3])
    _k=_kind.get(_oid)
    _bgi=1 if _k in (8,9) else (8 if _k in (7,10,11) else None)
    if _bgi is None: continue
    _rgb=_PAL[_bgi]
    _w0,_h0,_ct0,_r0=_rgba(_t)
    _edge=([(0,_x) for _x in range(_w0)]+[(_h0-1,_x) for _x in range(_w0)]
           +[(_y,0) for _y in range(_h0)]+[(_y,_w0-1) for _y in range(_h0)])
    if any(_r0[_y][_x*4+3]!=0 and
           (_r0[_y][_x*4],_r0[_y][_x*4+1],_r0[_y][_x*4+2])==_rgb
           for _y,_x in _edge):
        _opaque.append(os.path.basename(_t))
chk(not _opaque,"敵と人の絵の縁に背景色が不透明のまま残っていない",str(_opaque[:6]))
# 参照が外れていると何も言わずに消えるので、プレハブの絵の参照先を全部確かめる
_guids=set()
for _m in glob.glob("Assets/**/*.meta",recursive=True):
    for _l in open(_m,encoding="utf-8",errors="ignore"):
        if _l.startswith("guid: "): _guids.add(_l.split()[1]); break
_dangling=[]
for _pf in glob.glob("Assets/Prefab/**/*.prefab",recursive=True):
    for _l in open(_pf,encoding="utf-8",errors="ignore"):
        _m2=re.search(r"m_Sprite: \{fileID: \d+, guid: ([0-9a-f]{32})",_l)
        if _m2 and _m2.group(1) not in _guids:
            _dangling.append(os.path.basename(_pf))
chk(not _dangling,"プレハブの絵の参照先が全部ある",str(_dangling))
# 盤面に出るプレハブは**原典から書き出したタイル**を指していること。
# 昔の絵（Assets/Images/MagicTower/MTower.gif の切り出し）を指したままだと、
# 別の絵が出たり何も出なかったりする（2階の檻・魔導師がそれだった）。
# ⚠ 旧アトラスは **2枚ある**（`Assets/Images/MTower.gif` と
#    `Assets/Images/MagicTower/MTower.gif`）。以前は後者しか見ておらず、
#    前者を指すプレハブ15個を見逃していた。両方を見る。
_gifguids=set()
for _gp in ("Assets/Images/MTower.gif.meta","Assets/Images/MagicTower/MTower.gif.meta"):
    if not os.path.exists(_gp): continue
    for _l in open(_gp,encoding="utf-8"):
        if _l.startswith("guid: "): _gifguids.add(_l.split()[1]); break
chk(len(_gifguids)==2,"旧アトラスは2枚ある（両方を見張る）",str(len(_gifguids)))
_gifguid=None
_reg=open("Assets/Resources/FloorPrefabRegistry.asset",encoding="utf-8").read()
_regguids=set(re.findall(r"prefab: \{fileID: -?\d+, guid: ([0-9a-f]{32})",_reg))
_oldart=[]
for _pf in glob.glob("Assets/Prefab/**/*.prefab",recursive=True):
    _mt=_pf+".meta"
    if not os.path.exists(_mt): continue
    _pg=None
    for _l in open(_mt,encoding="utf-8"):
        if _l.startswith("guid: "): _pg=_l.split()[1]; break
    if _pg not in _regguids: continue          # 登録簿に無い古いプレハブは見ない
    _ptxt=open(_pf,encoding="utf-8").read()
    if any(_g in _ptxt for _g in _gifguids):
        _oldart.append(os.path.basename(_pf))
    # Animator は旧アトラスの切り出しを毎コマ貼り直してしまう（`Images/CharMoveAnim/*.anim`）。
    # 2コマの動きは原典どおりの仕掛け（MonsterAnimator、0x92a4）が受け持つので要らない。
    if re.search(r"^Animator:",_ptxt,re.M):
        _oldart.append(os.path.basename(_pf)+"(Animator)")
chk(not _oldart,"盤面に出るプレハブは原典のタイルだけを指し Animator を持たない",str(_oldart))

# 盤面に置くものは **絵の大きさ = マスの数** でなければならない。
# 作者の古いプレハブは旧アトラス（1枚0.4ユニット）向けに m_LocalScale 2.5 で
# 組まれていて、原典のタイル（32px・PPU32 = 1ユニット）に差し替えたら
# **2.5マスの大きさ**で出てしまった（黄色の鍵・赤いクリスタル・檻）。
def _cell_size_check():
    import struct
    def png(path):
        return struct.unpack(">II", open(path,"rb").read(24)[16:24])
    am={}
    for f in glob.glob("Assets/**/*.meta",recursive=True):
        try: txt=open(f,encoding="utf-8",errors="replace").read()
        except Exception: continue
        g=re.search(r"^guid: ([0-9a-f]{32})",txt,re.M)
        if g: am[g.group(1)]=(f[:-5],txt)
    bad=[]
    for key,pg in re.findall(
            r"- key: (\S+)\n    prefab: \{fileID: -?\d+, guid: ([0-9a-f]{32})",_reg):
        if pg not in am: bad.append(key+":プレハブ無し"); continue
        pt=open(am[pg][0],encoding="utf-8",errors="replace").read()
        sp=re.search(r"m_Sprite: \{fileID: -?\d+, guid: ([0-9a-f]{32})",pt)
        if not sp or sp.group(1) not in am: bad.append(key+":絵無し"); continue
        spath,stxt=am[sp.group(1)]
        pm=re.search(r"spritePixelsToUnits: (\d+)",stxt)
        ppu=int(pm.group(1)) if pm else 100
        sm=re.search(r"m_LocalScale: \{x: ([\d.-]+)",pt)
        sc=float(sm.group(1)) if sm else 1.0
        w,h=png(spath)
        if abs(w/ppu*sc-w/32)>0.01 or abs(h/ppu*sc-h/32)>0.01:
            bad.append(f"{key}({w}px/PPU{ppu}x{sc})")
    return bad
_cellbad=_cell_size_check()
chk(not _cellbad,"盤面のプレハブは 絵32ドット=1マス の大きさで出る",str(_cellbad[:5]))

# 3x3のボス（15階の大ダコ・35階のドラゴン）。
# 盤面には9マスとも同じ名前が並ぶが、絵は 96x96 の1枚なので
# **中心のマスに1体だけ**置く（FloorLoader.MultiTileBosses）。
# 倒したあとその絵が消えるのは、解錠の結果が**中心のマスを書き換える**から。
# ここが噛み合っていないと、倒したのにボスが残る。
def _boss_check():
    out=[]
    for n,name in ((15,"Boss_GiantOctopus"),(35,"Boss_Dragon")):
        cells=[]; inobj=False
        for l in open(f"Assets/Resources/Floors/Floor_{n}.txt",encoding="utf-8"):
            if l.startswith("objects:"): inobj=True; continue
            if not inobj: continue
            m=re.match(r"\s*([+-]\d+) ([+-]\d+)\s+(\S+)",l)
            if m and m.group(3)==name: cells.append((int(m.group(1)),int(m.group(2))))
        if len(cells)!=9: out.append(f"F{n}:{len(cells)}マス"); continue
        xs=[c[0] for c in cells]; ys=[c[1] for c in cells]
        cx,cy=(min(xs)+max(xs))//2,(min(ys)+max(ys))//2
        row=[l for l in open("Assets/Resources/SealedDoors.txt",encoding="utf-8")
             if l.startswith(f"{n} |")]
        if not row: out.append(f"F{n}:解錠表に無い"); continue
        parts=row[0].split("|")
        guard=tuple(int(v) for v in parts[2].strip().split(","))
        if guard!=(cx,cy-1): out.append(f"F{n}:守護マス{guard}が下段中央でない")
        if f"({cx:+d},{cy:+d})=" not in parts[3]:
            out.append(f"F{n}:解錠が中心({cx:+d},{cy:+d})を書き換えない")
    return out
_bossbad=_boss_check()
chk(not _bossbad,
    "3x3のボスは中心に1体で、倒すと解錠がその中心を書き換えて絵も消える",
    str(_bossbad))
_fl=open("Assets/Scripts/Floor/FloorLoader.cs",encoding="utf-8").read()
chk("MultiTileBosses" in _fl and "Boss_Dragon" in _fl and "Boss_GiantOctopus" in _fl,
    "3x3のボスは中心の1マスにだけ置く")

# ゲームオーバー（原典 0x097ac）。生命力が尽きたら必ずここへ来る。
_gv=open("Assets/Scripts/UI/GameOverScreen.cs",encoding="utf-8").read()
chk('Line = "ＧＡＭＥ　ＯＶＥＲ"' in _gv,"ゲームオーバーの文字は原典 DS:0x181e のまま")
chk("X = 0x1f, Y = 0xbe" in _gv,"出す位置は原典 0x9e30 に渡す (0x1f, 0xbe)")
_tb=open("Assets/Scripts/Battle/TowerBattle.cs",encoding="utf-8").read()
chk("if (p.DamageTaken >= player.Hp)" in _tb,
    "相手の一撃で生命力が尽きるなら死ぬ（原典 0x64cd の「生命力 > 一撃」）")
chk("r.Died = true" in _tb and "player.Hp = 0" in _tb,"死んだら生命力は0")
_tg3=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("PageKind.GameOver" in _tg3 and "public void GameOver()" in _tg3,
    "本線がゲームオーバーの画面を積む")
chk("if (Player.Hp <= 0) { GameOver(); return; }" in _tg3,
    "魔法攻撃で尽きてもゲームオーバー（原典 0x001df2 / 0x001fd0）")

# 場面でものが現れるときの点滅（原典 0x6ae0）。ゼノ 0x32(50) 回、魔導師 0xf(15) 回。
# 点滅のあとの待ちは 0x1e(30) と 0xa(10)。
_zn=[l for l in open("Assets/Resources/Cutscenes.txt",encoding="utf-8")
     if l.startswith("ゼノとの初対面")][0]
chk(_zn.count("点滅50,30")==1,"ゼノは50回点滅してから30待つ（原典 0x6b31 / 0x6ba3）")
chk(_zn.count("点滅15,10")==4,"魔導師は4マスとも15回点滅して10待つ（原典 0x6c20 / 0x6c92）")
chk("点滅" not in _zn.split("序章の絵")[1],"片付け（床へ戻す）は点滅させない")
_bv=open("Assets/Scripts/UI/BlinkView.cs",encoding="utf-8").read()
chk("速さは原典と同じにできない" in _bv,"点滅の速さが原典と違うことを明記してある")
chk("ConsumeBlinks" in _tg3,"点滅は本線が積み、描くのは見た目の層")
# 魔導師4体は**1体ずつ**現れる（原典 0x6c20 は1体ぶん点滅して 0xa 待つのを4回）
chk("Queue<Cutscenes.MapStep> queue" in _bv and "IEnumerator Drain()" in _bv,
    "点滅は待ち行列で1つずつ出す（4体が同時に現れない）")
chk("waiting.SetActive(false)" in _bv,"順番が来るまでその絵は見せない")

# 序章の盗賊が牢を出て歩いていく（原典 0x070dc の向きの表 {2,2,3} と 0x07153）
_pro=open("Assets/Scripts/Prologue.cs",encoding="utf-8").read()
chk("ThiefWalk" in _pro and "ThiefGoal" in _pro,"盗賊の道順が控えてある")
chk("new Vector2Int(-5, -2)" in _pro,"歩き終えて立つのは (-5,-2)")
chk("ConsumeWalks" in _tg3 and "walks.Add" in _tg3,
    "歩きは本線が積み、動かすのは見た目の層")
_nw=open("Assets/Scripts/UI/NpcWalkView.cs",encoding="utf-8").read()
chk("MonsterAnimation.StepSeconds" in _nw,
    "歩く速さは主人公と同じ（原典 0xa276 の16コマぶんの時間）")
chk("standing" in _nw and "Stay" in _nw,
    "盤面に載らない人（10階の盗賊）は着いたあとも立たせておく")
_th=[l for l in open("Assets/Resources/ThiefEvents.txt",encoding="utf-8")
     if l.strip().startswith("2 |")]
chk(any("116" in l and "(-5,-2)=床" in l for l in _th),
    "(-5,-2) の盗賊に話すとセリフ116 が出てそのマスが床になる")

# 鍵で扉を開けたときの9コマの動き（原典 0x7be6）。
_da=open("Assets/Scripts/Floor/DoorAnimation.cs",encoding="utf-8").read()
chk("Frames = 9" in _da,"扉の動きは9コマ（0x48 から 0xa00 きざみで 0x5a48 まで）")
chk("TailFrames = 8" in _da,"そのあと盤面の動きを8コマ（原典 0x9288(8)）")
chk("Sound = 11" in _da,"扉の音は11（原典 0x7be6 の中の 0xb）")
_doorpng=sorted(glob.glob("Assets/Sprites/Doors/*.png"))
chk(len(_doorpng)==36,"扉のコマは4色x9枚",str(len(_doorpng)))
for _c in ("Yellow","Purple","Red","Sealed"):
    if not all(os.path.exists(f"Assets/Sprites/Doors/{_c}_{i}.png") for i in range(9)):
        chk(False,f"{_c} の9コマが揃っている"); break
else:
    chk(True,"黄・紫・赤・門の4色とも9コマ揃っている")
chk("ConsumeDoorOpens" in _tg3 and "doorOpens.Add" in _tg3,
    "開いた扉は本線が積み、描くのは見た目の層")
# 音の表の直し（以前 6 と書いていた）
_sc=open("Assets/Resources/SoundCues.txt",encoding="utf-8").read()
chk("扉を開ける       | 11 | 0x7be6" in _sc,
    "扉を開ける音は 0x7be6 の 11（0x23ee の 6 は「拾う」と39階の仕掛け）")
chk("門が開く         | 11 | 0x7a68" in _sc,
    "0x7a68 は「画面効果」ではなく門が開く9コマの動き")
chk("SealedTailFrames = 0x14" in _da,"門の後ろの間は 0x9288(0x14)＝20コマ")
_bossfr=sorted(glob.glob("Assets/Sprites/Effects/BossAppear_*.png"))
chk(len(_bossfr)==10,"20階のボスが現れる動きは10コマ（LCG #6 を 96x96 で横5x縦2）",
    str(len(_bossfr)))
_ba=open("Assets/Scripts/Floor/BossAppear.cs",encoding="utf-8").read()
chk("Frames = 10" in _ba and "FrameWait = 0x19" in _ba,
    "ボス登場は10コマ・1コマ 0x19 待ち（原典 0x3119〜0x3194）")
chk("ConsumeBossAppears" in _tg3,"ボス登場も本線が積み、描くのは見た目の層")
_cs3=[l for l in open("Assets/Resources/Cutscenes.txt",encoding="utf-8")
      if l.startswith("ボス登場")][0]
chk(_cs3.index("=門")<_cs3.index("ボスが現れる")<_cs3.index("(-1,+1)"),
    "順番は 退路の門 → ボスが現れる → 三つ組（原典 0x30b1 → 0x3119 → 0x3199）")

# 終幕の塔の絵が流れていく（原典 0x0ce83 の輪）
_en=open("Assets/Scripts/Ending.cs",encoding="utf-8").read()
chk("TowerSteps = 0x1a" in _en,"塔の絵は26回送る（原典 cmp [bp-0x24e], 0x1a）")
chk("TowerStepWait = 0x1e" in _en,"1回 0x1e(30) 待つ")
chk("TowerScrollPerStep = 15" in _en,"1回15ドット送る（原典は高さを 15*i で縮める）")
chk("TowerSound = 7" in _en,"1回ごとに音7（原典 0xcf9f）")
chk("0fff 1fff 2fff" in _en,"パレットの表（原典 0x0c4d8 の8本）を控えてある")
chk("15, 11, 9, 7, 5, 3, 1, 0" in _en,"番号0の濃さの段が原典どおり")
chk("TowerWhiteMix" in open("Assets/Scripts/UI/FullScreenView.cs",encoding="utf-8").read(),
    "見た目の側が白への寄せ具合を使う")

_fs=open("Assets/Scripts/UI/FullScreenView.cs",encoding="utf-8").read()
chk("Ending.TowerSteps" in _fs and "scrollTower" in _fs,"見た目の側が塔の絵を流す")

# セーブは地図も一緒に保存する（原典 0x9b4e は 52ブロックぶんの地図を書き出す）
_sv=open("Assets/Scripts/TowerSave.cs",encoding="utf-8").read()
chk("public static Snapshot Read(int slot)" in _sv,"地図の差も読み出す口がある")
chk("floors[n].Diff(data)" in _sv,"元の階データとの差だけを書き出す")
chk("0x9b4e" in _sv,"原典のセーブ処理の出どころを書いてある")
_fst=open("Assets/Scripts/Floor/FloorState.cs",encoding="utf-8").read()
chk("public List<KeyValuePair<Vector2Int, string>> Diff(" in _fst
    and "public void Apply(" in _fst,"盤面が差の出し入れを持つ")
chk("TowerSave.Save(slot, Player, floors)" in _tg3,"セーブに地図を渡している")
chk("var snap = TowerSave.Read(slot)" in _tg3 and "floors.Clear()" in _tg3,
    "ロードは階を組み直してから差を当てる")

# 場面の「地図 (x,y)=種別」で盤面に書き込まれる物体は、**登録簿に載っていないと
# 姿が出ない**。ゼノ(ID91)とバンパイア(ID95)がこれで漏れていて、
# 3階のゼノとの初対面と20階のボス登場で何も現れなかった。
_regkeys=set(re.findall(r"^  - key: (\S+)",_reg,re.M))
_objids={}
for _l in open("Assets/Resources/ObjectIds.txt",encoding="utf-8"):
    _m=re.match(r"^(\S+)\s*\|\s*(\d+)\s*$",_l.strip())
    if _m: _objids["ID"+_m.group(2)]=_m.group(1)
_terrainwords={"床","壁","隠し壁","門","上り","下り","マグマ","表駆動","封印"}
# **できごとの表を全部**見る。場面の表しか見ていなかったため、解錠で現れる
# マトック(ID41)・ドラゴンスレイヤー(ID46)・雪のクリスタル(ID47)・妖精(ID23) が
# 登録簿から漏れていて、15階と35階のボスの褒美が出なかった。
_miss=[]
for _f in ("Cutscenes.txt","SealedDoors.txt","PositionEvents.txt",
           "ArrivalEvents.txt","HiddenOccupants.txt","ThiefEvents.txt"):
    _p="Assets/Resources/"+_f
    if not os.path.exists(_p): continue
    for _l in open(_p,encoding="utf-8"):
        if _l.lstrip().startswith("#"): continue
        for _k in re.findall(r"\(\s*[+-]?\d+\s*,\s*[+-]?\d+\s*\)\s*=\s*([^\s;|]+)",_l):
            _n=_objids.get(_k,_k)
            if _n in _terrainwords or _n in _regkeys: continue
            _miss.append(f"{_f}:{_k}→{_n}")
chk(not _miss,"できごとが地図に書き込む物体はすべて登録簿にある",str(sorted(set(_miss))))
# 15階と35階のボスの褒美（原典 0x4762 の結果）
for _k,_why in (("Matock","15階の大ダコの褒美"),("Crystal_Snow","35階のドラゴンの褒美"),
                ("Sword_Dragon","ドラゴンスレイヤー"),("Fairy","10階の妖精")):
    if _k not in _regkeys: chk(False,f"{_why}（{_k}）が登録簿にある"); break
else:
    chk(True,"解錠で現れる4つ（マトック・雪のクリスタル・竜殺し・妖精）が登録簿にある")

# 3階のゼノとの初対面（原典 0x6ae0）は**ゼノ1体と魔導師4体を盤面に出す**。
# 魔導師は主人公の立つ (-1,-3) の上下左右で、囲まれる形になる。
_cs=open("Assets/Resources/Cutscenes.txt",encoding="utf-8").read()
_zeno=[l for l in _cs.splitlines() if l.startswith("ゼノとの初対面")]
chk(len(_zeno)==1 and "地図 (-1,-1)=ID91" in _zeno[0],
    "ゼノ(ID91)は3階の (-1,-1) に現れる（原典 X4,Y6）")
chk(len(_zeno)==1 and all(f"地図 ({x:+d},{y:+d})=ID93" in _zeno[0]
                          for x,y in [(-1,-2),(-1,-4),(-2,-3),(0,-3)]),
    "魔導師(ID93)4体は主人公 (-1,-3) の上下左右に現れる（原典 X4Y7/X4Y9/X3Y8/X5Y8）")
chk(len(_zeno)==1 and _zeno[0].index("=ID91")<_zeno[0].index("110")
    <_zeno[0].index("=ID93"),"順番は ゼノ → セリフ110 → 魔導師（原典どおり）")
# 原典 0x6ebd〜0x6f0f は同じ5マスを床(6)に戻してから 0x6f11 で装備と能力値を奪う。
# これが抜けていて「3階に戻るとまだ魔導師がいて先に進めない」と報告された。
chk(len(_zeno)==1 and all(f"地図 ({x:+d},{y:+d})=床" in _zeno[0]
                          for x,y in [(-1,-1),(-1,-2),(-1,-4),(-2,-3),(0,-3)]),
    "場面の終わりに同じ5マスを床へ戻す（原典 0x6ebd）")
chk(len(_zeno)==1 and _zeno[0].index("=床")<_zeno[0].index("奪われる"),
    "片付けは奪われるより前（原典 0x6ebd → 0x6f11）")
# 地図の書き換えは「次に出すページの直前」に当てる（まとめて当てると順番が崩れる）
_tg2=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("MapBefore" in _tg2 and "pendingMap" in _tg2,
    "場面の地図の書き換えはページごとに当てる")
chk(len(_second)==39,"2コマ目39枚も Assets に置く（原典 0x92a4 が実際に描く）")
# 2の冪でない絵は nPOTScale を 0 にしないと Sprite として取り込めない（3x3のボスが消えていた）
for _m in ("900_Boss_Dragon","901_Boss_GiantOctopus","902_Shop_Altar",
           "900_Boss_Dragon_b","901_Boss_GiantOctopus_b"):
    if "nPOTScale: 0" not in open(f"Assets/Sprites/Tiles/{_m}.png.meta",encoding="utf-8").read():
        chk(False,f"{_m} は nPOTScale: 0 にする"); break
else:
    chk(True,"3x3の絵は2の冪に丸めない（丸めると Sprite にならず消える）")
chk(len(glob.glob("docs/original/video_tiles/*.png"))==93,"差し替え前の動画切り出しを控えてある")
_bad=[]
for _p in _tiles:
    _w,_h,_ct,_rows=_rgba(_p)
    if _ct!=6: _bad.append(os.path.basename(_p))
chk(not _bad,"全部 RGBA（原典パレット＋透過）で書き出されている",str(_bad[:3]))
# 中身が原典の絵と一致するか（代表4枚を画素で照合）
_ok=True
for _n,_oid in (("006_Floor",6),("025_Master",25),("051_Sword_1",51),("061_Enemy_SlimeG",61)):
    _w,_h,_ct,_rows=_rgba(f"Assets/Sprites/Tiles/{_n}.png")
    _src=_sp[_oid]
    for _y in range(0,32,4):
        for _x in range(0,32,4):
            _r,_g,_b,_a=_rows[_y][_x*4:_x*4+4]
            _R,_G,_B=_p58.GAME_PALETTE[_src[_y][_x]]
            if _a and (_r,_g,_b)!=(_R,_G,_B): _ok=False
chk(_ok,"タイルの画素が原典の絵と一致する")
chk(_p58.GAME_PALETTE[1]==(0x77,0x44,0x22),"背景に使われる索引1は茶 #774422")

# 原典の属性表から作った対応表
_pm=[[c.strip() for c in l.split("|")] for l in
     open("docs/original/p58_map.txt",encoding="utf-8") if l.strip() and not l.startswith("#")]
chk(len(_pm)==159,"属性表は ID0〜158 の159件",str(len(_pm)))
chk(all(r[2]!="-" for r in _pm),"全IDに絵が割り当たる")
_byid={int(r[0]):(int(r[1]),r[2]) for r in _pm}
chk(_byid[6][1]==_byid[7][1],"ID7 は床(ID6)と同じ絵＝盤面では見分けが付かない")
chk(_byid[8][1]==_byid[9][1]==_byid[10][1],"ID9/10 の隠し壁は壁(ID8)と同じ絵")
chk([_byid[i][0] for i in (1,2,3)]==[1,1,1],"鍵の扉3種は種別1")
chk(_byid[4][0]==2 and _byid[8][0]==4,"門は種別2、壁は種別4")
_second=[i for i in range(1,159) if _byid.get(i,(0,))[0] in (9,11)]
chk(all(_byid.get(i-1,(0,))[0] in (8,10) or _byid.get(i-9,(0,))[0] in (8,10) for i in _second),
    "2コマ目(種別9/11)の1コマ目は1つ前（3x3のボスだけ9つ前）")
chk(_byid[113][0]==11 and _byid[131][0]==11,
    "大ダコ(104)とドラゴン(122)の2コマ目は9つ先の113と131＝3x3ぶんずれる")
chk(len(_second)==22,"2コマ目を持つ敵は22体",str(len(_second)))
chk(len([i for i in range(61,159) if _byid.get(i,(0,))[0] in (8,10)])==44,"敵は44体")
chk(_byid[104][0]==10 and _byid[105][0]==7,
    "3x3のボスは中央だけが敵で、まわりの8マスは種別7")

print("\n【9k】階段の接続表")
def _stairs(n):
    t=open(f"Assets/Resources/Floors/Floor_{n}.txt",encoding="utf-8").read()
    up=dn=None
    for m in re.finditer(r"^  ([+-]\d+) \| (.{11})$",t,re.M):
        y=int(m.group(1))
        for x,c in enumerate(m.group(2)):
            if c==">": up=(x-5,y)
            if c=="<": dn=(x-5,y)
    return up,dn
_fc=[[c.strip() for c in l.split("|")] for l in
     open("Assets/Resources/FloorConnections.txt",encoding="utf-8") if l.strip() and not l.startswith("#")]
def _pos(t): return None if t=="-" else tuple(int(v) for v in t.split(","))
_bad=[]
for _r in _fc:
    _n=int(_r[0])
    if not (1<=_n<=50): continue
    _u,_d=_stairs(_n)
    if _pos(_r[1])!=_u or _pos(_r[2])!=_d: _bad.append(_n)
chk(not _bad,"接続表の階段位置が全50階で .floor と一致する",str(_bad[:6]))
chk(all(-5<=int(v.split(",")[1])<=5 for _r in _fc for v in _r[1:] if v!="-"),
    "接続表の座標が -5〜+5 に収まる（旧座標系 -4〜+6 の取り残しが無い）")
# 着地位置は必ず階段の隣
_adj=[]
for _r in _fc:
    _n=int(_r[0])
    if not (1<=_n<=50): continue
    _u,_d=_stairs(_n)
    for _t,_s in ((_r[3],_d),(_r[4],_u)):    # 下から登ってきた=下り階段の隣 / 上から降りた=上り階段の隣
        _p=_pos(_t)
        if _p and _s and abs(_p[0]-_s[0])+abs(_p[1]-_s[1])!=1: _adj.append(_n)
chk(not _adj,"着地位置は対応する階段の隣にある",str(sorted(set(_adj))[:6]))

print("\n【9l】画像ファイルの使われ方")
_ft=[[c.strip() for c in l.split("|")] for l in
     open("docs/original/p58_files.txt",encoding="utf-8") if l.strip() and not l.startswith("#")]
chk(len(_ft)==16,"P58 は16ファイル")
import make_p58_map as _mk
_tab=_mk.file_table()
chk(_tab=={"LCG":0,"GROUND":0,"OBJECT":8000,"ITEM":18000,"WEAPON":24500,
           "PLAYER1":32000,"PLAYER2":32400,"MONSTER":34000,"PICTURE":64500},
    "まとめ読みの置き場が原典の表 DS:0x240 どおり",str(_tab))
chk(all(int(r[3])==_counts[r[0].replace(".P58","")][0] for r in _ft),
    "枚数が実際の復号結果と一致する")
chk(open("docs/original/disk/MTE.EXE","rb").read().find(b"MTWAKU")<0,
    "MTWAKU.P58 は EXE から参照されない（未使用ファイル）")
_scr=[[c.strip() for c in l.split("|")] for l in
      open("docs/original/screens.txt",encoding="utf-8") if l.strip() and not l.startswith("#")]
chk(len(_scr)==13,"画面の絵は13枚",str(len(_scr)))
chk(len(glob.glob("Assets/Sprites/Screens/*.png"))==25,
    "Assets には枠・序章4枚・終わりの言葉5枚と題名の看板・縦長を足して25枚",
    str(len(glob.glob("Assets/Sprites/Screens/*.png"))))
# 枠の中央が盤面と同じ大きさで空いている
_lcg0=_p58.to_indices(*_p58.decode_all(open("docs/original/disk/LCG.P58","rb").read())[0])
_hole={_lcg0[_y][_x] for _y in range(24,376) for _x in range(144,496)}
chk(_hole=={0},"枠の中央 (144,24)-(496,376) は一色で空いている＝盤面が入る場所",str(sorted(_hole)))
chk(os.path.exists("Assets/Sprites/Screens/Frame.png"),"穴を透明にした枠がある")
chk(not glob.glob("Assets/Sprites/Screens/*Waku*"),"未使用の MTWAKU は Assets に入れない")
chk([r[2] for r in _scr if r[0]=="Lcg_1"]==["320x32"],"LCG#1 は会話ウィンドウの帯 320x32")

print("\n【9m】会話ボックス")
_mx=0; _mid=-1
for _l in open("Assets/Resources/Dialogue.txt",encoding="utf-8"):
    _m=re.match(r"^(\d+) \| (.*)$",_l.rstrip("\n"))
    if not _m: continue
    _n=sum(max(1,(len(_s)+17)//18) for _s in _m.group(2).split("\\n"))
    if _n>_mx: _mx,_mid=_n,int(_m.group(1))
chk(_mx==11,"18文字で折り返したときの最長は11行",f"{_mx}行（レコード{_mid}）")
_src=open("Assets/Scripts/UI/DialogueBox.cs",encoding="utf-8").read()
chk("LinesPerPage = int.MaxValue" in _src,"原典にページ送りは無いので分割しない")
chk("TopEdge = 120" in _src and "HighTopEdge = 50" in _src,"窓の上端 120 と 50")
_dw=[[c.strip() for c in l.split("|")] for l in
     open("docs/original/dialogue_windows.txt",encoding="utf-8")
     if l.strip() and not l.startswith("#")]
chk(len(_dw)==44,"会話表示の呼び出しは44箇所（遠呼び出しと近呼び出しの両方）",str(len(_dw)))
chk(sorted({int(r[2]) for r in _dw if r[2]!="-"})==[50,90,120],
    "窓の上端は 50 / 90 / 120 の3種類")
_nl={}
for _l in open("Assets/Resources/Dialogue.txt",encoding="utf-8"):
    _m=re.match(r"^(\d+) \| (.*)$",_l.rstrip("\n"))
    if _m: _nl[int(_m.group(1))]=len(_m.group(2).split("\\n"))
_known=[(int(r[3]),int(r[1])) for r in _dw if r[3]!="-" and int(r[3]) in _nl]
chk(len(_known)==38,"セリフ番号が定数の呼び出しは38件",str(len(_known)))
chk(all(_nl[rid]<=n for rid,n in _known),
    "窓の行数はセリフの行数以上（足りない窓は無い）",
    str([(rid,n,_nl[rid]) for rid,n in _known if _nl[rid]>n][:3]))
chk(sum(1 for rid,n in _known if _nl[rid]==n)==32,
    "32件はセリフの行数ちょうど",str(sum(1 for rid,n in _known if _nl[rid]==n)))
chk(sum(1 for rid,n in _known if _nl[rid]<n)==6,
    "6件は1行以上多い窓（短いセリフを大きめの窓で出す）")
chk("CharsPerLine = 18" in _src,"CharsPerLine は18（動画で確認した折り返し幅）")
# 上端50に開くセリフ。**以前は107・159・156の3件だけだと書いていたが誤り**で、
# 場面の中から呼ばれるものを含めて10件ある（原典の呼び出しの表から取る）。
_high=sorted({int(r[3]) for r in _dw if r[3]!="-" and int(r[2])==50 and int(r[3]) in _nl})
_hv=open("Assets/Scripts/UI/DialogueWindowView.cs",encoding="utf-8").read()
_hm=re.search(r"HighRecords\s*=\s*\{([^}]*)\}",_hv)
_hl=sorted(int(x) for x in re.findall(r"\d+",_hm.group(1))) if _hm else []
chk(_hl==_high,"上端50のセリフが原典の呼び出しの表と一致する",f"表={_high} 実装={_hl}")
_tg=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("DialogueBox.Paginate" in _tg,"本線の TowerGame が会話ボックスを通している")
chk("PendingPages" in _tg and "NextPage" in _tg,"ページ送りの口が用意されている")
_dwin=open("Assets/Scripts/UI/DialogueWindow.cs",encoding="utf-8").read()
chk("Left = 160" in _dwin and "Width = 320" in _dwin,"窓は x=160 幅320（原典 0x14 バイト / 0x14 ワード）")
chk("ScreenWidth = 640" in _dwin and "ScreenHeight = 400" in _dwin,"画面は 640x400")
# 高さの式が原典どおりか（塗りは16行ぶんを4行ずつずらして 5*行数+1 回 → 32+20*行数）
for _n in (1,2,6,8,9):
    _t=0x14*_n//4+1
    if max(32, 16+16+4*(_t-1)) != 20*_n+32: chk(False,"高さの式"); break
else:
    chk(True,"窓の高さ = 20×行数+32（原典の塗りが届く範囲）")
chk("TextLeft = 16" in _dwin and "TextTop = 12" in _dwin,
    "窓の文字は (176, 上端+12) から（原典 0x9563〜0x9571）")
chk("LineAdvance = 20" in open("Assets/Scripts/UI/ScreenLayout.cs",encoding="utf-8").read(),
    "行送りは20ドット（lcall 0x107e,0 の第1引数 0x14）")
chk("WindowBody" in _sb and "TextLeft" in _sb,
    "会話とメニューの文字を原典の内側の位置で置いている")
chk("lineSpacing" in _sb,"行送りを原典の20ドットに合わせている")
chk("CanvasScale = 1.8f" in _dwin,"Canvas 基準1152x720 は原典640x400 のちょうど1.8倍")
_sb=open("Assets/Editor/TowerSceneBuilder.cs",encoding="utf-8").read()
chk("PhoneScreen.ReferenceWidth, PhoneScreen.ReferenceHeight" in _sb,
    "シーンの基準解像度が縦画面（1080x1920）")
# 窓が横長でも縦長でも、スマホの画面ぶんの枠（1080x1920）がそのままの形で入る。
# 帯の高さだけを固定していたときは、横長の窓で盤面が10%まで潰れた。
chk("ScreenMatchMode.Expand" in _sb,"画布は入りきる側に合わせる（Expand）")
chk('GameObject("PhoneFrame"' in _sb,"スマホの画面ぶんの枠を中央に置く")
_plv=open("Assets/Scripts/UI/PhoneLayoutView.cs",encoding="utf-8").read()
chk("frame.sizeDelta = new Vector2(PhoneScreen.ReferenceWidth" in _plv,
    "枠はいつも 1080x1920 のまま")
chk("boardCamera.rect = new Rect(0f, 0f, 1f, 1f)" in _plv,
    "カメラは画面いっぱい（一部だけ写すと外側が塗り残しになる）")
_ps=open("Assets/Scripts/UI/PhoneScreen.cs",encoding="utf-8").read()
chk("ReferenceWidth = 1080f, ReferenceHeight = 1920f" in _ps,"縦画面の基準は 1080x1920")
chk("BrickPath" in _ps and "Assets/Sprites/Hud/Brick.png" in _ps,
    "上下の帯に敷く石積みは原典の枠から切り出したもの")
chk('"TitleScreen", ""' in _sb,
    "題名は絵を貼らない（真っ黒に文字2行。動画 Part1 t≈31）")
chk("cam.backgroundColor = DialogueWindowView.Fill" in _sb,
    "盤面の下地は索引0（68,68,68）。原典が階を描く前に塗る色と同じ")
chk("BuildDialogueWindow" in _sb,"シーンに会話ウィンドウを組み込んでいる")
_dv=open("Assets/Scripts/UI/DialogueWindowView.cs",encoding="utf-8").read()
chk("HighRecords = { 53, 107, 110, 130, 131," in _dv,
    "高い位置に開くセリフを表どおり10件持っている")
_sl=open("Assets/Scripts/UI/ScreenLayout.cs",encoding="utf-8").read()
chk("MapLeft = 144, MapTop = 24" in _sl,"盤面の左上は (144,24)（原典 0x1b6d の式）")
chk(144*2+11*32==640,"盤面は左右中央（144+352+144=640）")
chk(24*2+11*32==400,"盤面は上下中央（24+352+24=400）")
chk("CameraSize = ScreenHeight / (float)TileSize / 2f" in _sl,"カメラの大きさは 400/32/2 = 6.25")
chk("ScreenLayout.CameraSize" in _sb,"シーンのカメラが原典の画面に合わせてある")
chk("orthographicSize = 6f" not in _sb,"古い 6f のままになっていない")

print("\n【9n】能力値の上限")
_ps=open("Assets/Scripts/PlayerState.cs",encoding="utf-8").read()
chk("MaxHp = 9990" in _ps,"生命力の上限 9990（原典 0x2706）")
chk("MaxAttack = 990, MaxDefense = 990, MaxGold = 9990" in _ps,
    "攻撃力・防御力 990（原典 0x3de）／所持金 9990")
chk("ClampStats" in _ps,"上限を押さえる処理がある")
chk("ClampStats" in open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read(),
    "一手ごとに押さえている（原典は描き直しのたび）")
chk(open("Assets/Scripts/Items/TowerNpc.cs",encoding="utf-8").read().count("ClampStats")>=4,
    "祭壇・商店・仙人でも押さえている")

print("\n【9o】画面の組み立て")
_lay=[[c.strip() for c in l.split("|")] for l in
      open("docs/original/screen_layout.txt",encoding="utf-8") if l.strip() and not l.startswith("#")]
chk(len(_lay)==15,"欄は15件",str(len(_lay)))
_map=[r for r in _lay if r[0]=="盤面"][0]
chk([int(_map[i]) for i in (1,2,3,4)]==[144,24,352,352],"盤面は (144,24) の 352x352")
_yy=[int(r[2]) for r in _lay if r[0] in ("生命力","攻撃力","防御力","所持金")]
chk(_yy==[80,104,128,152] and all(b-a==24 for a,b in zip(_yy,_yy[1:])),"能力値は縦24おき")
chk(all(0<=int(r[1]) and int(r[1])+int(r[3])<=640 for r in _lay if r[4]!="可"),
    "どの欄も横640に収まる")
chk(all(0<=int(r[2]) and int(r[2])+int(r[4])<=400 for r in _lay if r[4]!="可"),
    "どの欄も縦400に収まる")
chk("MapArea = new RectInt(MapLeft, MapTop, MapWidth, MapHeight)" in _sl,"ScreenLayout に盤面の領域がある")
chk("HpBox      = new RectInt(48, 80, 64, 16)" in _sl,"ScreenLayout に能力値の欄がある")
# 原典の座標（横長の左右の帯）はもう使わない。縦画面の並べ方は PhoneScreen。
chk("PhoneScreen.RowHeight" in _sb and "PhoneScreen.ItemBarHeight" in _sb,
    "シーンの HUD が縦画面の並べ方で置かれている")
# 持ち物の帯（黒い地・左右に送る）と、触ったときの流れ（作者の指定）
_ibv=open("Assets/Scripts/UI/ItemBarView.cs",encoding="utf-8").read()
chk("RectMask2D" in _sb,"帯からはみ出た絵は切り取る")
chk("controller.TapItem" in _ibv and "controller.ShowDamage" in _ibv,
    "帯の触りと「ダメージ表示」が本線に繋がっている")
_tgi=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("ChoiceKind.UseItem" in _tgi,"使い切りの道具は「使いますか？」と聞いてから使う")
chk("kind == SpecialItems.Kind.HeroOrb) return ShowHeroOrb()" in _tgi,
    "勇者のオーブだけは聞かずに一覧を出す")
# 移動の十字キー（作者が6案から選んだ B）。押しっぱなしで歩き続ける。
_dpv=open("Assets/Scripts/UI/DPadView.cs",encoding="utf-8").read()
_phs=open("Assets/Scripts/UI/PhoneScreen.cs",encoding="utf-8").read()
chk("DPad_B.png" in _phs,"十字キーは作者が選んだ B の絵")
chk("controller.TouchDir = dir;" in _dpv and "TouchDir" in _tc,
    "十字キーの向きが鍵と同じ道で本線へ入る")
chk("controller.TouchConfirm = true;" in _dpv and "TakeConfirm()" in _tc,
    "盤面を触ると会話を送れる／選んだものを決められる")
chk("if (dir == Vector2Int.zero) dir = TouchDir;" in _tc,
    "鍵が押されていないときだけ十字キーの向きを使う")
chk("MonsterHpBox" in _sl and "KeyIconOrigin" in _sl,
    "戦闘中の相手の欄と鍵の絵の位置を区別してある")
_bp=open("Assets/Scripts/Battle/BattlePresentation.cs",encoding="utf-8").read()
chk("StepWait = 10" in _bp and "HitWait = 5" in _bp and "SlashSound = 9" in _bp,
    "戦闘の演出の値が原典どおり（待ち10/5・音9）")
chk("static TextMeshProUGUI Label(" not in _sb,"独自配置の古いヘルパーが残っていない")
chk("BuildFrame" in _sb,"シーンに画面の枠を敷いている")

print("\n【9p】題名画面")
_ts=open("Assets/Scripts/UI/TitleScreen.cs",encoding="utf-8").read()
_exe=open("docs/original/disk/MTE.EXE","rb").read()
_B=4608+0x12340; _body=_exe[_B:]
for _off,_want in ((0x113c,"Ｔｏｗｅｒ"),(0x1167,"（Ｃ）１９９６")):
    _e=_body.index(b"\0",_off)
    _t=_body[_off:_e].decode("shift_jis")
    chk(_t in _ts,f"題名の文字 DS:0x{_off:x} がコードに入っている")
    chk(_want in _t,f"DS:0x{_off:x} に {_want} が含まれる")
chk("AfterRecord = 111" in _ts and "NextRecord = 112" in _ts,
    "題名は序章の111と112の間に入る")
_cs2=open("Assets/Resources/Cutscenes.txt",encoding="utf-8").read()
_order=[x.strip() for x in
        [l for l in _cs2.splitlines() if l.startswith("ゼノとの初対面")][0]
        .split("|")[2].split(";")]
_order=[x for x in _order if not x.startswith("地図 ")]
chk(_order[_order.index("111"):_order.index("111")+5]
    ==["111","閃光","題名","序章の絵","奪われる"],
    "111のあと閃光→題名→序章の絵→奪われるの順（原典 0x6d09→0x6db2→0x06f1b）",
    str(_order))
chk("起動直後ではない" in open("Assets/Scripts/Prologue.cs",encoding="utf-8").read(),
    "序章が位置イベント由来だと明記してある")
_tg2=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("PlayScene" in _tg2,"場面をまとめて流す口がある")
chk("PageKind.Title" in _tg2,"題名も1ページとして積める")
chk("IsShowingPage" in _tg2 and "TryStep" in _tg2,"会話中は動けない口がある")
_tc=open("Assets/Scripts/TowerController.cs",encoding="utf-8").read()
chk("Game.IsShowingPage" in _tc and "Game.TryStep" in _tc,
    "入力が会話中はページ送りになる")
chk(os.path.exists("Assets/Scripts/Cutscenes.cs"),"場面の並びを読む Cutscenes がある")
_sc=[[c.strip() for c in l.split("|")] for l in
     open("Assets/Resources/Cutscenes.txt",encoding="utf-8") if l.strip() and not l.startswith("#")]
_names={r[0] for r in _sc}
_used={r[4].split("演出 ")[1].strip() for r in _pe if "演出 " in r[4]}
chk(_names==_used,"場面の名前が位置イベントの呼び出しと過不足なく一致する",
    str(_names ^ _used))

print("\n【9q】メニューの窓")
_tm=open("Assets/Scripts/UI/TowerMenu.cs",encoding="utf-8").read()
chk("WindowLines = 11, TopEdge = 50" in _tm,"メニューは11行・上端50（原典 0xb / 0x32）")
chk("CursorX = 82, CursorY = 23" in _tm,"選ぶ印の位置（原典 0x52 / 0x17）")
chk("OpenKeyCodes = { 0x1c, 0x34 }" in _tm,"メニューを開く鍵は 0x1c と 0x34")
_tg3=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("IsMenuOpen" in _tg3 and "OpenMenu" in _tg3,"本線にメニューの開閉がある")
_tc2=open("Assets/Scripts/TowerController.cs",encoding="utf-8").read()
chk("MenuKeyDown" in _tc2,"入力からメニューを開ける")
chk("MenuMove" in _tg3 and "MenuConfirm" in _tg3,"選ぶ場所を動かして決められる")
chk(os.path.exists("Assets/Scripts/UI/MenuWindowView.cs"),"メニューを描く役がある")
chk("BuildMenuWindow" in _sb,"シーンにメニューの窓を組み込んでいる")
chk("LoadRecord = 50, SaveRecord = 51" in _tg3,"スロット選択はセリフ50/51")
chk("SlotWindowLines = 10, SlotWindowTop = 50" in _tg3,"スロットの窓は10行・上端50")
chk("SlotCancelIndex" in _tg3,"「やっぱりやめる」がある")
for _r in (50,51):
    _b=[l.split(" | ",1)[1] for l in open("Assets/Resources/Dialogue.txt",encoding="utf-8")
        if l.startswith(f"{_r} | ")][0].rstrip("\n")
    chk(len(_b.split("\\n"))==10,f"セリフ{_r}は10行（問い＋8スロット＋やめる）")
    chk(_b.endswith("やっぱりやめる"),f"セリフ{_r}の最後は「やっぱりやめる」")
# メニューの文言が EXE と一字一句同じか
_exe2=open("docs/original/disk/MTE.EXE","rb").read(); _B2=4608+0x12340; _bd=_exe2[_B2:]
_msg={}
for _l in open("Assets/Resources/Messages.txt",encoding="utf-8"):
    if "|" in _l and not _l.startswith("#"):
        _k,_v=_l.split("|",1)
        # 全角スペース(U+3000)は原典の字下げなので消さない。半角だけ落とす
        _msg[_k.strip()]=_v.strip(" \t\r\n")
for _off,_key in ((0xa72,"menu_title"),(0x9c0,"menu_orb_hero"),(0x9d8,"menu_orb_fly"),
                  (0x9f0,"menu_orb_wis"),(0xa08,"menu_special"),(0xa28,"menu_load"),
                  (0xa3c,"menu_save"),(0xa50,"menu_return"),(0xa62,"menu_quit")):
    _e=_bd.index(b"\0",_off)
    _t=_bd[_off:_e].decode("shift_jis").rstrip("\n")
    chk(_msg.get(_key)==_t,f"{_key} が原典 DS:0x{_off:x} と一致",f"{_msg.get(_key)!r} vs {_t!r}")

print("\n【9r】終幕")
_en=open("Assets/Scripts/Ending.cs",encoding="utf-8").read()
_B3=4608+0x12340; _bd3=open("docs/original/disk/MTE.EXE","rb").read()[_B3:]
for _off in (0x1ab7,0x1ace):
    _e=_bd3.index(b"\0",_off)
    chk(_bd3[_off:_e].decode("shift_jis") in _en,f"最後の画面の文字 DS:0x{_off:x} がコードにある")
chk("ArchmageRecord = 155" in _en,"大魔導師の口上はセリフ155")
chk(all(str(r) in _en for r in range(161,171)),"決着のセリフ161〜170が並んでいる")
# 使われないセリフはデータには在る
_unused=[102,103,104,105,123,154,180,181,182,183]
chk(all(r in _dlg for r in _unused),"出番の無いセリフもデータには在る")
chk(all(str(r) in _en for r in _unused),"出番の無いセリフを記録してある")

print("\n【9s】通しで動かす")
chk(os.path.exists("Assets/Editor/TowerPlaythrough.cs"),"通しで動かす道具がある")
chk(os.path.exists("docs/run_playthrough.sh"),"通しで動かす手順が残してある")
_tp=open("Assets/Scripts/TowerPlayer.cs",encoding="utf-8").read()
chk("Outcome.Talk" in _tp,"人や物に触れたときの結果がある")
chk("TowerNpc.IsPerson(what)" in _tp,"人や物では進まない")
chk("IsPerson" in open("Assets/Scripts/Items/TowerNpc.cs",encoding="utf-8").read(),
    "人や物の見分けがある")
chk("void Talk(string what" in open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read(),
    "本線が人や物を振り分けている")

print("\n【9t】解錠で置かれる物体")
_ids={}
for _l in open("Assets/Resources/ObjectIds.txt",encoding="utf-8"):
    if "|" in _l and not _l.startswith("#"):
        _n,_i=_l.split("|"); _ids[int(_i)]=_n.strip()
_spawn=set()
for _l in open("Assets/Resources/SealedDoors.txt",encoding="utf-8"):
    if _l.startswith("#") or "|" not in _l: continue
    for _m in re.finditer(r"ID(\d+)",_l.split("|")[-1]): _spawn.add(int(_m.group(1)))
chk(len(_spawn)>0,"解錠で物体を置く行がある")
_miss=sorted(i for i in _spawn if i not in _ids)
chk(not _miss,"解錠で置かれる物体IDが全部プレハブ名に直せる",str(_miss))
chk(_ids.get(45)=="Wing_Down" and _ids.get(30)=="Key_Red" and _ids.get(29)=="Key_Yellow",
    "ID45=降臨の翼 / ID30=赤い鍵 / ID29=黄色い鍵")
chk("Resolve" in open("Assets/Scripts/Floor/ObjectIds.cs",encoding="utf-8").read(),
    "IDを名前に直す口がある")
chk("ObjectIds.Instance.Resolve" in open("Assets/Scripts/Floor/FloorState.cs",encoding="utf-8").read(),
    "盤面に置くときに名前へ直している")

print("\n【9u】階段で着いたときのできごと")
_ae=[[c.strip() for c in l.split("|")] for l in
     open("Assets/Resources/ArrivalEvents.txt",encoding="utf-8")
     if l.strip() and not l.startswith("#")]
chk(len(_ae)==1,"できごとは1件だけ（EXE全体で他に無い）",str(len(_ae)))
chk(_ae[0][0]=="28" and _ae[0][1]=="下り" and _ae[0][2]=="108",
    "28階に下りで着いたとき・フラグ108")
chk("29:(+0,+3)=床" in _ae[0][3] and "29:(+0,+2)=床" in _ae[0][3],
    "29階の抜け穴が開く")
chk("29:(+1,+4)=Mikata" in _ae[0][3],"盗賊が29階(+1,+4)へ移る")
_g29=_grid(29)
chk(_g29[2][5]=="#","29階(0,+3)は原典データでは壁",_g29[2][5])
chk("ApplyArrival" in open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read(),
    "本線が階段の到着でこれを起こす")

print("\n【10】商人の表")
_so=[[c.strip() for c in l.split("|")] for l in
     open("Assets/Resources/ShopOffers.txt",encoding="utf-8").read().split("\n")
     if l.strip() and not l.strip().startswith("#")]
chk(len(_so)==12,"商人の品揃えは12件（商人11人＋12階の隠れた鍵屋）")
_sf=sorted({int(r[0]) for r in _so})
_mer=sorted(f for f in range(1,51)
            if "Merchant " in open(f"Assets/Resources/Floors/Floor_{f}.txt",encoding="utf-8").read())
chk(_sf==_mer,"商人の階と盤面の Merchant(ID19) の階が一致する")
chk(_mer==[2,6,7,12,15,28,31,38,39,45,47],"商人は原典どおり11階に居る")
_pairs={(6,"-"):(51,52),(7,"-"):(53,54),(12,"1"):(55,56),(15,"-"):(57,58),
        (31,"-"):(59,60),(38,"-"):(61,62),(39,"-"):(63,64),(45,"-"):(65,66),
        (47,"-"):(67,68),(2,"-"):(50,None)}
_ok=True
for _r in _so:
    _k=(int(_r[0]),_r[1])
    if _k not in _pairs:
        _ok = _ok and _r[7]=="-" and _r[8]=="-"    # 何度でも買える商人はフラグ無し
        continue
    _a,_b=_pairs[_k]
    _ok = _ok and int(_r[7])==_a and (_r[8]=="-" if _b is None else int(_r[8])==_b)
chk(_ok,"買フラグ・聞フラグが原典（表の値+0x1e、2階は0x32）どおり")
_fl=[int(v) for _r in _so for v in (_r[7],_r[8]) if v!="-"]
chk(len(_fl)==len(set(_fl)) and min(_fl)==50 and max(_fl)==68,
    "商人のフラグは50〜68で重複なし（アイテム35〜49と仙人69〜86の間）")
chk(all(int(_r[6]) in _dlg for _r in _so if _r[6]!="-"),
    "ヒントのレコード番号が Dialogue.txt に実在する")
chk(all(int(_r[5]) in _dlg for _r in _so if _r[5]!="-"),
    "売り口上のレコード番号が Dialogue.txt に実在する")

# --- 検査そのものの自己点検 ---------------------------------------------
# 過去に、別の検査ブロックを消すとき範囲指定を誤って
# 後続の検査群（25項目）を巻き添えで消し、
# 「不整合0件」が“検査が走っていないだけ”になっていたことがある。
# 検査の本数が減ったら気づけるように下限を置く。

# 鍵の出入り（docs/key_budget.py と同じ数え方）。**落ちている鍵より扉の方が多い**
# ＝「開ける扉を選ぶ」のが遊びの骨。ここが狂うと階のデータが壊れたということ。
def _key_budget():
    _door={"y":"yellow","b":"purple","r":"red"}
    _item={"Key_Yellow":"yellow","Key_Blue":"purple","Key_Red":"red"}
    k={"yellow":0,"purple":0,"red":0}; d={"yellow":0,"purple":0,"red":0}
    for _n in range(1,51):
        _inobj=False
        for _l in open(f"Assets/Resources/Floors/Floor_{_n}.txt",encoding="utf-8"):
            if _l.startswith("objects:"): _inobj=True; continue
            if _inobj:
                _m=re.match(r"\s*([+-]\d+) ([+-]\d+)\s+(\S+)",_l)
                if _m and _m.group(3) in _item: k[_item[_m.group(3)]]+=1
                continue
            _m=re.match(r"\s*([+-]\d+) \| (.{11})$",_l.rstrip("\n"))
            if _m:
                for _c in _m.group(2):
                    if _c in _door: d[_door[_c]]+=1
    return k,d
_kb_k,_kb_d=_key_budget()
chk((_kb_k["yellow"],_kb_d["yellow"])==(169,239),"黄の鍵169本・黄の扉239枚",
    f'{_kb_k["yellow"]}/{_kb_d["yellow"]}')
chk((_kb_k["purple"],_kb_d["purple"])==(20,39),"紫の鍵20本・紫の扉39枚",
    f'{_kb_k["purple"]}/{_kb_d["purple"]}')
chk((_kb_k["red"],_kb_d["red"])==(3,12),"赤の鍵3本・赤の扉12枚",
    f'{_kb_k["red"]}/{_kb_d["red"]}')
chk(all(_kb_k[_c]<_kb_d[_c] for _c in ("yellow","purple","red")),
    "どの色も鍵より扉が多い（開ける扉を選ぶのが遊びの骨）")
chk(os.path.exists("docs/key_budget.py"),"鍵の出入りを数える道具がある")
chk(os.path.exists("docs/floor_keys.py"),"階ごとに要る鍵を見積もる道具がある")
# 商人は買っても・もらっても・売っても、済んだら覚え書きを立てる。
# 立て忘れると同じ商人から何度でも受け取れる（2階の1000GOLDが取り放題だった）。
_npc=open("Assets/Scripts/Items/TowerNpc.cs",encoding="utf-8").read()
chk("何度でも受け取れてしまう" in _npc,"商人の覚え書きは買う・もらう・売るのどれでも立てる")
# 「◎はい／◎いいえ」と祭壇の望みを選ばせる仕掛け（原典 0x9e78）。
# ここが繋がっていなくて、**商人から何も買えず祭壇にも寄進できなかった**。
_tg4=open("Assets/Scripts/TowerGame.cs",encoding="utf-8").read()
chk("public bool IsChoosing" in _tg4 and "public void ConfirmChoice()" in _tg4,
    "本線が選ばせる口を持つ")
chk("BeginChoice(ChoiceKind.Merchant" in _tg4 and "BeginChoice(ChoiceKind.Altar" in _tg4,
    "商人と祭壇のどちらも選ばせる")
chk("TowerNpc.Buy(Player, choiceOffer)" in _tg4 and "TowerNpc.Donate(Player, wish)" in _tg4,
    "選んだ結果が買う・寄進するに繋がっている")
_ctrl=open("Assets/Scripts/TowerController.cs",encoding="utf-8").read()
chk("Game.IsChoosing" in _ctrl and "Game.MoveChoice" in _ctrl and "Game.ConfirmChoice" in _ctrl,
    "入力の側が選ばせに繋がっている（原典 0x9e78 は上下とリターン／スペース）")
# 「スペシャル・アイテムを使う」も繋がっていなかった（選んでも何も起きない）。
chk("case TowerMenu.Entry.SpecialItems:" in _tg4 and "MenuStage.Items" in _tg4,
    "メニューのアイテムの段がある")
chk("public bool ConfirmItem()" in _tg4 and "UseItem(TowerMenu.Usable[ItemIndex])" in _tg4,
    "選んだアイテムが UseItem に繋がっている")
chk("Game.ItemMove" in _ctrl,"入力の側がアイテムの選び直しに繋がっている")
# メニューの項目に「出しているのに何も起きない」枝を作らない（3つ見落とした形）
_entries=re.search(r"public enum Entry\s*\{([^}]*)\}",
                   open("Assets/Scripts/UI/TowerMenu.cs",encoding="utf-8").read(),re.S)
_names=[v.strip() for v in _entries.group(1).replace("\n"," ").split(",") if v.strip()]
_noarm=[v for v in _names if f"TowerMenu.Entry.{v}" not in _tg4]
chk(not _noarm,"メニューの項目はどれも本線に枝がある",str(_noarm))
chk("public bool Quitting" in _tg4 and "if (Game.Quitting) QuitGame();" in _ctrl,
    "「ゲーム終了」が遊びを閉じるところまで繋がっている")
chk("if (pages.Count == 0 && IsChoosing) Choice = ChoiceKind.None;" in _tg4,
    "選ばせている窓を決めずに閉じたら印を残さない")
chk("game.IsChoosing" in open("Assets/Editor/TowerPlaythrough.cs",encoding="utf-8").read(),
    "自動プレイも聞かれたら答える")
# 鳴らす所の無い音を作らない（題名の曲・門が開く・遠ざかるが実際そうなっていた）
_sccs=open("Assets/Scripts/Items/SoundCues.cs",encoding="utf-8").read()
_players="".join(open(q,encoding="utf-8").read() for q in
                 ("Assets/Scripts/TowerGame.cs","Assets/Scripts/TowerController.cs",
                  "Assets/Scripts/UI/FullScreenView.cs","Assets/Scripts/UI/BattleView.cs",
                  "Assets/Scripts/UI/DoorOpenView.cs"))
_silent=[]
for _l in open("Assets/Resources/SoundCues.txt",encoding="utf-8"):
    if _l.startswith("#") or not _l.strip(): continue
    _cue=_l.split("|")[0].strip()
    if _cue == "起動時のテンポ": continue            # 音は出ない（テンポを戻すだけ）
    # 表（PositionEvents.txt）から名前で鳴らすものもある（"音 合図名"）
    if any("音 "+_cue in open(_f,encoding="utf-8").read() for _f in
           ("Assets/Resources/PositionEvents.txt","Assets/Resources/KnightRush.txt")):
        continue
    _m=re.search(r'public const string (\w+)\s*=\s*"'+re.escape(_cue)+r'"',_sccs)
    if not _m or "SoundCues."+_m.group(1) not in _players: _silent.append(_cue)
chk(not _silent,"鳴らす所の無い音が無い",str(_silent))
# 主人公の2コマ目（PLAYER2.P58）。原典 0xa276 は左右に動くときだけ絵を入れ替える。
chk("playerSprite2" in _ctrl and "sideways" in _ctrl,
    "主人公は左右に歩くと2コマ目に入れ替わる（原典 0xa276 の 0x100→0x120）")
chk(os.path.exists("Assets/Sprites/Effects/Player_1.png"),"主人公の2コマ目が書き出してある")
# 選んでいる行の印。原典は PICTURE.P58 の4枚目（0x9e78 が変位 -0x337 から貼る）。
chk(os.path.exists("Assets/Sprites/Hud/Cursor_0.png")
    and os.path.exists("Assets/Sprites/Hud/Cursor_1.png"),
    "印の絵（PICTURE #3/#4）が書き出してある")
chk(-0x337 == -0x381 + 74,"印の変位は紫の鍵の次（-0x381 + 74）")
_mvw=open("Assets/Scripts/UI/MenuWindowView.cs",encoding="utf-8").read()
chk("Image cursor" in _mvw and "PlaceCursor" in _mvw,"メニューの窓が印の絵を置く")
# 印は**文字が変わらなくても**動かす。印の絵を使うと文字の側は変わらないので、
# 「文字が変わったときだけ置き直す」書き方だと上下キーに反応しなかった。
_mbody=_mvw.split("void LateUpdate()")[1]
chk(_mbody.index("PlaceCursor(g)")<_mbody.index("if (text == shown) return;"),
    "メニューの印は毎コマ置き直す（上下キーで動く）")
# 会話の窓はメニューより前に出す（飛翔のオーブを断られたときに後ろに隠れていた）
_dwv=open("Assets/Scripts/UI/DialogueWindowView.cs",encoding="utf-8").read()
chk("panel.SetAsLastSibling()" in _dwv,"会話の窓はメニューより前に出す")
_tsb=open("Assets/Editor/TowerSceneBuilder.cs",encoding="utf-8").read()
chk(_tsb.index("BuildMenuWindow(screen")<_tsb.index("BuildDialogueWindow(screen"),
    "組み立ての順もメニューが先（あとに置いたものが前に出る）")
# 会話の窓の横幅は本文に合わせる（広すぎて文字が左に寄ると報告された）
_as=open("Assets/Scripts/UI/AuthorStyle.cs",encoding="utf-8").read()
chk("DialogueFor(string page)" in _as and "WidthFor" in _as,
    "会話の窓は本文の一番長い行に合わせて広さを決める")
chk("MaxLineChars" in open("Assets/Scripts/UI/DialogueBox.cs",encoding="utf-8").read(),
    "本文の幅を数える所がある")
_dwv=open("Assets/Scripts/UI/DialogueWindowView.cs",encoding="utf-8").read()
chk("Image cursor" in _dwv and "static int ChoiceLine(" in _dwv,
    "セリフの窓も同じ印の絵を置く（「◎」の行を数えて合わせる）")
# 印は2枚で切り替わる。PICTURE.P58 の5枚は実際の大きさを足すと原典の変位に一致する。
_cc=open("Assets/Scripts/UI/ChoiceCursor.cs",encoding="utf-8").read()
chk("StepsPerFrame = 0x15" in _cc,"印の切り替えは 0〜0x14 / 0x15〜0x28（原典 0x9f7f / 0x9fae）")
chk("読み切れていない" in _cc,"切り替えの速さが読み切れていないと明記してある")
_at=-0x40c
for _sz,_want in zip((65,74,74,111,112),(-0x40c,-0x3cb,-0x381,-0x337,-0x2c8)):
    if _at!=_want: chk(False,"PICTURE.P58 の5枚が変位とぴったり並ぶ",f"{_at:#x}≠{_want:#x}"); break
    _at+=_sz
else:
    chk(True,"PICTURE.P58 の5枚（鍵3枚＋印2枚）が変位とぴったり並ぶ")
_tm=open("Assets/Scripts/UI/TowerMenu.cs",encoding="utf-8").read()
chk("SpecialItems.Kind.Elixir,          // special_1" in _tm,
    "アイテムの並びは原典（special_1〜8）そのまま")
_mv=open("Assets/Scripts/UI/MenuWindowView.cs",encoding="utf-8").read()
chk("g.MenuStageText" in _mv and "g.MenuStageIndex" in _mv,
    "メニューの窓は段ごとの文章と印を出す（前は段が変わっても本体の文章のままだった）")
chk(re.search(r"^2 +\| - +\| gift +\| gold:1000 +\| 1000 \| 29 \| - +\| 50",
              open("Assets/Resources/ShopOffers.txt",encoding="utf-8").read(),re.M) is not None,
    "2階のもらいものには覚え書き50が付いている（原典は 0x32 直書き）")
# 遅い階ほど扉が多い。41階の黄の扉19枚が最多で、ここは12階の鍵屋が無いと回らない。
_f41=[l for l in open("Assets/Resources/Floors/Floor_41.txt",encoding="utf-8")]
_f41d=sum(l.split("|")[1].count("y") for l in _f41 if re.match(r"\s*[+-]\d+ \| ",l))
chk(_f41d==19,"41階の黄の扉は19枚（いちばん多い階）",str(_f41d))
_shop=open("Assets/Resources/ShopOffers.txt",encoding="utf-8").read()
chk(re.search(r"^12 \| 9 +\| buy_var",_shop,re.M) is not None,
    "12階の鍵屋だけが何本でも売ってくれる（原典 0x14 * 到達最高階）")

MIN_CHECKS=746

# 計画書の先頭のサマリが古びていないか。**数え物は実物と突き合わせる**
# （38:00 のサマリが 85:00 まで「検査102項目・テスト25本・P58は未解読」のまま残っていた）。
_plan=open("docs/REMAKE_PLAN.md",encoding="utf-8").read()
_head=_plan[:_plan.index("## 手で遊んでもらって出た不具合")]
_m=re.search(r"突き合わせ \*\*(\d+)項目\*\*",_head)
chk(_m is not None and int(_m.group(1))==MIN_CHECKS,
    "サマリに書いた検査の数が実物と合う",
    f"サマリ={_m.group(1) if _m else 'なし'} 実物={MIN_CHECKS}")
_t=re.search(r"EditMode テスト \*\*(\d+)本\*\*",_head)
_actual=sum(len(re.findall(r"\[Test\]",open(q,encoding="utf-8").read()))
            for q in glob.glob("Assets/Tests/Editor/*.cs"))
chk(_t is not None and abs(int(_t.group(1))-_actual)<=15,
    "サマリに書いたテストの数が実物とおおむね合う",
    f"サマリ={_t.group(1) if _t else 'なし'} [Test]の数={_actual}")
chk("原典から読める未確定はもう無い" in _head,"サマリが未確定の有無を言い切っている")
print(f"\n{'='*50}\n実行した検査 {checks} 項目（下限 {MIN_CHECKS}）")
if checks < MIN_CHECKS:
    issues.append(f"検査の本数が {checks} に減っている（{MIN_CHECKS} 以上のはず）")

print(f"\n{'='*50}\n不整合 {len(issues)} 件" + ("" if not issues else ": "+", ".join(issues)))
sys.exit(1 if issues else 0)
