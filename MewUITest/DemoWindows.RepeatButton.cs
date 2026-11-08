using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateRepeatButtonWindow()
    {
        var window = new Window();
        window.Title("RepeatButton 示例");
        window.Resizable(400, 300);
        window.Padding(0);

        // 计数标签
        var countLabel = new Label();
        countLabel.Text("计数：0");
        countLabel.CanvasPosition(50, 60);
        countLabel.Size(250, 24);

        int count = 0;

        // 创建 RepeatButton
        var repeatButton = new RepeatButton();
        repeatButton.Content("按住我，数值会持续增加");
        repeatButton.CanvasPosition(50, 120);
        repeatButton.Size(220, 36);

        // 每次重复触发都会执行
        repeatButton.Click += () =>
        {
            count++;
            countLabel.Text($"计数：{count}");
        };

        var canvas = new Canvas();
        canvas.Add(countLabel);
        canvas.Add(repeatButton);

        window.Content(canvas);
        return window;
    }
}