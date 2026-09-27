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
#      【おすすめ】App Store Connect の API キー:
#          ASC_KEY_ID    … キーID（10文字）
#          ASC_ISSUER_ID … Issuer ID（UUID）
#          ~/.appstoreconnect/private_keys/AuthKey_<ASC_KEY_ID>.p8
#        鍵は**ファイルとして置くだけ**なので、秘密がコマンド行にも環境変数にも出ない。
#      【次善】Apple ID と App用パスワード: APPLE_ID / APP_PASSWORD
#
#   ⚠ この Mac で実際に確かめた癖（Xcode 26.2 / altool 26.10.1）:
#      ・`xcrun altool` は DEVELOPER_DIR を付けないと見つからない
#        （xcode-select が CommandLineTools を指しているため）
#      ・`--store-password-in-keychain-item NAME` はヘルプの例のままでは動かず、
#        **`--item NAME` を足さないといけない**
#      ・そうして入れても `-p @keychain:NAME` は「見つからない」と言われる
#        （--keychain で場所を教えても同じ）。キーチェーン経由は使わない。
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
# パスワードは**コマンド行に直に書かない**。環境変数の名前だけを渡す
# （@env: は altool が自分で読むので、ps や履歴に中身が出ない）。
if [ -n "$APPLE_ID" ] && [ -n "$APP_PASSWORD" ]; then
  HAVE_PW=1; PW_REF="@env:APP_PASSWORD"
fi
if [ "$HAVE_KEY" = 0 ] && [ "$HAVE_PW" = 0 ]; then
  cat <<'EOS'
★ 送るための資格情報が無い。どちらかを用意する。

  【おすすめ】App Store Connect の API キー（秘密がファイルの中だけで済む）
    1. App Store Connect → Users and Access → Integrations
       → App Store Connect API → 「＋」でキーを作る（Role は App Manager で足りる）
    2. .p8 をダウンロード（**一度きり**）して、この Mac の
       ~/.appstoreconnect/private_keys/AuthKey_<キーID>.p8 に置く
    3. キーID（10文字）と Issuer ID（UUID。鍵の一覧の上に出ている）を渡す
         ASC_KEY_ID=XXXXXXXXXX ASC_ISSUER_ID=xxxxxxxx-xxxx-.... \
           sh docs/run_upload.sh send

  【次善】Apple ID と App用パスワード
    1. appleid.apple.com → サインインとセキュリティ → App用パスワード → ＋
       （そこで付ける名前は自分用の覚え書き。用途を書くとよい。
         このパスワードは Apple ID のもので、どのアプリにも使える）
    2. 値がコマンド行に出ない形で環境変数に入れて渡す:
         read -s APP_PASSWORD; export APP_PASSWORD
         APPLE_ID=you@example.com sh docs/run_upload.sh send

  ⚠ キーチェーンに入れて -p @keychain: で渡すやり方は**この版では動かない**
     （altool 26.10.1。--item を足して保存はできるが、読み出しで見つけられない）。
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
  AUTH="--api-key $ASC_KEY_ID --api-issuer $ASC_ISSUER_ID"
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
