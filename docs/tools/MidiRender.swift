// MIDI を WAV に書き出す道具（macOS 内蔵の音源を使う。外部の音色ファイルは要らない）。
//
//   swiftc -O docs/tools/MidiRender.swift -o /tmp/midirender
//   /tmp/midirender 入力.mid 出力.wav [秒数]
//
// Java版の BGM（midi/*.mid）を Unity で鳴らせる形にするために使う。
import AVFoundation

let args = CommandLine.arguments
guard args.count >= 3 else {
    FileHandle.standardError.write("使い方: midirender 入力.mid 出力.wav [秒数]\n".data(using: .utf8)!)
    exit(2)
}
let midiURL = URL(fileURLWithPath: args[1])
let outURL = URL(fileURLWithPath: args[2])
let limit = args.count > 3 ? Double(args[3]) ?? 0 : 0

let engine = AVAudioEngine()
var desc = AudioComponentDescription(
    componentType: kAudioUnitType_MusicDevice,
    componentSubType: kAudioUnitSubType_MIDISynth,
    componentManufacturer: kAudioUnitManufacturer_Apple,
    componentFlags: 0, componentFlagsMask: 0)

let sem = DispatchSemaphore(value: 0)
var synth: AVAudioUnit?
AVAudioUnit.instantiate(with: desc, options: []) { unit, _ in synth = unit; sem.signal() }
sem.wait()
guard let synth = synth else { FileHandle.standardError.write("音源を作れない\n".data(using: .utf8)!); exit(1) }

// macOS 内蔵の音色（GM）
var bank = URL(fileURLWithPath:
    "/System/Library/Components/CoreAudio.component/Contents/Resources/gs_instruments.dls") as CFURL
let st = AudioUnitSetProperty(synth.audioUnit, AudioUnitPropertyID(kMusicDeviceProperty_SoundBankURL),
                              AudioUnitScope(kAudioUnitScope_Global), 0,
                              &bank, UInt32(MemoryLayout<CFURL>.size))
if st != noErr { FileHandle.standardError.write("音色を読めない \(st)\n".data(using: .utf8)!) }

engine.attach(synth)
let format = AVAudioFormat(standardFormatWithSampleRate: 44100, channels: 2)!
engine.connect(synth, to: engine.mainMixerNode, format: format)

try engine.enableManualRenderingMode(.offline, format: format, maximumFrameCount: 4096)
try engine.start()

let sequencer = AVAudioSequencer(audioEngine: engine)
try sequencer.load(from: midiURL, options: [])
sequencer.prepareToPlay()
try sequencer.start()

var seconds = limit
if seconds <= 0 {
    var longest = 0.0
    for track in sequencer.tracks {
        let end = track.lengthInSeconds + track.offsetTime
        if end > longest { longest = end }
    }
    seconds = max(1.0, longest) + 2.0      // 余韻ぶん
}

var file: AVAudioFile? = try AVAudioFile(forWriting: outURL,
                           settings: [AVFormatIDKey: kAudioFormatLinearPCM,
                                      AVSampleRateKey: 44100,
                                      AVNumberOfChannelsKey: 2,
                                      AVLinearPCMBitDepthKey: 16,
                                      AVLinearPCMIsFloatKey: false,
                                      AVLinearPCMIsBigEndianKey: false],
                           commonFormat: .pcmFormatFloat32, interleaved: false)
let buffer = AVAudioPCMBuffer(pcmFormat: engine.manualRenderingFormat,
                              frameCapacity: engine.manualRenderingMaximumFrameCount)!
var remaining = AVAudioFramePosition(seconds * 44100)
var peak: Float = 0
while remaining > 0 {
    let n = AVAudioFrameCount(min(AVAudioFramePosition(buffer.frameCapacity), remaining))
    let status = try engine.renderOffline(n, to: buffer)
    if status != .success { break }
    try file!.write(from: buffer)
    peak = max(peak, buffer.floatChannelData![0][0])
    remaining -= AVAudioFramePosition(n)
}
sequencer.stop()
engine.stop()
file = nil          // ここで書き終わり（閉じないと長さが0のまま残る）
print(String(format: "%@ → %@ (%.1f秒)", midiURL.lastPathComponent, outURL.lastPathComponent, seconds))
