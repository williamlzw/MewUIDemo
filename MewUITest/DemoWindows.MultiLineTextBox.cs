using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;
using MewUI.Extended;
namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateAdvancedTextBoxWindow()
    {
        var window = new CustomWindow();
        window.Title("MewUI 编辑框进阶示例");
        window.Resizable(600, 550);
       

        // 1. 只读 MultiLineTextBox (展示日志或说明)
        var readOnlyBox = new MultiLineTextBox()
            .IsReadOnly(true)
            .Text("这是一个只读的多行文本框。\n用户无法编辑此内容，但可以选中和滚动。")
            .Height(80);

        // 2. 密码输入框 PasswordBox
        var passwordBox = new PasswordBox()
            .Placeholder("请输入密码...")
            .Width(250);

        // 3. 数字输入框
        var numberOnly = new TextBox()
            .Placeholder("只能输入字母...")
            .Width(250);
        numberOnly.EnableNumbersOnly();

        // 4. 带输入过滤的 TextBox (只允许字母)
        var letterOnlyBox = new TextBox()
            .Placeholder("只能输入字母...")
            .Width(250);
        letterOnlyBox.EnableLettersOnly();

        var numberLetterOnly = new TextBox()
           .Placeholder("只能输入数字和字母...")
           .Width(250);
        numberLetterOnly.EnableLettersAndNumbers();


        // 5. 可滚动的多行编辑框 + 滚动到末尾按钮
        var longText = string.Join("\n", System.Linq.Enumerable.Range(1, 50).Select(i => $"这是第 {i} 行内容。"));
        var scrollableBox = new MultiLineTextBox()
            .Text(longText)
            .Height(150)
            .Wrap(true)
            .AcceptTab(true);

        var scrollToEndButton = new Button()
            .Content("滚动到最后一行")
            .OnClick(() => {
                scrollableBox.AppendText(
                    Environment.NewLine,
                    scrollToCaret: true
                );
                Console.WriteLine("模拟滚动到最后一行...");
            });

        // 6. 带自定义背景色的编辑框
        var styledBox = new TextBox()
            .Placeholder("自定义背景色...")
            .Width(250)
            .Background(new Color(255, 240, 240))// 浅红色背景
            .Foreground(new Color(155, 155, 230)); 

        // 使用 StackPanel 垂直排列所有控件
        var panel = new StackPanel()
            .Spacing(15)
            .Padding(10)
            .Children(
                new Label().Text("1. 只读文本框:").Bold(),
                readOnlyBox,

                new Label().Text("2. 密码输入框 (PasswordBox):").Bold(),
                passwordBox,

                new Label().Text("3. 数字输入").Bold(),
                numberOnly,

                new Label().Text("4. 字母输入").Bold(),
                letterOnlyBox,

                new Label().Text("5. 数字和字母输入").Bold(),
                numberLetterOnly,

                new Label().Text("6. 自定义背景色和文字色:").Bold(),
                styledBox,

                new Label().Text("7. 长文本与滚动:").Bold(),
                scrollableBox,
                scrollToEndButton
            );

        // 将 StackPanel 放入 ScrollViewer 以防内容过长
        var scrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .Content(panel);

        window.Content(scrollViewer);
        return window;
    }
}