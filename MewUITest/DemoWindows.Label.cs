using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateLabelWindow()
    {
        var window = new Window();
        window.Title("Label 示例");
        window.Resizable(480, 360);
        window.Padding(0);

        // =====================================================
        // 1. ObservableValue 数据源
        // =====================================================

        var currentValue = new ObservableValue<string>("测试文本");

        // =====================================================
        // 2. 基础绑定
        // =====================================================

        var basicLabel = new Label()
            .BindText(currentValue)
            .CanvasPosition(50, 10)
            .Size(280, 30);

        // =====================================================
        // 3. 带转换函数的绑定
        // =====================================================

        var lengthLabel = new Label()
            .BindText(currentValue, value =>
                $"共 {value.Length} 个字符")
            .CanvasPosition(50, 50)
            .Size(280, 30)
            .FontSize(12);

        // =====================================================
        // 4. 字体样式：粗体、斜体、大字号
        // =====================================================

        var styledLabel = new Label()
            .BindText(currentValue, value =>
                $"样式演示：{value}")
            .CanvasPosition(50, 90)
            .Size(380, 30)
            .FontSize(18)
            .Bold()
            .Italic();

        // =====================================================
        // 5. 文本对齐
        // =====================================================

        var rightAlignLabel = new Label()
            .BindText(currentValue)
            .CanvasPosition(50, 130)
            .Size(280, 30)
            .TextAlignment(TextAlignment.Right)
            .VerticalTextAlignment(TextAlignment.Center)
            .Background(new Color(245, 245, 245));

        // =====================================================
        // 6. 自动省略号
        // =====================================================

        var longText = new ObservableValue<string>(
            "这是一段故意写得很长的文本，用来演示 Label 的自动省略号功能。");

        var ellipsisLabel = new Label()
            .BindText(longText)
            .CanvasPosition(50, 170)
            .Size(240, 30)
            .TextWrapping(TextWrapping.NoWrap)
            .TextTrimming(TextTrimming.CharacterEllipsis)
            .Background(new Color(245, 245, 245));

        // =====================================================
        // 7. 程序侧修改数据源
        // =====================================================

        var changeButton = new Button()
            .Content("修改文本")
            .CanvasPosition(50, 220)
            .Size(120, 32)
            .OnClick(() =>
            {
                currentValue.Value =
                    $"已更新 {DateTime.Now:HH:mm:ss}";
            });

        var changeLongTextButton = new Button()
            .Content("修改长文本")
            .CanvasPosition(190, 220)
            .Size(120, 32)
            .OnClick(() =>
            {
                longText.Value =
                    $"更新时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}，这是一段用于测试文本省略效果的长文本。";
            });

        // =====================================================
        // 8. 组装 Canvas
        // =====================================================

        var canvas = new Canvas()
            .Children(
                basicLabel,
                lengthLabel,
                styledLabel,
                rightAlignLabel,
                ellipsisLabel,
                changeButton,
                changeLongTextButton
            );

        window.Content(canvas);

        return window;
    }
}