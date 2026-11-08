using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateSliderWindow()
    {
        var window = new Window();
        window.Title("Slider 示例");
        window.Resizable(560, 480);
        window.Padding(0);

        // =====================================================
        // 示例 1：基础用法
        // =====================================================

        var basicSlider = new Slider()
            .Minimum(0)
            .Maximum(100)
            .Value(50)
            .Width(280);

        var basicValue = new TextBlock()
            .Text("当前值：50");

        basicSlider.OnValueChanged(value =>
        {
            basicValue.Text($"当前值：{value:F0}");
        });

        // =====================================================
        // 示例 2：ObservableValue 双向绑定
        // =====================================================

        var volume = new ObservableValue<double>(75);

        var boundSlider = new Slider()
            .Minimum(0)
            .Maximum(100)
            .BindValue(volume)
            .Width(280);

        // 实时显示绑定数值
        var volumeDisplay = new TextBlock()
            .BindText(volume, value => $"当前音量：{value:F0}");

        // 程序侧修改数值，Slider 自动同步
        var muteButton = new Button()
            .Content("静音 (设为 0)")
            .Size(120, 32)
            .OnClick(() =>
            {
                volume.Value = 0;
            });

        var fullButton = new Button()
            .Content("最大 (设为 100)")
            .Size(120, 32)
            .OnClick(() =>
            {
                volume.Value = 100;
            });

        // =====================================================
        // 示例 3：自定义步进
        // =====================================================

        var steppedSlider = new Slider()
            .Minimum(0)
            .Maximum(10)
            .Value(5)
            .SmallChange(1)
            .LargeChange(5)
            .Width(280);

        var steppedValue = new TextBlock()
            .Text("当前步进值：5");

        steppedSlider.OnValueChanged(value =>
        {
            steppedValue.Text($"当前步进值：{value:F0}");
        });

        // =====================================================
        // 示例 4：实用场景 - 速度调节
        // =====================================================

        var speedValue = new ObservableValue<double>(60);

        var speedSlider = new Slider()
            .Minimum(0)
            .Maximum(120)
            .BindValue(speedValue)
            .Width(300);

        var speedLabel = new TextBlock()
            .BindText(speedValue, value =>
                $"当前速度：{value:F0} km/h");

        // =====================================================
        // 主布局
        // =====================================================

        var stackPanel = new StackPanel()
            .Vertical()
            .Spacing(16)
            .Padding(24)
            .Children(

                // 示例 1
                new TextBlock()
                    .Text("基础 Slider（0–100）")
                    .Bold(),

                basicSlider,
                basicValue,

                // 示例 2
                new TextBlock()
                    .Text("双向绑定（ObservableValue）")
                    .Bold(),

                boundSlider,
                volumeDisplay,

                new StackPanel()
                    .Horizontal()
                    .Spacing(8)
                    .Children(
                        muteButton,
                        fullButton
                    ),

                // 示例 3
                new TextBlock()
                    .Text("自定义步进（SmallChange / LargeChange）")
                    .Bold(),

                steppedSlider,
                steppedValue,

                new TextBlock()
                    .Text("SmallChange = 1，LargeChange = 5")
                    ,

                // 示例 4
                new TextBlock()
                    .Text("实用场景：速度调节")
                    .Bold(),

                speedSlider,
                speedLabel
            );

        var scrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .Content(stackPanel);

        window.Content(scrollViewer);

        return window;
    }
}