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

        }
    }
}