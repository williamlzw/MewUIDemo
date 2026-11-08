using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateToolBarWindow()
    {
        var window = new Window();
        window.Title("ToolBar 示例");
        window.Resizable(640, 420);
        window.Padding(0);

        // =====================================================
        // 1. 定义 Command
        // =====================================================

        var saveCommand = new Command(
            "file.save", "保存");

        var openCommand = new Command(
            "file.open", "打开");

        var undoCommand = new Command(
            "edit.undo", "撤销");

        var redoCommand = new Command(
            "edit.redo", "重做");

        // =====================================================
        // 2. 构建 ToolBar
        // =====================================================

        var toolBar = new ToolBar()
            .Band(

                // 第一组：文件操作
                new ToolBarGroup()
                    .Item(saveCommand)
                    .Item(openCommand),

                // 第二组：编辑操作
                new ToolBarGroup()
                    .Item(undoCommand)
                    .Item(redoCommand)

            );

        // =====================================================
        // 3. 注册 Command 处理器
        // =====================================================

        toolBar.Commands.Register(
            saveCommand,
            () => Console.WriteLine("保存文档"),
            () => true
        );

        toolBar.Commands.Register(
            openCommand,
            () => Console.WriteLine("打开文档"),
            () => true
        );

        toolBar.Commands.Register(
            undoCommand,
            () => Console.WriteLine("撤销"),
            () => true
        );

        toolBar.Commands.Register(
            redoCommand,
            () => Console.WriteLine("重做"),
            () => true
        );

        // =====================================================
        // 4. 主内容区域
        // =====================================================

        var contentArea = new Border()
            .Padding(16)
            .Child(
                new TextBlock()
                    .Text("ToolBar 停靠在窗口顶部。\n\n可以点击保存、打开、撤销和重做按钮。")
            );

        // =====================================================
        // 5. DockPanel 布局
        // =====================================================

        var dockPanel = new DockPanel()
            .LastChildFill(true)
            .Children(
                toolBar.DockTop(),
                contentArea
            );

        window.Content(dockPanel);

        return window;
    }
}