using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateGroupBoxWindow()
    {
        var window = new Window();
        window.Title("GroupBox 示例");
        window.Resizable(480, 360);
        window.Padding(0);

        // 第一个 GroupBox：包含基本控件
        var groupBox1 = new GroupBox()
            .Header("基本信息")
            .Content(
                new StackPanel()
                    .Spacing(10)
                    .Children(
                        new Label().Text("用户名："),
                        new TextBox().Width(200),
                        new Button().Content("保存").Size(80, 28)
                    )
            );

        // 第二个 GroupBox：演示多个 GroupBox 并列
        var groupBox2 = new GroupBox()
            .Header("高级设置")
            .Content(
                new CheckBox().Content("启用日志")
            );

        // 垂直排列两个 GroupBox
        var stackPanel = new StackPanel()
            .Spacing(20)
            .Padding(20)
            .Children(groupBox1, groupBox2);

        // 使用 ScrollViewer 防止内容超出窗口
        var scrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .Content(stackPanel);

        window.Content(scrollViewer);
        return window;
    }
}