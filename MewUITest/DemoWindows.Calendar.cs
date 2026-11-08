using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateCalendarWindow()
    {
        var window = new Window();
        window.Title("Calendar 示例");
        window.Resizable(520, 580);
        window.Padding(0);

        // 示例 1：基础日历，默认显示当月，选中今天
        var basicCalendar = new Calendar()
            .Size(240, 200)
            .SelectedDate(DateTime.Today)
            .FirstDayOfWeek(DayOfWeek.Monday)   // 一周从周一开始
            .IsTodayHighlighted(true)               // 高亮今天（默认已开启，显式写出便于理解）
            .OnSelectedDateChanged(date =>
            {
                Console.WriteLine(date.HasValue
                    ? $"选中日期：{date.Value:yyyy-MM-dd}"
                    : "日期被清除");
            });

        // 示例 2：指定显示月份，并限制为“年”视图模式
        var yearViewCalendar = new Calendar()
            .Size(240, 200)
            .DisplayDate(new DateTime(2026, 1, 1))   // 日历打开时显示 2026 年 1 月
            .DisplayMode(CalendarMode.Decade)           // 启动即为“年”视图
            .FirstDayOfWeek(DayOfWeek.Sunday)         // 一周从周日开始
            .OnSelectedDateChanged(date =>
            {
                if (date.HasValue)
                    Console.WriteLine($"年视图选中：{date.Value:yyyy-MM-dd}");
            });

        var stackPanel = new StackPanel()
            .Spacing(20)
            .Padding(20)
            .Children(
                new TextBlock().Text("基础日历（选中今天，周一为起始日）").Bold(),
                basicCalendar,
                new TextBlock().Text("年视图日历（显示 2026 年，周日为起始日）").Bold(),
                yearViewCalendar
            );

        var scrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .Content(stackPanel);

        window.Content(scrollViewer);
        return window;
    }
}
