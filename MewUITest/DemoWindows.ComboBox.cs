using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateComboBoxWindow()
    {
        var window = new Window();
        window.Title("ComboBox 示例");
        window.Resizable(420, 280);
        window.Padding(0);

        var statusLabel = new Label();
        statusLabel.Text("当前选中：无");
        statusLabel.CanvasPosition(50, 170);
        statusLabel.Size(300, 24);

        // 1. 基础 ComboBox：填充项目，设置默认选中项
        var combo = new ComboBox();
        combo.Items("苹果", "香蕉", "橙子", "葡萄", "芒果", "梨子", "水蜜桃", "西瓜", "哈密瓜", "蓝莓");   // 填充选项
        combo.SelectedIndex(0);                       // 默认选中第一项
        combo.CanvasPosition(50, 60);
        combo.Size(200, 32);

        // 响应选中变化（ComboBox 通常提供 SelectedIndexChanged 或类似事件，
        // 若名称不同，请替换为 MewUI 实际的事件名，如 SelectionChanged）
        combo.SelectionChanged += (_) =>
        {
            int index = combo.SelectedIndex;
            if (index >= 0)
                statusLabel.Text($"当前选中：{combo.SelectedItem}");
            else
                statusLabel.Text("当前选中：无");
        };

        // 2. 只读 / 禁用状态的 ComboBox（可选演示）
        var comboDisabled = new ComboBox();
        comboDisabled.Items("选项 1", "选项 2", "选项 3");
        comboDisabled.SelectedIndex(1);
        comboDisabled.Disable();                      // 禁用，用户无法打开下拉
        comboDisabled.CanvasPosition(50, 110);
        comboDisabled.Size(200, 32);

        var canvas = new Canvas();
        canvas.Add(combo);
        canvas.Add(comboDisabled);
        canvas.Add(statusLabel);

        window.Content(canvas);
        return window;
    }
}
