using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateTransitionContentControlWindow()
    {
        var window = new Window();
        window.Title("TransitionContentControl 示例");
        window.Resizable(600, 450);
        window.Padding(20);

        // ---------- 1. 淡入淡出 (Fade) ----------
        int fadeIndex = 0;
        string[] fadeItems = ["Hello, World!", "MewUI Transitions", "Fade Effect", "Smooth & Simple"];
        var fadeView = new TransitionContentControl
        {
            Transition = ContentTransition.CreateFade(durationMs: 300),
            Content = MakeTransitionBlock(fadeItems[0])
        };

        // ---------- 2. 滑动 (Slide) ----------
        int slideIndex = 0;
        string[] slideItems = ["Page 1", "Page 2", "Page 3"];
        var slideView = new TransitionContentControl
        {
            Transition = ContentTransition.CreateSlide(SlideDirection.Left, durationMs: 300),
            Content = MakeTransitionBlock(slideItems[0])
        };

        // ---------- 3. 缩放 (Scale) ----------
        int scaleIndex = 0;
        string[] scaleItems = ["Zoom A", "Zoom B", "Zoom C"];
        var scaleView = new TransitionContentControl
        {
            Transition = ContentTransition.CreateScale(durationMs: 300),
            Content = MakeTransitionBlock(scaleItems[0])
        };

        // 用 TabControl 分别展示
        var tabControl = new TabControl()
            .Height(350)
            .TabItems(
                new TabItem()
                    .Header("淡入淡出")
                    .Content(
                        new StackPanel()
                            .Vertical()
                            .Spacing(12)
                            .Padding(16)
                            .Children(
                                fadeView,
                                new Button()
                                    .Content("切换内容")
                                    .OnClick(() =>
                                    {
                                        fadeIndex = (fadeIndex + 1) % fadeItems.Length;
                                        fadeView.Content = MakeTransitionBlock(fadeItems[fadeIndex]);
                                    })
                            )
                    ),

                new TabItem()
                    .Header("滑动")
                    .Content(
                        new StackPanel()
                            .Vertical()
                            .Spacing(12)
                            .Padding(16)
                            .Children(
                                slideView,
                                new Button()
                                    .Content("切换内容")
                                    .OnClick(() =>
                                    {
                                        slideIndex = (slideIndex + 1) % slideItems.Length;
                                        slideView.Content = MakeTransitionBlock(slideItems[slideIndex]);
                                    })
                            )
                    ),

                new TabItem()
                    .Header("缩放")
                    .Content(
                        new StackPanel()
                            .Vertical()
                            .Spacing(12)
                            .Padding(16)
                            .Children(
                                scaleView,
                                new Button()
                                    .Content("切换内容")
                                    .OnClick(() =>
                                    {
                                        scaleIndex = (scaleIndex + 1) % scaleItems.Length;
                                        scaleView.Content = MakeTransitionBlock(scaleItems[scaleIndex]);
                                    })
                            )
                    )
            );

        window.Content(tabControl);

        return window;
    }

    // 辅助方法：生成每次切换时显示的简单内容块
    private static FrameworkElement MakeTransitionBlock(string text)
    {
        return new Border()
            .Padding(20)
            .Child(
                new TextBlock()
                    .Text(text)
                    .FontSize(16)
            );
    }
}