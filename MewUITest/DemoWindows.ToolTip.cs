using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateToolTipWindow()
    {
        var window = new Window();
        window.Title("ToolTip 示例");
        window.Resizable(520, 360);
        window.Padding(0);

        // ---- 示例 1：基础文本提示 ----
        var basicButton = new Button()
            .Content("悬停查看提示")
            .Size(140, 32)
            .ToolTip("这是一个基础 ToolTip，鼠标悬停时显示。");

        // ---- 示例 2：为 TextBox 添加提示 ----
        var textBox = new TextBox()
            .Width(240)
            .ToolTip("请输入用户名（支持字母、数字和下划线）");

        // ---- 示例 3：ToolTip 内容可以是更丰富的控件 ----
        // 如果 ToolTip 支持富内容（通常通过 Control 或 string 重载），
        // 可以传入一个布局容器来展示更复杂的信息。
        var richToolTipContent = new StackPanel()
            .Spacing(4)
            .Padding(8)
            .Children(
                new TextBlock().Text("快捷键提示").Bold(),
                new TextBlock().Text("Ctrl + S  保存"),
                new TextBlock().Text("Ctrl + Z  撤销")
            );

        var richToolTipButton = new Button()
            .Content("复杂提示")
            .Size(140, 32)
            .ToolTip(richToolTipContent); // 假设存在接受 Control 的重载

        // ---- 布局 ----
        var stackPanel = new StackPanel()
            .Spacing(16)
            .Padding(24)
            .Children(
                new TextBlock().Text("Button + 基础 ToolTip").Bold(),
                basicButton,
                new TextBlock().Text("TextBox + ToolTip").Bold(),
                textBox,
                new TextBlock().Text("Button + 富内容 ToolTip").Bold(),
                richToolTipButton
            );

        var scrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .Content(stackPanel);

        window.Content(scrollViewer);
        return window;
    }
}