using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateButtonGroupWindow()
    {
        var window = new Window();
        window.Title("ButtonGroup 示例");
        window.Resizable(460, 280);
        window.Padding(12);

        var statusLabel = new Label()
            .Text("请选择一个按钮");

        // 1. 创建命令
        var newCommand = new Command("file.new", "新建");
        var editCommand = new Command("file.edit", "编辑");
        var deleteCommand = new Command("file.delete", "删除");
        var refreshCommand = new Command("file.refresh", "刷新");

        // 2. 注册命令执行事件
        window.Commands.Register(newCommand, () =>
        {
            statusLabel.Text("执行新建操作");
        });

        window.Commands.Register(editCommand, () =>
        {
            statusLabel.Text("执行编辑操作");
        });

        window.Commands.Register(deleteCommand, () =>
        {
            statusLabel.Text("执行删除操作");
        });

        window.Commands.Register(refreshCommand, () =>
        {
            statusLabel.Text("执行刷新操作");
        });

        // 3. 创建命令集合
        var commands = new[]
        {
            newCommand,
            editCommand,
            deleteCommand,
            refreshCommand
        };

        // 4. 创建 ButtonGroup
        var buttonGroup = new ButtonGroup()
            .Items(commands, command => command.Text)

            // 关键：将每个 Command 分配给对应的 SegmentButton
            .PrepareContainer<Command>((segment, command, index) =>
            {
                segment.Command(command);
            });

        // 5. 布局
        var layout = new StackPanel()
            .Vertical()
            .Spacing(16)
            .Children(
                buttonGroup,

                statusLabel
            );

        window.Content(layout);

        return window;
    }
}