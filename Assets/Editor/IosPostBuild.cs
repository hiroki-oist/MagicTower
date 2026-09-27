using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

/// <summary>
/// iOS の Xcode プロジェクトができたあとの後始末。
/// **App Store に出すために要るのに Unity の設定からは書けないもの**をここで足す。
/// </summary>
public static class IosPostBuild
{
    [PostProcessBuild(100)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;
        var plist = Path.Combine(path, "Info.plist");
        if (!File.Exists(plist)) { Debug.LogWarning("Info.plist が無い: " + plist); return; }
        var text = File.ReadAllText(plist);

        // ⓪ **ホーム画面に出る名前**。Unity は Player Settings の Product Name
        //    （MagicTower）をそのまま CFBundleDisplayName に書くので、日本語に直す。
        //    Product Name そのものを日本語にすると Xcode の生成物の名前（.ipa や
        //    スキーム）まで変わって書き出しの段取りが崩れるので、**ここで上書きする**。
        //    App Store の一覧に出る名前（「魔法の塔 復刻版」）は
        //    App Store Connect 側の「名前」で、こちらとは別もの。
        text = Set(text, "CFBundleDisplayName", "<string>" + DisplayName + "</string>");
        //    日本だけに出すアプリなので、既定の言語も日本語にしておく
        //    （Unity は en を書く）。
        text = Set(text, "CFBundleDevelopmentRegion", "<string>ja</string>");

        // ① 輸出コンプライアンス。暗号を使っていないので false。
        //    これが無いと、アップロードのたびに App Store Connect で聞かれる。
        text = Add(text, "ITSAppUsesNonExemptEncryption", "<false/>");

        // ② 著作権の表示（App Store の「著作権」欄とは別に、bundle にも入れておく）。
        text = Add(text, "NSHumanReadableCopyright",
                   "<string>(C) 1996 N.W / Remake by Hiroki Sawada</string>");

        File.WriteAllText(plist, text);
        Debug.Log("Info.plist に App Store 用の項目を足した");

        // ③ Swift（課金の実体 MagicTowerStore.swift）を組み上げるための設定。
        //    Unity は .swift を Xcode プロジェクトに入れてくれるが、
        //    Swift の版と**標準ライブラリを同梱する指定**は自分で入れる必要がある。
        SetSwift(path);
    }

    /// <summary>Swift を使うための build setting を入れる。</summary>
    static void SetSwift(string path)
    {
        var pbxPath = UnityEditor.iOS.Xcode.PBXProject.GetPBXProjectPath(path);
        if (!File.Exists(pbxPath)) { Debug.LogWarning("pbxproj が無い"); return; }
        var proj = new UnityEditor.iOS.Xcode.PBXProject();
        proj.ReadFromFile(pbxPath);
        foreach (var guid in new[] { proj.GetUnityMainTargetGuid(),
                                     proj.GetUnityFrameworkTargetGuid() })
        {
            proj.SetBuildProperty(guid, "SWIFT_VERSION", "5.0");
            proj.SetBuildProperty(guid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            proj.SetBuildProperty(guid, "SWIFT_OPTIMIZATION_LEVEL", "-Onone");
            // ⚠ **Swift を入れると Xcode が Debug の本体を dylib で作ろうとする**
            //   （ENABLE_DEBUG_DYLIB。プレビュー用の仕掛け）。Unity の main.mm は
            //   `__mh_execute_header`（実行ファイルにしか無い印）を見ているので、
            //   dylib にされると繋がらずに落ちる:
            //     Undefined symbols: "__mh_execute_header", referenced from:
            //       UnityFrameworkLoad() in main.o
            //   実行ファイルのまま作らせる。
            proj.SetBuildProperty(guid, "ENABLE_DEBUG_DYLIB", "NO");
        }
        proj.WriteToFile(pbxPath);
        Debug.Log("Swift の設定を入れた（SWIFT_VERSION / 標準ライブラリの同梱）");
    }

    /// <summary>ホーム画面のアイコンの下に出る名前（作者の指定）。</summary>
    public const string DisplayName = "魔法の塔";

    /// <summary>その鍵の値を差し替える（無ければ足す）。</summary>
    static string Set(string plist, string key, string value)
    {
        var mark = "<key>" + key + "</key>";
        int at = plist.IndexOf(mark);
        if (at < 0) return Add(plist, key, value);
        int from = at + mark.Length;
        // 次の要素（<string>…</string> など）ひとつぶんを置き換える
        int open = plist.IndexOf('<', from);
        if (open < 0) return plist;
        int close = plist.IndexOf('>', open);
        if (close < 0) return plist;
        var tag = plist.Substring(open + 1, close - open - 1);
        int end;
        if (tag.EndsWith("/")) end = close + 1;                  // <false/> のような空の要素
        else
        {
            var closing = "</" + tag + ">";
            int c = plist.IndexOf(closing, close);
            if (c < 0) return plist;
            end = c + closing.Length;
        }
        return plist.Substring(0, open) + value + plist.Substring(end);
    }

    /// <summary>その鍵がまだ無ければ足す（あるものは触らない）。</summary>
    static string Add(string plist, string key, string value)
    {
        if (plist.Contains("<key>" + key + "</key>")) return plist;
        int at = plist.LastIndexOf("</dict>");
        if (at < 0) return plist;
        return plist.Substring(0, at)
             + "\t<key>" + key + "</key>\n\t" + value + "\n"
             + plist.Substring(at);
    }
}
