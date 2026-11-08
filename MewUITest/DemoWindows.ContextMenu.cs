using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateContextMenuWindow()
    {
        var window = new Window();
        window.Title("ContextMenu 示例");
        window.Resizable(560, 440);
        window.Padding(0);

        // =====================================================
        // 命令注册辅助方法
        // =====================================================
        Command CreateMenuCommand(
            string id,
            string text,
            Action execute)
        {
            var command = new Command($"contextmenu.{id}", text);

            window.Commands.Register(command, execute);

            return command;
        }

        // =====================================================
        // 示例 1：TextBox 右键菜单
        // =====================================================
        var editContextMenu = new ContextMenu()
            .Item(StandardCommands.Cut)
            .Item(StandardCommands.Copy)
            .Item(StandardCommands.Paste)
            .Separator()
            .Item(StandardCommands.SelectAll);

        var textBox = new TextBox()
            .Text("在这个文本框内右键点击，会弹出编辑菜单。")
            .Width(300)
            .ContextMenu(editContextMenu);

        // =====================================================
        // 示例 2：DropDownButton 左键下拉菜单
        // =====================================================

        var exportCommand = CreateMenuCommand(
            "export",
            "导出 PDF",
            () => Console.WriteLine("导出 PDF"));

        var printCommand = CreateMenuCommand(
            "print",
            "打印",
            () => Console.WriteLine("打印"));

        var button = new DropDownButton
        {
            Content = new TextBlock().Text("更多操作"),

            DropDownMenu = new Menu()
                .Item(exportCommand)
                .Item(printCommand)
        };

        // =====================================================
        // 示例 3：Border 右键菜单
        // =====================================================

        var showGrid = new ObservableValue<bool>(false);

        var gridStatus = new ObservableValue<string>(
            "网格显示状态：关闭");

        var containerContextMenu = new ContextMenu()
            .Item(CreateMenuCommand(
                "toggle-grid",
                "切换网格",
                () =>
                {
                    showGrid.Value = !showGrid.Value;

                    gridStatus.Value =
                        $"网格显示状态：{(showGrid.Value ? "开启" : "关闭")}";

                    Console.WriteLine(gridStatus.Value);
                }))
            .Separator()
            .Item(CreateMenuCommand(
                "refresh",
                "刷新",
                () => Console.WriteLine("刷新")));

        var border = new Border()
            .Padding(24)
            .Background(new Color(245, 245, 245))
            .Child(
                new StackPanel()
                    .Spacing(10)
                    .Children(
                        new TextBlock()
                            .Text("在这个灰色区域内右键点击")
                            .Bold(),

                        new TextBlock()
                            .BindText(gridStatus)
                    )
            )
            .ContextMenu(containerContextMenu);

        // =====================================================
        // 布局
        // =====================================================

        var stackPanel = new StackPanel()
            .Spacing(16)
            .Padding(20)
            .Children(
                new TextBlock()
                    .Text("TextBox + 右键编辑菜单")
                    .Bold(),

                textBox,

                new TextBlock()
                    .Text("DropDownButton + 左键下拉菜单")
                    .Bold(),

                button,

                new TextBlock()
                    .Text("Border + 右键自定义菜单")
                    .Bold(),

                border
            );

        var scrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .Content(stackPanel);

        window.Content(scrollViewer);

        return window;
    }
}