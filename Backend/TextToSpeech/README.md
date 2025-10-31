您提的这一点非常专业，完全正确！

持续将麦克风的音频流实时传输到云端进行语音识别，确实会产生不小的费用，而且在用户没有主动交互时也是一种浪费。因此，**“本地唤醒词识别 + 云端指令识别”** 的两阶段方案是目前业界最主流、最经济高效的做法。

我们来详细探讨如何实现这个方案，并比较一下阿里云和火山引擎。

### 阶段一：本地唤醒词识别 (Wake-Word Detection)

这个阶段的目标是在设备本地（您的Linux桌面）上，用极低的资源消耗，7x24小时地监听一个特定的唤醒词（比如“你好，桌宠”）。

对于 C# 和跨平台（Linux）的需求，我强烈推荐使用 **Picovoice Porcupine** 引擎。

*   **为什么是 Porcupine?**
    *   **极致轻量**: 它的资源占用非常小，CPU 和内存消耗极低，非常适合作为后台服务一直运行而不影响性能。
    *   **高精度**: 误报率和漏报率都非常低。
    *   **跨平台**: 官方直接支持 Linux、Windows、macOS、Android、iOS 等，并且提供了 **C#/.NET 的 SDK**。
    *   **自定义唤醒词**: 你可以在他们的网站上轻松训练自己专属的唤醒词模型。

*   **在 Godot C# 中的实现思路**:
    1.  在你的 C# 项目中引入 Porcupine 的 NuGet 包。
    2.  编写一个 `WakeWordDetector.cs` 脚本。
    3.  在这个脚本里，初始化 Porcupine 引擎，并使用一个音频输入库（比如 `NAudio` 的跨平台替代方案或者直接处理 Godot 的 `AudioStreamMicrophone`）来持续获取麦克风的音频数据。
    4.  将音频数据帧持续喂给 Porcupine 引擎进行处理。
    5.  当 Porcupine 检测到唤醒词时，它会返回一个结果。此时，你的脚本就可以发出一个信号（Signal），通知应用“用户已经唤醒我了，准备好接收指令！”

这个阶段完全在本地进行，不产生任何网络流量和云服务费用。

### 阶段二：云端指令识别 (Cloud Speech-to-Text)

当唤醒词被检测到后，程序立即开始录音，并将接下来的用户指令发送到云端进行高精度的识别。这里我们就需要考察阿里云和火山引擎了。

由于您在境内，这两家都是非常好的选择。它们针对中文普通话的识别优化通常会比境外服务更好。对于 C# SDK，情况有点类似：

*   **阿里云智能语音交互**:
    *   **服务**: 提供了“实时语音识别”服务，非常适合唤醒后的指令识别场景。
    *   **C# SDK**: 阿里云官方的旧版 C# SDK 对 Linux 的支持并不理想，其 C# 版本是基于 Windows C++ SDK 的封装，有平台限制。
    *   **解决方案**: **直接使用 REST API**。阿里云的所有服务都提供了标准的 HTTP RESTful API。在 C# 中，使用内置的 `HttpClient` 类来调用这些 API 是非常简单和通用的方法，完全可以跨平台在 Linux 上运行。他们的“DashScope”平台也提供了清晰的 API 文档和 C# 调用示例。

*   **火山引擎语音识别**:
    *   **服务**: 同样提供了一句话识别、实时语音识别等全套服务。
    *   **C# SDK**: 和阿里云类似，可能没有提供一个完善且跨平台的官方 C# SDK。
    *   **解决方案**: 同样是**直接使用 REST API**。通过 `HttpClient` 来构造请求、发送音频数据并接收返回的文本。

#### 阿里云 vs. 火山引擎：如何选择？

既然两者很可能都需要你通过 `HttpClient` 直接调用 API，那么选择的关键就不在于有无现成的 C# SDK，而在于以下几点：

1.  **API 文档的清晰度**: 哪家的 REST API 文档写得更清晰、更容易理解？这直接影响你的开发效率。建议您分别浏览两家官网的开发者文档，看看哪个更符合您的习惯。
2.  **定价和免费额度**: 这是非常关键的一点。两家云服务商通常都会提供一定的**免费调用额度**（例如，每月几万次调用或几十个小时的免费识别时长）。对于个人开发者和小项目来说，一个慷慨的免费额度意味着你的项目可以零成本运行。请仔细比较它们的计费页面。
3.  **识别准确率和响应速度**: 对于中文，尤其是不同口音的识别，两家可能存在细微差异。最好的方法是，分别申请两家的 API Key，写一小段测试代码，用相同的几段录音分别调用它们的 API，亲自对比一下识别的准确率和返回结果的速度。

### 总结与架构建议

您的桌宠语音交互流程应该是这样的：

1.  **待机状态**: Godot 项目后台运行 **Porcupine** 引擎，持续监听麦克风，等待唤醒词。此阶段无网络消耗。
2.  **唤醒**: 用户说出“你好，桌宠”。Porcupine 成功检测，并触发一个 C# 事件/Godot 信号。
3.  **指令监听**: 程序播放一个提示音（例如 “叮咚”），并开始将麦克风的音频流录制下来。
4.  **云端识别**: 用户说完指令（比如“今天天气怎么样”），程序检测到静音后，将录制好的音频通过 `HttpClient` 发送给您最终选择的**阿里云**或**火山引擎**的语音识别 API。
5.  **获取结果**: 云服务返回识别出的文本 “今天天气怎么样”。
6.  **执行任务**: 您的程序将这段文本传递给 LLM 模块进行处理，后续再由 TTS 模块播报出来。

这个架构兼顾了成本、性能和用户体验，是一个非常成熟和理想的方案。

### 环境变量配置 (.env)

为避免在代码中硬编码 AccessKey，请在项目根目录创建或编辑 `.env` 文件，并设置如下变量：

```
PICOVOICE_ACCESS_KEY=你的PicovoiceAccessKey
```

Backend 入口在 `Backend/Core/BackendHost.Initialize()`：

```csharp
using Backend.Core;
using Backend.TextToSpeech;

// 初始化环境（读取 .env）
BackendHost.Initialize();

// 创建检测器（读取用户目录的关键词模型文件）
// 默认目录：<用户目录>\\chitose-chan\\porcupine_keywords
// 可覆盖：传入路径参数，或设置环境变量 PICOVOICE_KEYWORDS_DIR
var detector = WakeWordDetector.Create();
// 例如显式指定：
// var detector = WakeWordDetector.Create(@"C:\\Users\\<user>\\chitose-chan\\porcupine_keywords");

// 提供获取下一帧 PCM 的函数（short[]，长度需为 detector.FrameLength）
short[] getNextAudioFrame() {
    // .. 从麦克风或音频源获取一帧 PCM 数据
    return new short[detector.FrameLength];
}

// 启动监听（后台线程），并设置检测回调
detector.StartListening(getNextAudioFrame, keywordIndex => {
    // .. 收到唤醒词检测事件，可触发后续逻辑
});

// 需要停止监听时：
// detector.StopListening();
```

若未设置 `PICOVOICE_ACCESS_KEY` 将抛出明确错误。

在默认目录中放置 Porcupine 关键词模型文件（扩展名 `.ppn`）。若目录不存在或未发现 `.ppn` 文件，会抛出错误提示。你也可以通过设置 `PICOVOICE_KEYWORDS_DIR` 环境变量或在调用 `WakeWordDetector.Create(<dir>)` 时传入自定义目录来覆盖默认路径。

请确保 `.env` 不被提交到版本库（已通过 `.gitignore` 处理）。