using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreatePopupWindow()
    {
        var window = new Window();
        window.Title("Popup 示例");
        window.Resizable(520, 360);
        window.Padding(0);

        // =====================================================
        // 1. 创建 Popup
        // =====================================================
        Popup popup = null;
        popup = new Popup()
            .StaysOpen(false)
            .Content(
                new Border()
                    .Width(240)
                    .Padding(16)
                    .Background(new Color(255, 255, 255))
                    .BorderBrush(new Color(200, 200, 200))
                    .BorderThickness(1)
                    .CornerRadius(8)
                    .Child(
                        new StackPanel()
                            .Spacing(12)
                            .Children(
                                new TextBlock()
                                    .Text("Popup 弹出面板")
                                    .FontSize(16)
                                    .Bold(),

                                new TextBlock()
                                    .Text("这是一个自定义 Popup 控件。"),

                                new TextBlock()
                                    .Text("可以在这里放置任意 MewUI 控件。")
                                    .FontSize(12),

                                new Button()
                                    .Content("关闭 Popup")
                                    .Size(120, 32)
                                    .OnClick(() =>
                                    {
                                        popup.Close();
                                    })
                            )
                    )
            )
            .OnOpened((sender, args) =>
            {
                Console.WriteLine("Popup 已打开");
            })
            .OnClosed((sender, args) =>
            {
                Console.WriteLine("Popup 已关闭");
            });

        // =====================================================
        // 2. 打开 Popup
        // =====================================================

        const double buttonX = 40;
        const double buttonY = 40;
        const double buttonWidth = 140;
        const double buttonHeight = 36;

        var openButton = new Button()
            .Content("打开 Popup")
            .Size(buttonWidth, buttonHeight)
            .CanvasPosition(buttonX, buttonY);

        // Popup 显示在按钮下方
        openButton.OnClick(() =>
        {
            popup.ShowAt(
                openButton,
                new Rect(
                    buttonX,
                    buttonY,
                    buttonWidth,
                    buttonHeight
                ),
                PopupAnchorSide.Below
            );
        });

        // =====================================================
        // 3. 程序化关闭 Popup
        // =====================================================

        var closeButton = new Button()
            .Content("程序化关闭")
            .Size(140, 36)
            .CanvasPosition(200, 40)
            .OnClick(() =>
            {
                popup.Close();
            });

        // =====================================================
        // 4. 显示说明
        // =====================================================

        var description = new StackPanel()
            .Spacing(8)
            .Children(
                new TextBlock()
                    .Text("Popup 控件使用说明")
                    .Bold()
                    .FontSize(16),

                new TextBlock()
                    .Text("1. 点击打开 Popup 按钮显示弹出面板。"),

                new TextBlock()
                    .Text("2. 点击 Popup 内部的关闭按钮关闭面板。"),

                new TextBlock()
                    .Text("3. 点击程序化关闭按钮也可以关闭面板。"),

                new TextBlock()
                    .Text("4. 点击 Popup 外部区域自动关闭面板。")
            )
            .CanvasPosition(40, 110);

        // =====================================================
        // 5. 组装 Canvas
        // =====================================================

        var canvas = new Canvas()
            .Children(
                openButton,
                closeButton,
                description
            );

        window.Content(canvas);

        return window;
    }
}