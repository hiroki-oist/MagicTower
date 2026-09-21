#!/bin/sh
# 原典のディスクから **絵とデータ一式を作り直す**。Unity は最後のシーン作りだけ。
#
# `Assets/Sprites` `*.meta` `ProjectSettings` などは公開リポジトリに入れていない
# （`.gitignore` 参照。原典から直に起こしたものを置かない方針）ので、
# 手元に無くなったらここから作り直す。**原典のディスクの中身が要る**:
#
#   docs/original/disk/{INITIAL.DAT,GROUND.P58,OBJECT.P58,ITEM.P58,WEAPON.P58,
#                       MONSTER.P58,LCG.P58,MTOPSTR.P58,MTENDMJ.P58,...}
#
# エディタでプロジェクトを開いたままだと最後のシーン作りが落ちるので、先に閉じること。
set -e
cd "$(dirname "$0")/.."
DISK=docs/original/disk
UNITY=/Applications/Unity/Hub/Editor/2022.3.11f1/Unity.app/Contents/MacOS/Unity

[ -f "$DISK/INITIAL.DAT" ] || { echo "★ $DISK/INITIAL.DAT が無い"; exit 1; }

echo "== 1. 階のデータ（INITIAL.DAT → Assets/Resources/Floors/Floor_*.txt）"
python3 docs/decode_pc98.py "$DISK/INITIAL.DAT"

echo "== 2. 盤面のタイル（*.P58 → Assets/Sprites/Tiles、一覧は docs/tile_list.txt）"
python3 docs/p58_to_tiles.py

echo "== 3. 属性表を通さず直に貼られる絵（当たりの閃光など）"
python3 docs/p58_to_direct.py

echo "== 4. 扉が開く9コマ と 20階のボスが現れる10コマ（LCG.P58）"
python3 docs/lcg_to_doors.py

echo "== 5. 画面いっぱいの1枚絵（枠・序章・終幕）"
python3 docs/make_screens.py      # 枠 / 終幕の塔 / 終幕の1枚絵
python3 docs/make_prologue.py     # 序章の4枚
python3 docs/make_ending.py       # 終わりの言葉の5枚

echo "== 6. プレハブ（絵を指すだけの入れ物）と、尺度の揃え直し"
python3 docs/make_prefabs.py
python3 docs/fix_board_prefabs.py

echo "== 7. プレハブ登録簿（.floor とできごとの表から拾う）"
python3 docs/make_registry.py

echo "== 8. シーンを作り直す"
"$UNITY" -batchmode -nographics -quit -projectPath "$PWD" \
         -executeMethod TowerSceneBuilder.Build -logFile /tmp/mt_rebuild.log >/dev/null 2>&1 || true
grep -q "Tower.unity を作成した" /tmp/mt_rebuild.log \
  || { echo "★ シーンが作れていない"; grep -i "error CS" /tmp/mt_rebuild.log | head -5; exit 1; }

echo "== できあがり。検査は docs/run_tests.sh と docs/consistency.py で"
echo
echo "※ ここで作り直せないもの（原典から起こせない作者の持ち物）:"
echo "     Assets/Images/Dialogue.png … セリフと戦闘の枠の絵（旧版から使っている）"
echo "     TextMesh Pro/…/PixelMplus  … 日本語のピクセルフォント"
echo "     ProjectSettings/           … Unity の設定"
echo "   どれも .gitignore に入っているので、失うと戻せない。"
