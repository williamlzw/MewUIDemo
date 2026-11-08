
using System;

using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using MewUI.Extended;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateExPropertyGridWindow()
    {
        var window = new Window();

        window.Title("ExPropertyGrid 属性框示例");
        window.Resizable(620, 580);
        window.Padding(12);

        // ========================================================
        // 1. 创建属性框
        // ========================================================

        var propertyGrid = new ExPropertyGrid()
            .SetBackgroundColor(
            Color.FromRgb(245, 246, 248))
        .SetItemBackground(
            Color.FromRgb(255, 255, 255))
        .SetItemBorder(
            Color.FromRgb(220, 223, 228),
            1,
            3)
        .SetGridBorder(
            Color.FromRgb(205, 210, 218),
            1,
            5);

        // 无边框风格
        //var propertyGrid = new ExPropertyGrid()
        //    .SetBackgroundColor(
        //        Color.FromRgb(255, 255, 255))
        //    .SetItemBackground(
        //        Color.FromRgb(255, 255, 255))
        //    .HideItemBorder()
        //    .SetGridBorder(
        //        Color.FromRgb(225, 225, 225),
        //        1,
        //        4);

        // 监听属性改变事件。
        propertyGrid.ValueChanged += (sender, e) =>
        {
            Console.WriteLine(
                $"属性改变 Key={e.Key}, Value={e.Value}");
        };

        // ========================================================
        // 2. 第一个分组
        // ========================================================

        propertyGrid.AddGroup("属性框演示", true);

        propertyGrid
            .AddText(
                "Text",
                "普通编辑框",
                "Aa测试123")

            .AddColor(
                "Color",
                "颜色框演示",
                Color.FromRgb(53, 145, 211))

            .AddSwitch(
                "Switch1",
                "选择框演示",
                true)

            .AddSwitch(
                "Switch2",
                "AAA",
                false)

            .AddSwitch(
                "Switch3",
                "BBB",
                false)

            .AddSwitch(
                "Switch4",
                "CCC",
                true)

            .AddComboBox(
                "Font",
                "组合框演示",
                new[]
                {
                    "宋体",
                    "黑体",
                    "微软雅黑",
                    "Arial"
                },
                2)

            .AddDate(
                "Date",
                "日期框演示",
                new DateTime(2026, 1, 1))

            .AddButton(
                "测试按钮",
                "按钮1",
                () =>
                {
                    Console.WriteLine("属性按钮被点击");
                })

            .AddEditButton(
                "EditButton1",
                "编辑框混合按钮1",
                "测试",
                "...",
                () =>
                {
                    Console.WriteLine("编辑框1按钮被点击");
                })

            .AddEditButton(
                "EditButton2",
                "编辑框混合按钮2",
                "随意输入",
                "选择",
                () =>
                {
                    Console.WriteLine("编辑框2按钮被点击");
                });

        // ========================================================
        // 3. 第二个分组
        // ========================================================

        propertyGrid.AddGroup("编辑框 Style 演示", true);

        propertyGrid
    // 数字模式：0-9、空格、换行
    .AddNumbersOnly(
        "Number",
        "数字模式",
        "25")

    // 字母模式：A-Z、a-z、空格、换行
    .AddLettersOnly(
        "Letters",
        "字母模式",
        "Aa")

    // 字母数字模式：A-Z、a-z、0-9、空格、换行
    .AddLettersAndNumbers(
        "LettersNumbers",
        "字母数字模式",
        "Aa123")

    // 只读模式：不需要安装输入过滤器
    .AddText(
        "ReadOnly",
        "只读模式",
        "Aa测试123",
        true);

        // ========================================================
        // 4. 第三个分组
        // ========================================================

        propertyGrid.AddGroup("占位使用", false);

        for (int i = 1; i <= 10; i++)
        {
            propertyGrid.AddText(
                $"Param{i}",
                $"参数{i}",
                $"默认值{i}");
        }

        // ========================================================
        // 5. 底部操作栏
        // ========================================================

        var toolbar = new StackPanel()
            .Horizontal()
            .Spacing(8)
            .Children(
                new Button()
                    .Content("取表项内容")
                    .OnClick(() =>
                    {
                        var value = propertyGrid.GetValue("Text");

                        Console.WriteLine(
                            $"普通编辑框对应值：{value}");
                    }),

                new Button()
                    .Content("置表项内容")
                    .OnClick(() =>
                    {
                        propertyGrid.SetValue(
                            "Text",
                            "普通编辑框");
                    }),

                new Button()
                    .Content("清空")
                    .OnClick(() =>
                    {
                        propertyGrid.Clear();
                    })
            );

        // ========================================================
        // 6. 整体布局
        // ========================================================

        var layout = new Grid()
            .Rows("*,Auto")
            .Columns("*")
            .Children(
                propertyGrid.Row(0),
                toolbar.Row(1)
            );

        window.Content(layout);

        return window;
    }
}
