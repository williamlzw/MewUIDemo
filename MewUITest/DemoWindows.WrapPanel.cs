using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateWrapPanelWindow()
    {
        var window = new Window();
        window.Title("WrapPanel 示例");
        window.Resizable(600, 400);
        window.Padding(16);

        // 核心：WrapPanel
        var wrapPanel = new WrapPanel()
            .Orientation(Orientation.Horizontal) // 水平流式排列（默认）
            .Spacing(8)                          // 所有间隙统一使用这一个属性
            .ItemWidth(100)                      // 可选：统一每个子项的宽度
            .ItemHeight(80);                     // 可选：统一每个子项的高度

        // 动态生成 10 个带序号的色块，展示自动换行效果
        for (int i = 1; i <= 10; i++)
        {
            int index = i; // 避免闭包捕获循环变量
            var border = new Border()
                .Background(new Color((byte)(70 + index * 15), 130, 200)) // 渐变色
                .CornerRadius(6)
                .Child(
                    new Label()
                        .Text($"项目 {index}")
                        .Foreground(Color.White)
                        .FontSize(14)
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .VerticalAlignment(VerticalAlignment.Center)
                );

            wrapPanel.Children(border); // 逐个添加，超出宽度时自动换行
        }

        var infoLabel = new Label()
            .Text("水平排列 · 间距 8 · 子项统一 100×80 · 容器宽度不足时自动换行")
            .FontSize(12)
            .Foreground(Color.Gray);

        var rootPanel = new StackPanel()
            .Vertical()
            .Spacing(12)
            .Children(infoLabel, wrapPanel);

        window.Content(rootPanel);
        return window;
    }
}