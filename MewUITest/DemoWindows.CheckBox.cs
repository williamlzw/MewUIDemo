using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateCheckBoxWindow()
    {
        var window = new Window();
        window.Title("CheckBox 示例");
        window.Resizable(450, 320);
        window.Padding(0);

        // 状态显示 Label
        var statusLabel = new Label();
        statusLabel.Text("当前状态：未勾选");
        statusLabel.CanvasPosition(50, 230);
        statusLabel.Size(350, 24);

        // 1. 基础 CheckBox：默认勾选，并监听状态变化
        var checkBasic = new CheckBox();
        checkBasic.Content("启用功能 A");          // CheckBox 使用 .Text()，不是 .Content()
        checkBasic.IsChecked(true);             // 初始为勾选状态
        checkBasic.CanvasPosition(50, 60);
        checkBasic.Size(200, 28);
        checkBasic.OnCheckStateChanged(state =>
        {
            statusLabel.Text($"功能 A 状态：{state}");
            Console.WriteLine($"CheckBox 状态变更: {state}");
        });

        // 2. 禁用状态的 CheckBox
        var checkDisabled = new CheckBox();
        checkDisabled.Content("功能 B（禁用）");
        checkDisabled.IsChecked(false);
        checkDisabled.Disable();                // 禁用后用户无法点击
        checkDisabled.CanvasPosition(50, 100);
        checkDisabled.Size(200, 28);

        // 3. 三态 CheckBox（勾选 / 不确定 / 未勾选）
        var checkThreeState = new CheckBox();
        checkThreeState.Content("三态选项");
        checkThreeState.IsThreeState(true);     // 启用三态支持
        checkThreeState.IsChecked(null);        // null 通常对应“不确定”状态
        checkThreeState.CanvasPosition(50, 140);
        checkThreeState.Size(200, 28);
        checkThreeState.OnCheckStateChanged(isChecked =>
        {
            statusLabel.Text($"三态选项当前状态：{isChecked}");
        });


        var canvas = new Canvas();
        canvas.Add(checkBasic);
        canvas.Add(checkDisabled);
        canvas.Add(checkThreeState);
        canvas.Add(statusLabel);

        window.Content(canvas);
        return window;
    }
}
