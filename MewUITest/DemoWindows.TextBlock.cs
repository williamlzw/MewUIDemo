using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateTextBlockWindow()
    {
        var window = new Window();
        window.Title("TextBlock 示例");
        window.Resizable(400, 200);
        window.Padding(0);

        // 创建 TextBlock
        var textBlock = new TextBlock();
        textBlock.CanvasPosition(50, 60);
        textBlock.Size(320, 100);
        textBlock.FontSize(16);

        // 通过 Inlines 添加不同样式的 Run
        textBlock.Inlines.Add(new Run("这是普通文本。"));
        textBlock.Inlines.Add(new Run("这是粗体文本。") { FontWeight = FontWeight.Bold });
        textBlock.Inlines.Add(new Run("这是斜体文本。") { Italic = true });
        textBlock.Inlines.Add(new Run("这是红色文本。") { Foreground = new Color(255, 0, 0) });

        var canvas = new Canvas();
        canvas.Add(textBlock);

        window.Content(canvas);
        return window;
    }
}
