
using System.IO;

using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using MewUI.Extended;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateExIconButtonWindow()
    {
        var window = new Window();

        window.Title("ExIconButton 图标按钮示例");
        window.Resizable(560, 350);
        window.Padding(0);

        // --------------------------------------------------------
        // 状态文字
        // --------------------------------------------------------

        var statusLabel = new Label()
            .Text("等待点击按钮...")
            .FontSize(14)
            .CenterHorizontal();

        // --------------------------------------------------------
        // 图标路径
        // --------------------------------------------------------

        string iconDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "assets",
            "icons");

        // --------------------------------------------------------
        // 按钮 1：主页
        // --------------------------------------------------------

        var homeButton = new ExIconButton(
            Path.Combine(iconDirectory, "0.png"),
            "主页")
            .Size(100, 70)
            .SetIconSize(30)
            .Background(Color.FromArgb(255, 255, 255, 255))
            .OnClick(() =>
            {
                statusLabel.Text = "你点击了：主页";
            });

        // --------------------------------------------------------
        // 按钮 2：设置
        // --------------------------------------------------------

        var settingsButton = new ExIconButton(
            Path.Combine(iconDirectory, "1.png"),
            "设置")
            .Size(100, 70)
            .SetIconSize(30)
            .Background(Color.FromArgb(255, 255, 255, 255))
            .OnClick(() =>
            {
                statusLabel.Text = "你点击了：设置";
            });

        // --------------------------------------------------------
        // 按钮 3：退出
        // --------------------------------------------------------

        var exitButton = new ExIconButton(
            Path.Combine(iconDirectory, "2.png"),
            "退出")
            .Size(100, 70)
            .SetIconSize(30)
            .Background(Color.FromArgb(255, 255, 255, 255))
            .OnClick(() =>
            {
                statusLabel.Text = "你点击了：退出";
            });

        // --------------------------------------------------------
        // 按钮排列
        // --------------------------------------------------------

        var buttonPanel = new StackPanel()
            .Horizontal()
            .Spacing(18)
            .CenterHorizontal()
            .Children(
                homeButton,
                settingsButton,
                exitButton
            );

        // --------------------------------------------------------
        // 主布局
        // --------------------------------------------------------

        var root = new StackPanel()
            .Vertical()
            .Spacing(25)
            .Padding(20)
            .Children(
                new Label()
                    .Text("ExIconButton 图标按钮")
                    .FontSize(20)
                    .Bold()
                    .CenterHorizontal(),

                buttonPanel,

                statusLabel
            );

        window.Content(root);

        return window;
    }
}
