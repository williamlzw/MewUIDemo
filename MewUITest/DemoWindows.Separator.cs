using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateSeparatorWindow()
    {
        var window = new Window();
        window.Title("Separator 示例");
        window.Resizable(560, 420);
        window.Padding(0);

        // =====================================================
        // 示例 1：水平分隔线
        // 用于纵向布局，分隔上下内容
        // =====================================================

        var horizontalSection = new StackPanel()
            .Vertical()
            .Spacing(10)
            .Children(
                new TextBlock()
                    .Text("用户信息")
                    .Bold()
                    .FontSize(16),

                new TextBlock()
                    .Text("用户名：William"),

                new TextBlock()
                    .Text("邮箱：william@example.com"),

                // 水平分隔线
                new Separator()
                    .Orientation(Orientation.Vertical),

                new TextBlock()
                    .Text("账户设置")
                    .Bold()
                    .FontSize(16),

                new TextBlock()
                    .Text("账户状态：正常"),

                new TextBlock()
                    .Text("注册时间：2026-01-01")
            );

        // =====================================================
        // 示例 2：垂直分隔线
        // 用于横向布局，分隔左右内容
        // =====================================================

        var verticalSection = new StackPanel()
            .Horizontal()
            .Spacing(12)
            .Children(
                new Button()
                    .Content("新建")
                    .Size(90, 32),

                new Button()
                    .Content("打开")
                    .Size(90, 32),

                // 垂直分隔线
                new Separator()
                    .Orientation(Orientation.Horizontal)
                    .Height(32),

                new Button()
                    .Content("保存")
                    .Size(90, 32),

                new Button()
                    .Content("退出")
                    .Size(90, 32)
            );

        // =====================================================
        // 示例 3：设置面板
        // =====================================================

        var settingsPanel = new Border()
            .Padding(16)
            .Child(
                new StackPanel()
                    .Vertical()
                    .Spacing(12)
                    .Children(
                        new TextBlock()
                            .Text("系统设置")
                            .FontSize(18)
                            .Bold(),

                        new TextBlock()
                            .Text("显示设置"),

                        new TextBlock()
                            .Text("调整窗口大小和界面显示效果。"),

                        new Separator()
                            .Orientation(Orientation.Vertical),

                        new TextBlock()
                            .Text("声音设置"),

                        new TextBlock()
                            .Text("调整系统音量和提示音。"),

                        new Separator()
                            .Orientation(Orientation.Vertical),

                        new TextBlock()
                            .Text("网络设置"),

                        new TextBlock()
                            .Text("管理网络连接和代理配置。")
                    )
            );

        // =====================================================
        // 主布局
        // =====================================================

        var stackPanel = new StackPanel()
            .Vertical()
            .Spacing(16)
            .Padding(24)
            .Children(
                new TextBlock()
                    .Text("1. 水平分隔线")
                    .Bold(),

                horizontalSection,

                new TextBlock()
                    .Text("2. 垂直分隔线")
                    .Bold(),

                verticalSection,

                new TextBlock()
                    .Text("3. 实际设置面板")
                    .Bold(),

                settingsPanel
            );

        var scrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .Content(stackPanel);

        window.Content(scrollViewer);

        return window;
    }
}