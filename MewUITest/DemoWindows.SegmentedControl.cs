using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateSegmentedControlWindow()
    {
        var window = new Window();
        window.Title("SegmentedControl 示例");
        window.Resizable(420, 280);
        window.Padding(0);

        // 状态标签
        var statusLabel = new Label();
        statusLabel.Text("当前视图：列表");
        statusLabel.CanvasPosition(50, 180);
        statusLabel.Size(320, 24);

        // 创建 SegmentedControl
        var segmented = new SegmentedControl();
        segmented.Items("列表", "网格", "详细信息");
        segmented.SelectedIndex(0);               // 默认选中第一项
        segmented.CanvasPosition(50, 80);
        segmented.Size(320, 36);

        // 选中项变化时更新状态标签
        segmented.SelectionChanged += (item) =>
        {
            if (item is string index)
            {
                statusLabel.Text($"当前视图：{index}");
            }
        };

        var canvas = new Canvas();
        canvas.Add(segmented);
        canvas.Add(statusLabel);

        window.Content(canvas);
        return window;
    }
}