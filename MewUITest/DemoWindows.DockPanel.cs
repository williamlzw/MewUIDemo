using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateDockPanelWindow()
    {
        var window = new Window();
        window.Title("DockPanel 示例");
        window.Resizable(800, 600);
        window.Padding(0);

        // 模拟顶部菜单栏
        var topMenu = new Border()
            .Background(new Color(45, 48, 60))
            .Padding(12, 8)
            .Child(new Label().Text("文件  编辑  视图  帮助").Foreground(Color.White));

        // 模拟底部状态栏
        var statusBar = new Border()
            .Background(new Color(35, 38, 50))
            .Padding(12, 6)
            .Child(new Label().Text("就绪").Foreground(Color.LightGray));

        // 模拟左侧导航树
        var leftNav = new Border()
            .Background(new Color(245, 246, 250))
            .Padding(10)
            .Child(new Label().Text("导航树"));

        // 模拟右侧内容区（将填充剩余空间）
        var contentArea = new Border()
            .Background(Color.White)
            .Padding(20)
            .Child(new Label().Text("主内容区域\n这里将填充 DockPanel 的剩余空间").FontSize(16));

        // 核心：DockPanel
        var dockPanel = new DockPanel()
            .LastChildFill(true) // 最后一个子元素填充剩余空间（默认即为 true）
            .Children(
                topMenu.DockTo(Dock.Top),          // 顶部停靠
                statusBar.DockTo(Dock.Bottom),     // 底部停靠
                leftNav.DockTo(Dock.Left).Width(200), // 左侧停靠，固定宽度
                contentArea                        // 最后一个子元素自动填充
            );

        window.Content(dockPanel);
        return window;
    }
}