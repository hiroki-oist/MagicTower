using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲームの一手をまとめる。原典 PC98版の進行をそのまま写す。
///
/// <code>
///   1. 行きたい方向へ1マス（壁・門なら動けない。鍵の扉は鍵を使って開く）
///   2. そのマスに物があれば拾う／敵がいれば戦う
///   3. 動いたなら、隣接マスの魔法攻撃を受ける
///   4. 解錠条件をまとめて判定する
/// </code>
///
/// 3と4は原典の判定関数（0x1ab0 の魔法攻撃、0x4762 の解錠）に対応する。
/// 戦闘に勝つと**そのマスを床にしてから**解錠を判定するのも原典どおり。
/// </summary>
public class TowerGame
{
    public PlayerState Player { get; private set; }
    public FloorState Floor { get; private set; }

    readonly ItemPowerTable power;
    readonly List<string> log = new List<string>();
    readonly List<int> sounds = new List<int>();

    /// <summary>直前の一手で出たメッセージ。原典の右下の欄は履歴が積み上がる。</summary>
    public IReadOnlyList<string> Log { get { return log; } }

    public TowerGame(PlayerState player, ItemPowerTable powerTable = null)
    {
        Player = player ?? PlayerState.NewGame();
        power = powerTable ?? ItemPowerTable.Instance;
        EnterFloor(Player.Floor, Player.Position);
    }

    /// <summary>
    /// 一度入った階の盤面はそのまま取っておく。
    /// 原典は50階ぶんを1本の可変な配列で持っていて、倒した敵も開けた抜け穴もそのまま残る。
    /// </summary>
    readonly Dictionary<int, FloorState> floors = new Dictionary<int, FloorState>();

    /// <summary>その階の盤面を取り出す（まだ入っていない階も組み立てる）。</summary>
    public FloorState FloorAt(int floor)
    {
        FloorState st;
        if (floors.TryGetValue(floor, out st)) return st;
        var data = FloorData.Load(floor);
        if (data == null) return null;
        st = new FloorState(data);
        floors[floor] = st;
        return st;
    }

    public void EnterFloor(int floor, Vector2Int at)
    {
        var st = FloorAt(floor);
        if (st == null) { Say($"F{floor} のデータが読めない"); return; }
        Floor = st;
        Player.Floor = floor;
        Player.Position = at;
        Player.MarkVisited(floor);
        Evaluate();
    }

    /// <summary>出すものの種類。</summary>
    public enum PageKind { Dialogue, Title, Effect, FinalScreen, Prologue,
                           EndingTower, EndingText, GameOver, HeroOrb }

    /// <summary>出すもの1つぶん。</summary>
    public struct Page
    {
        public PageKind Kind;
        public string Text;
        /// <summary>もとのセリフ番号。題名など番号が無いものは -1。</summary>
        public int Record;
        /// <summary>起こすできごとの名前（Kind が Effect のときだけ）。</summary>
        public string Effect;
        /// <summary>
        /// **このページを出す前に**地図へ書き込むもの（マスと種別）。
        /// 原典の場面はセリフの合間に地図を書き換える（3階の 0x6ae0 は
        /// ゼノを出す → セリフ110 → 魔導師を出す → セリフ111 の順）ので、
        /// 場面を積むときにまとめて書いてしまうと**順番が崩れる**。
        /// </summary>
        public List<Cutscenes.MapStep> MapBefore;
        /// <summary>
        /// そのページを**出すときに**鳴らす合図（曲の切り替え）。
        /// 場面を積む時点で鳴らすと、まだ何も出ていないうちに曲が変わってしまう
        /// （3階で踏み込んだ瞬間に曲が変わって一瞬止まる、という報告）。
        /// </summary>
        public string Cue;
    }

    readonly Queue<Page> pages = new Queue<Page>();

    /// <summary>
    /// これから出すページ。原典は1つ出すたびにキー入力を待つ
    /// （セリフ表示のあとに必ず `lcall 0x107e, 0x4fa` が入っている）。
    /// </summary>
    public int PendingPages { get { return pages.Count; } }

    /// <summary>今出すべき本文。無ければ null。</summary>
    public string CurrentPage { get { return pages.Count > 0 ? pages.Peek().Text : null; } }

    /// <summary>今出すべきものの中身。無ければ null。</summary>
    public Page? Current { get { return pages.Count > 0 ? pages.Peek() : (Page?)null; } }

    /// <summary>今出すべきものの番号（序章の絵なら何行目まで出すか）。無ければ 0。</summary>
    public int CurrentRecord { get { return pages.Count > 0 ? pages.Peek().Record : 0; } }

    /// <summary>今出すべきものの種類。何も無ければ null。</summary>
    public PageKind? CurrentKind
    {
        get { return pages.Count > 0 ? pages.Peek().Kind : (PageKind?)null; }
    }

    /// <summary>先頭のページの地図の書き換えを済ませたか。</summary>
    bool frontMapDone;

    /// <summary>今のページを読み終えた。まだ続きがあれば true。</summary>
    public bool NextPage()
    {
        // 出ているのが「できごと」なら、送るのではなく**その手を起こす**。
        // 押しても飛ばせない（原典の場面もキーでは飛ばせない）。
        if (pages.Count > 0 && pages.Peek().Kind == PageKind.Effect)
        {
            ResolveEffects();
            return pages.Count > 0;
        }
        // 送る前に、そのページの地図の書き換えを必ず当てておく。
        // 演出を1手ずつ待つようになって「当てないまま送ってしまう」道ができた。
        if (pages.Count > 0 && !frontMapDone)
        {
            ApplyMap(pages.Peek().MapBefore);
            PlaySound(pages.Peek().Cue);
        }
        if (pages.Count > 0) { pages.Dequeue(); frontMapDone = false; }
        ResolveEffects();
        // 選ばせている窓を**決めずに閉じた**ら「いいえ」と同じ扱いにする。
        // 決めないまま印だけ残ると、次に何を押しても選び直しになってしまう。
        if (pages.Count == 0 && IsChoosing) Choice = ChoiceKind.None;
        return pages.Count > 0;
    }

    /// <summary>地図の書き換えをまとめて当てる。点滅の指定があれば見た目の層へ渡す。</summary>
    void ApplyMap(List<Cutscenes.MapStep> writes)
    {
        if (writes == null || Floor == null) return;
        foreach (var w in writes)
        {
            if (w.Kind == "床") Floor.Clear(w.Pos);
            else Floor.Put(w.Pos, FloorState.SymbolOfKind(w.Kind));
            if (w.Blinks > 0) blinks.Add(w);
        }
    }

    /// <summary>
    /// これから点滅させるマス。原典の場面はものを出すとき
    /// **床とものを交互に描いて点滅させる**（3階の 0x6ae0）。
    /// 音や閃光と同じで、ここは積むだけにして描くのは見た目の層に任せる。
    /// </summary>
    readonly List<Cutscenes.MapStep> blinks = new List<Cutscenes.MapStep>();

    /// <summary>これから点滅させるものが残っているか（窓はこれが片付くまで待つ）。</summary>
    public bool HasBlinks { get { return blinks.Count > 0; } }

    /// <summary>
    /// 見た目へ渡したまま、まだ動き出していない仕事があるか。
    /// 積んだコマと、見た目がそれを拾うコマの間に1コマの隙があるので、
    /// ここを見ないとその隙に入力が通ってしまう（扉が閉まる前に動けた報告）。
    /// </summary>
    public bool HasPendingView
    {
        get
        {
            return blinks.Count > 0 || walks.Count > 0 || doorOpens.Count > 0
                   || bossAppears.Count > 0 || vanishes.Count > 0 || deathQueued
                   || PlayerSlide != null;
        }
    }

    /// <summary>積んだ点滅を取り出して空にする。</summary>
    public List<Cutscenes.MapStep> ConsumeBlinks()
    {
        var copy = new List<Cutscenes.MapStep>(blinks);
        blinks.Clear();
        return copy;
    }

    /// <summary>
    /// 先頭ができごとなら、その場で起こして次へ進める。
    /// 原典も画面を出す合間に状態を書き換えている（題名のあとに 0x06f1b が走る）。
    /// </summary>
    void ResolveEffects()
    {
        while (pages.Count > 0)
        {
            // 原典の場面は1本道で、絵が動き終わるまで次の行に進まない。
            // 見た目の層が動いている間は地図も書き換えない（出す順が崩れるため）。
            // ただし**戦いの後始末だけ**は、ひと呼吸を待たずに当てる。
            bool afterBattle = pages.Peek().Kind == PageKind.Effect
                               && pages.Peek().Effect != null
                               && pages.Peek().Effect.StartsWith(AfterBattleStep);
            if (afterBattle ? ViewMoving : ViewBusy) break;
            int before = ViewWork;
            // そのページを出す前に地図を書き換える（原典も場面の途中で書いている）
            if (!frontMapDone)
            {
                ApplyMap(pages.Peek().MapBefore);
                PlaySound(pages.Peek().Cue);     // 曲の切り替えは**出すとき**に
                frontMapDone = true;
            }
            // 見た目に何か渡したら、そこで止めて動き終わるのを待つ。
            // 10階の場面は骸骨を1体ずつ寄せてくるので、ここで止めないと重なってしまう。
            if (ViewWork != before) break;
            if (pages.Peek().Kind != PageKind.Effect) break;
            var e = pages.Dequeue();
            frontMapDone = false;
            if (e.Effect == Cutscenes.ZenoTakesStep)
            {
                Player.ApplyZenoEvent();
                EnterFloor(Player.Floor, Player.Position);
                // 原典 0x7038。奪われて本編が始まるところで本編の曲に戻す。
                // これが無いと、題名の曲のまま塔を登り続けることになる。
                PlaySound(SoundCues.FieldMusic);
            }
            else if (e.Effect == DyingStep)
            {
                // 見せるのは見た目の層（BattleView）。ここは**間を作る**のが仕事なので、
                // 渡した印を立てて、動き終わるまで次（ＧＡＭＥ　ＯＶＥＲ）へ進ませない。
                viewTicket++;
            }
            else if (e.Effect != null && e.Effect.StartsWith(ClearCellStep))
            {
                Vector2Int gone;
                if (Cutscenes.TryParseCell(e.Effect.Substring(ClearCellStep.Length).Trim(),
                                           "", out gone))
                    Floor.Clear(gone);
            }
            else if (e.Effect != null && e.Effect.StartsWith(AfterBattleStep))
            {
                Vector2Int at;
                if (Cutscenes.TryParseCell(e.Effect.Substring(AfterBattleStep.Length).Trim(),
                                           "", out at))
                {
                    Floor.Clear(at);
                    Player.Position = at;
                }
                if (pendingGoldNote > 0)
                {
                    Note(GameText.Instance.Message("battle_gold", pendingGoldNote));
                    pendingGoldNote = 0;
                }
            }
            else if (e.Effect != null && e.Effect.StartsWith(Cutscenes.BattlePrefix))
            {
                // 原典 0x68b0 は歩かせたあと戦闘（0x6082）を直に呼ぶ。
                // 踏み込みではないので主人公は動かず、相手のマスも空けない。
                var arg = e.Effect.Substring(Cutscenes.BattlePrefix.Length).Trim();
                int paren = arg.IndexOf('(');
                Vector2Int spot = Player.Position;
                if (paren > 0)
                {
                    Cutscenes.TryParseCell(arg.Substring(paren), "", out spot);
                    arg = arg.Substring(0, paren).Trim();
                }
                RunBattle(arg, spot);
            }
            else if (e.Effect == Cutscenes.BossAppearStep)
            {
                // 原典 0x3119〜0x3194。3x3の中心に10コマを貼る。描くのは見た目の層。
                bossAppears.Add(BossAppear.Center);
            }
            else if (e.Effect != null && e.Effect.StartsWith(Cutscenes.FlashStep))
            {
                // 原典は音7のあと当たりの絵を貼る。貼る先は場面によって違う
                // （3階は自分のマス、42階は金騎士のマス＝0x73e6 の貼り先 0x4da6）。
                PlaySound(SoundCues.SceneBeat);
                Vector2Int flashAt;
                PlayEffect(Cutscenes.TryParseFlash(e.Effect, out flashAt)
                           ? flashAt : Player.Position);
            }
            else if (e.Effect == Cutscenes.ClimbStep)
            {
                LastClimb = new List<int>(Ending.ClimbFloors());
                // 原典 0x2386〜0x23cd は現在階を1つずつ50まで上げて見せ、
                // 最後に X=5, Y=6（盤面の (0,-1)）へ立たせる。
                Player.HighestFloor = Mathf.Max(Player.HighestFloor, Ending.LastFloor);
                EnterFloor(Ending.LastFloor, Ending.LastPosition);
            }
            else if (e.Effect == Cutscenes.ThiefLeavesStep)
            {
                foreach (var c in leaving.Changes)
                {
                    var st = FloorAt(c.Floor);
                    if (st != null) st.Clear(c.Pos);
                }
                // 去り方は表のとおり（原典 0x3e0/0x529 で歩くのは
                // 2階＝下へ2歩、15階＝抜け穴を開けて左へ2歩、の2件だけ）。
                // 歩き終えたところで消える（原典 0x45be がマスを描き直す）。
                if (leaving.Walk != null)
                {
                    walks.Add(new NpcWalk
                    {
                        Prefab = Prologue.ThiefPrefab,
                        From = leaving.WalkFrom,
                        Path = new List<Vector2Int>(leaving.Walk)
                    });
                }
            }
            else if (e.Effect == Cutscenes.PushUpStep)
            {
                // 原典 0x3ebc。主人公を上へ1マス滑らせて Y を1つ減らす。
                var to = Player.Position + Vector2Int.up;
                PlayerSlide = new NpcWalk
                {
                    Prefab = null, From = Player.Position,
                    Path = new List<Vector2Int> { to }
                };
                Player.Position = to;
            }
            else if (e.Effect != null && e.Effect.StartsWith(Cutscenes.WalkPrefix))
            {
                string prefab; Vector2Int from; List<Vector2Int> path; bool slow;
                if (Cutscenes.TryParseWalk(e.Effect, out prefab, out from, out path, out slow))
                    walks.Add(new NpcWalk
                    {
                        Prefab = prefab, From = from, Path = path, Slow = slow,
                        // 盤面に載っていない人は、着いたあとも絵だけで立たせておく
                        Stay = Floor == null || Floor.IsCleared(path[path.Count - 1])
                    });
            }
            else if (e.Effect != null && e.Effect.StartsWith(Cutscenes.DoorOpenPrefix))
            {
                Vector2Int at;
                if (Cutscenes.TryParseCell(e.Effect, Cutscenes.DoorOpenPrefix, out at))
                    doorOpens.Add(new DoorOpen { At = at, Cell = "s" });   // 音は動きの頭で
            }
            else if (e.Effect != null && e.Effect.StartsWith(Cutscenes.DoorClosePrefix))
            {
                Vector2Int at;
                if (Cutscenes.TryParseCell(e.Effect, Cutscenes.DoorClosePrefix, out at))
                    doorOpens.Add(new DoorOpen { At = at, Cell = "s", Closing = true });
            }
            else if (e.Effect != null && e.Effect.StartsWith(Cutscenes.SoundPrefix))
            {
                PlaySound(e.Effect.Substring(Cutscenes.SoundPrefix.Length).Trim());
            }
            else if (e.Effect != null && e.Effect.StartsWith(Cutscenes.VanishPrefix))
            {
                Vector2Int at;
                if (Cutscenes.TryParseCell(e.Effect, Cutscenes.VanishPrefix, out at))
                    vanishes.Add(at);
            }
            else if (e.Effect == Cutscenes.PrisonOpensStep)
            {
                // 原典 0x070c8〜0x07153。盗賊が去って牢の壁が開き、
                // **盗賊は左・左・下と歩いて (-5,-2) に立ったまま残る**。
                var f2 = FloorAt(Prologue.PrisonFloor);
                if (f2 != null)
                {
                    foreach (var c in Prologue.PrisonOpens) f2.Clear(c);
                    f2.Put(Prologue.ThiefGoal, Prologue.ThiefPrefab);
                    walks.Add(new NpcWalk
                    {
                        Prefab = Prologue.ThiefPrefab,
                        From = Prologue.ThiefStart,
                        Path = new List<Vector2Int>(Prologue.ThiefWalk)
                    });
                }
            }
            if (ViewWork != before) break;
        }
        // 場面や戦いが終わったところで、ためておいた解錠を見る。
        // **ひと呼吸は待たない** — 待っている間に次の一歩が通ってしまい、
        // まだ開いていない扉やまだ消えていない相手に突っ込めてしまう
        // （15階のタコを2回倒さないと通れない、という報告がこれ）。
        // 見た目をひと呼吸あとにするのは扉の動きの側で行う。
        if (pendingEvaluate && pages.Count == 0 && !ViewMoving)
        {
            pendingEvaluate = false;
            Evaluate();
        }
    }

    /// <summary>
    /// 斬り合いの絵が終わったあとに当てる後始末の印。
    /// 「勝ったら相手のマスへ進んでそこを床にする」を、絵が終わるまで待たせる。
    /// </summary>
    public const string AfterBattleStep = "戦闘の後始末";

    /// <summary>そのマスを空ける段（妖精は読み終えてから消える）。</summary>
    public const string ClearCellStep = "マスを空ける";

    /// <summary>倒れる様子を見せる段（主人公が点滅して消え、暗くなる）。</summary>
    public const string DyingStep = "倒れる";

    bool deathQueued;

    /// <summary>倒れる様子を見せる番が来たか。取り出すと下ろす。</summary>
    public bool ConsumeDeath()
    {
        bool v = deathQueued;
        deathQueued = false;
        return v;
    }

    /// <summary>まだ知らせていない、倒した相手からの金。</summary>
    int pendingGoldNote;

    /// <summary>場面が終わってから解錠を見直すか。</summary>
    bool pendingEvaluate;

    /// <summary>
    /// 見た目へ渡した「数え上げられない仕事」の通し番号。
    /// 斬りかかりや場面の中の戦いは並びに積むのではなく最新の1件を置くので、
    /// これを増やして「渡した」ことを伝える。
    /// </summary>
    int viewTicket;

    /// <summary>
    /// 見た目の層に渡した仕事の数。増えていたら、その手は絵が動くまで続きがある。
    /// </summary>
    int ViewWork
    {
        get
        {
            return walks.Count + doorOpens.Count + blinks.Count
                   + bossAppears.Count + vanishes.Count + viewTicket
                   + (deathQueued ? 1 : 0)
                   + (PlayerSlide != null ? 1 : 0);
        }
    }

    /// <summary>
    /// 場面を頭から流す。並びは Resources/Cutscenes.txt（原典の関数から書き起こしたもの）。
    /// 「題名」の行は題名画面になる。
    /// </summary>
    public bool PlayScene(string name)
    {
        var steps = Cutscenes.Instance.Steps(name);
        if (steps == null) return false;
        // 地図の書き換えは**次に出すページの直前**に当てる。原典の場面は
        // 「ゼノを出す → セリフ110 → 魔導師を出す → セリフ111 → …
        //   → 5マスを床に戻す → 奪われる」の順に書いているので、
        // ここでまとめて当ててしまうと出す順も消す順も崩れる。
        List<Cutscenes.MapStep> pendingMap = null;
        System.Func<Page, Page> withMap = page =>
        {
            page.MapBefore = pendingMap; pendingMap = null; return page;
        };
        foreach (var step in steps)
        {
            if (step == Cutscenes.ZenoTakesStep || step == Cutscenes.PrisonOpensStep
                || step == Cutscenes.ClimbStep || step.StartsWith(Cutscenes.FlashStep)
                || step == Cutscenes.BossAppearStep
                || step.StartsWith(Cutscenes.BattlePrefix)
                || step == Cutscenes.PushUpStep
                || step.StartsWith(Cutscenes.WalkPrefix)
                || step.StartsWith(Cutscenes.DoorOpenPrefix)
                || step.StartsWith(Cutscenes.DoorClosePrefix)
                || step.StartsWith(Cutscenes.VanishPrefix)
                || step.StartsWith(Cutscenes.SoundPrefix))
            {
                pages.Enqueue(withMap(new Page
                {
                    Kind = PageKind.Effect, Text = null, Record = -1, Effect = step
                }));
                continue;
            }
            if (step == Cutscenes.FinalScreenStep)
            {
                pages.Enqueue(withMap(new Page
                {
                    Kind = PageKind.FinalScreen,
                    Text = Ending.TitleLine + "\n" + Ending.AnyKeyLine,
                    Record = -1
                }));
                continue;
            }
            if (step == Cutscenes.EndingTowerStep)
            {
                // 原典 0xcd20 は塔が流れ出す前に「遠ざかる」音を鳴らし、
                // 0xbf5b で題名の曲へ切り替える（題名と終幕は同じ曲）。
                PlaySound(SoundCues.Receding);
                pages.Enqueue(withMap(new Page
                {
                    Kind = PageKind.EndingTower, Text = "", Record = 1,
                    Cue = SoundCues.TitleMusic
                }));
                continue;
            }
            if (step == Cutscenes.EndingTextStep)
            {
                // 終わりの言葉は段落が1つずつ現れる
                for (int shown = 1; shown <= Cutscenes.EndingTextParagraphs; shown++)
                    pages.Enqueue(withMap(new Page
                    {
                        Kind = PageKind.EndingText, Text = "", Record = shown
                    }));
                continue;
            }
            if (step == Cutscenes.PrologueStep)
            {
                // 序章の1枚絵。行が1つずつ現れるので、行数ぶんページを積む。
                for (int shown = 1; shown <= Cutscenes.PrologueLines; shown++)
                    pages.Enqueue(withMap(new Page
                    {
                        Kind = PageKind.Prologue,
                        Text = "",
                        Record = shown           // 何行目まで出すか
                    }));
                continue;
            }
            if (step == Cutscenes.TitleStep)
            {
                // 原典 0xbf5b は題名（と終幕）でこの曲に切り替える。
                // **題名を出すときに**鳴らす（積んだ時点だと、まだ盤面に居るうちに
                // 曲が変わり、その読み込みで一瞬止まる）。
                pages.Enqueue(withMap(new Page
                {
                    Cue = SoundCues.TitleMusic,
                    Kind = PageKind.Title,
                    Text = TitleScreen.Title + "\n" + TitleScreen.Copyright,
                    Record = -1
                }));
                continue;
            }
            Cutscenes.MapStep mapStep;
            if (Cutscenes.TryParseMap(step, out mapStep))
            {
                // 場面の中の地図の書き換え。当てるのは**次のページの直前**。
                if (pendingMap == null) pendingMap = new List<Cutscenes.MapStep>();
                pendingMap.Add(mapStep);
                continue;
            }
            int id;
            if (!int.TryParse(step, out id)) continue;
            var line = GameText.Instance.Dialogue(id);
            if (!line.HasValue) continue;
            log.Add(line.Value.Body);
            foreach (var t in DialogueBox.Paginate(line.Value.Body))
                pages.Enqueue(withMap(new Page
                    { Kind = PageKind.Dialogue, Text = t, Record = id }));
        }
        // 最後に残った書き換えは**終わりの1手**として積む。その場で当てると
        // 場面が始まる前に効いてしまう（42階で魔導士が消えなかったのがこれ）。
        if (pendingMap != null)
            pages.Enqueue(withMap(new Page
            {
                Kind = PageKind.Effect, Text = null, Record = -1, Effect = ""
            }));
        ResolveEffects();
        return true;
    }

    /// <summary>会話を出す。原典の改行位置のまま、必要ならページに分ける。</summary>
    public void Speak(string text) { Say(text); }

    /// <summary>
    /// 鳴らした音の番号。原典は `lcall 0x10da, 0x20b` に番号を渡すだけなので、
    /// ここも**番号を積むだけ**にして、実際に鳴らすのは外側（見た目の層）に任せる。
    /// こうすると音の出どころを単体テストで確かめられる。
    /// </summary>
    public IReadOnlyList<int> Sounds { get { return sounds; } }

    /// <summary>
    /// 直前の戦闘の演出のひとこま。見た目の層がこれを順に再生する。
    /// 中身は <see cref="BattlePresentation.Steps"/>（原典 0x6082 の流れ）そのもの。
    /// </summary>
    public List<BattlePresentation.Step> LastBattle { get; private set; }

    /// <summary>
    /// 直前の戦闘の相手（プレハブ名）。原典は戦闘に入るとき右下の欄に
    /// 相手の絵と能力値を出す（0x61e7 → 0x9626）ので、見た目の層がこれを見る。
    /// </summary>
    public string LastBattleEnemy { get; private set; }

    /// <summary>
    /// 直前の戦闘の数字ひとそろい。**作者の旧版の見せ方**（黒い帯）が
    /// 1ターンごとのHPを出すのに使う。<see cref="AuthorStyle"/> を参照。
    /// </summary>
    public BattleBand.Numbers LastBattleNumbers { get; private set; }

    /// <summary>
    /// 出すべき閃光の場所。原典は戦闘・魔法攻撃・場面の節目の**7か所すべてで
    /// 同じ絵**（GROUND.P58 の11枚目、buffer 変位 0x87a）を貼る。
    /// 音と同じで、ここは**場所を積むだけ**にして描くのは見た目の層に任せる。
    /// </summary>
    readonly List<Vector2Int> effects = new List<Vector2Int>();

    /// <summary>
    /// 駆け上がりで数字が出る順。見た目の層がこれを流す。無ければ null。
    /// 原典は地図を変えず**階の数字だけ**を 24→50 と上げていく。
    /// </summary>
    public List<int> LastClimb { get; private set; }

    /// <summary>開いた扉ひとつぶん。</summary>
    public struct DoorOpen
    {
        /// <summary>扉のあったマス。</summary>
        public Vector2Int At;
        /// <summary>扉の記号（"y" / "b" / "r"）。<see cref="DoorAnimation.ColorOf"/> で絵を選ぶ。</summary>
        public string Cell;
        /// <summary>
        /// 閉まる向きか。原典は開く 0x07a68 と閉まる 0x07b26 で**同じ9コマを逆から**貼る
        /// （0x5048 から 0xa00 ずつ引いていく）。
        /// </summary>
        public bool Closing;
        /// <summary>
        /// 出す前にひと呼吸おくか（作者の指定）。戦いのあとに開く扉は、
        /// 戦闘の箱が消えてから少し間をおいてから開く。
        /// </summary>
        public bool Delayed;
    }

    /// <summary>
    /// 開いた扉。原典（0x7be6）はそのマスに9コマの動きを貼る。
    /// 音や閃光と同じで、ここは積むだけにして描くのは見た目の層に任せる。
    /// </summary>
    readonly List<DoorOpen> doorOpens = new List<DoorOpen>();

    /// <summary>
    /// ボスが現れる場所。原典（0x3044）は3x3の中心に10コマの動きを貼る。
    /// 音や閃光と同じで、ここは積むだけにして描くのは見た目の層に任せる。
    /// </summary>
    readonly List<Vector2Int> bossAppears = new List<Vector2Int>();

    /// <summary>積んだボス登場を取り出して空にする。</summary>
    public List<Vector2Int> ConsumeBossAppears()
    {
        var copy = new List<Vector2Int>(bossAppears);
        bossAppears.Clear();
        return copy;
    }

    /// <summary>人が1マスずつ歩いていく動き。</summary>
    public class NpcWalk
    {
        /// <summary>歩く人のプレハブ名。</summary>
        public string Prefab;
        /// <summary>歩き始めのマス。</summary>
        public Vector2Int From;
        /// <summary>通っていくマス（最後が立ち止まる所）。</summary>
        public List<Vector2Int> Path;
        /// <summary>
        /// 着いたあとも立たせたままにするか。盤面に載らない人（10階の盗賊）は
        /// 原典も地図に書かずに絵だけ出しているので、"消す" が来るまで残す。
        /// </summary>
        public bool Stay;
        /// <summary>半分の速さで歩くか（32階の金騎士。作者の指定）。</summary>
        public bool Slow;
    }

    /// <summary>
    /// これから歩かせる人。原典は場面の中で**主人公と同じ1マス滑らせる処理（0xa276）**を
    /// 向きの表どおりに呼ぶ（序章の盗賊 0x070dc）。
    /// 音や点滅と同じで、ここは積むだけにして動かすのは見た目の層に任せる。
    /// </summary>
    readonly List<NpcWalk> walks = new List<NpcWalk>();

    /// <summary>
    /// 見た目の層が何かを動かしている最中か。控えの層はこれが下りるまで
    /// 次の手へ進まない（原典の場面は1本道で、絵が動き終わるまで止まっている）。
    /// </summary>
    public bool ViewBusy { get; set; }

    /// <summary>
    /// 見た目の層を毎コマ回す。動きが終わったら続きの手へ進む。
    /// </summary>
    public void Pump(bool viewBusy) { Pump(viewBusy, viewBusy); }

    /// <summary>
    /// 見た目の層を毎コマ回す。動きが終わったら続きの手へ進む。
    ///
    /// <paramref name="viewBusy"/> は「動き＋そのあとのひと呼吸」、
    /// <paramref name="viewMoving"/> は「動いている最中」だけ。
    /// **戦いの後始末だけは、ひと呼吸を待たずに当てる**（作者の指定。
    /// 普通の戦闘のあとに動けるようになるまでが遅くなってしまうため）。
    /// 倒した結果から起きること（扉が開く・褒美が出る）は、ひと呼吸のあと。
    /// </summary>
    public void Pump(bool viewBusy, bool viewMoving)
    {
        ViewBusy = viewBusy;
        ViewMoving = viewMoving;
        if (!viewBusy || !viewMoving) ResolveEffects();
    }

    /// <summary>見た目が動いている最中か（ひと呼吸は含まない）。</summary>
    public bool ViewMoving { get; set; }

    /// <summary>
    /// 場面が主人公を動かす1歩（原典 0x3ebc の押し上げ）。無ければ null。
    /// </summary>
    public NpcWalk PlayerSlide { get; private set; }

    /// <summary>積んだ主人公の1歩を取り出して空にする。</summary>
    public NpcWalk ConsumePlayerSlide()
    {
        var copy = PlayerSlide;
        PlayerSlide = null;
        return copy;
    }

    /// <summary>これから消す立ち絵のマス。</summary>
    readonly List<Vector2Int> vanishes = new List<Vector2Int>();

    /// <summary>積んだ「消す」を取り出して空にする。</summary>
    public List<Vector2Int> ConsumeVanishes()
    {
        var copy = new List<Vector2Int>(vanishes);
        vanishes.Clear();
        return copy;
    }

    /// <summary>積んだ歩きを取り出して空にする。</summary>
    public List<NpcWalk> ConsumeWalks()
    {
        var copy = new List<NpcWalk>(walks);
        walks.Clear();
        return copy;
    }

    /// <summary>
    /// そのマスに**これから出す扉の動き**が積んであるか。
    /// 見た目の側は、これを見て地図の描き直しを待つ（描き直すと扉が一瞬消える）。
    /// </summary>
    public bool DoorPendingAt(Vector2Int at)
    {
        // 待たせるのは**間をおいて開く門**と**閉まる門**（鍵の扉は待たせない）。
        foreach (var d in doorOpens) if ((d.Delayed || d.Closing) && d.At == at) return true;
        return false;
    }

    /// <summary>積んだ扉を取り出して空にする。</summary>
    public List<DoorOpen> ConsumeDoorOpens()
    {
        var copy = new List<DoorOpen>(doorOpens);
        doorOpens.Clear();
        return copy;
    }

    /// <summary>閃光を出す場所を積む。</summary>
    public void PlayEffect(Vector2Int at) { effects.Add(at); }

    /// <summary>積んだ閃光を取り出して空にする。</summary>
    public List<Vector2Int> ConsumeEffects()
    {
        var copy = new List<Vector2Int>(effects);
        effects.Clear();
        return copy;
    }

    /// <summary>鳴らした音を取り出して空にする。見た目の層が毎手これを引く。</summary>
    public List<int> ConsumeSounds()
    {
        var copy = new List<int>(sounds);
        sounds.Clear();
        return copy;
    }

    /// <summary>合図の名前で音を鳴らす。番号は Resources/SoundCues.txt が持つ。</summary>
    public void PlaySound(string cue)
    {
        int n = SoundCues.Instance.Number(cue);
        if (n > 0) sounds.Add(n);
    }

    void Say(string s)
    {
        if (string.IsNullOrEmpty(s)) return;
        log.Add(s);
        foreach (var t in DialogueBox.Paginate(s))
            pages.Enqueue(new Page { Kind = PageKind.Dialogue, Text = t, Record = -1 });
    }

    /// <summary>
    /// 右下の欄にだけ出す。窓は開かない（原典は 0x9d80 を直に呼ぶ形）。
    /// </summary>
    void Note(string s)
    {
        if (!string.IsNullOrEmpty(s)) log.Add(s);
    }

    /// <summary>
    /// 高速モードの切り替え。原典は主処理の中で鍵 0x70（PC98のシフト）を見て
    /// 旗31 を反転し、右下の欄に切り替えの知らせを出す（0xa922〜0xa95f）。
    /// 窓は開かず、旗はセーブにも入る。
    /// </summary>
    public void ToggleFastMode()
    {
        Player.FastMode = !Player.FastMode;
        Note(GameText.Instance.Message(Player.FastMode ? "mode_fast" : "mode_normal"));
    }

    /// <summary>メニューが開いているか。</summary>
    public bool IsMenuOpen { get; private set; }

    /// <summary>今どの項目を選んでいるか。</summary>
    public int MenuIndex { get; private set; }

    /// <summary>今出ている項目の一覧。</summary>
    public List<TowerMenu.Entry> MenuEntries { get { return TowerMenu.Entries(Player); } }

    /// <summary>メニューを開く。会話が出ている間は開けない（原典も同じ）。</summary>
    public bool OpenMenu()
    {
        if (pages.Count > 0) return false;
        IsMenuOpen = true;
        MenuIndex = 0;
        Stage = MenuStage.Root;
        SlotIndex = 0;
        return true;
    }

    /// <summary>メニューを閉じる。</summary>
    public void CloseMenu() { IsMenuOpen = false; Stage = MenuStage.Root; }

    /// <summary>選ぶ場所を動かす。端では止まる。</summary>
    public void MenuMove(int delta)
    {
        if (!IsMenuOpen) return;
        int n = MenuEntries.Count;
        if (n == 0) return;
        MenuIndex = Mathf.Clamp(MenuIndex + delta, 0, n - 1);
    }

    /// <summary>今選んでいる項目。</summary>
    public TowerMenu.Entry MenuCurrent
    {
        get
        {
            var e = MenuEntries;
            return e[Mathf.Clamp(MenuIndex, 0, e.Count - 1)];
        }
    }

    /// <summary>メニューのどの段にいるか。</summary>
    public enum MenuStage { Root, LoadSlots, SaveSlots, FlightFloors, Items }

    /// <summary>今の段。</summary>
    public MenuStage Stage { get; private set; }

    /// <summary>スロットの数。原典も8つ（選ぶ処理に 8 を渡している）。</summary>
    public const int SlotCount = TowerSave.SlotCount;

    /// <summary>
    /// 今選んでいるスロット（0〜7）。8 は「やっぱりやめる」。
    /// 原典も選ぶ処理に 8 を渡し、返りが 8 なら取りやめとして扱う（0x0d99 / 0x0e03）。
    /// </summary>
    public int SlotIndex { get; private set; }

    /// <summary>「やっぱりやめる」の位置。</summary>
    public const int SlotCancelIndex = SlotCount;

    /// <summary>
    /// スロットを選ぶ画面に出すセリフ。原典はロードが50、セーブが51で、
    /// どちらも10行の窓・上端50（MTE.EXE 0x0d64 / 0x0dcb）。
    /// </summary>
    public const int LoadRecord = 50, SaveRecord = 51;
    public const int SlotWindowLines = 10, SlotWindowTop = 50;

    /// <summary>今の段で出す文章。</summary>
    public string MenuStageText
    {
        get
        {
            if (Stage == MenuStage.Items) return TowerMenu.ItemText();
            if (Stage == MenuStage.LoadSlots || Stage == MenuStage.SaveSlots)
            {
                var rec = Stage == MenuStage.LoadSlots ? LoadRecord : SaveRecord;
                var line = GameText.Instance.Dialogue(rec);
                return line.HasValue ? line.Value.Body : "";
            }
            return MenuText;                      // 本体と、階選び（階の案内は別に出す）
        }
    }

    /// <summary>選んだ項目を決める。まだ中身の無いものは何も起きない。</summary>
    public TowerMenu.Entry MenuConfirm()
    {
        if (Stage == MenuStage.FlightFloors) { ConfirmFlight(); return TowerMenu.Entry.FlightOrb; }
        if (Stage == MenuStage.Items) { ConfirmItem(); return TowerMenu.Entry.SpecialItems; }
        if (Stage != MenuStage.Root) { ConfirmSlot(); return TowerMenu.Entry.Resume; }

        var e = MenuCurrent;
        switch (e)
        {
            case TowerMenu.Entry.Resume: CloseMenu(); break;
            case TowerMenu.Entry.FlightOrb:
            {
                // 原典（0x8fda）は上下左右のどれかが階段(11/12)でなければ断る。
                if (!TowerMenu.NextToStairs(Floor, Player.Position))
                {
                    Say(TowerMenu.FlightBlockedMessage());
                    break;
                }
                Stage = MenuStage.FlightFloors;
                FlightFloor = Player.Floor;
                Say(TowerMenu.FlightPrompt());
                break;
            }
            case TowerMenu.Entry.WisdomOrb:
            {
                // 原典（0x8a46）は持ち物の能力と、聞いた情報の番号を並べる。
                var text = WisdomOrb.Text(Player);
                if (!string.IsNullOrEmpty(text)) Say(text);
                break;
            }
            case TowerMenu.Entry.HeroOrb:
                ShowHeroOrb();
                break;
            case TowerMenu.Entry.SpecialItems:
                // ⚠ ここが繋がっていなくて、**スペシャル・アイテムが一度も使えなかった**。
                Stage = MenuStage.Items; ItemIndex = 0;
                break;
            case TowerMenu.Entry.Load: Stage = MenuStage.LoadSlots; SlotIndex = 0; break;
            case TowerMenu.Entry.Save: Stage = MenuStage.SaveSlots; SlotIndex = 0; break;
            case TowerMenu.Entry.Quit:
                // 原典はここで DOS へ戻る。こちらは**入力の側**が受け取って
                // 遊びを閉じる（本線は「終わりたい」と言うだけにして、
                // 画面や実行の後始末は見た目の層に任せる）。
                Quitting = true;
                CloseMenu();
                break;
        }
        return e;
    }

    /// <summary>
    /// その階の相手の能力と予想ダメージの一覧を出す（原典 0x8c2a の勇者のオーブ）。
    /// メニューからと、下の「ダメージ表示」のボタンからの両方で呼ぶ（作者の指定）。
    /// 見せ方だけ <see cref="HeroOrbPanel"/> で一覧にしてあり、中身は原典のまま。
    /// </summary>
    /// <returns>出したか。</returns>
    public bool ShowHeroOrb()
    {
        var text = HeroOrb.Text(Floor, Player);
        if (string.IsNullOrEmpty(text)) return false;
        log.Add(text);
        pages.Enqueue(new Page { Kind = PageKind.HeroOrb, Text = text, Record = -1 });
        return true;
    }

    /// <summary>
    /// 持ち物の帯のひとつを触ったとき（作者の指定。原典にはこの入口は無い）。
    ///
    ///   ・**勇者のオーブ**は触ると一覧を出す（「使う？」とは聞かない）
    ///   ・知恵のオーブは持ち物と聞いた情報を出す
    ///   ・飛翔のオーブはメニューと同じく階を選ばせる
    ///   ・使い切りの道具は「使いますか？」と聞いてから使う
    ///   ・十字架・ドラゴンスレイヤー・幸運の金貨は**持っているだけで効く**ので何もしない
    /// </summary>
    public bool TapItem(SpecialItems.Kind kind)
    {
        if (kind == SpecialItems.Kind.None || !Player.Has(kind)) return false;
        if (pages.Count > 0 || IsMenuOpen) return false;

        if (kind == SpecialItems.Kind.HeroOrb) return ShowHeroOrb();
        if (kind == SpecialItems.Kind.WisdomOrb)
        {
            var text = WisdomOrb.Text(Player);
            if (string.IsNullOrEmpty(text)) return false;
            Say(text);
            return true;
        }
        if (kind == SpecialItems.Kind.FlightOrb)
        {
            // メニューの「飛翔のオーブを使う」と同じ道を通す
            if (!OpenMenu()) return false;
            var entries = MenuEntries;
            int at = entries.IndexOf(TowerMenu.Entry.FlightOrb);
            if (at < 0) { CloseMenu(); return false; }
            MenuMove(at - MenuIndex);
            MenuConfirm();
            return true;
        }
        if (!SpecialItemEffects.IsConsumedOnUse(kind)) return false;   // 持つだけで効くもの

        Say(string.Format(PhoneScreen.UseAsk, TowerMenu.ItemName(kind)));
        BeginChoice(ChoiceKind.UseItem, 2, default(Shop.Offer));
        choiceItem = kind;
        return true;
    }

    /// <summary>
    /// 「ゲーム終了」が選ばれたか。原典（メニューの最後の項目）は DOS へ戻る。
    /// 本線はここで印を立てるだけで、実際に閉じるのは入力の側。
    /// </summary>
    public bool Quitting { get; private set; }

    /// <summary>アイテムの一覧で今選んでいるもの。</summary>
    public int ItemIndex { get; private set; }

    /// <summary>今の段で印を置く行（0から）。見出しは数えない。</summary>
    public int MenuStageIndex
    {
        get
        {
            if (Stage == MenuStage.Items) return ItemIndex;
            if (Stage == MenuStage.LoadSlots || Stage == MenuStage.SaveSlots) return SlotIndex;
            return MenuIndex;
        }
    }

    /// <summary>一覧の中で選び直す。</summary>
    public void ItemMove(int delta)
    {
        if (Stage != MenuStage.Items) return;
        ItemIndex = Mathf.Clamp(ItemIndex + delta, 0, TowerMenu.ItemCount - 1);
    }

    /// <summary>
    /// 選んだものを使う。持っていないもの・その場では効かないものを選んでも何も起きない
    /// （原典も一覧は持ち物に関わらず8つとも並べる）。
    /// </summary>
    public bool ConfirmItem()
    {
        if (Stage != MenuStage.Items) return false;
        Stage = MenuStage.Root;
        if (ItemIndex < 0 || ItemIndex >= TowerMenu.Usable.Length) return false;  // やめる
        bool ok = UseItem(TowerMenu.Usable[ItemIndex]);
        if (ok) CloseMenu();
        return ok;
    }

    /// <summary>飛翔のオーブで今選んでいる階。</summary>
    public int FlightFloor { get; private set; }

    /// <summary>
    /// 飛翔のオーブの階選び。原典（0x91b4〜0x921d）は
    /// **1階から到達最高階まで**を上下し、**44階（幻の階）は飛ばす**。
    /// </summary>
    public void FlightMove(int delta)
    {
        if (Stage != MenuStage.FlightFloors || delta == 0) return;
        int step = delta > 0 ? 1 : -1;
        int f = FlightFloor + step;
        if (f == FloorTransition.PhantomFloor) f += step;     // 44階は飛ばす
        if (f < 1 || f > Player.HighestFloor) return;         // 端では止まる
        FlightFloor = f;
    }

    /// <summary>
    /// 選んだ階へ飛ぶ。着地は階段の隣（原典は地図の後ろに控えてある着地位置を読む）。
    /// 上の階へ行くなら下から上がってきたときの位置、下の階なら上から降りてきたときの位置。
    /// </summary>
    public bool ConfirmFlight()
    {
        if (Stage != MenuStage.FlightFloors) return false;
        int to = FlightFloor;
        Stage = MenuStage.Root;
        if (to == Player.Floor) { CloseMenu(); return false; }

        var info = FloorConnections.Instance.Get(to);
        var at = to > Player.Floor ? info.ArriveFromBelow : info.ArriveFromAbove;
        if (at == null) at = info.ArriveFromBelow ?? info.ArriveFromAbove;
        if (at == null) return false;

        EnterFloor(to, at.Value);
        CloseMenu();
        return true;
    }

    /// <summary>スロットを選ぶ段で上下に動かす。</summary>
    public void SlotMove(int delta)
    {
        if (Stage == MenuStage.Root) return;
        SlotIndex = Mathf.Clamp(SlotIndex + delta, 0, SlotCancelIndex);
    }

    /// <summary>スロットを決める。読み込めたら true。</summary>
    public bool ConfirmSlot()
    {
        if (SlotIndex == SlotCancelIndex) { Stage = MenuStage.Root; return false; }
        int slot = SlotIndex + 1;               // 原典もスロットは1から
        if (Stage == MenuStage.SaveSlots)
        {
            // 原典（0x9b4e）は地図もまるごと書き出すので、こちらも組み立て済みの階を渡す。
            TowerSave.Save(slot, Player, floors);
            Stage = MenuStage.Root;
            return true;
        }
        if (Stage == MenuStage.LoadSlots)
        {
            var snap = TowerSave.Read(slot);
            Stage = MenuStage.Root;
            if (snap == null || snap.Player == null) return false;
            Player = snap.Player;
            // 倒した敵・開けた扉・拾った物を戻す。**組み立て直してから**差を当てる
            // （そうしないと今の盤面が残ってしまう）。
            floors.Clear();
            foreach (var kv in snap.Floors)
            {
                var st = FloorAt(kv.Key);
                if (st != null) st.Apply(kv.Value);
            }
            EnterFloor(Player.Floor, Player.Position);
            CloseMenu();
            return true;
        }
        return false;
    }

    /// <summary>スロットを選ぶのをやめて1つ前の段へ戻る。</summary>
    public void MenuBack()
    {
        if (Stage != MenuStage.Root) Stage = MenuStage.Root;
        else CloseMenu();
    }

    /// <summary>メニューに出す文章。原典と同じ連結の仕方で作る。</summary>
    public string MenuText { get { return TowerMenu.Text(Player); } }

    /// <summary>
    /// 会話や演出が出ている間は動けない。原典も1つ出すごとにキー入力を待っていて、
    /// その間は移動の処理に戻らない（`lcall 0x107e, 0x4fa`）。
    /// </summary>
    public bool IsShowingPage { get { return pages.Count > 0; } }

    /// <summary>
    /// 出ているものがあれば送るだけ、無ければ一手進める。入力はこれを通す。
    /// </summary>
    public TowerPlayer.Outcome TryStep(Vector2Int dir)
    {
        if (IsMenuOpen) return TowerPlayer.Outcome.Blocked;
        if (IsShowingPage)
        {
            NextPage();
            return TowerPlayer.Outcome.Blocked;
        }
        return Step(dir);
    }

    /// <summary>一手進める。</summary>    /// <summary>一手進める。</summary>
    public TowerPlayer.Outcome Step(Vector2Int dir)
    {
        if (Floor == null) return TowerPlayer.Outcome.Blocked;

        var before = Player.Position;
        var r = TowerPlayer.Step(Floor, Player, dir);

        // 隠し壁と見た目が同じマスを壊すと、そこに隠れていた者が現れる（原典 0x02c6）。
        // プレーヤーは手前に立ったままなので、そこから話しかける形になる。
        if (r.Outcome == TowerPlayer.Outcome.Blocked && r.What == "Marker_ID10")
            Reveal(r.Target);
        CheckMagicSeal();
        // 場面が流れ出したか。原典の場面（3階の 0x6ae0 など）は地図に敵を書き込んだあと
        // **そのまま画面を乗っ取る**ので、書き込まれた魔導師から魔法攻撃を受けることはない。
        // ここで覚えておいて、この手の魔法攻撃の判定は飛ばす。
        bool sceneStarted = CheckPositionEvent() && !string.IsNullOrEmpty(PendingScene);

        switch (r.Outcome)
        {
            case TowerPlayer.Outcome.OpenedDoor:
                // 原典 0x023ee は地図を床にしてから 0x7be6（扉が開く9コマ）を呼ぶ。
                // **音はその9コマの頭で鳴る**ので、ここでは場所と色を積むだけにする
                // （ここでも鳴らしていて二重に聞こえていた）。
                doorOpens.Add(new DoorOpen { At = r.Target, Cell = r.What });
                break;

            case TowerPlayer.Outcome.Talk:
            {
                Talk(r.What, r.Target);
                break;
            }
            case TowerPlayer.Outcome.TookItem:
            {
                var got = ItemPickup.Apply(Player, r.What, power);
                if (got.Taken)
                {
                    PlaySound(SoundCues.TakeItem);
                    // **消す → そのマスへ進む → 手に入れる** の順（作者の指定）。
                    // 拾ったあとその場に立つので、押しっぱなしで歩き続けられる。
                    Floor.Clear(r.Target);
                    if (AuthorStyle.StepOntoItem) Player.Position = r.Target;
                    // 原典（0x23ee → 0x9d80）は**窓を開かず右下の欄に書くだけ**。
                    // こちらは画面の中央に出して薄くなって消える（MessageToastView）。
                    Note(GameText.Instance.Message(got.MessageKey, got.Value));
                }
                break;
            }
            case TowerPlayer.Outcome.Encounter:
            {
                if (RunBattle(r.What, r.Target))
                {
                    // 敵だけは**進む**。移動処理（0x00a6fb）は座標を変えないが、
                    // 戦闘処理そのもの（0x61ef〜0x6232）が踏み込みの一歩として
                    // プレーヤーの X か Y を敵の方へ1つ動かし、勝つと相手のマスを
                    // 床(6)にする（0x631f）ので、戦いが終わると相手のマスに立っている。
                    //
                    // **斬り合いの絵が終わってから**当てる。原典 0x6082 は斬り合いが
                    // 片付くまでその場で回り続けるので、倒した結果（マスが床になる →
                    // 解錠で扉が開く・妖精が出る）は戦いのあとに見える。
                    // ここで即座に当てていたため「戦闘開始直後に褒美が出る」と報告された。
                    pages.Enqueue(new Page
                    {
                        Kind = PageKind.Effect, Text = null, Record = -1,
                        Effect = AfterBattleStep + "(" + r.Target.x + "," + r.Target.y + ")"
                    });
                }
                break;
            }
            case TowerPlayer.Outcome.Stairs:
            {
                PlaySound(SoundCues.Stairs);
                bool down = r.What == "<";
                var t = down ? FloorTransition.GoDown(Player.Floor)
                             : FloorTransition.GoUp(Player.Floor);
                if (t.Ok)
                {
                    EnterFloor(t.Floor, t.Arrive);
                    ApplyArrival(t.Floor, down);
                    // **着いたマスでも立ち入りのできごとを見る**。原典 0x23ee は
                    // 階段で X,Y を書き換えたあと、そのまま `call 0x1588` している
                    // （0x29e9〜0x2a00）。ここが抜けていたので、31階から上がると
                    // ちょうど発動マス (0,-4) に着地する32階の金騎士が飛ばされていた。
                    CheckPositionEvent();
                }
                return r.Outcome;
            }
        }

        if (Player.Position != before && !sceneStarted) ApplyMagicAttack();
        // 場面の途中では解錠を見ない。原典の場面は1本の関数で最後まで走り切るので、
        // その間に解錠（0x4762）が回ることはない。ここで見てしまうと、
        // 10階なら骸骨が来る前に、20階ならボスが出る前に、条件が満たされてしまう。
        if (pages.Count > 0) pendingEvaluate = true; else Evaluate();
        return r.Outcome;
    }

    /// <summary>
    /// ゲームオーバー。原典（0x097ac）は画面を黒く塗って
    /// 「ＧＡＭＥ　ＯＶＥＲ」を出し、キーを待って INITIAL.DAT を読み直す。
    /// ここでは1枚の画面として積み、送ったら最初からやり直す。
    /// </summary>
    public void GameOver()
    {
        if (gameOverStarted) return;
        gameOverStarted = true;
        Player.Hp = 0;
        // 残っている場面の続き（突撃の残りなど）は捨てる。
        // 倒れたのに次の相手が歩いてきては困る。
        pages.Clear();
        walks.Clear(); doorOpens.Clear(); blinks.Clear(); vanishes.Clear();
        frontMapDone = true;
        // **倒れる様子を見せてから**画面を出す（作者の指定）。
        // 相手はそのまま、主人公だけが点滅して消え、少しずつ暗くなってから
        // ＧＡＭＥ　ＯＶＥＲ。見せるのは見た目の層（BattleView）。
        deathQueued = true;
        pages.Enqueue(new Page
        {
            Kind = PageKind.Effect, Text = null, Record = -1, Effect = DyingStep
        });
        pages.Enqueue(new Page
        {
            Kind = PageKind.GameOver, Text = GameOverScreen.Line, Record = -1
        });
    }

    /// <summary>もう倒れる流れに入っているか（二重に入らないようにする印）。</summary>
    bool gameOverStarted;

    // ---------------------------------------------------------------- 選ばせる
    //
    // 原典 `0x9e78` が「◎はい／◎いいえ」や祭壇の望みを選ばせる仕掛け。
    //   上（0x43 / 0x3a）で印を1つ上へ、リターン(0x1c)かスペース(0x34)で決める。
    // 印は行の高さ16ドットぶんずつ動く。
    //
    // ⚠ **印そのものの絵はまだ同定できていない**ので、選んでいる行の頭に「→」を置く形で
    //   代用している（文言の「◎」は原典の文字列にもともと入っている）。

    /// <summary>何を選ばせているか。</summary>
    public enum ChoiceKind { None, Merchant, Altar, AltarOffer, UseItem }

    public ChoiceKind Choice { get; private set; }
    public int ChoiceIndex { get; private set; }
    public int ChoiceCount { get; private set; }
    Shop.Offer choiceOffer;
    SpecialItems.Kind choiceItem;

    /// <summary>選んでいる最中か。</summary>
    public bool IsChoosing { get { return Choice != ChoiceKind.None; } }

    void BeginChoice(ChoiceKind kind, int count, Shop.Offer offer)
    {
        Choice = kind; ChoiceCount = count; ChoiceIndex = 0; choiceOffer = offer;
    }

    /// <summary>印を動かす。</summary>
    public void MoveChoice(int delta)
    {
        if (!IsChoosing) return;
        ChoiceIndex = Mathf.Clamp(ChoiceIndex + delta, 0, ChoiceCount - 1);
    }

    /// <summary>決める。選んだ結果の一言を積む。</summary>
    public void ConfirmChoice()
    {
        if (!IsChoosing) return;
        var kind = Choice; int at = ChoiceIndex;
        Choice = ChoiceKind.None;

        if (kind == ChoiceKind.Merchant)
        {
            if (at != 0) return;                       // 「いいえ」
            var said = TowerNpc.Buy(Player, choiceOffer);
            if (!string.IsNullOrEmpty(said)) Say(said);
            return;
        }
        if (kind == ChoiceKind.UseItem)
        {
            if (at == 0) UseItem(choiceItem);          // 「はい」
            return;
        }
        if (kind == ChoiceKind.AltarOffer)
        {
            if (at != 0) return;                       // 「いいえ」
            // 金が足りないときは原典と同じ文言で断る
            if (Player.Gold < TowerNpc.AltarCost(Player))
            {
                Say(GameText.Instance.Message("altar_poor"));
                return;
            }
            Speak(TowerNpc.AltarMenu(Player));
            BeginChoice(ChoiceKind.Altar, 4, default(Shop.Offer));
            return;
        }
        // 祭壇。並びは 生命力 / 攻撃力 / 防御力 / やめる
        var wish = at == 0 ? TowerNpc.Wish.Hp
                 : at == 1 ? TowerNpc.Wish.Attack
                 : at == 2 ? TowerNpc.Wish.Defense
                 : TowerNpc.Wish.Cancel;
        var text = TowerNpc.Donate(Player, wish);
        if (!string.IsNullOrEmpty(text)) Say(text);
    }

    /// <summary>ゲームオーバーの画面を出している最中か。</summary>
    public bool IsGameOver
    {
        get { return pages.Count > 0 && pages.Peek().Kind == PageKind.GameOver; }
    }

    /// <summary>隣接マスの魔術士・魔導師から魔法攻撃を受ける。神盾があれば無効。</summary>
    void ApplyMagicAttack()
    {
        var ids = ObjectIds.Instance;
        var n = Floor.NeighborsOf(Player.Position, ids.Of);
        string key;
        int after = MagicAttack.Apply(Player.Hp, Player.ShieldRank, n, out key);
        if (key == null) return;
        Player.Hp = after;
        // 原典 0x001df2 / 0x001fd0 も、ここで尽きたらゲームオーバーへ行く。
        if (Player.Hp <= 0) { GameOver(); return; }
        // 原典（0x1b75〜0x1ba7）は**自分の立っているマス**に閃光を出す。
        PlayEffect(Player.Position);
        PlaySound(SoundCues.Magic);
        Say(GameText.Instance.Message(key));
    }

    /// <summary>解錠条件を判定して、満たしたものを盤面に反映する。</summary>
    void Evaluate()
    {
        Player.ClampStats();       // 原典は描き直すたびに上限で頭を押さえる
        if (Floor == null) return;
        // 門が開いたマスを拾うために、開く前の門の場所を控えておく。
        // 原典（0x4762）は門1マスごとに 0x7a68（9コマの動き）を呼ぶ。
        var gates = new List<Vector2Int>();
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
            {
                var at = new Vector2Int(x, y);
                if (Floor.At(at) == "s") gates.Add(at);
            }
        var fired = Floor.ApplyUnlocks(f => Player.EventFlags.Contains(f));
        foreach (var u in fired)
        {
            PlaySound(u.Floor == 2 ? SoundCues.BigUnlock : SoundCues.Unlock);
            // 49階で壁が書き込まれるとき（原典 0x226b）は別の音が鳴る
            foreach (var r in u.Results)
                if (r.Kind == "封印") { PlaySound(SoundCues.WallAppear); break; }
        }
        if (fired.Count > 0)
            foreach (var at in gates)
                if (Floor.At(at) != "s")
                {
                    // 原典 0x7a68 は門1マスごとに9コマの動きを出し、**その頭で**音11を鳴らす。
                    // 鳴らすのは見た目の側（動きが始まるとき）。ここで鳴らすと
                    // 「音が鳴り終わってから扉が開く」「何枚ぶんも重なって聞こえる」
                    // ことになる（作者の報告）。
                    // 戦いのあとに開くものなので、**ひと呼吸おいてから**出す（作者の指定）。
                    doorOpens.Add(new DoorOpen { At = at, Cell = "s", Delayed = true });
                }
    }

    /// <summary>
    /// 特殊アイテムを使う。原典のメニューの並びは
    /// エリクサー / マトック / 破壊の玉 / 空間転移の秘宝 / 昇華の翼 / 降臨の翼 /
    /// 魔法の鍵 / スーパーマトック / やっぱりやめる。
    /// </summary>
    /// <returns>使えたか。</returns>
    /// <summary>
    /// 26階の中央にある人形に触れる。原典（MTE.EXE 0x2a44、物体ID27）は
    /// セリフ160 を出したあと **24階の中央(0,0) に門を置く**。これが「時空の門」。
    /// </summary>
    public string TouchDoll()
    {
        var line = GameText.Instance.Dialogue(DollRecord);
        var text = line.HasValue ? line.Value.Body : "";

        var gate = FloorAt(SpacetimeGate.GateFloor);
        if (gate != null) gate.Put(SpacetimeGate.GatePos, "s");

        Say(text);
        return text;
    }

    /// <summary>人形のセリフ（MT.GTX のレコード番号）。原典 0x2a61 の即値 0xa0。</summary>
    public const int DollRecord = 160;

    /// <summary>いま去ろうとしている盗賊のできごと（セリフのあとに効かせる）。</summary>
    ThiefEvents.Event leaving;

    /// <summary>直前に発火した場面の名前。演出はこれを見て流す。</summary>
    public string PendingScene { get; private set; }

    /// <summary>
    /// 今いるマスで起きることを判定する（原典 0x1588、物体ディスパッチャから毎手）。
    /// </summary>
    public bool CheckPositionEvent()
    {
        var hit = PositionEvents.Instance.At(Player.Floor, Player.Position,
                                             Player.EventFlags, Floor.IsCleared,
                                             p => Floor.At(p) == "s",
                                             k => Player.Has(k),
                                             p => Floor.At(p));
        if (!hit.HasValue) return false;
        var ev = hit.Value;

        if (ev.FlagId >= 0) Player.EventFlags.Add(ev.FlagId);
        // 持ち物が要るできごとは、その持ち物を使い切る（原典 13階 0x2022 が所持を 0 に戻す）。
        // 二度目が起きないのは**持ち物がもう無いから**で、一度きりの印は持っていない。
        if (ev.NeedsItem != SpecialItems.Kind.None)
        {
            Player.Consume(ev.NeedsItem);
            Note(GameText.Instance.Message(SpecialItemEffects.UseMessageKey(ev.NeedsItem)));
        }
        // ⚠ ここで音を鳴らしてはいけない。原典 0x1588 の中の音6は3か所とも
        //   **その場の仕掛け**（13階の雪の結晶・47階の左の列）のもので、
        //   立ち入って場面が始まるだけでは鳴らない。3階のゼノとの初対面
        //   （0x68b:0x230 ＝ 0x6ae0）も、鳴るのは閃光の音7だけ。
        //   「ゼノの出るマスに入ると扉が開く音がする」という報告がこれだった。
        foreach (var c in ev.Changes)
        {
            var st = FloorAt(c.Floor);
            if (st == null) continue;
            bool wasGate = st.At(c.Pos) == "s";
            if (c.Kind == "床") st.Clear(c.Pos);
            else st.Put(c.Pos, FloorState.SymbolOfKind(c.Kind));
            bool isGate = st.At(c.Pos) == "s";
            // 原典 0x1588 は門を書いたすぐあとに 0x7b26（閉まる9コマ）を呼ぶ。
            // 15階(0,-3) / 25階(0,-4) / 30階(0,+2)(0,-1) / 33階(+4,+2)(+4,-2) の6か所と、
            // 24階(0,0) の 0x7a68（開く）で、**表の書き換えと1対1**になっている。
            if (st == Floor && wasGate != isGate)
            {
                PlaySound(SoundCues.OpenDoor);
                doorOpens.Add(new DoorOpen { At = c.Pos, Cell = "s", Closing = isGate });
            }
            // 持ち物で溶かすものは**1マスずつ**音が鳴る（原典 0x2079 が1周ごとに音6）
            if (ev.NeedsItem != SpecialItems.Kind.None) PlaySound(SoundCues.SnowMelt);
        }
        // 隣のものが**もう1マス先へ逃げる**（原典 47階 0x2102〜0x2150）。
        if (ev.FleeDir != Vector2Int.zero)
        {
            var from = Player.Position + ev.FleeDir;
            var to = from + ev.FleeDir;
            var who = Floor.At(from);
            Floor.Clear(from);
            Floor.Put(to, who);
        }
        // 表に書いてある音を順に鳴らす。
        // ⚠ 原典は43階だけ音6と音11の間に 0x1e（30コマ）待つが、こちらは間を置かない。
        foreach (var cue in ev.Cues) PlaySound(cue);
        if (ev.Record >= 0)
        {
            var line = GameText.Instance.Dialogue(ev.Record);
            if (line.HasValue) Say(line.Value.Body);
        }
        PendingScene = ev.Scene;
        if (!string.IsNullOrEmpty(ev.Scene)) PlayScene(ev.Scene);
        return true;
    }



    /// <summary>
    /// 階段で着いたときのできごと（原典 0x2982）。29階の抜け穴がこれで開く。
    /// </summary>
    public bool ApplyArrival(int floor, bool descending)
    {
        var hit = ArrivalEvents.Instance.At(floor, descending, Player.EventFlags);
        if (!hit.HasValue) return false;
        var e = hit.Value;
        Player.EventFlags.Add(e.FlagId);
        foreach (var c in e.Changes)
        {
            var st = FloorAt(c.Floor);
            if (st == null) continue;
            st.Put(c.Pos, FloorState.SymbolOfKind(c.What));
        }
        return true;
    }

    /// <summary>
    /// 階段のできごとを全部起こす。通しの確認で「先の階まで見る」ときだけ使う。
    /// </summary>
    public void ApplyAllArrivals()
    {
        foreach (var e in ArrivalEvents.Instance.All)
            ApplyArrival(e.Floor, e.Descending);
    }

    /// <summary>
    /// サイコロの５の目ができていれば魔を封じる    /// <summary>
    /// サイコロの５の目ができていれば魔を封じる（原典 0x5cdb）。移動のたびに見る。
    /// </summary>
    public bool CheckMagicSeal()
    {
        if (Player.Floor != MagicSeal.Floor) return false;
        if (Player.EventFlags.Contains(MagicSeal.FlagId)) return false;
        if (Player.Position != MagicSeal.Center) return false;
        if (!MagicSeal.IsFormed(Floor)) return false;

        Player.EventFlags.Add(MagicSeal.FlagId);
        var line = GameText.Instance.Dialogue(MagicSeal.Record);
        if (line.HasValue) Say(line.Value.Body);
        return true;
    }

    /// <summary>今いる場所から話せる商人の品揃え。X は原典と同じ 0〜10 で渡す。</summary>
    public IEnumerable<Shop.Offer> MerchantOffersHere()
    {
        return Shop.Instance.ForFloor(Player.Floor, Player.Position.x + 5);
    }

    /// <summary>
    /// 人や物に触れたときの振り分け。    /// <summary>
    /// 人や物に触れたときの振り分け。原典の物体ディスパッチャ（0x23ee）と同じ割り当て:
    /// ID25=仙人 / ID19=商人 / ID21=盗賊 / ID27=人形 / ID16,17=祭壇。
    /// </summary>
    /// <summary>
    /// 1回戦う。勝って生き残ったら true。
    ///
    /// 原典の戦闘（0x6082）は**踏み込みとは別の処理**で、場面の中から直に呼ばれることもある
    /// （32階の金騎士 0x68b0）。だから「相手のマスへ進む」のは呼ぶ側の仕事にしてある。
    /// </summary>
    bool RunBattle(string what, Vector2Int target)
    {
        var from = Player.Position;
        int hpBefore = Player.Hp;
        var b = TowerBattle.Resolve(Player, what);
        LastBattle = BattlePresentation.Steps(from, target, b.Hits, b.Died);
        viewTicket++;
        LastBattleEnemy = what;
        // 作者の旧版の帯（BattleBand）に出す数字。戦う前の値を残しておく。
        LastBattleNumbers = new BattleBand.Numbers
        {
            Enemy = what,
            PlayerHp = hpBefore,
            PlayerAttack = Player.Attack,
            PlayerDefense = Player.Defense,
            EnemyHp = b.EnemyHp,
            EnemyAttack = b.EnemyAttack,
            EnemyDefense = b.EnemyDefense,
            PlayerDamage = b.PlayerDamage,
            EnemyDamage = b.EnemyDamage,
            Hits = b.Hits,
            Gold = b.GoldGained,
        };
        // 原典は一撃ごとに音9（自分の攻撃）、相手が生きていれば音10（反撃）。
        for (int i = 0; i < b.Hits; i++)
        {
            PlaySound(SoundCues.Slash);
            if (i < b.Hits - 1) PlaySound(SoundCues.Hit);
        }
        if (b.Died)
        {
            // 原典 0x64cd → 0x68b:0x2efc。生命力が尽きたらゲームオーバー。
            GameOver();
            return false;
        }
        // 倒した相手からの金は、鍵や薬と同じやり方で知らせる（作者の指定）。
        // 原典は右下の欄に数字が増えるだけで、文言は持っていない。
        // 出すのは**斬り合いが終わってから**（帯が出ている間は知らせを出さない作り）。
        pendingGoldNote = b.Won ? b.GoldGained : 0;
        if (!b.Won)
        {
            Say(GameText.Instance.Message(b.MessageKey));
            return false;
        }
        return true;
    }

    void Talk(string what, Vector2Int at)
    {
        if (TowerNpc.IsMaster(what))
        {
            PlaySound(SoundCues.TalkMaster);
            Speak(TowerNpc.TalkToMaster(Player)); return;
        }
        if (TowerNpc.IsThief(what))  { TalkToThief(); return; }
        if (TowerNpc.IsDoll(what))   { TouchDoll(); return; }
        if (TowerNpc.IsMerchant(what))
        {
            PlaySound(SoundCues.TalkShop);
            foreach (var o in Shop.Instance.ForFloor(Player.Floor, Player.Position.x + 5))
            {
                var talk = TowerNpc.TalkToMerchant(Player, o);
                Speak(talk.Text);
                // 「◎はい／◎いいえ」を聞かれたら**選ばせる**（原典 0x9e78）。
                // ここが繋がっていなくて、**商人から何も買えなかった**。
                if (talk.AskYesNo) BeginChoice(ChoiceKind.Merchant, 2, o);
                return;
            }
            return;
        }
        if (TowerNpc.IsAltar(what))
        {
            // 原典は**まず寄進額を言って「寄進するか？」と聞く**（altar_offer）。
            // ここを飛ばして望みの一覧をいきなり出していたので、
            // 「いくら払うのか出ない」と報告された。
            Speak(GameText.Instance.Message("altar_offer", TowerNpc.AltarCost(Player)));
            BeginChoice(ChoiceKind.AltarOffer, 2, default(Shop.Offer));
            return;
        }
        if (TowerNpc.IsFairy(what))
        {
            Speak(FairyText());
            // 採点したら**その場から消える**。20階・25階・49階では解錠の表の最後が
            // 妖精のマスを床に戻しており（出したその場で採点して消す）、
            // 10階でも話したあとは残らない。
            // **消えるのは読み終えてから**（話しかけた瞬間に消えると報告された）。
            pages.Enqueue(new Page
            {
                Kind = PageKind.Effect, Text = null, Record = -1,
                Effect = ClearCellStep + "(" + at.x + "," + at.y + ")"
            });
            return;
        }
    }

    /// <summary>
    /// 妖精の採点を原典の文面の順に組み立てる。
    /// 「評価すると・・・」→「○点です。」→ 評価。称号がもらえるならそのあとに称号と締め。
    /// </summary>
    string FairyText()
    {
        var t = GameText.Instance;
        var r = TowerNpc.AskFairy(Player);
        if (!r.Valid) return t.Message("fairy_here");
        // もらった称号は右上の「実績」に出すので覚えておく（作者の指定）
        if (r.Rank != null) Player.Rank = r.Rank;
        var body = t.Message("fairy_ask") + t.Message("fairy_score", r.Score)
                 + t.Message(r.EvaluationKey);
        if (r.Rank == null) return body;
        // 称号つきの文面は『 で切れているので、称号と締めをつなぐ
        return body + r.Rank
             + t.Message(Player.Floor >= 49 ? "fairy_end_fin" : "fairy_end_go");
    }

    /// <summary>隠れていた者を盤面に出す。条件を満たしていなければ何も起きない。</summary>
    void Reveal(Vector2Int at)
    {
        var hit = HiddenOccupants.Instance.At(Player.Floor, at);
        if (!hit.HasValue) return;
        var h = hit.Value;
        if (h.RequiresCleared.HasValue && !Floor.IsCleared(h.RequiresCleared.Value)) return;
        PlaySound(SoundCues.Reveal);
        Floor.Put(at, h.What);
    }

    /// <summary>
    /// 盗賊に話しかける。原典（MTE.EXE 0x03e0）の分岐表を上から見て最初に合った1件を実行する。
    /// 書き換えは別の階にも及ぶ（2階の盗賊が35階の抜け穴を開ける）。
    /// </summary>
    public string TalkToThief()
    {
        var hit = ThiefEvents.Instance.For(Player.Floor, Player.Position.y, Player.EventFlags);
        if (!hit.HasValue) return null;
        var e = hit.Value;

        if (e.SetsFlag >= 0) Player.EventFlags.Add(e.SetsFlag);

        var line = GameText.Instance.Dialogue(e.Record);
        var text = line.HasValue ? line.Value.Body : "";
        Say(text);
        // 地図の書き換え（＝盗賊が居なくなる）と歩き去りは**セリフを読み終えてから**。
        // 原典 0x003e0 も、セリフ → キー待ち → マスを床にして歩かせる、の順。
        leaving = e;
        pages.Enqueue(new Page
        {
            Kind = PageKind.Effect, Text = null, Record = -1,
            Effect = Cutscenes.ThiefLeavesStep
        });
        return text;
    }

    public bool UseItem(SpecialItems.Kind kind)
    {
        if (Floor == null || !Player.Has(kind)) return false;
        PlaySound(SoundCues.UseSpecial);
        var ids = ObjectIds.Instance;
        System.Func<Vector2Int, int> cellId = p => ids.Of(Floor.At(p));

        switch (kind)
        {
            case SpecialItems.Kind.Elixir:
            {
                int heal = SpecialItemEffects.ElixirHeal(Player.Attack, Player.Defense);
                Player.Heal(heal);
                Say(GameText.Instance.Message("elixir_use", heal));
                break;
            }
            case SpecialItems.Kind.Matock:
                if (!BreakWalls(SpecialItemEffects.MatockTargets(Player.Position, WallId))) return false;
                Say(GameText.Instance.Message("matock_use"));
                break;

            case SpecialItems.Kind.SuperMatock:
                BreakWalls(SpecialItemEffects.SuperMatockTargets(WallId));
                Say(GameText.Instance.Message("super_matock_use"));
                break;

            case SpecialItems.Kind.MagicKey:
                foreach (var p in SpecialItemEffects.MagicKeyTargets(YellowDoorId)) Floor.Clear(p);
                Say(GameText.Instance.Message("magic_key_use"));
                break;

            case SpecialItems.Kind.DestructionOrb:
            {
                // 原典は属性表の種別が8か10（モンスター）のマスを対象にする。
                // ここでは物体IDが61以上ならモンスターとみなす（表と同じ範囲）。
                var t = SpecialItemEffects.DestructionTargets(Player.Position, cellId, IsMonsterId);
                foreach (var p in t) Floor.Clear(p);
                Say(GameText.Instance.Message("destruction_use"));
                break;
            }
            case SpecialItems.Kind.WingUp:   return UseWing(true);
            case SpecialItems.Kind.WingDown: return UseWing(false);

            case SpecialItems.Kind.TeleportRelic:
                Player.Hp -= SpecialItemEffects.TeleportHpCost;
                Say(GameText.Instance.Message("teleport_use"));
                break;

            default:
                return false;   // オーブ・十字架・幸運の金貨などは「使う」ものではない
        }

        if (SpecialItemEffects.IsConsumedOnUse(kind)) Player.Consume(kind);
        Evaluate();
        return true;
    }

    /// <summary>壁のマスを床にする。1つも無ければ使わない（原典も空振りしない）。</summary>
    bool BreakWalls(System.Collections.Generic.List<Vector2Int> targets)
    {
        if (targets == null || targets.Count == 0) return false;
        foreach (var p in targets) Floor.Clear(p);
        return true;
    }

    int WallId(Vector2Int p)
    {
        return Floor.At(p) == "#" ? SpecialItemEffects.CellWall : 0;
    }

    int YellowDoorId(Vector2Int p)
    {
        return Floor.At(p) == "y" ? SpecialItemEffects.CellLockYellow : 0;
    }

    /// <summary>モンスターの物体IDか。原典の表はID61から始まる。</summary>
    static bool IsMonsterId(int id) { return id >= 61; }

    /// <summary>翼で階を移る。階段と違い44階を飛ばさない。</summary>
    public bool UseWing(bool up)
    {
        var kind = up ? SpecialItems.Kind.WingUp : SpecialItems.Kind.WingDown;
        if (!Player.Has(kind)) return false;

        int to = SpecialItemEffects.WingFloor(Player.Floor, up);
        if (to < FloorConnections.MinFloor || to > FloorConnections.MaxFloor) return false;

        Player.Consume(kind);
        Say(GameText.Instance.Message(up ? "wing_up_use" : "wing_down_use"));
        EnterFloor(to, Player.Position);
        return true;
    }
}
