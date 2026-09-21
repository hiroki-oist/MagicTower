using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 鳴らす側。原典の譜面（<see cref="SoundBank"/>）を <see cref="SoundSynth"/> で波にして流す。
///
/// 原典はゲーム側が音番号を渡すだけで、鳴らすのはドライバの仕事だった。
/// リメイクも同じ形にしてあり、本線（<see cref="TowerGame"/>）は番号を積むだけ、
/// ここが毎手 <see cref="TowerGame.ConsumeSounds"/> で引いて鳴らす。
///
/// 曲（音1・音2）は繰り返し、効果音は重ねて鳴らす。
/// </summary>
public class SoundPlayer : MonoBehaviour
{
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource effectSource;
    [SerializeField, Range(0f, 1f)] float volume = 0.6f;

    readonly Dictionary<int, AudioClip> cache = new Dictionary<int, AudioClip>();

    /// <summary>今流れている曲の番号。無ければ -1。</summary>
    public int PlayingMusic { get; private set; }

    void Awake()
    {
        PlayingMusic = -1;
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        if (effectSource == null)
        {
            effectSource = gameObject.AddComponent<AudioSource>();
            effectSource.playOnAwake = false;
        }
    }

    /// <summary>作った波を覚えておく。曲は20秒を超えるので作り直さない。</summary>
    public AudioClip ClipOf(int number)
    {
        AudioClip c;
        if (cache.TryGetValue(number, out c)) return c;
        c = SoundSynth.Clip(number);
        cache[number] = c;
        return c;
    }

    /// <summary>音番号で鳴らす。曲なら流しっぱなしにする。</summary>
    public void Play(int number)
    {
        var clip = ClipOf(number);
        if (clip == null) return;
        if (SoundBank.Instance.IsMusic(number))
        {
            if (PlayingMusic == number && musicSource.isPlaying) return;
            PlayingMusic = number;
            musicSource.clip = clip;
            musicSource.volume = volume;
            musicSource.loop = true;
            musicSource.Play();
            return;
        }
        effectSource.PlayOneShot(clip, volume);
    }

    /// <summary>合図の名前で鳴らす。</summary>
    public void PlayCue(string cue) { Play(SoundCues.Instance.Number(cue)); }

    /// <summary>本線が積んだ音を全部鳴らす。毎手これを呼ぶ。</summary>
    public void Flush(TowerGame game)
    {
        if (game == null) return;
        foreach (var n in game.ConsumeSounds()) Play(n);
    }

    public void StopMusic()
    {
        PlayingMusic = -1;
        if (musicSource != null) musicSource.Stop();
    }
}
