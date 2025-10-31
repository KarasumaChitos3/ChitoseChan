// Backend/SpeechToText/WhisperService.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;

namespace Backend.SpeechToText
{
    public class WhisperService : IDisposable
    {
        private readonly WhisperProcessor _processor;
        private readonly WhisperFactory _whisperFactory;

        // 在构造函数中加载模型，只需一次
        public WhisperService(string modelPath)
        {
            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException($"Whisper model not found at {modelPath}");
            }
            
            // 从模型路径创建工厂
            _whisperFactory = WhisperFactory.FromPath(modelPath);

            // 使用 Builder 模式配置识别器
            _processor = _whisperFactory.CreateBuilder()
                .WithLanguage("auto") // 自动检测语言
                .Build();
                
            Console.WriteLine("WhisperService initialized successfully.");
        }

        /// <summary>
        /// 接收一个音频流，并异步返回识别出的文本。
        /// </summary>
        /// <param name="audioStream">包含音频数据 (例如 .wav 格式) 的流。</param>
        /// <returns>识别出的完整文本。</returns>
        public async Task<string> TranscribeAsync(Stream audioStream)
        {
            if (_processor == null)
            {
                throw new InvalidOperationException("WhisperProcessor is not initialized.");
            }

            string fullText = "";
            
            // 异步处理流，并拼接所有识别出的片段
            await foreach (var result in _processor.ProcessAsync(audioStream))
            {
                Console.WriteLine($"[Whisper] Segment: {result.Start} -> {result.End} : {result.Text}");
                fullText += result.Text;
            }

            return fullText.Trim();
        }

        public void Dispose()
        {
            _processor?.Dispose();
            _whisperFactory?.Dispose();
        }
    }
}