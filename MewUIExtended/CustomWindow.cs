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
    // 平台判断
    // =====================================================

    // 判断是否使用原生窗口框架 (Windows / macOS 支持客户区延伸)
    // Linux (X11/Wayland) 不支持，必须使用无边框+透明背景
    private static bool UsesNativeWindowFrame
    {
        get
        {
#if MEWUI_WIN32 || MEWUI_MACOS
            return true;
#elif MEWUI_LINUX || MEWUI_X11
            return false;
#else
            return System.OperatingSystem.IsWindows() || System.OperatingSystem.IsMacOS();
#endif
        }
    }

    // =====================================================
    // 颜色配置
    // =====================================================

    // 标题栏背景
    private static readonly Color TitleBackground = new Color();
    private static readonly Color ButtonNormal = new Color();
    private static readonly Color ButtonHover = new Color(70, 78, 98);
    private static readonly Color ButtonPressed = new Color(90, 100, 125);
    private static readonly Color CloseNormal = new Color();
    private static readonly Color CloseHover = new Color(220, 50, 65);
    private static readonly Color ClosePressed = new Color(170, 30, 40);
    private Color TitleLabelForeground = new Color(255, 255, 255);
    private static readonly Color TitleForeground = new Color(215, 245, 245);
    private static readonly Color ContentBackground = new Color();

    // =====================================================
    // 背景配置
    // =====================================================

    private readonly ImageSource? _backgroundImage;
    private readonly Color _backgroundColor;
    private readonly bool _showProgramIcon;
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
        _backgroundImage = backgroundImage;
        _backgroundColor = backgroundColor ?? new Color(150, 150, 150);
        TitleLabelForeground = titleforegroundColor ?? new Color(255, 255, 255);
        _showProgramIcon = showProgramIcon;
        _programIcon = programIcon;

        if (taskbarIcon != null) Icon = taskbarIcon;
        ShowInTaskbar = true;
         
        this.Resizable(windowWidth, windowHeight, minWindowWidth, minWindowHeight);
        // =================================================
        // 原生窗口配置 
        // =================================================
        if (UsesNativeWindowFrame)
        {
            Borderless = false;
            this.ExtendClientAreaTitleBarHeight(TitleBarHeight);
        }
        else
        {
            // Linux/X11 必须使用无边框+透明背景
            this.AllowsTransparency(true);
            this.Borderless(true);
            this.Background = new Color(0, 0, 0, 0);
        }

        
        Padding = 0;

        // =================================================
        // 自定义窗口模板
        // =================================================
        Template = new DelegateControlTemplate<CustomWindow>(
            (window, ctx) =>
            {
                var titleText = new TextBlock()
                    .FontSize(13)
                    .Foreground(window.TitleLabelForeground)
                    .CenterVertical();

                titleText.SetBinding(TextBlock.TextProperty, window, TitleProperty);

                FrameworkElement titleContent;
                if (window._showProgramIcon && window._programIcon != null)
                {
                    var icon = new Image()
                        .Source(window._programIcon)
                        .Size(18, 18)
                        .StretchMode(Stretch.Uniform)
                        .ImageScaleQuality(ImageScaleQuality.HighQuality)
                        .CenterVertical();

                    titleContent = new StackPanel()
                        .Horizontal()
                        .Spacing(8)
                        .Margin(12, 0)
                        .CenterVertical()
                        .Children(icon, titleText);
                }
                else
                {
                    titleContent = titleText.Margin(12, 0).CenterVertical();
                }

                var dragArea = new AlphaTextPanel
                {
                    Background = TitleBackground,
                    MinHeight = TitleBarHeight,
                    Content = titleContent
                };

                dragArea.OnMouseDown(e =>
                {
                    if (e.Button != MouseButton.Left) return;
                    window.DragMove();
                    e.Handled = true;
                });

                // 4. 最小化按钮 (修复：改用 Minimize() 方法)
                var minimizeButton = CreateCaptionButton(
                    "—", ButtonNormal, ButtonHover, ButtonPressed,
                    () => window.Minimize()
                );

                // 5. 最大化 / 还原按钮
                Button maximizeButton = null!;
                maximizeButton = CreateCaptionButton(
                    "□", ButtonNormal, ButtonHover, ButtonPressed,
                    () => ToggleMaximize(window, maximizeButton)
                );

                // 6. 关闭按钮
                var closeButton = CreateCaptionButton(
                    "×", CloseNormal, CloseHover, ClosePressed,
                    () => window.Close()
                );

                var captionButtons = new StackPanel()
                    .Horizontal()
                    .Children(minimizeButton, maximizeButton, closeButton);

                var titleBar = new AlphaTextPanel
                {
                    Background = TitleBackground,
                    MinHeight = TitleBarHeight,
                    Content = new DockPanel()
                        .LastChildFill()
                        .Children(captionButtons.DockRight(), dragArea)
                };

                var appContent = new Border
                {
                    Background = ContentBackground,
                    Child = new DockPanel()
                        .LastChildFill()
                        .Children(titleBar.DockTop(), new ContentPresenter())
                };

                var backgroundColorLayer = new Border
                {
                    Background = window._backgroundColor,
                    CornerRadius = UsesNativeWindowFrame ? 0 : 8
                };

                var layers = new List<FrameworkElement> { backgroundColorLayer };
                if (window._backgroundImage != null)
                {
                    var bgImg = new Image()
                        .Source(window._backgroundImage)
                        .StretchMode(Stretch.UniformToFill)
                        .ImageScaleQuality(ImageScaleQuality.HighQuality);
                    layers.Add(bgImg);
                }
                layers.Add(appContent);

                var root = new Grid()
                    .Rows("*")
                    .Columns("*")
                    .ClipToBounds(true)
                    .Children(layers.ToArray());

                window.OnWindowStateChanged(state =>
                {
                    UpdateMaximizeGlyph(maximizeButton, state == WindowState.Maximized);
                    window.UpdateNativeChromeLayout(titleBar, captionButtons);
                });

                window.OnLoaded(() => window.UpdateNativeChromeLayout(titleBar, captionButtons));
                window.OnSizeChanged(_ => window.UpdateNativeChromeLayout(titleBar, captionButtons));

                return root;
            }
        );
    }

    private void UpdateNativeChromeLayout(AlphaTextPanel titleBar, StackPanel captionButtons)
    {
        if (UsesNativeWindowFrame)
        {
            bool hasExtend = ChromeCapabilities.HasFlag(WindowChromeCapabilities.ExtendClientArea);
            titleBar.IsVisible = hasExtend;
            captionButtons.IsVisible = !HasNativeChromeButtons;
            titleBar.Padding = NativeChromeButtonInset;
        }
        else
        {
            titleBar.IsVisible = true;
            captionButtons.IsVisible = true;
            titleBar.Padding = new Thickness(0);
        }
    }

    private static Button CreateCaptionButton(string glyph, Color normal, Color hover, Color pressed, Action onClick)
    {
        bool isHovered = false;
        var button = new Button()
            .Content(new TextBlock().Text(glyph).FontSize(17).Foreground(TitleForeground).Center())
            .Size(ButtonWidth, TitleBarHeight)
            .Padding(0)
            .Background(normal)
            .BorderThickness(0)
            .CornerRadius(0);

        button.OnMouseEnter(() => { isHovered = true; button.Background(hover); });
        button.OnMouseLeave(() => { isHovered = false; button.Background(normal); });
        button.OnMouseDown(e => { if (e.Button == MouseButton.Left) button.Background(pressed); });
        button.OnMouseUp(e => { button.Background(isHovered ? hover : normal); });
        button.OnClick(onClick);
        return button;
    }

    // =====================================================
    // 最大化 / 还原 (核心修复区域)
    // =====================================================
    private void ToggleMaximize(CustomWindow window, Button maximizeButton)
    {
        if(!this.CanMaximize)
        {
            return;
        }
        // 修复：在 Linux 下，直接赋值 WindowState 可能无法触发 X11 WM 的状态变更。
        // 必须调用框架提供的 Maximize() 和 Restore() 方法，它们内部包含了针对透明无边框窗口的平台适配逻辑。
        if (window.WindowState == WindowState.Maximized)
        {
            window.Restore();
        }
        else
        {
            window.Maximize();
        }

        UpdateMaximizeGlyph(maximizeButton, window.WindowState == WindowState.Maximized);
    }

    private static void UpdateMaximizeGlyph(Button button, bool isMaximized)
    {
        button.Content(
            new TextBlock()
                .Text(isMaximized ? "❐" : "□")
                .FontSize(16)
                .Foreground(TitleForeground)
                .Center()
        );
    }

    private sealed class AlphaTextPanel : ContentControl
    {
        protected override void RenderSubtree(IGraphicsContext context)
        {
            context.EnableAlphaTextHint = true;
            try { base.RenderSubtree(context); }
            finally { context.EnableAlphaTextHint = false; }
        }
    }
}