using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using MewUI.Extended;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateExSwitchWindow()
    {
        var window = new Window();
        window.Title("ExSwitch 示例");
        window.Resizable(400, 200);
        window.Padding(0);

        var currentValue = new ObservableValue<bool>(false);

        var label = new Label();
        label.BindText(currentValue, v => $"当前状态：{(v ? "开启" : "关闭")}");
        label.CanvasPosition(50, 30);
        label.Size(240, 30);

        var toggle = new ExSwitch();
        toggle.IsChecked = false;
        toggle.CheckedText = "开启";
        toggle.UncheckedText = "关闭";
        toggle.CanvasPosition(50, 80);
        toggle.Size(100, 30);
        //toggle.BorderBrush = Color.Red;
        //toggle.BorderThickness = 1;
        toggle.CornerRadius = 0;
        toggle.IsCheckedChanged += (sender, e) => currentValue.Value = e.NewValue;

        var canvas = new Canvas();
        canvas.Add(label);
        canvas.Add(toggle);

        window.Content(canvas);
        return window;
    }
}