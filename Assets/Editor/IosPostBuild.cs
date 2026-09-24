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

        // ① 輸出コンプライアンス。暗号を使っていないので false。
        //    これが無いと、アップロードのたびに App Store Connect で聞かれる。
        text = Add(text, "ITSAppUsesNonExemptEncryption", "<false/>");

        // ② 著作権の表示（App Store の「著作権」欄とは別に、bundle にも入れておく）。
        text = Add(text, "NSHumanReadableCopyright",
                   "<string>(C) 1996 N.W / Remake by Hiroki Sawada</string>");

        File.WriteAllText(plist, text);
        Debug.Log("Info.plist に App Store 用の項目を足した");
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
