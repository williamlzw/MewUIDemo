using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateProgressBarWindow()
    {
        var window = new Window();
        window.Title("ProgressBar 示例");
        window.Resizable(500, 400);
        window.Padding(30);

        var panel = new StackPanel()
            .Vertical()
            .Spacing(20);

        // 1. 确定性进度：直接设置 Value（默认范围 0–100）
        var progress1 = new ProgressBar()
            .Value(35);

        // 2. 确定性进度：显式设定范围
        var progress2 = new ProgressBar()
            .Minimum(0)
            .Maximum(200)
            .Value(120);

        // 3. 不确定状态（加载中）：不显示具体进度，仅播放循环动画
        var progress3 = new ProgressBar()
            .IsIndeterminate();

        // 4. 禁用状态
        var progress4 = new ProgressBar()
            .Value(60)
            .Disable();

        panel.Children(
            new TextBlock().Text("确定性进度（Value = 35）").FontSize(13),
            progress1,
            new TextBlock().Text("自定义范围（0–200，Value = 120）").FontSize(13),
            progress2,
            new TextBlock().Text("不确定状态（加载中）").FontSize(13),
            progress3,
            new TextBlock().Text("禁用状态").FontSize(13),
            progress4
        );

        window.Content(new ScrollViewer().VerticalScroll(ScrollMode.Auto).Content(panel));
        return window;
    }
}