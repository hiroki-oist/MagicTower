using UnityEngine;

/// <summary>
/// **作者（このリメイクの持ち主）が先に書いた旧版の見せ方**。原典 PC98版の再現ではない。
///
/// 遊んでもらった報告の5番目
/// 「戦闘時の見せ方は旧版を参考にしてほしい。黒い帯が出てきてHPが1ターンごとに減る。
///   セリフも私が作ったやつの方が見やすいのでそのやり方に」
/// を受けて入れたもの。**ここだけは完全再現から外れる**ので、
/// <see cref="Enabled"/> を false にすれば原典どおりの見せ方に戻る。
///
/// 寸法の出どころは旧版のシーン `Assets/Scenes/Floor1.unity`（commit a58a72a）の
/// BattleUI と、`Assets/Scenes/Floor_1.unity`（commit 2b383ea^）の DialogueUI。
/// どちらも Canvas は等倍（ScaleMode 0）で、**縦長のスマホ画面（およそ 600x1200）**
/// を前提に置かれていた:
///
/// <code>
///   BattleUI/BattleCanvas/Turns        幅 画面-300, 高さ  50, 中心 (   0, 800)
///   BattleUI/BattleCanvas/PlayerImage  幅 画面-310, 高さ 200, 中心 (-125, 600)
///   BattleUI/BattleCanvas/EnemyImage   幅 画面-310, 高さ 200, 中心 ( 125, 600)
///   BattleUI/BattleCanvas/PlayerStats  幅 画面-350, 高さ 150, 中心 (-125, 350)  HP:/ATK:/DEF:
///   BattleUI/BattleCanvas/EnemyStats   幅 画面-350, 高さ 150, 中心 ( 125, 350)  HP:/ATK:/DEF:
///   DialogueUI/DialogueCanvas/Dialogue 700x250, 中心 (0, 500), 文字 20
/// </code>
///
/// こちらの画布は横長（原典と同じ 640x400 を1.8倍）なので、
/// **左半分が自分・右半分が相手**の帯に置き換えてある。
/// 数字は原典の画面座標（640x400、左上が原点）で書く。
/// </summary>
public static class AuthorStyle
{
    /// <summary>作者の見せ方にするか。false なら原典どおり。</summary>
    public static readonly bool Enabled = true;

    // ------------------------------------------------------------------ 会話

    /// <summary>
    /// 主人公の絵を **Java版（2001年の公式移植）の4方向×2コマ**にするか。
    ///
    /// ⚠ **原典（PC98版）の主人公は PLAYER1/PLAYER2 の2枚だけ**で、後ろ姿も横向きも無い。
    /// Java版のアトラス `Assets/Images/MagicTower/MTower.gif`（40x40で切ってある）には
    /// 上・下・左・右 × 2コマ の8枚があり、作者の旧版はこちらを使っていた。
    /// 「縦に歩くときも歩いて見えるように」という作者の指定でこちらへ差し替える。
    ///
    /// 並びは上段の x=80 から順に:
    ///   MTower_1/2 … 上（後ろ姿） MTower_3/4 … 下（正面）
    ///   MTower_5/6 … 左           MTower_7/8 … 右
    /// </summary>
    public static readonly bool JavaHeroSprites = true;

    public const string HeroAtlasPath = "Assets/Images/MagicTower/MTower.gif";

    /// <summary>上・下・左・右 × 2コマ の順。</summary>
    public static readonly string[] HeroSpriteNames =
    {
        "MTower_1", "MTower_2",   // 上（後ろ姿）
        "MTower_3", "MTower_4",   // 下（正面）
        "MTower_5", "MTower_6",   // 左
        "MTower_7", "MTower_8",   // 右
    };

    /// <summary>
    /// Java版の絵は40ドット（原典のタイルは32ドット）。**1マスに収める**。
    /// 取り込みは 100ドット=1単位なので、40ドットの絵は 0.4単位。2.5倍で1マス。
    ///
    /// ⚠ 一時は「奇数階は原寸（1.25マス）／偶数階は1マス」で出し分けて見比べてもらったが、
    /// **作者が1マスの方を選んだ**ので全部そちらにしてある。
    /// </summary>
    public const float HeroScaleFit = 2.5f;
    public const float HeroScaleFull = HeroScaleFit;
    public const float HeroFullOffsetY = 0f;

    /// <summary>原寸で出す階（いまはどの階も1マスに収めるので常に false）。</summary>
    public static bool FullSizeHeroOn(int floor) { return false; }

    /// <summary>
    /// 物を拾ったとき、**そのマスへ進む**か。
    /// ⚠ **原典は進まない**（0x00a6fb は座標を変えない＝その場で拾うだけ）。
    /// 「消す→進む→手に入れる の流れにしたい」という作者の指定で入れている。
    /// 押しっぱなしで歩き続けられるようにもなる。
    /// </summary>
    public static readonly bool StepOntoItem = true;

    /// <summary>
    /// 戦いに勝って相手のマスへ踏み込んだあとにも、**魔法攻撃の判定を通す**か。
    ///
    /// ⚠ **原典（PC98版）は通さない**。原典の戦闘（0x61ef〜0x6232）は踏み込みの一歩として
    /// プレーヤーの X か Y を自分で動かしてしまい、そのあと後処理 0x1588（魔法攻撃を含む）を
    /// 呼ばない（0x23ee の戦闘の枝は `call 0x6082` の直後が `retf`）。
    /// つまり原典では、魔導師に挟まれたマスでも**倒して入った瞬間は無傷**で通れる。
    ///
    /// **Java版（2001年の公式移植）はそうならない**。Java版の `attackMonster` は
    /// charaXI/charaYI を一切書かない＝倒しても相手のマスに入らないので、そのマスへは
    /// あとから**歩いて**入ることになる。そして `eventMT` は
    /// 「マスにぴったり乗っていて、かつ前のマスと違う」たびに `ichicheck2`（魔法攻撃）を
    /// 呼ぶので、挟まれたマスに入れば必ず食らう。
    ///
    /// 49階のゼノ戦は、四方の魔導師を倒して入るマスがどれも左右（または上下）を
    /// 魔導師に挟まれている。Java版の挙動だと**神盾が無ければ HP が半分になり続ける**＝
    /// 神盾が実質必須の設計になる。ヒント28「神盾は、魔法使いたちの魔法攻撃を防ぐ力がある」や
    /// 44階（幻の階）の作りからしてそちらが作者の意図と見て、
    /// **Java版に寄せる**（作者の指定）。false にすれば原典どおり無傷で通れる。
    ///
    /// 立ち入りのできごと（0x209a 以降）の方は原典のまま踏み込みでは見ない。
    /// Java版は同じ `eventMT` でそちらも回すが、こちらは階ごとの発動マスを
    /// 別表で持っており、踏み込みで撃つと 32階の金騎士などの出方が変わってしまう。
    /// </summary>
    public static readonly bool MagicAfterBattleStep = true;

    /// <summary>
    /// 勇者のオーブの一覧に**十字架とドラゴンスレイヤーの2倍を入れる**か。
    ///
    /// ⚠ **原典は入れない**。0x8c2a は攻撃力をそのまま使い、補正を掛ける枝が無い。
    /// 原典の説明（MT.GTX レコード60）も「特殊なアイテムを持っている場合には
    /// 実際に受けるダメージと異なる場合がある」と**わざわざ断っている**。
    ///
    /// 作者の指定（2026-09-25）「十字架取った後に勇者のオーブ使ってもちゃんと
    /// 計算に反映されてないな。十字架、ドラゴンスレイヤーをちゃんと計算に入れて」。
    /// 実際の戦いのほうは原典どおり2倍で計算されているので、**窓の数字だけが
    /// 食い違って見える**のを直す形になる。false にすれば原典どおり。
    ///
    /// 入れるのは「必要な手数」と「受けるダメージ」だけ。相手の命・攻撃力・防御力・
    /// 持っているゴールドは**相手そのものの数値**なので触らない
    /// （サイコロの五の目だけは原典が表を書き換えるので、そちらは反映される）。
    /// </summary>
    public static readonly bool HeroOrbCountsCharms = true;

    /// <summary>
    /// 縦に歩くときも主人公の絵を2コマで入れ替えるか。
    ///
    /// ⚠ **原典は横に歩くときだけ入れ替える**（0xa276 は 向き1/2 で 0x100↔0x120 を
    /// 使い分け、向き3/4 は 0x140 のまま）。主人公の絵は PLAYER1/PLAYER2 の
    /// **2枚しか無く、後ろ姿も横向きも存在しない**。
    /// 「縦のときも歩いて見えるように」という作者の指定でここだけ足している。
    /// </summary>
    public static readonly bool WalkAnimationBothWays = true;

    /// <summary>
    /// 窓を**一瞬で**出すか。原典（0x94ba）は上の帯を貼ってから本体を4行ずつ
    /// 塗り広げるが、「待つのがストレス」という作者の指定で一息に出す。
    /// false にすれば原典どおり塗り広げる。
    /// </summary>
    public static readonly bool InstantWindow = true;

    /// <summary>
    /// 本文を1秒に何文字ずつ出すか（作者の指定）。0 なら一度に全部出す。
    /// 出し終わる前に決定を押すと**まず全部出し**、もう一度押すと次へ送る。
    /// 原典は一度に全部書くので、これは作者の見せ方。
    /// </summary>
    public static readonly float TypeCharsPerSecond = 10f;

    /// <summary>
    /// 戦いや登場の動きが終わってから、次の手に移るまでの間（秒。作者の指定）。
    /// 合体の途中でセリフが出たり、戦闘の帯と重なって扉が開いたりするのを防ぐ。
    /// </summary>
    public static readonly float SettleSeconds = 0.5f;

    /// <summary>
    /// **ものが別のものに変わる**ときにかける時間（秒。作者の指定）。
    ///
    /// ⚠ 原典は入れ替えの回数を決め打ちで交互に描くだけ（終幕 0xcb52〜0xcbee は
    /// 盗賊(21)とゼノ(91)を**50回**）。作者の指定で、
    /// 「点滅させながら少しずつゼノ成分を増やして、最後には完全にゼノで点滅しなくなる」
    /// ——つまり**この秒数のあいだ、新しい方を出す割合を 0 から 1 へ上げていく**形にする。
    /// 0 にすれば一瞬で入れ替わる。
    ///
    /// 出たり消えたりするだけの点滅（3階のゼノ登場 0x6ae0＝元が床）は原典のまま。
    /// </summary>
    public static readonly float SwapSeconds = 2f;

    /// <summary>
    /// 終幕を**Java版（2001年の公式移植）のものにする**か（作者の指定）。
    ///
    /// Java版は塔が沈むところを **edata01〜15 の15枚**で描いてあり、1枚ずつ
    /// 40x40 のマス目のモザイクで溶かして切り替える。終わりの言葉（edata17）は
    /// 段落に分かれない1枚絵で、最後は「The End」（edata18）。
    ///
    /// ⚠ 原典 PC98版は MTOWER1（塔）と MTOWER2（荒野）を重ねて塔だけを
    /// 15ドットずつ26回下げ、パレットで白へ寄せる作り（0x0ce83）。
    /// false にすればそちらに戻る。
    /// </summary>
    /// ⚠ **作者の指定でいまは false（原典 PC98版の終幕）**。2026-09-24 に一度 true に
    /// したが、見比べて「エンディングはやっぱりPC98版でお願いします」となった。
    public static readonly bool JavaEnding = false;

    /// <summary>盤面を描き出したあとの間（秒）。暗転が明けてすぐだと誰の声か分からない。</summary>
    public static readonly float SettleAfterDrawSeconds = 1f;

    /// <summary>
    /// ⚠ **もう使っていない**。ボスが現れたあとの間は、台本（Cutscenes.txt）の
    /// 「待つ 30」＝0.5秒に移した（作者の指定「合体が終わってから0.5秒後」）。
    /// ここで待たせると、その間は地図も書けないので**合体が消えたあと
    /// 空っぽの3マス**ができてしまう。原典 0x3044 の順は
    /// 「10コマ → 地図にバンパイアを書く → 60回待つ → セリフ121」。
    /// </summary>
    public static readonly float SettleAfterBossSeconds = 1f;

    /// <summary>
    /// 会話の窓の横幅（ドット）。**本文の一番長い行に合わせて決める**。
    ///
    /// 以前はここを 608（画面いっぱい近く）で固定していたが、折り返しは
    /// <see cref="DialogueBox.CharsPerLine"/>（18文字）のままなので、
    /// 右側に 170 ドット余って**文字が左に寄って見える**と報告された。
    /// 窓を文字の幅に合わせ、画面の中央に置く。
    /// </summary>
    public static readonly int DialogueWidth = Even(MaxDialogueChars);

    /// <summary>窓に入れられる一番長い行（全角の数）。原典の折り返しと同じ。</summary>
    public const int MaxDialogueChars = DialogueBox.CharsPerLine;

    /// <summary>窓の中心の高さ。旧版は画面の下から42%（1200中の500）＝上から58%。</summary>
    public const int DialogueCenterY = 232;

    /// <summary>行送り（ドット）。原典の20より広く取る。</summary>
    public const int DialogueLineAdvance = 26;

    /// <summary>窓の中の文字の位置（窓の左上からのずれ、ドット）。</summary>
    public const int DialogueTextLeft = 24, DialogueTextTop = 14;

    /// <summary>
    /// 文字の大きさ（ドット）。旧版は画面幅600に対して20なので、
    /// 原典の16ドット/640 より 4/3 だけ大きい。
    /// </summary>
    public const float DialogueTextDots = 64f / 3f;

    /// <summary>画布の上での文字の大きさ。</summary>
    public const float DialogueTextOnCanvas = DialogueTextDots * DialogueWindow.CanvasScale;

    /// <summary>
    /// TMP の行間（文字の大きさに対する％）。
    /// 行送り = 文字の大きさ × (1.084 + 行間/100) になるよう合わせてある
    /// （原典側の <see cref="TowerHud.InfoLineSpacing"/> と同じ出し方）。
    /// </summary>
    public const float DialogueLineSpacing = 13.5f;

    /// <summary>行数から窓の高さ（ドット）。上下の余白は原典と同じ32ドット。</summary>
    public static int DialogueHeight(int lines)
    {
        if (lines < 1) lines = 1;
        return DialogueLineAdvance * lines + DialogueWindow.StripHeight;
    }

    /// <summary>行数から窓の位置と大きさ。画面の中心に寄せる。</summary>
    public static RectInt DialogueFor(int lines)
    {
        return DialogueFor(lines, DialogueWidth);
    }

    /// <summary>
    /// そのセリフを出す窓。**横幅は本文の一番長い行に合わせる**（報告: 窓が広すぎて
    /// 文字が左に寄る）。折り返しの位置は原典のままなので、行の中身は変わらない。
    /// </summary>
    public static RectInt DialogueFor(string page)
    {
        return DialogueFor(DialogueBox.WindowLines(page), WidthFor(page));
    }

    /// <summary>本文の一番長い行に合う窓の横幅（ドット）。</summary>
    public static int WidthFor(string page)
    {
        int cap = DialogueBox.LimitFor(page);
        // ★**広い窓（課金）は目一杯の幅で固定する**。
        //   本文に合わせて詰めると、値段が現地の表記（`$0.99` `R$ 29,90` など）で
        //   長さが変わるたびに足りなくなって折り返してしまう
        //   （作者の報告: 「$0.9」で切れて次の行に「9」が落ちていた）。
        //   ⚠ TMP の `<nobr>` は**漢字かなの折り返しは止めない**ので、幅で解くしかない。
        if (cap == DialogueBox.WideCharsPerLine) return Even(cap);
        float chars = DialogueBox.MaxLineChars(page);
        if (chars <= 0f) chars = 1f;
        if (chars > cap) chars = cap;          // ふつうの窓は折り返しの幅で頭打ち
        return Even(chars);
    }

    /// <summary>その文字数ぶんの横幅（ドット）。**偶数に丸める**と左右の余白が揃う。</summary>
    static int Even(float chars)
    {
        int w = Mathf.CeilToInt(chars * DialogueTextDots) + 2 * DialogueTextLeft;
        return (w & 1) == 0 ? w : w + 1;
    }

    static RectInt DialogueFor(int lines, int width)
    {
        int h = DialogueHeight(lines);
        // ★**原典の画面（400ドット）より高い窓も置けるようにする**（作者の指定）。
        //   課金の窓は品目を大きく見せるぶん高くなる。スマホの画面は縦に余裕があるので、
        //   上下に少しはみ出しても構わない、とのこと。そのときは真ん中に置く。
        int top = h >= DialogueWindow.ScreenHeight
                ? (DialogueWindow.ScreenHeight - h) / 2
                : Mathf.Clamp(DialogueCenterY - h / 2, 0, DialogueWindow.ScreenHeight - h);
        int left = (DialogueWindow.ScreenWidth - width) / 2;      // 画面の真ん中に置く
        return new RectInt(left, top, width, h);
    }

    // ------------------------------------------------------------------ 戦闘の帯

    /// <summary>
    /// 枠の絵。**旧版がそのまま使っていたもの**（`Assets/Images/Dialogue.png`、
    /// 32x32 の9スライス・border 6/6/6/5・中身は真っ黒）。
    /// 旧版はこの1枚をセリフの枠にも戦闘の枠にも使っていた
    /// （Dialogue / Turns / PlayerImage / EnemyImage / PlayerStats / EnemyStats すべて
    ///   `m_Sprite: {guid: 8a21752aa0a2a47d19be1cd65397ae9f}`）。
    /// **作者の言う「黒い帯」はこの絵**なので、こちらでも同じものを貼る。
    /// </summary>
    public const string PanelSpritePath = "Assets/Images/Dialogue.png";

    /// <summary>枠の絵に掛ける色。絵がもともと黒なので白（＝そのまま）。</summary>
    public static readonly Color BandFill = Color.white;

    /// <summary>帯の占める範囲（中の枠はこの中に収める）。</summary>
    public static RectInt Band { get { return new RectInt(0, 96, 640, 208); } }

    /// <summary>「第 N ターン」の枠（旧版 Turns）。</summary>
    public static RectInt TurnPanel { get { return new RectInt(160, 96, 320, 28); } }

    /// <summary>絵と名前の枠（旧版 PlayerImage / EnemyImage）。</summary>
    public static RectInt SidePanel(int centerX)
    {
        return new RectInt(centerX - 144, 124, 288, 98);
    }

    /// <summary>能力値の枠（旧版 PlayerStats / EnemyStats）。</summary>
    public static RectInt StatPanel(int centerX)
    {
        return new RectInt(centerX - 144, 222, 288, 82);
    }

    /// <summary>「第 N ターン」の文字（枠の中）。</summary>
    public static RectInt TurnBox { get { return new RectInt(160, 99, 320, 22); } }

    /// <summary>左半分が自分、右半分が相手（旧版は PlayerImage が左、EnemyImage が右）。</summary>
    public const int PlayerCenterX = 160, EnemyCenterX = 480;

    /// <summary>
    /// 絵の大きさと上端。盤面のタイルは32ドットなので、**その2倍**にして
    /// 拡大が整数倍になるようにしてある（半端に伸ばすと絵の点がゆがむ）。
    /// </summary>
    public const int IconSize = 64, IconTop = 130;

    /// <summary>名前の欄。</summary>
    public const int NameTop = 198, NameWidth = 240, NameHeight = 22;

    /// <summary>能力値の欄（HP: / ATK: / DEF: の3行、旧版 List の並びそのまま）。</summary>
    public const int StatWidth = 160, StatHeight = 22;
    public static readonly int[] StatTops = { 226, 250, 274 };

    /// <summary>旧版の見出し（EnemyStats/List の m_text そのまま）。</summary>
    public static readonly string[] StatLabels = { "HP: ", "ATK: ", "DEF: " };

    /// <summary>自分の名前。旧版 PlayerName の文字そのまま。</summary>
    public const string PlayerName = "勇者";

    public static RectInt Icon(int centerX)
    {
        return new RectInt(centerX - IconSize / 2, IconTop, IconSize, IconSize);
    }

    public static RectInt Name(int centerX)
    {
        return new RectInt(centerX - NameWidth / 2, NameTop, NameWidth, NameHeight);
    }

    public static RectInt Stat(int centerX, int row)
    {
        return new RectInt(centerX - StatWidth / 2, StatTops[row], StatWidth, StatHeight);
    }
}
