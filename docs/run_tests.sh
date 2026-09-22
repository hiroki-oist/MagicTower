#!/bin/sh
# Unity をバッチモードで起動して EditMode テストを走らせる。
# エディタでプロジェクトを開いたままだとロックで失敗するので、先に閉じること。
#
# まれに「Running tests for EditMode」を出したきり進まなくなることがある
# （CPU 0.2% のまま何十分も止まる）。2026-09-21 に2回続けて起きたが、
# 止めて Temp/UnityLockfile を消してから走らせ直すと通った。
# 絞り込みたいときは -testFilter "クラス名" を足すと切り分けが早い。
set -e
UNITY=/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity
OUT="${1:-/tmp/mt_tests}"
mkdir -p "$OUT"
# 前回の結果を消しておく。**Unity が落ちると結果ファイルが古いまま残り、
# それを読んで「前回の失敗」を今回の結果だと思い込む**ことがあった（Bus error）。
rm -f "$OUT/results.xml"
"$UNITY" -batchmode -nographics \
  -projectPath "$HOME/Code/MagicTower" \
  -runTests -testPlatform EditMode \
  -testResults "$OUT/results.xml" \
  -logFile "$OUT/unity.log" || true
if [ ! -f "$OUT/results.xml" ]; then
  echo "結果が出ていない（Unity が落ちたか、コンパイルできていない）"
  grep -i "error CS" "$OUT/unity.log" | head -5
  exit 1
fi
python3 - "$OUT/results.xml" <<'PY'
import sys, xml.etree.ElementTree as ET
r = ET.parse(sys.argv[1]).getroot()
print(f"テスト {r.get('total')} 本 / 成功 {r.get('passed')} / 失敗 {r.get('failed')} → {r.get('result')}")
for tc in r.iter("test-case"):
    if tc.get("result") != "Passed":
        print("  ✗", tc.get("name"))
sys.exit(0 if r.get("failed") == "0" else 1)
PY
