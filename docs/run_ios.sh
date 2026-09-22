#!/bin/sh
# **iPhone（実機）／シミュレータへ流し込む**。Unity の書き出しから起動まで一息で。
#
#   sh docs/run_ios.sh            … 実機（USB/ネットワークで繋がっている iPhone）
#   sh docs/run_ios.sh simulator  … いま起動しているシミュレータ
#
# 前提:
#   ・エディタで同じプロジェクトを開いていないこと（Unity がバッチで起動できない）
#   ・実機は「デベロッパモード」を有効にしてあること（設定→プライバシーとセキュリティ）
#   ・Xcode に Apple ID を登録してあること（署名に使う）
#
# ⚠ この Mac は xcode-select がコマンドラインツール側を向いているので、
#   ここで DEVELOPER_DIR を Xcode に向けている（`sudo xcode-select -s` でも直せる）。
set -e
MODE="${1:-device}"
export DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer
UNITY=/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity
ROOT="$HOME/Code/MagicTower"
BUNDLE=com.magictower.remake
DD=/tmp/mt_dd

# 署名する組（Team）。**Xcode が作ったプロビジョニングプロファイル**から拾う
# （証明書の側の ID とは違うことがある）。無ければ証明書から。
team_from_profile() {
  for f in "$HOME/Library/Developer/Xcode/UserData/Provisioning Profiles/"*.mobileprovision; do
    [ -e "$f" ] || continue
    security cms -D -i "$f" 2>/dev/null \
      | sed -n '/<key>TeamIdentifier<\/key>/,/<\/array>/p' \
      | sed -n 's:.*<string>\(.*\)</string>.*:\1:p' | head -1
    return
  done
}
TEAM="${IOS_TEAM_ID:-$(team_from_profile)}"
[ -n "$TEAM" ] || TEAM=$(security find-identity -p codesigning -v \
      | sed -n 's/.*Apple Development: .*(\([A-Z0-9]*\))".*/\1/p' | head -1)

cd "$ROOT"

if [ "$MODE" = "simulator" ]; then
  echo "== 1/4 Unity（シミュレータ向け）"
  "$UNITY" -batchmode -quit -projectPath "$ROOT" \
           -executeMethod IosBuild.BuildSimulator -logFile /tmp/mt_ios_build.log
  grep -q "iOS の書き出し: Succeeded" /tmp/mt_ios_build.log \
    || { echo "★ Unity の書き出しに失敗"; tail -20 /tmp/mt_ios_build.log; exit 1; }
  PROJ="$ROOT/Build/iOS/Unity-iPhone.xcodeproj"
  SDK=iphonesimulator
  DEST='generic/platform=iOS Simulator'
  OUT="$DD/Build/Products/Debug-iphonesimulator/MagicTower.app"
else
  echo "== 1/4 Unity（実機向け）"
  "$UNITY" -batchmode -quit -projectPath "$ROOT" \
           -executeMethod IosBuild.BuildDevice -logFile /tmp/mt_ios_build.log
  grep -q "iOS の書き出し: Succeeded" /tmp/mt_ios_build.log \
    || { echo "★ Unity の書き出しに失敗"; tail -20 /tmp/mt_ios_build.log; exit 1; }
  PROJ="$ROOT/Build/iOS-Device/Unity-iPhone.xcodeproj"
  SDK=iphoneos
  DEST='generic/platform=iOS'
  OUT="$DD/Build/Products/Debug-iphoneos/MagicTower.app"
fi

echo "== 2/4 Xcode（署名: ${TEAM:-なし}）"
xcodebuild -project "$PROJ" -scheme Unity-iPhone -configuration Debug \
  -sdk "$SDK" -destination "$DEST" -derivedDataPath "$DD" \
  -allowProvisioningUpdates DEVELOPMENT_TEAM="$TEAM" \
  build > /tmp/mt_xcodebuild.log 2>&1 \
  || { echo "★ Xcode のビルドに失敗"; grep -E "error:" /tmp/mt_xcodebuild.log | head -10; exit 1; }
grep -q "BUILD SUCCEEDED" /tmp/mt_xcodebuild.log || { echo "★ BUILD FAILED"; exit 1; }

if [ "$MODE" = "simulator" ]; then
  echo "== 3/4 シミュレータへ入れる"
  xcrun simctl terminate booted "$BUNDLE" 2>/dev/null || true
  xcrun simctl install booted "$OUT"
  echo "== 4/4 起動"
  xcrun simctl launch booted "$BUNDLE"
else
  DEVICE=$(xcrun devicectl list devices 2>/dev/null \
           | awk '/connected/ {print $3; exit}')
  [ -n "$DEVICE" ] || { echo "★ 繋がっている iPhone が見つからない"; exit 1; }
  echo "== 3/4 iPhone へ入れる（$DEVICE）"
  xcrun devicectl device install app --device "$DEVICE" "$OUT"
  echo "== 4/4 起動"
  xcrun devicectl device process launch --device "$DEVICE" "$BUNDLE"
fi
echo "できあがり"
