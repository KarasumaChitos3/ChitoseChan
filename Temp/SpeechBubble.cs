using Godot;
using System;

public partial class SpeechBubble : Control
{
    private Label _label;
    private NinePatchRect _background;

    public override void _Ready()
    {
        _label = GetNode<Label>("MarginContainer/Label"); 
        _background = GetNode<NinePatchRect>("NinePatchRect");
    }

    /// <summary>
    /// 设置气泡消息
    /// </summary>
    /// <param name="text">消息内容</param>
    /// <param name="isPlayer">是否是玩家发送的</param>
    public void SetMessage(string text, bool isPlayer)
    {
        _label.Text = text;

        // if (isPlayer)
        // {
        //     // 玩家消息靠右
        //     SetAlignment(Alignment.End);
        //     // 可以设置不同的颜色来区分
        //     _background.Modulate = new Color(0.7f, 0.9f, 1.0f); // 淡蓝色
        // }
        // else
        // {
        //     // NPC消息靠左
        //     SetAlignment(Alignment.Begin);
        //     _background.Modulate = new Color(0.9f, 0.9f, 0.9f); // 淡灰色
        // }
    }
}
