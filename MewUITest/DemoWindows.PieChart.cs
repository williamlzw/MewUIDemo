
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.MewCharts.Painting;
using Aprillz.MewUI.MewCharts.Views;

using LiveChartsCore.Measure;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreatePieChartWindow()
    {
        var window = new Window();
        window.Title("MewCharts 饼图示例");
        window.Resizable(560, 460);
        window.Padding(12);

        // ---- 销售数据 ----
        const double total = 150;

        // 创建饼图数据项
        PieSeries<double> CreateSlice(double value, string name)
        {
            return new PieSeries<double>(value)
            {
                Name = name,

                // 显示数据标签
                ShowDataLabels = true,

                // 数据标签颜色
                DataLabelsPaint = new SolidColorPaint(
                    new Color(255, 255, 255, 255)
                ),

                DataLabelsSize = 14,

                // 标签显示在饼图扇区中间
                DataLabelsPosition = PolarLabelsPosition.Middle,

                // 标签显示名称、数值和百分比
                DataLabelsFormatter = point =>
                    $"{name}\n{point.Coordinate.PrimaryValue:0} " +
                    $"({point.Coordinate.PrimaryValue / total:P0})"
            };
        }

        // ---- 饼图 ----
        var chart = new PieChart
        {
            Series =
            [
                CreateSlice(10, "Mary"),
                CreateSlice(20, "John"),
                CreateSlice(30, "Alice"),
                CreateSlice(40, "Bob"),
                CreateSlice(50, "Charlie"),
            ],

            // 图例放在底部
            LegendPosition = LegendPosition.Bottom
        };

        window.Content(chart);

        return window;
    }
}
