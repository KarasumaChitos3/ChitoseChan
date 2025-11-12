using Godot;
using System.Collections.Generic;
using Backend.Core;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

public partial class Main : Node
{
    // 状态枚举，用于管理录音和播放流程
    private enum RecordState { Idle, Recording, WaitingToPlay, Playing }
    private RecordState _currentState = RecordState.Idle;

    private AudioStreamPlayer _recorder;
    private AudioStreamPlayer _player;
    private Timer _playbackTimer;
    private AudioEffectCapture _effect;
    private AudioStreamGeneratorPlayback _playback;

    // 用于存储录制的音频数据
    private List<Vector2> _recordedSamples = new List<Vector2>();

    public override void _Ready()
    {
        var ggml_path = OS.HasFeature("editor")?ProjectSettings.GlobalizePath("res://models/ggml-base.bin"):OS.GetExecutablePath().GetBaseDir().PathJoin("models/ggml-base.bin");
        BackendHost.Initialize(ggml_path);
        // 获取节点引用
        _recorder = GetNode<AudioStreamPlayer>("../AudioRecorder");
        _player = GetNode<AudioStreamPlayer>("../AudioPlayer");
        _playbackTimer = GetNode<Timer>("../PlaybackTimer");

        // 获取 "MicInput" 总线上的 AudioEffectCapture
        int busIndex = AudioServer.GetBusIndex("MicInput");
        if (busIndex != -1)
        {
            _effect = (AudioEffectCapture)AudioServer.GetBusEffect(busIndex, 0);
        }
        else
        {
            GD.PrintErr("Audio bus 'MicInput' not found. Please set it up in the Audio tab.");
            GetTree().Quit();
        }

        // 获取用于手动播放的 AudioStreamGeneratorPlayback
        var streamGenerator = _player.Stream as AudioStreamGenerator;
        if (streamGenerator != null)
        {
            // 合理的缓冲长度(秒)，避免巨大的环形缓冲导致状态异常
            streamGenerator.BufferLength = 0.25f;
            streamGenerator.MixRate = AudioServer.GetMixRate();
        }
        // _playback = _player.GetStreamPlayback() as AudioStreamGeneratorPlayback;

        GD.Print("Press [Space] to start recording for 3 seconds.");
    }

    public override void _Process(double delta)
    {
        switch (_currentState)
        {
            case RecordState.Idle:
                // 在空闲状态下，等待用户输入以开始录音
                if (Input.IsActionJustPressed("ui_accept")) // "ui_accept" 默认是空格键
                {
                    StartRecording();
                }
                break;

            case RecordState.Recording:
                // 在录音状态下，从效果器中捕获音频帧
                CaptureAudio();
                break;

            case RecordState.WaitingToPlay:
                // 等待计时器触发回放
                break;

            case RecordState.Playing:
                // 检查播放是否已完成
                if (!_player.Playing)
                {
                    GD.Print("Playback finished. Press [Space] to record again.");
                    _currentState = RecordState.Idle;
                }
                break;
        }
    }

    private void StartRecording()
    {
        GD.Print("Recording...");
        _recordedSamples.Clear(); // 清空之前的录音数据
        _recorder.Play(); // 开始驱动麦克风
        _effect.ClearBuffer(); // 清空效果器的环形缓冲区
        _currentState = RecordState.Recording;

        // 我们将使用另一个计时器来自动停止录音，这里用 SceneTreeTimer 演示
        GetTree().CreateTimer(3.0).Timeout += StopRecording;
    }

    private void CaptureAudio()
    {
        // 尽可能多地读取可用的音频帧
        int framesAvailable = _effect.GetFramesAvailable();
        if (framesAvailable > 0)
        {
            _recordedSamples.AddRange(_effect.GetBuffer(framesAvailable));
        }
    }

    private void StopRecording()
    {
        if (_currentState != RecordState.Recording) return;

        // 停止录音前，再捕获一次剩余的音频数据
        CaptureAudio();
        _recorder.Stop(); // 停止麦克风

        GD.Print($"Recording stopped. {_recordedSamples.Count / AudioServer.GetMixRate():F2} seconds recorded. Waiting 2 seconds to play back...");
        _currentState = RecordState.WaitingToPlay;
        _playbackTimer.Start(); // 启动2秒倒计时
    }

    // **【修改】**：重写 PlaybackRecording 方法
    private async void PlaybackRecording()
    {
        GD.Print("Playing back...");

        if (_recordedSamples.Count == 0)
        {
            GD.Print("No audio data to play.");
            _currentState = RecordState.Idle;
            return;
        }
        
        // 1. 开始播放。这将激活播放器并创建 Playback 对象
        _player.Play();
        
        // 2. 在 Play() 之后立即获取 Playback 对象
        var playback = _player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        
        if (playback != null)
        {
            // 3. 将我们录制的所有样本推送到播放缓冲区
            playback.PushBuffer(_recordedSamples.ToArray());
            _currentState = RecordState.Playing;

            // 3.1 计算录音时长，并在播放完成后主动停止播放器
            double durationSec = (double)_recordedSamples.Count / AudioServer.GetMixRate();
            // 略加裕量，避免浮点误差导致提前停止或延后
            var stopTimer = GetTree().CreateTimer(Mathf.Max(0.0, durationSec + 0.05));
            stopTimer.Timeout += () =>
            {
                _player.Stop();
                _currentState = RecordState.Idle;
                GD.Print("Playback finished.");
            };

            // 4. 在回放的同时，构建 WAV 流并调用 Whisper 进行识别
            try
            {
                int sampleRate = (int)AudioServer.GetMixRate();
                var wavBytes = CreateWavMono16(_recordedSamples, sampleRate);
                using var ms = new MemoryStream(wavBytes);
                var text = await BackendHost.RecognizeSpeechAsync(ms);
                GD.Print($"[Whisper] 识别结果: {text}");
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"Whisper 识别失败: {ex.Message}");
            }
        }
        else
        {
            GD.PrintErr("Failed to get AudioStreamGeneratorPlayback.");
            _currentState = RecordState.Idle;
        }
    }

    // 这是连接到 PlaybackTimer 的 timeout 信号的方法
    private void _on_playback_timer_timeout()
    {
        PlaybackRecording();
    }

    // 将录制的样本转换为 16kHz 单声道 16-bit PCM WAV（NAudio）
    private static byte[] CreateWavMono16(List<Vector2> samples, int sourceSampleRate)
    {
        const int targetSampleRate = 16000;

        // 构建立体声 float 数组（NAudio 的 ISampleProvider 以 float[-1,1] 为单位）
        int srcCount = samples.Count;
        var stereo = new float[srcCount * 2];
        for (int i = 0; i < srcCount; i++)
        {
            var v = samples[i];
            stereo[2 * i] = v.X;
            stereo[2 * i + 1] = v.Y;
        }

        // 用自定义 ArraySampleProvider 提供源音频
        var sourceProvider = new FloatArraySampleProvider(
            stereo,
            WaveFormat.CreateIeeeFloatWaveFormat(sourceSampleRate, 2)
        );

        // 下混为单声道（左/右各 50%）
        var monoProvider = new StereoToMonoSampleProvider(sourceProvider)
        {
            LeftVolume = 0.5f,
            RightVolume = 0.5f
        };

        // 重采样至 16kHz（WDL 高质量重采样）
        var resampledProvider = new WdlResamplingSampleProvider(monoProvider, targetSampleRate);

        // 转换为 16-bit PCM，并写入 WAV 封装
        var wave16 = new SampleToWaveProvider16(resampledProvider);
        using var ms = new MemoryStream();
        WaveFileWriter.WriteWavFileToStream(ms, wave16);
        return ms.ToArray();
    }

    // 简单的数组驱动的 ISampleProvider，用于把内存中的 float[] 暴露给 NAudio 管线
    private sealed class FloatArraySampleProvider : ISampleProvider
    {
        private readonly float[] _data;
        private int _position;
        public WaveFormat WaveFormat { get; }

        public FloatArraySampleProvider(float[] data, WaveFormat format)
        {
            _data = data ?? System.Array.Empty<float>();
            WaveFormat = format;
            _position = 0;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int available = _data.Length - _position;
            int toCopy = System.Math.Min(count, available);
            if (toCopy > 0)
            {
                System.Array.Copy(_data, _position, buffer, offset, toCopy);
                _position += toCopy;
            }
            return toCopy;
        }
    }
}