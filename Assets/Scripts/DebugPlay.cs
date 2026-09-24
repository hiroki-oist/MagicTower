using UnityEngine;

/// <summary>
/// **調べもの用の細工**。原典の再現ではないので、見終わったら必ず false に戻すこと。
///
/// ⚠ **2026-09-25 現在、通しで見てもらうために攻守300・鍵30本を入れてある**
/// （作者の頼み「3Fでのイベント後にデバッグ状態（各鍵を30個，攻守を300）で始まる
/// ようにして…最後に通しで回して致命的なバグがないか確認します」）。
/// **Apple へ出す前に両方 false へ戻すこと。**
///
/// 作者からの頼み（2026-09-21）:
/// 「本来はゲーム開始時（3Fのイベント後）は攻守ともに10なんだろうけど、
///   デバッグ用にいまは一旦1000にしておいてくれる？
///   そしたらイベント側の不具合とかがないか見れるから」
///
/// <see cref="AuthorStyle"/> と同じで、**原典から外れる所はここ1か所にまとめる**。
/// </summary>
public static class DebugPlay
{
    /// <summary>
    /// ゼノに奪われたあとの攻撃力・防御力を <see cref="StrongStat"/> にする。
    /// **false が原典どおり**（攻10 / 防10、原典 0x06f1b〜0x06f49）。
    /// </summary>
    public static readonly bool StrongAfterZeno = true;    // ★出すとき false（原典どおり攻10/防10）

    /// <summary>
    /// そのときの攻撃力・防御力。
    ///
    /// ⚠ 以前は**上限いっぱいの 990**（<see cref="PlayerState.MaxAttack"/>）にしていたが、
    /// それだと祭壇で攻撃力を買っても宝石を拾っても **ClampStats で即座に戻される**ので
    /// 「上がらない」と報告された。上限に当たらない 300 にしてある。
    /// </summary>
    public const int StrongStat = 300;

    /// <summary>
    /// 生命力は原典のまま 400 にしてある。**魔法攻撃は防御力を素通りする**
    /// （魔術士Ａ=200・Ｂ=100・魔導師は半分、原典 0x1b75）ので、
    /// 塔を上まで見て回るなら合わせて上げる必要が出るかもしれない。
    /// そのときはここを true にする。
    /// </summary>
    public static readonly bool StrongHpAfterZeno = false;

    /// <summary>そのときの生命力。</summary>
    public const int StrongHp = 9990;

    /// <summary>
    /// ゼノに奪われたあとの鍵の本数を <see cref="StartKeys"/> 本ずつにする。
    /// **false が原典どおり**（黄0/紫0/赤0。原典は鍵を持たずに牢から始まる）。
    ///
    /// 作者からの頼み（2026-09-22）:
    /// 「デバッグ用として鍵の数は武器取り上げられた後の初期値を30ずつでお願い」
    /// </summary>
    public static readonly bool KeysAfterZeno = true;      // ★出すとき false（原典どおり鍵0本）

    /// <summary>そのときの鍵の本数（黄・紫・赤とも同じ）。</summary>
    public const int StartKeys = 30;

    /// <summary>
    /// 起動のたびに**調べもの用のセーブ**を
    /// <see cref="PhoneScreen.QuickSlot"/>（セーブ／ロードのボタンが使うスロット）へ書く。
    /// 中身は <see cref="DebugEndgameSave"/>。
    ///
    /// 作者の頼み（2026-09-24）「49階ボス〜エンディングまでだけ見たい」。
    /// ⚠ **起動するたびに上書きする**ので、途中経過をそこへセーブしても
    /// 次に立ち上げると調べもの用の状態に戻る。見終わったら false に。
    /// </summary>
    public static readonly bool MakeEndgameSave = false;   // ★出すとき false（セーブを上書きするため）

    /// <summary>
    /// その調べもの用のセーブを**どこから**始めるか。
    ///   <see cref="DebugEndgameSave.BeforeZeno"/> … 49階の下り階段（ボス戦から）
    ///   <see cref="DebugEndgameSave.BeforeGate"/> … 24階の時空の門の1歩手前（終幕だけ）
    /// 作者の頼み（2026-09-24）「セーブデータを時空の扉を通る寸前で用意して」。
    /// </summary>
    public static readonly string EndgameSaveAt = DebugEndgameSave.BeforeGate;
}
