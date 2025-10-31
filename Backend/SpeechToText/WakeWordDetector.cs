using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Pv;

namespace Backend.SpeechToText
{
    public class WakeWordDetector : IDisposable
    {
        private readonly Porcupine _porcupine;
        private Thread _listenerThread;
        private volatile bool _running;

        public Func<short[]> FrameProvider { get; set; }
        public event Action<int> OnDetected;

        private WakeWordDetector(Porcupine porcupine)
        {
            _porcupine = porcupine ?? throw new ArgumentNullException(nameof(porcupine));
        }

        // Factory: build detector from keyword model directory (.ppn files)
        // Default dir: <UserProfile>\\chitose-chan\\porcupine_keywords
        // Override via parameter or env PICOVOICE_KEYWORDS_DIR
        public static WakeWordDetector Create(string keywordsDir = null)
        {
            var accessKey = Environment.GetEnvironmentVariable("PICOVOICE_ACCESS_KEY");
            if (string.IsNullOrWhiteSpace(accessKey))
                throw new InvalidOperationException("Porcupine AccessKey not found. Set PICOVOICE_ACCESS_KEY in environment or .env.");

            var dir = !string.IsNullOrWhiteSpace(keywordsDir)
                ? keywordsDir
                : (Environment.GetEnvironmentVariable("PICOVOICE_KEYWORDS_DIR") ??
                   Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                       "chitose-chan", "porcupine_keywords"));

            if (!Directory.Exists(dir))
                throw new DirectoryNotFoundException($"Porcupine keywords directory not found: {dir}");

            var keywordPaths = new List<string>(Directory.GetFiles(dir, "*.ppn", SearchOption.TopDirectoryOnly));
            if (keywordPaths.Count == 0)
                throw new InvalidOperationException($"No .ppn keyword files found in: {dir}");

            var porcupine = Porcupine.FromKeywordPaths(accessKey, keywordPaths);
            return new WakeWordDetector(porcupine);
        }

        public int FrameLength => _porcupine.FrameLength;
        public int SampleRate => _porcupine.SampleRate;

        // Process a single frame (synchronous)
        public int ProcessFrame(short[] pcmFrame)
        {
            if (pcmFrame == null || pcmFrame.Length != _porcupine.FrameLength)
                throw new ArgumentException($"pcmFrame must be non-null and length {_porcupine.FrameLength}.");
            return _porcupine.Process(pcmFrame);
        }

        // Start background listening with preset FrameProvider and optional callback
        public void StartListening(Action<int> onDetected = null)
        {
            if (FrameProvider == null)
                throw new InvalidOperationException("FrameProvider is null. Set FrameProvider or use StartListening(Func<short[]>, Action<int>).");
            StartListening(FrameProvider, onDetected);
        }

        // Start background listening with a frame provider and optional detection callback
        public void StartListening(Func<short[]> getNextAudioFrame, Action<int> onDetected = null)
        {
            if (getNextAudioFrame == null) throw new ArgumentNullException(nameof(getNextAudioFrame));
            if (_running) return;
            _running = true;

            _listenerThread = new Thread(() =>
            {
                while (_running)
                {
                    try
                    {
                        var frame = getNextAudioFrame();
                        if (frame == null)
                        {
                            Thread.Sleep(1);
                            continue;
                        }
                        if (frame.Length != _porcupine.FrameLength)
                        {
                            // Skip mismatched frames
                            continue;
                        }

                        var keywordIndex = _porcupine.Process(frame);
                        if (keywordIndex >= 0)
                        {
                            onDetected?.Invoke(keywordIndex);
                            OnDetected?.Invoke(keywordIndex);
                        }
                    }
                    catch
                    {
                        // Keep listener alive; consider logging externally
                    }
                }
            })
            {
                IsBackground = true,
                Name = "WakeWordListener"
            };

            _listenerThread.Start();
        }

        public void StopListening()
        {
            _running = false;
            try
            {
                _listenerThread?.Join();
            }
            catch { }
            finally
            {
                _listenerThread = null;
            }
        }

        public void Dispose()
        {
            StopListening();
            _porcupine?.Dispose();
        }
    }
}