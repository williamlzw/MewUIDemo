using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using MewUI.Extended;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateExLunarDateBoxWindow()
    {
        var window = new CustomWindow();
        window.Title("LunarDateBox 示例");
        window.Resizable(420, 440);
        window.Padding(0);

        var currentDate = new ObservableValue<DateTime?>(DateTime.Today);

        var dateBox = new ExLunarDateBox
        {
            SelectedDate = DateTime.Today,
            DateFormat = "yyyy-MM-dd",
            Placeholder = "请选择日期",
            FirstDayOfWeek = DayOfWeek.Monday
        };
        dateBox.CanvasPosition(20, 20);
        dateBox.Size(360, 380);

        dateBox.BorderBrush = Color.Gray;
        dateBox.BorderThickness = 1;
        dateBox.CornerRadius = 4;

        dateBox.SelectedDateChanged += date => currentDate.Value = date;

        var canvas = new Canvas();
        canvas.Add(dateBox);

        window.Content(canvas);
        return window;
    }
}