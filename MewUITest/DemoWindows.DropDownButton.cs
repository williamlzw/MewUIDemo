using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateDropDownButtonWindow()
    {
        var window = new Window();
        window.Title("DropDownButton 示例");
        window.Resizable(400, 320);
        window.Padding(0);

        // 状态标签
        var statusLabel = new Label();
        statusLabel.Text("操作结果：等待选择…");
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

        // 创建 DropDownButton：点击整个按钮都会展开下拉菜单
        var dropDownButton = new DropDownButton();
        dropDownButton.Content("更多操作");
        dropDownButton.CanvasPosition(50, 80);
        dropDownButton.Size(150, 36);

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
        dropDownButton.DropDownMenu = menu;

        var canvas = new Canvas();
        canvas.Add(dropDownButton);
        canvas.Add(statusLabel);

        window.Content(canvas);
        return window;
    }
}
