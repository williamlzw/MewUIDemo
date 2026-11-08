
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

/// <summary>
/// 信息卡片自定义控件。
/// 使用 UserControl 将标题、分隔线和主体内容封装为一个可复用组件。
/// </summary>
public class InfoCard : UserControl
{
    private readonly TextBlock _titleBlock;
    private readonly ContentControl _contentHost;

    public InfoCard()
    {
        // 初始化标题
        _titleBlock = new TextBlock()
            .FontSize(18)
            .Bold();

        // 初始化内容宿主
        _contentHost = new ContentControl();

        // 构建 UserControl 内部布局
        Content =
            new Border()
                .Padding(16)
                .CornerRadius(8)
                .Background(new Color(248, 249, 250))
                .Child(
                    new StackPanel()
                        .Spacing(12)
                        .Children(
                            _titleBlock,
                            new Separator(),
                            _contentHost
                        )
                );
    }

    /// <summary>
    /// 设置卡片标题。
    /// </summary>
    public InfoCard Title(string title)
    {
        _titleBlock.Text(title ?? string.Empty);
        return this;
    }

    /// <summary>
    /// 设置卡片主体内容。
    ///
    /// 使用 Element 而不是 Control，
    /// 使其同时支持普通控件和布局面板。
    /// </summary>
    public InfoCard Body(Element content)
    {
        ArgumentNullException.ThrowIfNull(content);

        _contentHost.Content(content);

        return this;
    }
}

static partial class DemoWindows
{
    public static Window CreateUserControlWindow()
    {
        var window = new Window();
        window.Title("UserControl 示例");
        window.Resizable(560, 420);
        window.Padding(0);

        // =====================================================
        // 示例 1：普通文本内容
        // =====================================================

        var card1 = new InfoCard()
            .Title("项目状态")
            .Body(
                new TextBlock()
                    .Text("所有系统运行正常。")
            );

        // =====================================================
        // 示例 2：复杂布局内容
        // =====================================================

        var card2 = new InfoCard()
            .Title("快速操作")
            .Body(
                new StackPanel()
                    .Spacing(8)
                    .Children(
                        new Button()
                            .Content("开始同步")
                            .Size(120, 30),

                        new Button()
                            .Content("打开设置")
                            .Size(120, 30)
                    )
            );

        // =====================================================
        // 示例 3：嵌套控件内容
        // =====================================================

        var card3 = new InfoCard()
            .Title("用户信息")
            .Body(
                new StackPanel()
                    .Spacing(8)
                    .Children(
                        new TextBlock().Text("用户名：William"),
                        new TextBlock().Text("账号状态：正常"),
                        new TextBlock().Text("用户等级：VIP")
                    )
            );

        // =====================================================
        // 窗口布局
        // =====================================================

        var stackPanel = new StackPanel()
            .Spacing(20)
            .Padding(20)
            .Children(
                new TextBlock()
                    .Text("UserControl 将重复使用的 UI 组合封装成独立组件。")
                    .Bold(),

                card1,
                card2,
                card3
            );

        var scrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .Content(stackPanel);

        window.Content(scrollViewer);

        return window;
    }
}
