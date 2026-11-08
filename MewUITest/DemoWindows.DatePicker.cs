using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateDatePickerWindow()
    {
        var window = new Window();
        window.Title("DatePicker 示例");
        window.Resizable(480, 320);
        window.Padding(0);

        // ---- 1. 创建一个可观察的日期状态（初始为今天）----
        var selectedDate = new ObservableValue<DateTime?>(DateTime.Today);

        // ---- 2. 基础 DatePicker，绑定到 ObservableValue ----
        // BindSelectedDate 是双向绑定：用户选择日期会写回 selectedDate，
        // 程序修改 selectedDate 也会同步更新 DatePicker 的显示。
        var datePicker = new DatePicker()
            .BindSelectedDate(selectedDate)
            .Size(240, 30)
            .CanvasPosition(50, 30);

        // ---- 3. 另一个 DatePicker，演示转换绑定 ----
        // 假设有一个 string 类型的 ObservableValue，用于与日期互转
        var dateString = new ObservableValue<string>("2026-01-01");
        var convertedDatePicker = new DatePicker()
            .BindSelectedDate(
                dateString,
                convert: s => DateTime.TryParse(s, out var d) ? d : (DateTime?)null,
                convertBack: d => d?.ToString("yyyy-MM-dd") ?? ""
            )
            .Size(240, 30)
            .CanvasPosition(50, 90);

        // ---- 4. 通过 OnSelectedDateChanged 订阅日期变化事件 ----
        var dateChangeLog = new Label()
            .Text("尚未选择日期")
            .FontSize(12)
            .CanvasPosition(50, 150)
            .Size(360, 24);

        datePicker.OnSelectedDateChanged(date =>
        {
            dateChangeLog.Text(date.HasValue
                ? $"已选择：{date.Value:yyyy年MM月dd日}"
                : "日期已清除");
        });

        // ---- 5. 程序侧按钮：修改日期，验证双向绑定 ----
        var setTodayButton = new Button()
            .Content("设为今天")
            .Size(100, 32)
            .CanvasPosition(50, 190)
            .OnClick(() => selectedDate.Value = DateTime.Today);

        var clearDateButton = new Button()
            .Content("清除日期")
            .Size(100, 32)
            .CanvasPosition(160, 190)
            .OnClick(() => selectedDate.Value = null);

        // ---- 6. 显示当前选中日期（随 ObservableValue 自动更新）----
        var currentDateLabel = new Label()
            .BindText(selectedDate, d => d.HasValue
                ? $"当前选中：{d.Value:yyyy-MM-dd}"
                : "当前未选择日期")
            .FontSize(13)
            .CanvasPosition(50, 240)
            .Size(360, 24);

        // ---- 组装 Canvas ----
        var canvas = new Canvas();
        canvas.Add(datePicker);
        canvas.Add(convertedDatePicker);
        canvas.Add(dateChangeLog);
        canvas.Add(setTodayButton);
        canvas.Add(clearDateButton);
        canvas.Add(currentDateLabel);

        window.Content(canvas);
        return window;
    }
}