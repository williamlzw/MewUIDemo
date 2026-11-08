using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateListBoxWindow()
    {
        var window = new Window();
        window.Title("ListBox 示例");
        window.Resizable(420, 320);
        window.Padding(0);

        // 状态标签
        var statusLabel = new Label();
        statusLabel.Text("当前选中：无");
        statusLabel.CanvasPosition(50, 240);
        statusLabel.Size(300, 24);

        // 创建 ListBox，直接填充字符串集合
        var listBox = new ListBox();
        listBox.CanvasPosition(50, 40);
        listBox.Size(300, 180);
        listBox.Items("苹果", "香蕉", "橙子", "葡萄", "西瓜");

        // 选中项变化时更新状态标签
        listBox.OnSelectionChanged(obj =>
        {
            var text = obj as string;
            statusLabel.Text(string.IsNullOrEmpty(text)
                ? "当前选中：无"
                : $"当前选中：{text}");
        });

        var canvas = new Canvas();
        canvas.Add(listBox);
        canvas.Add(statusLabel);

        window.Content(canvas);
        return window;
    }

    // 数据模型：增加 AvatarPath 和 Signature
    record Friend(int Id, string Name, string Signature, string AvatarPath);

    public static Window CreateListBoxTemplateWindow()
    {
        var window = new Window();
        window.Title("ListBox 自定义模板 示例");
        window.Resizable(480, 420);
        window.Padding(0);

        var statusLabel = new Label();
        statusLabel.Text("当前选中：无");
        statusLabel.CanvasPosition(50, 340);
        statusLabel.Size(380, 24);

        var friends = new[]
        {
            new Friend(1, "Alice",   "在写代码，勿扰",     @"assets/images/ai.png"),
            new Friend(2, "Bob",     "今天也要开心",       @"assets/images/ai.png"),
            new Friend(3, "Charlie", "出差中，消息晚回",   @"assets/images/ai.png"),
            new Friend(4, "Diana",   "正在听歌",           @"assets/images/ai.png"),
        };

        var listBox = new ListBox();
        listBox.CanvasPosition(50, 40);
        listBox.Size(380, 280);
        listBox.VariableHeightPresenter();//根据模板内容自动计算行高
        listBox.Items(friends,
            textSelector: f => f.Name,
            keySelector: f => f.Id);

        listBox.ItemTemplate(new DelegateTemplate<Friend>(
            build: ctx =>
            {
                // 两行两列的 Grid：左列头像固定宽度，右列自适应
                var grid = new Grid()
                    .Rows("Auto,Auto")
                    .Columns("56,*")
                    .Spacing(4);

                // 头像：跨两行，固定在左列
                var avatar = new Image()
                    .GridPosition(0, 0, 2, 1)
                    .Size(48, 48)
                    .Register(ctx, "Avatar");

                // 昵称：第一行，右列
                var nameText = new TextBlock()
                    .FontSize(15)
                    .GridPosition(0, 1)
                    .Register(ctx, "Name");

                // 签名：第二行，右列，字号稍小、颜色稍淡
                var signatureText = new TextBlock()
                    .FontSize(12)
                    .Foreground(new Color(130, 130, 130))
                    .GridPosition(1, 1)
                    .Register(ctx, "Signature");

                grid.Children(avatar, nameText, signatureText);
                return grid;
            },
            bind: (view, item, index, ctx) =>
            {
                ctx.Get<Image>("Avatar").SourceFile(item.AvatarPath);
                ctx.Get<TextBlock>("Name").Text(item.Name);
                ctx.Get<TextBlock>("Signature").Text(item.Signature);
            }
        ));

        listBox.OnSelectionChanged(obj =>
        {
            var friend = obj as Friend;
            statusLabel.Text(friend == null
                ? "当前选中：无"
                : $"当前选中：{friend.Name}");
        });

        var canvas = new Canvas();
        canvas.Add(listBox);
        canvas.Add(statusLabel);

        window.Content(canvas);
        return window;
    }
}
