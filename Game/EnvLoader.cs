using System;
using System.IO;
using Godot;

public static class EnvLoader
{
    public static void LoadEnv(string filePath = ".env")
    {
        filePath = OS.HasFeature("editor")?ProjectSettings.GlobalizePath("res://.env"):OS.GetExecutablePath().GetBaseDir().PathJoin(".env");
        // 检查文件是否存在
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"警告: 未找到环境文件 {filePath}");
            return;
        }

        // 读取所有行
        var lines = File.ReadAllLines(filePath);
        
        foreach (var line in lines)
        {
            // 跳过空行和注释
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            // 按等号分割键值对
            var parts = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            
            // 确保有键和值
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            // 设置环境变量（仅当前进程）
            System.Environment.SetEnvironmentVariable(key, value);
        }
    }
}