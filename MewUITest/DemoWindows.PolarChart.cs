
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.MewCharts;
using Aprillz.MewUI.MewCharts.Painting;
using Aprillz.MewUI.MewCharts.Views;

using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreatePolarChartWindow()
    {
        var window = new Window();
        window.Title("MewCharts 极坐标图示例");
        window.Resizable(600, 560);
        window.Padding(12);

        // ---- 产品 A ----
        var productA = new PolarLineSeries<int>(
            90, 85, 75, 95, 80, 88
        )
        {
            Name = "产品 A",

            // 闭合曲线，形成雷达图
            IsClosed = true,

            // 填充颜色
            Fill = new SolidColorPaint(
                new Color(75, 33, 150, 243)
            ),

            GeometrySize = 8
        };

        // ---- 产品 B ----
        var productB = new PolarLineSeries<int>(
            75, 90, 95, 70, 85, 78
        )
        {
            Name = "产品 B",

            IsClosed = true,

            Fill = new SolidColorPaint(
                new Color(75, 244, 67, 54)
            ),

            GeometrySize = 8
        };

        // ---- 极坐标图 ----
        var chart = new PolarChart
        {
            // 数据系列
            Series =
            [
                productA,
                productB
            ],

            // 角度轴标签
            AngleAxes =
            [
                new PolarAxis
                {
                    Labels =
                    [
                        "性能",
                        "设计",
                        "价格",
                        "质量",
                        "服务",
                        "易用性"
                    ]
                }
            ],

            // 初始旋转角度
            InitialRotation = -90,

            // 中心空白区域半径
            InnerRadius = 24,

            // 完整圆周
            TotalAngle = 360,

            // 图例位置
            LegendPosition = LegendPosition.Bottom
        };

        window.Content(chart);

        return window;
    }
}
