
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateButtonWindow()
    {
        var window = new Window();
        window.Title("Button 示例");
        window.Resizable(560, 480);
        window.Padding(0);

        // =====================================================
        // 示例 1：最简单的按钮
        // 使用 OnClick 扩展方法注册点击事件
        // =====================================================

        var basicButton = new Button()
            .Content("点击我")
            .Size(120, 32)
            .OnClick(() =>
            {
                Console.WriteLine("按钮被点击");
            });

        // =====================================================
        // 示例 2：直接订阅 Click 事件
        // 与 OnClick 等价
        // =====================================================

        var eventButton = new Button()
            .Content("Click 事件")
            .Size(120, 32);

        eventButton.Click += () =>
        {
            Console.WriteLine("Click 事件触发");
        };

        // =====================================================
        // 示例 3：使用 Command
        // Command 负责执行操作和控制按钮启用状态
        // =====================================================

        var saveCommand = new Command("file.save", "_保存");

        window.Commands.Register(
            saveCommand,
            () =>
            {
                Console.WriteLine("保存命令执行");
            },
            () => true
        );

        // 使用 CommandPresentationMode 自动生成按钮内容
        // 此处不设置 Content，使用 Command 自带的文本
        var commandButton = new Button()
            .Size(140, 32)
            .Command(
                saveCommand,
                presentation: CommandPresentationMode.TextAndIcon
            );

        // =====================================================
        // 示例 4：Command + 自定义按钮文本
        // 显式 Content 优先于 Command 自动生成的内容
        // =====================================================

        var customCommandButton = new Button()
            .Content("自定义保存按钮")
            .Size(140, 32)
            .Command(saveCommand);

        // =====================================================
        // 示例 5：禁用状态按钮
        // =====================================================

        var disabledButton = new Button()
            .Content("不可点击")
            .Size(120, 32)
            .IsEnabled(false);

        // =====================================================
        // 布局
        // =====================================================

        var stackPanel = new StackPanel()
            .Spacing(12)
            .Padding(24)
            .Children(
                new TextBlock()
                    .Text("示例 1：OnClick 扩展方法")
                    .Bold(),

                basicButton,

                new TextBlock()
                    .Text("示例 2：Click 事件")
                    .Bold(),

                eventButton,

                new TextBlock()
                    .Text("示例 3：Command 自动生成按钮内容")
                    .Bold(),

                commandButton,

                new TextBlock()
                    .Text("示例 4：Command 与自定义文本")
                    .Bold(),

                customCommandButton,

                new TextBlock()
                    .Text("示例 5：禁用状态")
                    .Bold(),

                disabledButton
            );

        var scrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .Content(stackPanel);

        window.Content(scrollViewer);

        return window;
    }
}
