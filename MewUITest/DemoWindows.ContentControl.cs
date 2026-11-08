
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateContentControlWindow()
    {
        var window = new Window();
        window.Title("ContentControl 示例");
        window.Resizable(560, 460);
        window.Padding(0);

        // =====================================================
        // 示例 1：ContentControl 承载一个完整的控件树
        // =====================================================

        var basicContentControl = new ContentControl()
            .Content(
                new Border()
                    .Padding(16)
                    .Background(new Color(245, 245, 245))
                    .Child(
                        new StackPanel()
                            .Spacing(10)
                            .Children(
                                new TextBlock()
                                    .Text("ContentControl 内部的标题")
                                    .Bold(),

                                new TextBlock()
                                    .Text("这里包含多个子控件，但 ContentControl 本身只承载一个根内容元素。"),

                                new Button()
                                    .Content("内部按钮")
                                    .Size(120, 32)
                            )
                    )
            );

        // =====================================================
        // 示例 2：ContentControl 作为内容区域
        // =====================================================

        // 创建内容 A
        static Border CreateContentA()
        {
            return new Border()
                .Padding(20)
                .Background(new Color(230, 240, 255))
                .Child(
                    new StackPanel()
                        .Spacing(10)
                        .Children(
                            new TextBlock()
                                .Text("这是内容 A")
                                .Bold(),

                            new TextBlock()
                                .Text("当前 ContentControl 正在显示第一组内容。"),

                            new TextBlock()
                                .Text("这里可以放置任意控件。")
                        )
                );
        }

        // 创建内容 B
        static Border CreateContentB()
        {
            return new Border()
                .Padding(20)
                .Background(new Color(255, 245, 225))
                .Child(
                    new StackPanel()
                        .Spacing(10)
                        .Children(
                            new TextBlock()
                                .Text("这是内容 B")
                                .Bold(),

                            new TextBlock()
                                .Text("ContentControl 的 Content 已经被替换。"),

                            new TextBlock()
                                .Text("原来的内容 A 不再作为当前内容显示。")
                        )
                );
        }

        // ContentControl 初始显示内容 A
        var contentHost = new ContentControl()
            .Content(CreateContentA());

        var currentStatus = new TextBlock()
            .Text("当前显示：内容 A")
            .Bold();

        // 切换到内容 A
        var showAButton = new Button()
            .Content("显示内容 A")
            .Size(120, 34)
            .OnClick(() =>
            {
                contentHost.Content(CreateContentA());
                currentStatus.Text("当前显示：内容 A");
            });

        // 切换到内容 B
        var showBButton = new Button()
            .Content("显示内容 B")
            .Size(120, 34)
            .OnClick(() =>
            {
                contentHost.Content(CreateContentB());
                currentStatus.Text("当前显示：内容 B");
            });

        // =====================================================
        // 布局
        // =====================================================

        var stackPanel = new StackPanel()
            .Spacing(16)
            .Padding(20)
            .Children(
                // 示例 1
                new TextBlock()
                    .Text("示例 1：ContentControl 承载控件树")
                    .Bold(),

                basicContentControl,

                // 示例 2
                new TextBlock()
                    .Text("示例 2：动态替换 Content")
                    .Bold(),

                currentStatus,

                contentHost,

                new StackPanel()
                    .Horizontal()
                    .Spacing(10)
                    .Children(
                        showAButton,
                        showBButton
                    )
            );

        var scrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .Content(stackPanel);

        window.Content(scrollViewer);

        return window;
    }
}
