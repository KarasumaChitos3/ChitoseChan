using System;
using Backend.Core;
using Backend.SpeechToText;

namespace Backend.Core
{
    
    public static class BackendHost
    {
        private static bool _lock;
        private static WakeWordDetector _wakeWordDetector;
        private static WhisperService _whisperService;
        // 使用 Action<int> 委托，它将传递被触发的唤醒词的索引
        public static event Action<int> WakeWordDetected;
        public static void Initialize(string envPath = ".env")
        {
            Env.LoadFromFile(envPath);

            _wakeWordDetector = WakeWordDetector.Create();
            _whisperService = new WhisperService("");
            _wakeWordDetector.OnDetected += OnWakeWordDetected;
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

        public static void Shutdown()
        {
            _wakeWordDetector?.Dispose();
        }
    }
}