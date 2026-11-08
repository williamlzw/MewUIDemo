using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateTabControlWindow()
    {
        var window = new Window();
        window.Title("TabControl 示例");
        window.Resizable(600, 400);
        window.Padding(20);

        var tabControl = new TabControl()
            .Height(320)
            .TabItems(
                // Tab 1：简单文本内容
                new TabItem()
                    .Header("基础设置")
                    .Content(
                        new StackPanel()
                            .Vertical()
                            .Spacing(12)
                            .Padding(16)
                            .Children(
                                new TextBlock()
                                    .Text("这是“基础设置”页的内容。")
                                    .FontSize(14),

                                new CheckBox()
                                    .Content("启用自动更新"),

                                new Slider()
                                    .Minimum(0)
                                    .Maximum(100)
                                    .Value(60)
                            )
                    ),

                // Tab 2：复杂布局内容
                new TabItem()
                    .Header("高级选项")
                    .Content(
                        new StackPanel()
                            .Vertical()
                            .Spacing(12)
                            .Padding(16)
                            .Children(
                                new TextBlock()
                                    .Text("高级选项页，包含更多控件。")
                                    .FontSize(14),

                                new ComboBox()
                                    .Items("选项 A", "选项 B", "选项 C")
                                    .SelectedIndex(0),

                                new NumericUpDown()
                                    .Minimum(0)
                                    .Maximum(100)
                                    .Value(25)
                            )
                    ),

                // Tab 3：富标题（图标 + 文本）
                new TabItem()
                    .Header(
                        new StackPanel()
                            .Horizontal()
                            .Spacing(6)
                            .Children(
                                new TextBlock()
                                    .Text("ℹ")
                                    .FontSize(14),

                                new TextBlock()
                                    .Text("关于")
                            )
                    )
                    .Content(
                        new StackPanel()
                            .Vertical()
                            .Padding(16)
                            .Children(
                                new TextBlock()
                                    .Text("TabControl 是 MewUI 中轻量且代码优先的控件，支持 NativeAOT。")
                                    .FontSize(14)
                            )
                    )
            );

        window.Content(tabControl);

        return window;
    }
}
