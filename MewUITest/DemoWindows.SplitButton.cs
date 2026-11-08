
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateSplitButtonWindow()
    {
        var window = new Window();
        window.Title("SplitButton 示例");
        window.Resizable(400, 320);
        window.Padding(0);

        // 状态标签
        var statusLabel = new Label();
        statusLabel.Text("操作结果：等待点击…");
        statusLabel.CanvasPosition(50, 240);
        statusLabel.Size(300, 24);

        // 定义菜单命令
        var saveAsCommand = new Command(
            "file.saveAs", "另存为…");

        var exportPdfCommand = new Command(
            "file.exportPdf", "导出为 PDF");

        var shareCommand = new Command(
            "file.share", "分享");

        // 注册菜单命令执行事件
        // 注册在 Window.Commands 上，菜单可以通过窗口命令作用域找到处理程序
        window.Commands.Register(saveAsCommand, () =>
        {
            statusLabel.Text("操作结果：选择了「另存为」");
        });

        window.Commands.Register(exportPdfCommand, () =>
        {
            statusLabel.Text("操作结果：选择了「导出为 PDF」");
        });

        window.Commands.Register(shareCommand, () =>
        {
            statusLabel.Text("操作结果：选择了「分享」");
        });

        // 创建 SplitButton
        var splitButton = new SplitButton();
        splitButton.Content("保存文件");
        splitButton.CanvasPosition(50, 80);
        splitButton.Size(150, 36);

        // 主按钮点击
        splitButton.Click += () =>
        {
            statusLabel.Text("操作结果：执行「保存文件」主操作");
        };

        // 创建菜单
        var menu = new Menu();
        
        menu.Items.Add(new MenuItem()
            .Text("另存为…                    ")
            .Command(saveAsCommand));

        menu.Items.Add(new MenuItem()
            .Text("导出为 PDF                 ")
            .Command(exportPdfCommand));

        menu.Items.Add(new MenuItem()
            .Text("分享                       ")
            .Command(shareCommand));

        // 绑定下拉菜单
        splitButton.DropDownMenu = menu;

        var canvas = new Canvas();
        canvas.Add(splitButton);
        canvas.Add(statusLabel);

        window.Content(canvas);

        return window;
    }
}
