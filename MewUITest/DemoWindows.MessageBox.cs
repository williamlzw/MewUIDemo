
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateMessageBoxWindow()
    {
        var window = new Window();
        window.Title("MessageBox 示例");
        window.Resizable(560, 480);
        window.Padding(0);

        // ---- 示例 1：最简单的信息提示（仅确定按钮） ----
        var infoButton = new Button()
            .Content("显示信息提示")
            .Width(160)
            .Height(32)
            .OnClick(() =>
            {
                MessageBox.Notify(
                    "操作已成功完成。",
                    PromptIconKind.Info
                );
            });

        // ---- 示例 2：确认对话框（是 / 否） ----
        var confirmButton = new Button()
            .Content("删除确认")
            .Width(160)
            .Height(32)
            .OnClick(() =>
            {
                var result = MessageBox.AskYesNo(
                    "确定要删除这条记录吗？此操作不可撤销。",
                    PromptIconKind.Warning
                );

                if (result.ToString() == "Yes")
                {
                    Console.WriteLine("用户确认删除，执行删除逻辑...");
                    // 在这里调用实际的删除方法
                }
                else
                {
                    Console.WriteLine("用户取消了删除操作。");
                }
            });

        // ---- 示例 3：警告提示（确定 / 取消） ----
        var warningButton = new Button()
            .Content("显示警告")
            .Width(160)
            .Height(32)
            .OnClick(() =>
            {
                var result = MessageBox.AskYesNo(
                    "当前设置可能导致数据丢失，是否继续？",
                    PromptIconKind.Question
                );

                if (result.ToString() == "Ok")
                {
                    Console.WriteLine("用户选择继续。");
                }
                else
                {
                    Console.WriteLine("用户取消操作。");
                }
            });

        // ---- 示例 4：错误提示 ----
        var errorButton = new Button()
            .Content("显示错误")
            .Width(160)
            .Height(32)
            .OnClick(() =>
            {
                MessageBox.Notify(
                    "无法连接到服务器，请检查网络设置。",
                    PromptIconKind.Error
                );
            });

        // ---- 示例 5：三按钮对话框（是 / 否 / 取消） ----
        var triButton = new Button()
            .Content("三按钮对话框")
            .Width(160)
            .Height(32)
            .OnClick(() =>
            {
                var result = MessageBox.AskYesNoCancel(
                    "是否保存对文档的更改？",
                    PromptIconKind.Question
                );

                switch (result.ToString())
                {
                    case "Yes":
                        Console.WriteLine("保存并继续...");
                        break;

                    case "No":
                        Console.WriteLine("不保存，直接继续...");
                        break;

                    case "Cancel":
                        Console.WriteLine("取消操作。");
                        break;
                }
            });

        // ---- 示例 6：指定父窗口句柄 ----
        var ownedButton = new Button()
            .Content("指定父窗口的消息框")
            .Width(180)
            .Height(32)
            .OnClick(() =>
            {
                MessageBox.Notify(
                    "这个消息框归属于当前窗口。",
                    PromptIconKind.Info, owner: window
                );
            });

        // ---- 布局 ----
        var panel = new StackPanel()
            .Vertical()
            .Spacing(12)
            .Padding(24)
            .Children(
                new TextBlock()
                    .Text("基础提示")
                    .Bold(),

                infoButton,

                new TextBlock()
                    .Text("带返回值判断的确认框")
                    .Bold(),

                confirmButton,

                new TextBlock()
                    .Text("警告与错误")
                    .Bold(),

                new StackPanel()
                    .Horizontal()
                    .Spacing(12)
                    .Children(
                        warningButton,
                        errorButton
                    ),

                new TextBlock()
                    .Text("三按钮与父窗口归属")
                    .Bold(),

                new StackPanel()
                    .Horizontal()
                    .Spacing(12)
                    .Children(
                        triButton,
                        ownedButton
                    )
            );

        window.Content(
            new ScrollViewer()
                .VerticalScroll(ScrollMode.Auto)
                .Content(panel)
        );

        return window;
    }
}
