using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateToggleSwitchWindow()
    {
        var window = new Window();
        window.Title("ToggleSwitch 示例");
        window.Resizable(560, 420);
        window.Padding(0);

        // =====================================================
        // 示例 1：基础用法
        // =====================================================

        var basicSwitch = new ToggleSwitch()
            .IsChecked(true)
            .OnCheckedChanged(isChecked =>
            {
                Console.WriteLine(
                    $"开关状态变为：{(isChecked ? "开启" : "关闭")}");
            });

        // =====================================================
        // 示例 2：ObservableValue 双向绑定
        // =====================================================

        var isDarkMode = new ObservableValue<bool>(false);

        var boundSwitch = new ToggleSwitch()
            .BindIsChecked(isDarkMode);

        // 程序化修改状态
        var programmaticButton = new Button()
            .Content("按钮切换")
            .Size(180, 32)
            .OnClick(() =>
            {
                isDarkMode.Value = !isDarkMode.Value;
            });

        // 状态文本：随着 ObservableValue 自动更新
        var darkModeStatus = new TextBlock()
            .BindText(isDarkMode, isChecked =>
                $"当前选中：{(isChecked ? "开启" : "关闭")}");

        // =====================================================
        // 示例 3：带标签说明的设置面板
        // =====================================================

        var wifiSwitch = new ToggleSwitch();

        var bluetoothSwitch = new ToggleSwitch()
            .IsChecked(true);

        var autoUpdateSwitch = new ToggleSwitch();

        var settingsPanel = new StackPanel()
            .Spacing(12)
            .Children(
                CreateSettingRow(
                    "Wi-Fi",
                    "连接到无线网络",
                    wifiSwitch),

                CreateSettingRow(
                    "蓝牙",
                    "发现并连接附近设备",
                    bluetoothSwitch),

                CreateSettingRow(
                    "自动更新",
                    "在后台下载并安装更新",
                    autoUpdateSwitch)
            );

        // =====================================================
        // 主布局
        // =====================================================

        var stackPanel = new StackPanel()
            .Vertical()
            .Spacing(20)
            .Padding(24)
            .Children(

                // 基础用法
                new TextBlock()
                    .Text("基础 ToggleSwitch（默认开启）")
                    .Bold(),

                basicSwitch,

                // 数据绑定
                new TextBlock()
                    .Text("数据绑定（ObservableValue）")
                    .Bold(),

                boundSwitch,

                programmaticButton,

                darkModeStatus,

                // 设置面板
                new TextBlock()
                    .Text("实用设置面板")
                    .Bold(),

                settingsPanel
            );

        var scrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .Content(stackPanel);

        window.Content(scrollViewer);

        return window;
    }

    // =========================================================
    // 设置行：左侧标题及说明，右侧 ToggleSwitch
    // =========================================================

    private static Border CreateSettingRow(
        string title,
        string description,
        ToggleSwitch toggle)
    {
        var titleText = new TextBlock()
            .Text(title);

        var descriptionText = new TextBlock()
            .Text(description)
            .Apply(text => text.FontSize = 12);

        var textPanel = new StackPanel()
            .Spacing(2)
            .Children(
                titleText,
                descriptionText
            );

        var row = new DockPanel()
            .LastChildFill(true)
            .Children(
                toggle.DockRight(),
                textPanel
            );

        return new Border()
            .Padding(12, 8)
            .Child(row);
    }
}