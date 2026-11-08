using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateMenuBarWindow()
    {
        var window = new Window();
        window.Title("MenuBar 示例");
        window.Resizable(600, 400);
        window.Padding(0);

        // =================================================
        // 命令注册辅助方法
        // =================================================
        Command MenuCommand(
            string id,
            string text,
            Action execute,
            KeyGesture? shortcut = null)
        {
            var command = new Command($"menubar.{id}", text);

            window.Commands.Register(command, execute);

            if (shortcut is KeyGesture gesture)
                window.InputMap.Map(command, gesture);

            return command;
        }

        // =================================================
        // 1. 文件菜单
        // =================================================
        var fileMenu = new Menu()
            .Item(MenuCommand(
                "new",
                "新建",
                () => Console.WriteLine("新建")))

            .Item(MenuCommand(
                "open",
                "打开...",
                () => Console.WriteLine("打开")))

            .Separator()

            .Item(MenuCommand(
                "exit",
                "退出",
                () => Application.Shutdown()));

        // =================================================
        // 2. 最近打开子菜单
        // =================================================
        var recentMenu = new Menu()
            .Apply(menu =>
            {
                for (char c = 'a'; c <= 'e'; c++)
                {
                    string fileName = $"{c}.txt";

                    menu.Item(MenuCommand(
                        $"recent.{c}",
                        fileName,
                        () => Console.WriteLine($"打开 {fileName}")));
                }
            });

        // =================================================
        // 3. 编辑菜单
        // =================================================
        var editMenu = new Menu()
            .Item(MenuCommand(
                "copy",
                "复制",
                () => Console.WriteLine("复制"),
                new KeyGesture(Key.C, ModifierKeys.Primary)))

            .Item(MenuCommand(
                "paste",
                "粘贴",
                () => Console.WriteLine("粘贴"),
                new KeyGesture(Key.V, ModifierKeys.Primary)))

            .Separator()

            .SubMenu("最近打开", recentMenu);

        // =================================================
        // 4. 视图菜单
        // =================================================
        var showGrid = new ObservableValue<bool>(true);

        var gridStatus = new ObservableValue<string>(
            "网格显示状态：开启");

        var viewMenu = new Menu()
            .Item(MenuCommand(
                "toggle-grid",
                "显示网格",
                () =>
                {
                    showGrid.Value = !showGrid.Value;

                    gridStatus.Value =
                        $"网格显示状态：{(showGrid.Value ? "开启" : "关闭")}";

                    Console.WriteLine(gridStatus.Value);
                }));

        // =================================================
        // 5. 帮助菜单
        // =================================================
        var docsMenu = new Menu()
            .Item(MenuCommand(
                "online-docs",
                "在线文档",
                () => Console.WriteLine("打开在线文档")))

            .Item(MenuCommand(
                "samples",
                "示例代码",
                () => Console.WriteLine("查看示例代码")));

        var helpMenu = new Menu()
            .SubMenu("文档", docsMenu)

            .Separator()

            .Item(MenuCommand(
                "about",
                "关于",
                () => Console.WriteLine("关于 MewUI")));

        // =================================================
        // 6. MenuBar
        // =================================================
        var menuBar = new MenuBar()
            .Items(
                new MenuItem("_文件").Menu(fileMenu),
                new MenuItem("_编辑").Menu(editMenu),
                new MenuItem("_视图").Menu(viewMenu),
                new MenuItem("_帮助").Menu(helpMenu)
            );

        // =================================================
        // 7. 主内容区域
        // =================================================
        var contentArea = new Border()
            .Padding(16)
            .Child(
                new StackPanel()
                    .Spacing(12)
                    .Children(
                        new TextBlock()
                            .Text("这里是主内容区域。"),

                        new TextBlock()
                            .BindText(gridStatus)
                    )
            );

        // =================================================
        // 8. DockPanel 布局
        // =================================================
        var dockPanel = new DockPanel()
            .LastChildFill(true)
            .Children(
                menuBar.DockTop(),
                contentArea
            );

        window.Content(dockPanel);

        return window;
    }
}