using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateUniformGridWindow()
    {
        var window = new Window();
        window.Title("UniformGrid 示例");
        window.Resizable(520, 360);
        window.Padding(16);

        // 4 行 3 列，共 12 个等分单元格
        var uniformGrid = new UniformGrid()
            .Rows(4)
            .Columns(3);

        // 填充 12 个按钮，序号一目了然
        for (int i = 1; i <= 12; i++)
        {
            int index = i; // 避免闭包捕获循环变量
            var button = new Button()
                .Content($"单元格 {index}")
                .OnClick(() => Console.WriteLine($"点击了单元格 {index}"));
            uniformGrid.Children(button); // 逐个加入，自动按顺序填充
        }

        // 也可以用 Children(...) 一次性传入多个控件
        // 但为了演示自动填充顺序，这里保留逐个添加

        var infoLabel = new Label()
            .Text("4 行 × 3 列 · 所有单元格等宽等高")
            .FontSize(13);

        var panel = new StackPanel()
            .Spacing(10)
            .Children(
                infoLabel,
                uniformGrid
            );

        window.Content(panel);
        return window;
    }
}