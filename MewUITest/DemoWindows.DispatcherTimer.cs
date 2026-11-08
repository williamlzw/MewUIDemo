using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateDispatcherTimerWindow()
    {
        var window = new Window();
        window.Title("DispatcherTimer 示例");
        window.Resizable(500, 360);
        window.Padding(20);

        var timer = new DispatcherTimer(TimeSpan.FromSeconds(1));
        int tickCount = 0;

        var statusLabel = new Label()
            .Text("已停止")
            .FontSize(16);

        var countLabel = new Label()
            .Text("计时次数: 0")
            .FontSize(14);

        timer.Tick += () =>
        {
            tickCount++;
            countLabel.Text($"计时次数: {tickCount}");
            statusLabel.Text("运行中...");
        };

        var startButton = new Button()
            .Content("开始")
            .Size(120, 36);
        startButton.Click += () =>
        {
            timer.Start();
            statusLabel.Text("运行中...");
        };

        var stopButton = new Button()
            .Content("暂停")
            .Size(120, 36);
        stopButton.Click += () =>
        {
            timer.Stop();
            statusLabel.Text("已暂停");
        };

        var resetButton = new Button()
            .Content("重置")
            .Size(120, 36);
        resetButton.Click += () =>
        {
            timer.Stop();
            tickCount = 0;
            countLabel.Text("计时次数: 0");
            statusLabel.Text("已重置");
        };

        // 窗口关闭时释放计时器，避免资源泄漏
        window.OnClosed(() => timer.Dispose());

        var panel = new StackPanel()
            .Spacing(16)
            .Children(
                statusLabel,
                countLabel,
                new StackPanel()
                    .Orientation(Orientation.Horizontal)
                    .Spacing(10)
                    .Children(startButton, stopButton, resetButton)
            );

        window.Content(panel);
        return window;
    }
}