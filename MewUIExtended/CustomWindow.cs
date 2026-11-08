using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Rendering;
using static Aprillz.MewUI.Controls.ElementExtensions;
namespace MewUI.Extended;

public class CustomWindow : Window
{
    private const double TitleBarHeight = 36;
    private const double ButtonWidth = 46;

    // =====================================================
    // 颜色配置
    // =====================================================

    // 标题栏背景
    private static readonly Color TitleBackground =
        new Color();

    // 普通按钮颜色
    private static readonly Color ButtonNormal =
        new Color();

    // 鼠标悬停颜色
    private static readonly Color ButtonHover =
        new Color(70, 78, 98);

    // 鼠标按下颜色
    private static readonly Color ButtonPressed =
        new Color(90, 100, 125);

    // 关闭按钮正常颜色
    private static readonly Color CloseNormal =
        new Color();

    // 关闭按钮悬停颜色
    private static readonly Color CloseHover =
        new Color(220, 50, 65);

    // 关闭按钮按下颜色
    private static readonly Color ClosePressed =
        new Color(170, 30, 40);

    // 标题文字颜色
    private Color TitleLabelForeground =
        new Color(255, 255, 255);

    // 按钮文字颜色
    private static readonly Color TitleForeground =
        new Color(215, 245, 245);

    // 内容背景颜色
    private static readonly Color ContentBackground =
        new Color();

    // =====================================================
    // 背景配置
    // =====================================================

    // 外部传入的背景图片
    private readonly ImageSource? _backgroundImage;

    // 图片缺省时使用的背景颜色
    private readonly Color _backgroundColor;

    // 是否显示程序图标
    private readonly bool _showProgramIcon;

    // 标题栏程序图标
    private readonly ImageSource? _programIcon;

    // =====================================================
    // 构造函数
    // =====================================================

    public CustomWindow(
        ImageSource? backgroundImage = null,
        Color? backgroundColor = null,
        Color? titleforegroundColor = null,
        bool showProgramIcon = false,
        ImageSource? programIcon = null,
        IconSource? taskbarIcon = null,
        double windowWidth = 800,
        double windowHeight = 600,
        double minWindowWidth = 0,
        double minWindowHeight = 0)
    {
        Title = string.Empty;

        // 背景配置
        _backgroundImage = backgroundImage;

        _backgroundColor = backgroundColor
            ?? new Color(150, 150, 150);

        TitleLabelForeground = titleforegroundColor
            ?? new Color(255, 255, 255);

        // 图标配置
        _showProgramIcon = showProgramIcon;
        _programIcon = programIcon;

        // 任务栏图标
        if (taskbarIcon != null)
            Icon = taskbarIcon;

        // 显示在任务栏
        ShowInTaskbar = true;

        // =================================================
        // 原生窗口配置
        // =================================================

        // 保留原生窗口边框
        Borderless = false;

        // 启用原生窗口缩放
        this.Resizable(
            windowWidth,
            windowHeight,
            minWindowWidth,
            minWindowHeight
        );

        // 将客户区延伸到标题栏区域
        // 保留原生窗口框架，由操作系统负责窗口边缘缩放
        this.ExtendClientAreaTitleBarHeight(TitleBarHeight);

        // 清除默认边距
        Padding = 0;

        // =================================================
        // 自定义窗口模板
        // =================================================

        Template = new DelegateControlTemplate<CustomWindow>(
            (window, ctx) =>
            {
                // =================================================
                // 1. 标题文字
                // =================================================

                var titleText = new TextBlock()
                    .FontSize(13)
                    .Foreground(window.TitleLabelForeground)
                    .CenterVertical();

                // 绑定窗口标题
                titleText.SetBinding(
                    TextBlock.TextProperty,
                    window,
                    TitleProperty
                );

                // =================================================
                // 2. 标题栏图标 + 标题
                // =================================================

                FrameworkElement titleContent;

                if (window._showProgramIcon &&
                    window._programIcon != null)
                {
                    // 创建程序图标
                    var icon = new Image()
                        .Source(window._programIcon)
                        .Size(18, 18)
                        .StretchMode(Stretch.Uniform)
                        .ImageScaleQuality(ImageScaleQuality.HighQuality)
                        .CenterVertical();

                    // 图标 + 标题
                    titleContent = new StackPanel()
                        .Horizontal()
                        .Spacing(8)
                        .Margin(12, 0)
                        .CenterVertical()
                        .Children(
                            icon,
                            titleText
                        );
                }
                else
                {
                    // 不显示图标，只显示标题
                    titleContent = titleText
                        .Margin(12, 0)
                        .CenterVertical();
                }

                // =================================================
                // 3. 标题栏拖动区域
                // =================================================

                var dragArea = new AlphaTextPanel
                {
                    Background = TitleBackground,
                    MinHeight = TitleBarHeight,
                    Content = titleContent
                };

                // 拖动窗口
                dragArea.OnMouseDown(e =>
                {
                    if (e.Button != MouseButton.Left)
                        return;

                    window.DragMove();

                    e.Handled = true;
                });

                // =================================================
                // 4. 最小化按钮
                // =================================================

                var minimizeButton = CreateCaptionButton(
                    "—",
                    ButtonNormal,
                    ButtonHover,
                    ButtonPressed,
                    () =>
                    {
                        window.WindowState = WindowState.Minimized;
                    }
                );

                // =================================================
                // 5. 最大化 / 还原按钮
                // =================================================

                Button maximizeButton = null!;

                maximizeButton = CreateCaptionButton(
                    "□",
                    ButtonNormal,
                    ButtonHover,
                    ButtonPressed,
                    () =>
                    {
                        ToggleMaximize(window, maximizeButton);
                    }
                );

                // =================================================
                // 6. 关闭按钮
                // =================================================

                var closeButton = CreateCaptionButton(
                    "×",
                    CloseNormal,
                    CloseHover,
                    ClosePressed,
                    () =>
                    {
                        window.Close();
                    }
                );

                // =================================================
                // 7. 标题栏按钮容器
                // =================================================

                var captionButtons = new StackPanel()
                    .Horizontal()
                    .Children(
                        minimizeButton,
                        maximizeButton,
                        closeButton
                    );

                // =================================================
                // 8. 标题栏布局
                // =================================================

                var titleBar = new AlphaTextPanel
                {
                    Background = TitleBackground,
                    MinHeight = TitleBarHeight,

                    Content = new DockPanel()
                        .LastChildFill()
                        .Children(
                            captionButtons.DockRight(),
                            dragArea
                        )
                };

                // =================================================
                // 9. 窗口内容
                // =================================================

                var appContent = new Border
                {
                    Background = ContentBackground,

                    Child = new DockPanel()
                        .LastChildFill()
                        .Children(
                            titleBar.DockTop(),

                            // 外部设置的 Window.Content
                            new ContentPresenter()
                        )
                };

                // =================================================
                // 10. 背景颜色层
                // =================================================

                var backgroundColorLayer = new Border
                {
                    Background = window._backgroundColor
                };

                // =================================================
                // 11. 背景图片层（可选）
                // =================================================

                var layers = new List<FrameworkElement>
                {
                    // 最底层始终有背景色
                    backgroundColorLayer
                };

                if (window._backgroundImage != null)
                {
                    var backgroundImage = new Image()
                        .Source(window._backgroundImage)
                        .StretchMode(Stretch.UniformToFill)
                        .ImageScaleQuality(
                            ImageScaleQuality.HighQuality
                        );

                    layers.Add(backgroundImage);
                }

                // 最上层：标题栏和主体内容
                layers.Add(appContent);

                // =================================================
                // 12. 合成窗口
                // =================================================

                var root = new Grid()
                    .Rows("*")
                    .Columns("*")
                    .Children(layers.ToArray());

                // =================================================
                // 13. 最大化 / 还原状态同步
                // =================================================

                window.OnWindowStateChanged(state =>
                {
                    UpdateMaximizeGlyph(
                        maximizeButton,
                        state == WindowState.Maximized
                    );

                    window.UpdateNativeChromeLayout(
                        titleBar,
                        captionButtons
                    );
                });

                // =================================================
                // 14. 原生标题栏和自绘按钮同步
                // =================================================

                // 窗口加载完成后更新
                window.OnLoaded(() =>
                {
                    window.UpdateNativeChromeLayout(
                        titleBar,
                        captionButtons
                    );
                });

                // 窗口尺寸变化后更新
                window.OnSizeChanged(_ =>
                {
                    window.UpdateNativeChromeLayout(
                        titleBar,
                        captionButtons
                    );
                });

                return root;
            }
        );
    }

    // =====================================================
    // 同步原生窗口标题栏
    // =====================================================

    private void UpdateNativeChromeLayout(
        AlphaTextPanel titleBar,
        StackPanel captionButtons)
    {
        // 是否支持客户区扩展到原生标题栏
        bool hasExtend = ChromeCapabilities.HasFlag(
            WindowChromeCapabilities.ExtendClientArea
        );

        // 仅在支持标题栏扩展的平台显示自绘标题栏
        titleBar.IsVisible = hasExtend;

        // 存在原生标题栏按钮时，不显示重复的自绘按钮
        captionButtons.IsVisible = !HasNativeChromeButtons;

        // 为原生标题栏按钮预留位置
        titleBar.Padding = NativeChromeButtonInset;
    }

    // =====================================================
    // 创建标题栏按钮
    // =====================================================

    private static Button CreateCaptionButton(
        string glyph,
        Color normal,
        Color hover,
        Color pressed,
        Action onClick)
    {
        bool isHovered = false;

        var button = new Button()
            .Content(
                new TextBlock()
                    .Text(glyph)
                    .FontSize(17)
                    .Foreground(TitleForeground)
                    .Center()
            )
            .Size(ButtonWidth, TitleBarHeight)
            .Padding(0)
            .Background(normal)
            .BorderThickness(0)
            .CornerRadius(0);

        // 鼠标进入
        button.OnMouseEnter(() =>
        {
            isHovered = true;
            button.Background(hover);
        });

        // 鼠标离开
        button.OnMouseLeave(() =>
        {
            isHovered = false;
            button.Background(normal);
        });

        // 鼠标按下
        button.OnMouseDown(e =>
        {
            if (e.Button == MouseButton.Left)
            {
                button.Background(pressed);
            }
        });

        // 鼠标释放
        button.OnMouseUp(e =>
        {
            button.Background(
                isHovered ? hover : normal
            );
        });

        // 点击事件
        button.OnClick(onClick);

        return button;
    }

    // =====================================================
    // 最大化 / 还原
    // =====================================================

    private static void ToggleMaximize(
        CustomWindow window,
        Button maximizeButton)
    {
        bool isMaximized =
            window.WindowState == WindowState.Maximized;

        window.WindowState = isMaximized
            ? WindowState.Normal
            : WindowState.Maximized;

        UpdateMaximizeGlyph(
            maximizeButton,
            !isMaximized
        );
    }

    // =====================================================
    // 更新最大化按钮图标
    // =====================================================

    private static void UpdateMaximizeGlyph(
        Button button,
        bool isMaximized)
    {
        button.Content(
            new TextBlock()
                .Text(isMaximized ? "❐" : "□")
                .FontSize(16)
                .Foreground(TitleForeground)
                .Center()
        );
    }

    // =====================================================
    // 标题栏文字渲染辅助控件
    // =====================================================

    // 与 MewUI 官方 NativeCustomWindow 示例一致，
    // 确保扩展标题栏区域的文字透明度渲染正确。

    private sealed class AlphaTextPanel : ContentControl
    {
        protected override void RenderSubtree(
            IGraphicsContext context)
        {
            context.EnableAlphaTextHint = true;

            try
            {
                base.RenderSubtree(context);
            }
            finally
            {
                context.EnableAlphaTextHint = false;
            }
        }
    }
}
