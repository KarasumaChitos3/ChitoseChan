using Godot;

public partial class MicrophoneMonitor : Node
{
    private AudioEffectCapture _effect;
    private const int BufferSize = 2048; // 可以根据需要调整缓冲区大小

    public override void _Ready()
    {
        // 获取 "MicInput" 总线的索引
        int busIndex = AudioServer.GetBusIndex("MicInput");
        if (busIndex != -1)
        {
            // 获取该总线上的第一个效果，即我们添加的 AudioEffectCapture
            _effect = (AudioEffectCapture)AudioServer.GetBusEffect(busIndex, 0);
        }
        else
        {
            GD.PrintErr("Audio bus 'MicInput' not found.");
        }
    }

    public override void _Process(double delta)
    {
        if (_effect == null)
        {
            return;
        }

        // 检查是否有足够的数据可供读取
        if (_effect.GetFramesAvailable() >= BufferSize)
        {
            // 从效果中获取音频数据
            Vector2[] audioData = _effect.GetBuffer(BufferSize);

            float peakVolume = 0.0f;
            float rmsVolume = 0.0f;
            float sumOfSquares = 0.0f;

            foreach (Vector2 sample in audioData)
            {
                // sample.X 是左声道，sample.Y 是右声道。这里我们取绝对值的最大值作为峰值。
                float currentPeak = Mathf.Max(Mathf.Abs(sample.X), Mathf.Abs(sample.Y));
                if (currentPeak > peakVolume)
                {
                    peakVolume = currentPeak;
                }

                // 计算平方和以用于 RMS 计算
                sumOfSquares += sample.X * sample.X + sample.Y * sample.Y;
            }

            // 计算 RMS 音量
            rmsVolume = Mathf.Sqrt(sumOfSquares / (audioData.Length * 2)); // *2 因为是立体声

            // 将音量转换为分贝 (dB) - 0.00001 是为了避免 log(0)
            float peakDb = 20 * Mathf.Log(peakVolume + 0.00001f);
            float rmsDb = 20 * Mathf.Log(rmsVolume + 0.00001f);

            GD.Print($"Peak Volume: {peakDb} dB, RMS Volume: {rmsDb} dB");
        }
    }
}