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

    /// <summary>
    /// **Bundle ID**。App Store Connect に登録するのと同じ字でなければならない。
    /// 逆順のドメイン名にするのが決まりなので、持っているドメインに合わせて変えること。
    /// </summary>
    public const string BundleId = "com.magictower.remake";

    /// <summary>作り手の名前（Xcode の書き出しと bundle の既定値に出る）。</summary>
    public const string CompanyName = "Hiroki Sawada";

    /// <summary>App Store に出す版の数字（CFBundleShortVersionString）。</summary>
    public const string Version = "1.0";

    /// <summary>ビルド番号（CFBundleVersion）。提出のたびに上げる。</summary>
    public const string DefaultBuildNumber = "1";

    [MenuItem("MagicTower/iOS シミュレータ向けに書き出す")]
    public static void BuildSimulator() { Build(iOSSdkVersion.SimulatorSDK); }

    [MenuItem("MagicTower/iOS 実機向けに書き出す")]
    public static void BuildDevice() { Build(iOSSdkVersion.DeviceSDK); }

    /// <summary>アイコンの元絵（1024x1024）。作り方は `docs/make_icon.py`。</summary>
    public const string IconPath = "Assets/Sprites/Icon.png";

    /// <summary>
    /// アイコンを全部の欄へ入れる。Unity は欄ごとに大きさが違うが、
    /// **同じ1枚**を入れておけば書き出すときにそれぞれの大きさへ縮めてくれる。
    /// 入れないと Unity のロゴのまま出てしまう（App Store には出せない）。
    /// </summary>
    static void SetIcon()
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (tex == null) { Debug.LogWarning("アイコンが無い: " + IconPath); return; }
        foreach (iOSTargetDevice dev in new[] { iOSTargetDevice.iPhoneAndiPad })
        {
            foreach (var kind in new[] { iOSIconKind.Application, iOSIconKind.Settings,
                                         iOSIconKind.Spotlight, iOSIconKind.Notification,
                                         iOSIconKind.Marketing })
            {
                int n = PlayerSettings.GetIconSizesForTargetGroup(
                            BuildTargetGroup.iOS, (IconKind)(int)kind).Length;
                if (n == 0) continue;
                var icons = new Texture2D[n];
                for (int i = 0; i < n; i++) icons[i] = tex;
                PlayerSettings.SetIconsForTargetGroup(
                    BuildTargetGroup.iOS, icons, (IconKind)(int)kind);
            }
            break;
        }
        Debug.Log("アイコンを入れた: " + IconPath);
    }

    /// <summary>iOS のアイコンの欄（UnityEditor.IconKind に合わせた並び）。</summary>
    enum iOSIconKind { Application = 0, Settings = 1, Notification = 2, Spotlight = 3,
                       Store = 4, Marketing = 5 }

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
        // ★StoreKit 2（Apple In-App Purchase）は iOS 15 から。
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        // 実機向けは Xcode に署名を任せる（Apple ID を入れて Team を選ぶだけで通る）。
        PlayerSettings.iOS.appleEnableAutomaticSigning = sdk == iOSSdkVersion.DeviceSDK;
        // 署名する組。**この場で証明書から拾う**（誰の機械かを書き込まないため）。
        // 環境変数 IOS_TEAM_ID があればそちらを優先する。
        var team = System.Environment.GetEnvironmentVariable("IOS_TEAM_ID");
        if (!string.IsNullOrEmpty(team)) PlayerSettings.iOS.appleDeveloperTeamID = team;

        // ★**iPhone 専用**（作者の指定）。画面の並べ方（PhoneScreen）は
        //   縦長の手に持つ画面を前提にしていて、iPad の 4:3 では左右に灰色の余白が
        //   出るだけになる。iPad 対応を名乗ると
        //     ・App Store Connect が 13インチ iPad の写しを要求する
        //     ・審査も iPad で行われる
        //   ので、名乗らない。iPhone 専用のアプリは iPad でも
        //   iPhone 互換モードで入れて遊べるので、遊べる人は減らない。
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;

        // **縦画面だけ**。画面の並べ方（PhoneScreen）が縦を前提にしている。
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.statusBarHidden = true;

        // ------------------------------------------------ App Store に出すための設え
        // 会社名は bundle の既定値や Xcode の書き出しに出るので、DefaultCompany のままにしない。
        PlayerSettings.companyName = CompanyName;
        // ビルド番号は**提出のたびに上げる**（同じ番号は受け付けられない）。
        // 環境変数 IOS_BUILD_NUMBER があればそれを使う。
        var build = System.Environment.GetEnvironmentVariable("IOS_BUILD_NUMBER");
        PlayerSettings.iOS.buildNumber = string.IsNullOrEmpty(build) ? DefaultBuildNumber : build;
        PlayerSettings.bundleVersion = Version;
        SetIcon();
        // 起動画面は Unity が作る storyboard（無地）に任せる。
        // ⚠ Unity の既定（iOSLaunchScreenType.Default）は**黒地に何も無い画面**で、
        //    App Store の規定は満たす。凝った起動画面が要るなら
        //    Player Settings の Launch Screen に絵を差すこと。
        PlayerSettings.iOS.appleEnableAutomaticSigning = sdk == iOSSdkVersion.DeviceSDK;

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
