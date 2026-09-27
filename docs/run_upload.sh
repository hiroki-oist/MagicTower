#!/bin/sh
# App Store Connect へ送る（archive → export → upload）。
#
# 実機へ入れる docs/run_ios.sh との違い:
#   run_ios.sh   … Debug 構成・**開発用**の署名・端末へ直接入れる
#   これ         … Release 構成・**配布用**の署名・.ipa にして送る
#
# 要るもの（どちらも無いと途中で止まる。作り方は下の案内に出る）:
#   ① Apple Distribution の証明書（キーチェーンの中）
#   ② 送るための資格情報。どちらか一方でよい。
#      【かんたん】Apple ID と「App用パスワード」を**キーチェーンに入れておく**:
#          xcrun altool --store-password-in-keychain-item AC_PASSWORD \
#                       -u あなたのAppleID -p xxxx-xxxx-xxxx-xxxx
#        （この1回だけ自分の端末で打つ。以後 APPLE_ID だけ渡せばよく、
#          パスワードが履歴にもコマンド行にも残らない）
#        ※ App用パスワードは **Apple ID のもの**で、アプリには紐づかない。
#          名前も入れ物の札にすぎないので、ほかのアプリを出すときも使い回せる。
#        例: APPLE_ID=you@example.com sh docs/run_upload.sh send
#      【本式】App Store Connect の API キー:
#          ASC_KEY_ID    … キーID（10文字）
#          ASC_ISSUER_ID … Issuer ID（UUID）
#          ~/.appstoreconnect/private_keys/AuthKey_<ASC_KEY_ID>.p8
#
# 使い方:
#   sh docs/run_upload.sh              … 検査だけして送らない（--validate まで）
#   sh docs/run_upload.sh send         … 実際に送る
#
# ビルド番号は**送るたびに違う数字**でなければ弾かれるので、
# 既定では日時（YYYYMMDDHHMM）を使う。IOS_BUILD_NUMBER で上書きできる。
set -e

MODE="${1:-check}"
ROOT="$HOME/Code/MagicTower"
UNITY=/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity
export DEVELOPER_DIR="${DEVELOPER_DIR:-/Applications/Xcode.app/Contents/Developer}"
BUNDLE=com.magictower.remake
PROJ="$ROOT/Build/iOS-Device/Unity-iPhone.xcodeproj"
WORK=/tmp/mt_upload
ARCHIVE="$WORK/MagicTower.xcarchive"
EXPORT="$WORK/export"
PLIST="$WORK/ExportOptions.plist"
IOS_BUILD_NUMBER="${IOS_BUILD_NUMBER:-$(date +%Y%m%d%H%M)}"
export IOS_BUILD_NUMBER

TEAM=$(security find-identity -v -p codesigning \
       | sed -n 's/.*Apple Distribution: .*(\([A-Z0-9]*\))".*/\1/p' | head -1)
if [ -z "$TEAM" ]; then
  cat <<'EOS'
★ Apple Distribution の証明書がキーチェーンに無い。

  いまあるのは「Apple Development」だけで、これでは App Store へ出す署名ができない。
  作り方（どちらでもよい）:
    ・Xcode → Settings → Accounts → 自分の Apple ID → Manage Certificates
      → 左下の「＋」→ Apple Distribution
    ・または、このスクリプトの archive に -allowProvisioningUpdates が付いているので、
      Xcode に Apple ID が入っていれば自動で作られることもある（作られなければ上の手順で）

  ※ App Store への配布は **Apple Developer Program（有料）** の登録が要る。
EOS
  exit 1
fi

KEY="$HOME/.appstoreconnect/private_keys/AuthKey_${ASC_KEY_ID}.p8"
HAVE_KEY=0
if [ -n "$ASC_KEY_ID" ] && [ -n "$ASC_ISSUER_ID" ] && [ -f "$KEY" ]; then HAVE_KEY=1; fi
# パスワードは**直に渡さない**。キーチェーンに入れたものを名前で指す
# （コマンド行に書くと ps や履歴から見えてしまう）。
# 入れ物の名前は何でもよく、**アプリごとに分ける必要は無い**
# （App用パスワードは Apple ID に紐づくもので、アプリには紐づかない）。
# 慣例の AC_PASSWORD を先に見て、無ければこの仕事用の名前、
# それも無ければ環境変数から拾う。ALTOOL_KEYCHAIN_ITEM で名指しもできる。
HAVE_PW=0
PW_REF=""
if [ -n "$APPLE_ID" ]; then
  for item in "$ALTOOL_KEYCHAIN_ITEM" AC_PASSWORD MT_APP_PASSWORD; do
    [ -n "$item" ] || continue
    if security find-generic-password -s "$item" >/dev/null 2>&1; then
      HAVE_PW=1; PW_REF="@keychain:$item"; break
    fi
  done
  if [ "$HAVE_PW" = 0 ] && [ -n "$APP_PASSWORD" ]; then
    HAVE_PW=1; PW_REF="@env:APP_PASSWORD"
  fi
fi
if [ "$HAVE_KEY" = 0 ] && [ "$HAVE_PW" = 0 ]; then
  cat <<'EOS'
★ 送るための資格情報が無い。どちらかを用意する。

  【おすすめ】App Store Connect の API キー
    App Store Connect → Users and Access → Integrations → App Store Connect API
    → 「＋」でキーを作り（Role は App Manager で足りる）、.p8 をダウンロード
    → ~/.appstoreconnect/private_keys/AuthKey_<キーID>.p8 に置く
    → 環境変数 ASC_KEY_ID と ASC_ISSUER_ID を渡す
       例: ASC_KEY_ID=XXXXXXXXXX ASC_ISSUER_ID=xxxxxxxx-.... sh docs/run_upload.sh send
    ※ .p8 は**一度しかダウンロードできない**ので無くさないこと。

  【かんたん】Apple ID と「App用パスワード」
    1. appleid.apple.com → サインインとセキュリティ → App用パスワード → ＋
       （そこで付ける名前は**自分用の覚え書き**で、用途を書くとよい。
         例「App Store アップロード」。アプリ名にする必要は無い —
         このパスワードは Apple ID のもので、どのアプリにも使えるため。
         作ると xxxx-xxxx-xxxx-xxxx が出る）
    2. **自分の端末で1回だけ**これを打ってキーチェーンに入れる:
         xcrun altool --store-password-in-keychain-item AC_PASSWORD \
                      -u あなたのAppleID -p xxxx-xxxx-xxxx-xxxx
       （AC_PASSWORD は入れ物の名前。ほかのアプリを出すときも同じものが使える）
    3. あとは APPLE_ID だけ渡せばよい:
         APPLE_ID=you@example.com sh docs/run_upload.sh send
EOS
  exit 1
fi

rm -rf "$WORK"; mkdir -p "$WORK"
cd "$ROOT"

echo "== 1/4 Unity（実機向け・ビルド番号 $IOS_BUILD_NUMBER）"
"$UNITY" -batchmode -quit -projectPath "$ROOT" \
         -executeMethod IosBuild.BuildDevice -logFile /tmp/mt_ios_build.log
grep -q "iOS の書き出し: Succeeded" /tmp/mt_ios_build.log \
  || { echo "★ Unity の書き出しに失敗"; tail -20 /tmp/mt_ios_build.log; exit 1; }

echo "== 2/4 archive（Release・署名 $TEAM）"
xcodebuild -project "$PROJ" -scheme Unity-iPhone -configuration Release \
  -sdk iphoneos -destination 'generic/platform=iOS' \
  -archivePath "$ARCHIVE" archive \
  -allowProvisioningUpdates DEVELOPMENT_TEAM="$TEAM" \
  > /tmp/mt_archive.log 2>&1 \
  || { echo "★ archive に失敗"; grep -E "error:" /tmp/mt_archive.log | head -10; exit 1; }

cat > "$PLIST" <<EOS
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>method</key><string>app-store-connect</string>
  <key>teamID</key><string>$TEAM</string>
  <key>signingStyle</key><string>automatic</string>
  <key>uploadSymbols</key><true/>
  <key>destination</key><string>export</string>
</dict></plist>
EOS

echo "== 3/4 export（.ipa を作る）"
xcodebuild -exportArchive -archivePath "$ARCHIVE" -exportPath "$EXPORT" \
  -exportOptionsPlist "$PLIST" -allowProvisioningUpdates \
  > /tmp/mt_export.log 2>&1 \
  || { echo "★ export に失敗"; grep -E "error:" /tmp/mt_export.log | head -10; exit 1; }
IPA=$(ls "$EXPORT"/*.ipa 2>/dev/null | head -1)
[ -n "$IPA" ] || { echo "★ .ipa ができていない"; tail -20 /tmp/mt_export.log; exit 1; }
echo "   → $IPA"

ALTOOL="$DEVELOPER_DIR/usr/bin/altool"
if [ "$HAVE_KEY" = 1 ]; then
  AUTH="--apiKey $ASC_KEY_ID --apiIssuer $ASC_ISSUER_ID"
else
  AUTH="--username $APPLE_ID --password $PW_REF"
fi

echo "== 4/4 検査（--validate-app）"
# shellcheck disable=SC2086
"$ALTOOL" --validate-app -f "$IPA" -t ios $AUTH --output-format normal

if [ "$MODE" != "send" ]; then
  echo "できあがり（検査まで。送るときは: sh docs/run_upload.sh send）"
  exit 0
fi

echo "== 送信（--upload-app）"
# shellcheck disable=SC2086
"$ALTOOL" --upload-app -f "$IPA" -t ios $AUTH --output-format normal
echo "送った。App Store Connect の TestFlight に出るまで数分〜十数分かかる。"
