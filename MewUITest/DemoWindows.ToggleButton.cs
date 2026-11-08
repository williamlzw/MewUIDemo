using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateToggleButtonWindow()
    {
        var window = new Window();
        window.Title("ToggleButton 示例");
        window.Resizable(400, 300);
        window.Padding(0);

        // 用于显示当前选中项的 Label
        var statusLabel = new Label();
        statusLabel.Text("当前状态：未选中");
        statusLabel.CanvasPosition(50, 220);
        statusLabel.Size(250, 24);

        // 创建 ToggleButton：演示基础、默认选中、禁用两种场景
        var toggleA = new ToggleButton();
        toggleA.Content("切换状态 A");
        toggleA.IsChecked(false);
        toggleA.CanvasPosition(50, 60);
        toggleA.Size(150, 30);
        toggleA.CheckedChanged += (isChecked) =>
        {
            statusLabel.Text($"当前状态：{(isChecked ? "A 已选中" : "A 未选中")}");
        };

        var toggleB = new ToggleButton();
        toggleB.Content("切换状态 B");
        toggleB.IsChecked(true); // 默认选中
        toggleB.CanvasPosition(50, 110);
        toggleB.Size(150, 30);
        toggleB.CheckedChanged += (isChecked) =>
        {
            statusLabel.Text($"当前状态：{(isChecked ? "B 已选中" : "B 未选中")}");
        };

        var toggleC = new ToggleButton();
        toggleC.Content("切换状态 C（禁用）");
        toggleC.IsChecked(true);
        toggleC.Disable(); // 禁用，但保留选中状态
        toggleC.CanvasPosition(50, 160);
        toggleC.Size(180, 30);

        var canvas = new Canvas();
        canvas.Add(toggleA);
        canvas.Add(toggleB);
        canvas.Add(toggleC);
        canvas.Add(statusLabel);

        window.Content(canvas);
        return window;
    }
}
