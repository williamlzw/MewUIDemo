
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.MewCharts;
using Aprillz.MewUI.MewCharts.Views;

using LiveChartsCore;
using LiveChartsCore.Measure;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateCartesianChartWindow()
    {
        var window = new Window();
        window.Title("MewCharts 折线图示例");
        window.Resizable(680, 460);
        window.Padding(12);

        // ---- 系列 1：今年销售额 ----
        var currentYear = new LineSeries<double>(
            120, 150, 138, 180, 210, 195, 240
        )
        {
            Name = "今年销售额",

            // 数据点大小
            GeometrySize = 8,

            // 曲线平滑程度
            LineSmoothness = 0.3
        };

        // ---- 系列 2：去年销售额 ----
        var previousYear = new LineSeries<double>(
            90, 110, 125, 130, 160, 155, 170
        )
        {
            Name = "去年销售额",

            GeometrySize = 8,
            LineSmoothness = 0.3
        };

        // ---- 笛卡尔坐标图 ----
        var chart = new CartesianChart
        {
            // 数据系列
            Series =
            [
                currentYear,
                previousYear
            ],

            // X 轴
            XAxes =
            [
                new Axis
                {
                    Name = "月份",

                    Labels =
                    [
                        "一月",
                        "二月",
                        "三月",
                        "四月",
                        "五月",
                        "六月",
                        "七月"
                    ]
                }
            ],

            // Y 轴
            YAxes =
            [
                new Axis
                {
                    Name = "销售额（万元）",

                    // 自定义刻度显示格式
                    Labeler = value => value.ToString("N0")
                }
            ],

            // 图例位置
            LegendPosition = LegendPosition.Bottom,

            // 鼠标悬停时提示框位置
            TooltipPosition = TooltipPosition.Top
        };

        window.Content(chart);

        return window;
    }
}
