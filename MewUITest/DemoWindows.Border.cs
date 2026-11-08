using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateBorderWindow()
    {
        var window = new Window();
        window.Title("Border 示例");
        window.Resizable(500, 420);
        window.Padding(0);
        
        // 示例 1：流式 API，带主题色的圆角边框
        var themedBorder = new Border()
            .Padding(16)
            .BorderThickness(1)
            .CornerRadius(8)
            .WithTheme((t, b) => b
                .Background(t.Palette.ContainerBackground)
                .BorderBrush(t.Palette.ControlBorder))
            .Child(
                new StackPanel()
                    .Vertical()
                    .Spacing(8)
                    .Children(
                        new TextBlock().Text("流式 API 写法").Bold(),
                        new TextBlock().Text("使用 .WithTheme() 跟随系统主题。"),
                        new Button().Content("确定").Size(80, 28)
                    )
            );

        // 示例 2：对象初始化器，手动指定颜色和粗细
        var customBorder = new Border
        {
            Background = Aprillz.MewUI.Color.FromArgb(255, 250, 250, 250),
            BorderBrush = Aprillz.MewUI.Color.DodgerBlue,
            BorderThickness = 2,
            CornerRadius = 12,
            Padding = new Thickness(16),
            Child = new StackPanel()
                .Vertical()
                .Spacing(6)
                .Children(
                    new TextBlock().Text("手动设置样式").Bold(),
                    new TextBlock().Text("BorderBrush / BorderThickness / CornerRadius。"),
                    new CheckBox().Content("启用选项")
                )
        };

        // 示例 3：无边框、仅作内边距容器
        var plainBorder = new Border()
            .Background(new Aprillz.MewUI.Color(240, 240, 240))
            .Padding(12)
            .Child(new TextBlock().Text("仅用作内边距容器，没有边框。"));

        var stackPanel = new StackPanel()
            .Spacing(20)
            .Padding(20)
            .Children(themedBorder, customBorder, plainBorder);

        var scrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .Content(stackPanel);

        window.Content(scrollViewer);
        return window;
    }
}
