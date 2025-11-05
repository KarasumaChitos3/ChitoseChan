using Godot;
using System;

public partial class ChatUi : Control
{
           // 加载之前创建的气泡场景
        [Export]
        private PackedScene _speechBubbleScene;

        private VBoxContainer _chatHistory;
        private LineEdit _inputBox;
        private Button _sendButton;
        private ScrollContainer _scrollContainer;


        public override void _Ready()
        {
            // 获取节点引用
            _chatHistory = GetNode<VBoxContainer>("ScrollContainer/ChatHistory");
            _inputBox = GetNode<LineEdit>("HBoxContainer/LineEdit"); // 替换成你自己的节点路径
            _sendButton = GetNode<Button>("HBoxContainer/Button");   // 替换成你自己的节点路径
            _scrollContainer = GetNode<ScrollContainer>("ScrollContainer");

            // ------------------------------------------------------------------
            // 信号连接 - 将你的输入组件信号连接到这里的处理函数
            // ------------------------------------------------------------------

            // 当用户在输入框按回车时
            _inputBox.TextSubmitted += OnInputTextSubmitted;
            // 当用户点击发送按钮时
            _sendButton.Pressed += OnSendButtonPressed;

            // --- 用于演示的初始对话 ---
            AddMessage("你好！有什么可以帮你的吗？", false);
        }

        /// <summary>
        /// 处理输入框回车事件
        /// </summary>
        private void OnInputTextSubmitted(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                AddPlayerMessage(text);
                _inputBox.Clear();
            }
        }

        /// <summary>
        /// 处理发送按钮点击事件
        /// </summary>
        private void OnSendButtonPressed()
        {
            var text = _inputBox.Text;
            if (!string.IsNullOrEmpty(text))
            {
                AddPlayerMessage(text);
                _inputBox.Clear();
            }
        }
        
        /// <summary>
        /// 滚动到底部，确保最新消息可见
        /// </summary>
        private void ScrollToBottom()
        {
            // 延迟一帧执行，等待容器更新布局
            Callable.From(() => _scrollContainer.ScrollVertical = (int)_scrollContainer.GetVScrollBar().MaxValue).CallDeferred();
        }

        // ------------------------------------------------------------------
        //  暴露给外部的接口
        // ------------------------------------------------------------------

        /// <summary>
        /// 添加一条玩家消息 (右侧)
        /// </summary>
        public void AddPlayerMessage(string text)
        {
            AddMessage(text, true);
            // 这里可以添加你自己的逻辑，比如将消息发送到服务器或触发NPC回应
        }

        /// <summary>
        /// 添加一条对方的消息 (左侧)
        /// </summary>
        public void AddNpcMessage(string text)
        {
            AddMessage(text, false);
        }


        /// <summary>
        /// 核心逻辑：创建并添加一个新的气泡消息
        /// </summary>
        /// <param name="text">消息内容</param>
        /// <param name="isPlayer">是否是玩家</param>
        private void AddMessage(string text, bool isPlayer)
        {
            // 实例化气泡场景
            var speechBubbleInstance = _speechBubbleScene.Instantiate<SpeechBubble>();
            if (speechBubbleInstance != null)
            {
                // 添加到聊天记录中
                _chatHistory.AddChild(speechBubbleInstance);
                // 设置消息内容和对齐方式
                speechBubbleInstance.SetMessage(text, isPlayer);
                // 自动滚动到底部
                ScrollToBottom();
            }
            else
            {
                GD.PrintErr("无法实例化 SpeechBubble 场景！");
            }
        }
}
