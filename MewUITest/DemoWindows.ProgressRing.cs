
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateProgressRingWindow()
    {
        var window = new Window();
        window.Title("ProgressRing 示例");
        window.Resizable(500, 400);
        window.Padding(30);

        var panel = new StackPanel()
            .Vertical()
            .Spacing(12);

        // 1. 环形加载动画：持续旋转
        var ring1 = new ProgressRing()
            .Width(40)
            .Height(40)
            .IsActive(true);

        // 2. 停止状态：不显示旋转动画
        var ring2 = new ProgressRing()
            .Width(40)
            .Height(40)
            .IsActive(false);

        panel.Children(
            new TextBlock()
                .Text("1. 环形加载动画（IsActive = true）")
                .FontSize(14),

            ring1,

            new TextBlock()
                .Text("2. 停止动画（IsActive = false）")
                .FontSize(14),

            ring2

        );

        window.Content(
            new ScrollViewer()
                .VerticalScroll(ScrollMode.Auto)
                .Content(panel)
        );

        return window;
    }
}
