using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateScrollViewerWindow()
    {
        var window = new Window();
        window.Title("ScrollViewer 示例");
        window.Resizable(560, 400);
        window.Padding(0);

        // ---- 示例 1：垂直滚动 ----
        // 构造一个高度明显超过窗口的内容
        var tallContent = new StackPanel()
            .Spacing(10)
            .Padding(16);

        for (int i = 1; i <= 30; i++)
        {
            tallContent.Children(
                new TextBlock().Text($"第 {i} 行内容 — 当内容高度超出可视区域时，垂直滚动条自动出现。")
            );
        }

        var verticalScrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)   // 内容超出时自动显示纵向滚动条
            .HorizontalScroll(ScrollMode.Disabled) // 禁用横向滚动
            .Content(tallContent);

        // ---- 示例 2：水平滚动 ----
        var wideContent = new StackPanel()
            .Horizontal()                      // 横向排列
            .Spacing(12)
            .Padding(16);

        for (int i = 1; i <= 20; i++)
        {
            wideContent.Children(
                new Border()
                    .Padding(20)
                    .Background(new Color(240, 248, 255))
                    .Child(new TextBlock().Text($"卡片 {i}"))
            );
        }

        var horizontalScrollViewer = new ScrollViewer()
            .HorizontalScroll(ScrollMode.Auto) // 内容超出时自动显示横向滚动条
            .VerticalScroll(ScrollMode.Disabled)
            .Content(wideContent);

        // ---- 示例 3：双向滚动 + ScrollMode.Visible 强制显示滚动条 ----
        var gridContent = new StackPanel()
            .Spacing(10)
            .Padding(16);

        for (int row = 1; row <= 25; row++)
        {
            var rowPanel = new StackPanel()
                .Horizontal()
                .Spacing(10)
                .Children(
                    new TextBlock().Text($"行 {row}").Width(60),
                    new TextBlock().Text("这是一段较长的说明文字，用于撑开横向宽度，使双向滚动成为可能。这是一段较长的说明文字，用于撑开横向宽度，使双向滚动成为可能")
                );
            gridContent.Children(rowPanel);
        }

        var bothScrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Visible)   // 无论是否需要，始终显示纵向滚动条
            .HorizontalScroll(ScrollMode.Auto)
            .Content(gridContent);

        // ---- 布局：用 TabControl 分别展示三种滚动模式 ----
        var tabControl = new TabControl()
            .TabItems(
                new TabItem().Header("垂直滚动").Content(verticalScrollViewer),
                new TabItem().Header("水平滚动").Content(horizontalScrollViewer),
                new TabItem().Header("双向滚动").Content(bothScrollViewer)
            );

        window.Content(tabControl);
        return window;
    }
}
