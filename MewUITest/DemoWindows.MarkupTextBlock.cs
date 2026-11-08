using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateMarkupTextBlockWindow()
    {
        var window = new Window();
        window.Title("MarkupTextBlock 示例");
        window.Resizable(420, 220);
        window.Padding(0);

        // 创建 MarkupTextBlock
        var markupBlock = new MarkupTextBlock();
        markupBlock.CanvasPosition(50, 60);
        markupBlock.Size(340, 120);
        markupBlock.FontSize = 16;
        markupBlock.TextWrapping = TextWrapping.Wrap; // 长文本自动换行

        // 使用 HTML 风格的标记定义富文本
        markupBlock.Markup(
            "这是普通文本。 " +
            "<b>这是粗体。</b> " +
            "<i>这是斜体。</i> " +
            "<font color=\"red\">这是红色文本。</font> " +
            "<u>这是下划线。</u>"
        );

        var canvas = new Canvas();
        canvas.Add(markupBlock);

        window.Content(canvas);
        return window;
    }
}