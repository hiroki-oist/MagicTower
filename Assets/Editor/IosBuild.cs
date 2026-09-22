using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// iOS（まずは**シミュレータ向け**）の書き出し。
///
/// Unity は iOS の実行ファイルを直に作らず、**Xcode プロジェクトを吐く**。
/// それを Xcode で開いて iPhone 16 などのシミュレータや実機に流す、という段取り。
///
///   Unity -batchmode -quit -projectPath . -executeMethod IosBuild.BuildSimulator
///   → Build/iOS に Xcode プロジェクトができる（Unity-iPhone.xcodeproj）
///
/// 署名は要らない（シミュレータ向けのため）。実機に入れるときは
/// <see cref="BuildDevice"/> で Device SDK にしてから、Xcode 側で署名する。
/// </summary>
public static class IosBuild
{
    /// <summary>書き出し先。シミュレータ向けと実機向けは別にする。</summary>
    public const string OutDir = "Build/iOS";
    public const string DeviceOutDir = "Build/iOS-Device";
    public const string Scene = "Assets/Scenes/Tower.unity";

    /// <summary>アプリの名前と id（実機に入れるときは id を自分のものに変える）。</summary>
    public const string ProductName = "MagicTower";
    public const string BundleId = "com.magictower.remake";

    [MenuItem("MagicTower/iOS シミュレータ向けに書き出す")]
    public static void BuildSimulator() { Build(iOSSdkVersion.SimulatorSDK); }

    [MenuItem("MagicTower/iOS 実機向けに書き出す")]
    public static void BuildDevice() { Build(iOSSdkVersion.DeviceSDK); }

    static void Build(iOSSdkVersion sdk)
    {
        PlayerSettings.productName = ProductName;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, BundleId);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.iOS.sdkVersion = sdk;
        // シミュレータ向けは、既定だと x86_64（Intel Mac 用）で作られてしまう。
        // Apple Silicon の Mac のシミュレータは **arm64** しか動かせないので明示する。
        // （0=ARMv7 / 1=ARM64 / 2=Universal）
        PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1);
        Debug.Log($"iOS の作り: SDK={sdk} / Architecture="
                + PlayerSettings.GetArchitecture(BuildTargetGroup.iOS));
        PlayerSettings.iOS.targetOSVersionString = "13.0";
        // 実機向けは Xcode に署名を任せる（Apple ID を入れて Team を選ぶだけで通る）。
        PlayerSettings.iOS.appleEnableAutomaticSigning = sdk == iOSSdkVersion.DeviceSDK;

        // **縦画面だけ**。画面の並べ方（PhoneScreen）が縦を前提にしている。
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.statusBarHidden = true;

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = sdk == iOSSdkVersion.SimulatorSDK ? OutDir : DeviceOutDir,
            target = BuildTarget.iOS,
            targetGroup = BuildTargetGroup.iOS,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        if (sdk == iOSSdkVersion.SimulatorSDK) MakeSimulatorArm64();
        var s = report.summary;
        Debug.Log($"iOS の書き出し: {s.result} / {s.totalSize} バイト / "
                + $"{s.totalTime.TotalMinutes:0.0}分 → {opts.locationPathName}  (SDK={sdk})");
        if (s.result != BuildResult.Succeeded)
            Debug.LogError("iOS の書き出しに失敗した");
    }

    /// <summary>
    /// **シミュレータ向けを arm64 に直す**。
    ///
    /// Unity は simulator SDK のとき `libil2cpp-sim-x64…`（Intel Mac 用）を置き、
    /// Xcode プロジェクトにも `ARCHS = x86_64` と書く。**Apple Silicon の Mac の
    /// シミュレータは arm64 のアプリしか動かせない**ので、そのままだと
    /// 「デベロッパによるアップデートが必要です」と言われて入らない。
    ///
    /// Unity 本体は arm64 版のライブラリを持っている（`*-sim-arm64-*.a`）ので、
    /// 置き換えて `ARCHS` を arm64 に書き換える。Xcode の中の IL2CPP の呼び出しは
    /// `$ARCHS` を見て `--architecture` を決めるので、これだけで通る。
    /// </summary>
    static void MakeSimulatorArm64()
    {
        var libs = Path.GetFullPath(Path.Combine(
            EditorApplication.applicationContentsPath,
            "../../PlaybackEngines/iOSSupport/Trampoline/Libraries"));
        var outLibs = Path.Combine(OutDir, "Libraries");
        if (!Directory.Exists(libs) || !Directory.Exists(outLibs))
        {
            Debug.LogWarning($"arm64 への差し替えができない: {libs} / {outLibs}");
            return;
        }

        Swap(libs, outLibs, "libil2cpp.a", new[]
        {
            "libil2cpp-sim-arm64-net40-wbarriers.a",
            "libil2cpp-sim-arm64-net40.a",
            "libil2cpp-sim-arm64.a",
        });
        Swap(libs, outLibs, "baselib.a", new[] { "baselib-sim-arm64.a" });
        // エンジン本体。GC の実体（_GC_ignore_warn_proc など）はここにある。
        // これを忘れると、arm64 で組み上がっても起動時に
        // 「symbol not found in flat namespace '_GC_ignore_warn_proc'」で落ちる。
        Swap(libs, outLibs, "libiPhone-lib.dylib", new[] { "libiPhone-lib-sim-arm64.dylib" });

        var pbx = Path.Combine(OutDir, "Unity-iPhone.xcodeproj/project.pbxproj");
        if (File.Exists(pbx))
        {
            var text = File.ReadAllText(pbx);
            int n = 0;
            var idx = 0;
            while ((idx = text.IndexOf("ARCHS = x86_64;", idx)) >= 0)
            {
                text = text.Remove(idx, "ARCHS = x86_64;".Length)
                           .Insert(idx, "ARCHS = arm64;");
                n++;
            }
            File.WriteAllText(pbx, text);
            Debug.Log($"ARCHS を arm64 に書き換えた（{n}か所）");
        }
    }

    static void Swap(string from, string to, string name, string[] candidates)
    {
        foreach (var c in candidates)
        {
            var src = Path.Combine(from, c);
            if (!File.Exists(src)) continue;
            File.Copy(src, Path.Combine(to, name), true);
            Debug.Log($"{name} ← {c}");
            return;
        }
        Debug.LogWarning($"{name} の arm64 版が見つからない");
    }
}
