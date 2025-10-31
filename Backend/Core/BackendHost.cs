using Backend.Core;
using Backend.TextToSpeech;

namespace Backend.Core
{
    
    public static class BackendHost
    {

        private static WakeWordDetector _wakeWordDetector;
        public static void Initialize(string envPath = ".env")
        {
            Env.LoadFromFile(envPath);

            _wakeWordDetector = WakeWordDetector.Create();
            _wakeWordDetector.OnDetected += OnWakeWordDetected;
        }

        private static void OnWakeWordDetected(int keywordIndex)
        {
            // .. 收到唤醒词检测事件，可触发后续逻辑
        }
    }
}