using System;
using System.IO;
using System.Threading.Tasks;
using Backend.Core;
using Backend.SpeechToText;
using Backend.LLMService;

namespace Backend.Core
{
    
    public static class BackendHost
    {
        private static bool _lock;
        private static WakeWordDetector _wakeWordDetector;
        private static WhisperService _whisperService;
        private static LLMService.LLMService _llmService;
        // 使用 Action<int> 委托，它将传递被触发的唤醒词的索引
        public static event Action<int> WakeWordDetected;
        public static void Initialize(string ggmlPath,string envPath = ".env")
        {
            Env.LoadFromFile(envPath);

            // _wakeWordDetector = WakeWordDetector.Create();
            _whisperService = new WhisperService(ggmlPath);
            _llmService = Backend.LLMService.LLMService.CreateDefaultFromEnv();
            // _wakeWordDetector.OnDetected += OnWakeWordDetected;
        }

        private static void OnWakeWordDetected(int keywordIndex)
        {
            if (_lock)
            {
                return;
            }
            _lock = true;
            // 可以在这里先进行内部日志记录或处理
            Console.WriteLine($"[BackendHost] Wake word detected with index: {keywordIndex}. Forwarding event to subscribers.");

            // 使用 ?.Invoke() 安全地触发事件
            // 这会通知所有订阅者（比如 Godot 端）
            WakeWordDetected?.Invoke(keywordIndex);
        }

        /// <summary>
        /// 公共 API，用于从音频流中识别语音。
        /// 这是前端（Godot）将会调用的核心方法。
        /// </summary>
        public static async Task<string> RecognizeSpeechAsync(Stream audioStream)
        {
            if (_whisperService == null)
            {
                throw new InvalidOperationException("BackendHost is not initialized.");
            }
            Console.WriteLine("BackendHost received audio stream, starting transcription...");
            var result = await _whisperService.TranscribeAsync(audioStream);
            Console.WriteLine($"BackendHost transcription result: {result}");
            return result;
        }

        public static void Shutdown()
        {
            _wakeWordDetector?.Dispose();
        }

        public static async Task<string> TestWhisper(Stream audio)
        {
            return await _whisperService.TranscribeAsync(audio);
        }

        /// <summary>
        /// 公共 API：向 LLM 提问并返回回答文本。
        /// </summary>
        public static async Task<string> AskLLMAsync(string text)
        {
            if (_llmService == null)
            {
                throw new InvalidOperationException("BackendHost is not initialized.");
            }
            return await _llmService.AskAsync(text);
        }
    }
}