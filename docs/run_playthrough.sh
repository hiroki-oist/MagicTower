#!/bin/sh
# 通しで動かしてみる。単体テストは「規則が正しいか」しか見ないので、
# **遊べるか**はこちらで確かめる。
#
# エディタでプロジェクトを開いたままだと起動できないので、先に閉じること
# （開いていると "another Unity instance is running" で落ちる）。
set -e
UNITY=/Applications/Unity/Hub/Editor/2022.3.11f1/Unity.app/Contents/MacOS/Unity
LOG="${1:-/tmp/mt_play.log}"
"$UNITY" -batchmode -nographics -quit \
  -projectPath "$HOME/Code/MagicTower" \
  -executeMethod TowerPlaythrough.RunBatch \
  -logFile "$LOG" >/dev/null 2>&1 || true
awk '/=== 通し ===/{f=1;n++} f&&n==1{print} /UnityEngine.StackTrace/{exit}' "$LOG"
