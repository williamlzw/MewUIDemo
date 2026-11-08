
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateNumericUpDownWindow()
    {
        var window = new Window();
        window.Title("NumericUpDown 示例");
        window.Resizable(500, 400);
        window.Padding(30);

        var panel = new StackPanel()
            .Vertical()
            .Spacing(20);

        // 1. 基础用法：设置初始值和数值范围
        var numeric1 = new NumericUpDown()
            .Minimum(0)
            .Maximum(100)
            .Value(50);

        // 2. 限制范围：Minimum / Maximum
        var numeric2 = new NumericUpDown()
            .Minimum(0)
            .Maximum(100)
            .Value(75);

        // 3. 控制步长和显示精度
        var numeric3 = new NumericUpDown()
            .Minimum(0)
            .Maximum(10)
            .Value(3.14)
            .Step(0.1)
            .Format("0.##");

        // 4. 监听值变化事件
        var numeric4 = new NumericUpDown()
            .Minimum(0)
            .Maximum(100)
            .Value(10);

        numeric4.OnValueChanged(newValue =>
        {
            Console.WriteLine($"NumericUpDown 值变为: {newValue}");
        });

        panel.Children(
            new TextBlock()
                .Text("基础用法 (初始值 50，范围 0-100)")
                .FontSize(13),
            numeric1,

            new TextBlock()
                .Text("范围限制 (0-100，当前 75)")
                .FontSize(13),
            numeric2,

            new TextBlock()
                .Text("格式控制 (范围 0-10，步长 0.1，最多两位小数)")
                .FontSize(13),
            numeric3,

            new TextBlock()
                .Text("值变化事件 (控制台输出，范围 0-100)")
                .FontSize(13),
            numeric4
        );

        window.Content(
            new ScrollViewer()
                .VerticalScroll(ScrollMode.Auto)
                .Content(panel)
        );

        return window;
    }
}
