using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateExpanderWindow()
    {
        var window = new Window();
        window.Title("Expander 示例");
        window.Resizable(480, 360);
        window.Padding(0);

        // 示例 1：基础用法，默认折叠
        var expander1 = new Expander()
            .Header("点击展开高级设置")
            .Content(
                new StackPanel()
                    .Spacing(10)
                    .Padding(10)
                    .Children(
                        new CheckBox().Content("启用日志"),
                        new CheckBox().Content("启用调试模式")
                    )
            );

        // 示例 2：初始展开，包含更复杂的内容
        var expander2 = new Expander()
            .Header("系统信息")
            .IsExpanded(true) // 假设此方法控制初始展开状态
            .Content(
                new StackPanel()
                    .Spacing(8)
                    .Padding(10)
                    .Children(
                        new Label().Text("操作系统: Windows 10+"),
                        new Label().Text("运行环境: .NET NativeAOT"),
                        new Button().Content("刷新信息").Size(100, 28)
                    )
            );

        var stackPanel = new StackPanel()
            .Spacing(20)
            .Padding(20)
            .Children(expander1, expander2);

        var scrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .Content(stackPanel);

        window.Content(scrollViewer);
        return window;
    }
}