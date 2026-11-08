using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateStackPanelWindow()
    {
        var window = new Window();
        window.Title("StackPanel 示例");
        window.Resizable(640, 480);
        window.Padding(16);

        // ---------- 垂直 StackPanel（默认方向） ----------
        var verticalPanel = new StackPanel()
            .Vertical()          // 也可写 .Orientation(Orientation.Vertical)
            .Spacing(8)          // 子元素之间的间距
            .Children(
                new Label().Text("垂直 StackPanel").FontSize(16).Bold(),
                new Label().Text("子元素从上到下依次排列"),
                new Button().Content("按钮 1").Size(160, 32),
                new Button().Content("按钮 2").Size(160, 32),
                new Button().Content("按钮 3").Size(160, 32)
            );

        // ---------- 水平 StackPanel ----------
        var horizontalPanel = new StackPanel()
            .Horizontal()        // 等价于 .Orientation(Orientation.Horizontal)
            .Spacing(10)
            .Children(
                new Button().Content("保存").Size(80, 32),
                new Button().Content("取消").Size(80, 32),
                new Button().Content("应用").Size(80, 32)
            );

        // ---------- 组合：外层垂直 StackPanel 承载两个示例 ----------
        var rootPanel = new StackPanel()
            .Vertical()
            .Spacing(24)
            .Children(
                verticalPanel,
                new Border()
                    .Height(1)
                    .Background(new Color(220, 220, 220))
                    .Margin(0, 4, 0, 4),
                horizontalPanel
            );

        window.Content(rootPanel);
        return window;
    }
}