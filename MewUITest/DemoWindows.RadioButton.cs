using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateRadioButtonWindow()
    {
        var window = new Window();
        window.Title("RadioButton 示例");
        window.Resizable(400, 300);
        window.Padding(0);

        // 用于显示当前选中项的 Label
        var statusLabel = new Label();
        statusLabel.Text("当前选中：无");
        statusLabel.CanvasPosition(50, 200);
        statusLabel.Size(250, 24);

        // 创建 RadioButton 并分组
        var radioA = new RadioButton();
        radioA.Content("选项 A");
        radioA.GroupName("options");          // 同一 GroupName 实现互斥
        radioA.IsChecked(true);               // 默认选中 A
        radioA.CanvasPosition(50, 60);
        radioA.Size(150, 28);
        radioA.CheckedChanged += (isChecked) =>
        {
            if (isChecked) statusLabel.Text("当前选中：选项 A");
        };

        var radioB = new RadioButton();
        radioB.Content("选项 B");
        radioB.GroupName("options");
        radioB.CanvasPosition(50, 100);
        radioB.Size(150, 28);
        radioB.CheckedChanged += (isChecked) =>
        {
            if (isChecked) statusLabel.Text("当前选中：选项 B");
        };

        var radioC = new RadioButton();
        radioC.Content("选项 C（禁用）");
        radioC.GroupName("options");
        radioC.IsChecked(false);
        radioC.Disable();                     // 禁用，但仍参与分组逻辑
        radioC.CanvasPosition(50, 140);
        radioC.Size(180, 28);

        var canvas = new Canvas();
        canvas.Add(radioA);
        canvas.Add(radioB);
        canvas.Add(radioC);
        canvas.Add(statusLabel);

        window.Content(canvas);
        return window;
    }
}