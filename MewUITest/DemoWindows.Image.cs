using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Svg;
namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateImageWindow()
    {
        var window = new Window();
        window.Title("Image 示例");
        window.Resizable(600, 520);
        window.Padding(0);

        // ---- 示例 1：最简用法，从文件加载 PNG 并等比缩放 ----
        // SourceFile 是 Image 控件的便捷方法，内部会创建 ImageSource
        var basicImage = new Image()
            .SourceFile(@"assets/images/bkg.jpg")
            .StretchMode(Stretch.Uniform)   // 保持宽高比，完整显示在区域内
            .Size(260, 180);
        
        // ---- 示例 2：使用 ImageSource.FromFile + Source 属性 ----
        // 与 SourceFile 等价，但更灵活：可以提前创建 ImageSource 并在多处复用
        var logoSource = ImageSource.FromFile(@"assets/images/icon.ico");
        var logoImage = new Image()
            .Source(logoSource)
            .StretchMode(Stretch.Uniform)
            .Size(64, 64);

        // ---- 示例 3：不同 StretchMode 的对比 ----
        // Uniform:   等比缩放，完整显示（可能有留白）
        // UniformToFill: 等比缩放，填满区域（可能裁剪）
        // Fill:      非等比拉伸，填满区域（可能变形）
        // None:      原始大小，不缩放
        var uniformImage = new Image()
            .SourceFile(@"assets/images/bkg.jpg")
            .StretchMode(Stretch.Uniform)
            .Size(160, 100);

        var uniformToFillImage = new Image()
            .SourceFile(@"assets/images/bkg.jpg")
            .StretchMode(Stretch.UniformToFill)
            .Size(160, 100);

        var fillImage = new Image()
            .SourceFile(@"assets/images/bkg.jpg")
            .StretchMode(Stretch.Fill)
            .Size(160, 100);

        // ---- 示例 4：SVG 矢量图（需要 Aprillz.MewUI.Svg 扩展包）----
        // SvgImageSource 是 IVectorImageSource，Image 会按布局尺寸重新渲染矢量，
        // 因此在任何大小下都保持清晰，不会出现位图拉伸的模糊
        var svgImage = new Image
        {
            Source = SvgImageSource.FromFile(@"assets/images/niu.svg"),
            StretchMode = Stretch.Uniform,
        }.Size(48, 48);

        // ---- 布局 ----
        var stretchRow = new StackPanel()
            .Horizontal()
            .Spacing(12)
            .Children(
                new StackPanel().Spacing(4).Children(
                    new TextBlock().Text("Uniform").FontSize(12),
                    uniformImage
                ),
                new StackPanel().Spacing(4).Children(
                    new TextBlock().Text("UniformToFill").FontSize(12),
                    uniformToFillImage
                ),
                new StackPanel().Spacing(4).Children(
                    new TextBlock().Text("Fill").FontSize(12),
                    fillImage
                )
            );

        var stackPanel = new StackPanel()
            .Spacing(20)
            .Padding(24)
            .Children(
                new TextBlock().Text("基础用法（SourceFile + Uniform）").Bold(),
                basicImage,
                new TextBlock().Text("ImageSource + Source 属性").Bold(),
                logoImage,
                new TextBlock().Text("SVG渲染").Bold(),
                svgImage,
                new TextBlock().Text("StretchMode 对比").Bold(),
                stretchRow
            );

        var scrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .Content(stackPanel);

        window.Content(scrollViewer);
        return window;
    }
}