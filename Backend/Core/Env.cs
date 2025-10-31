using System;
using System.IO;

namespace Backend.Core
{
    public static class Env
    {
        public static void LoadFromFile(string path = ".env")
        {
            if (!File.Exists(path)) return;
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();

                if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                    (value.StartsWith("'") && value.EndsWith("'")))
                {
                    if (value.Length >= 2)
                        value = value.Substring(1, value.Length - 2);
                }

                if (key.Length > 0)
                    Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}