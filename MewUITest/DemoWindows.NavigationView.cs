using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateNavigationViewWindow()
    {
        var window = new Window();
        window.Title("NavigationView 风格布局");
        window.Resizable(720, 480);
        window.Padding(0);

        // =====================================================
        // 1. 定义导航数据
        // =====================================================

        var navItems = new[]
        {
            new NavItem("首页", "home"),
            new NavItem("设置", "settings"),
            new NavItem("关于", "info")
        };

        // 当前选中索引
        var selectedIndex = new ObservableValue<int>(0);

        // =====================================================
        // 2. 左侧导航列表
        // =====================================================

        var navList = new ListBox()
            .Items(navItems, item => item.Title)
            .Width(180)
            .BindSelectedIndex(selectedIndex);

        // =====================================================
        // 3. 右侧内容区域
        // =====================================================

        var contentArea = new Border()
            .Padding(24);

        // 更新页面内容
        void UpdateContent()
        {
            int index = selectedIndex.Value;

            if (index < 0 || index >= navItems.Length)
            {
                contentArea.Child(
                    new TextBlock()
                        .Text("请选择左侧导航项")
                        .FontSize(16)
                );

                return;
            }

            var item = navItems[index];

            contentArea.Child(
                new StackPanel()
                    .Spacing(12)
                    .Children(
                        new TextBlock()
                            .Text(item.Title)
                            .FontSize(24)
                            .Bold(),

                        new TextBlock()
                            .Text($"当前页面标识：{item.Key}"),

                        new TextBlock()
                            .Text("这里是根据导航项动态生成的内容区域。")
                    )
            );
        }

        // 监听选中索引变化
        selectedIndex.Changed += UpdateContent;

        // 初始化首页
        UpdateContent();

        // =====================================================
        // 4. DockPanel 布局
        // =====================================================

        var dockPanel = new DockPanel()
            .LastChildFill(true)
            .Children(
                navList.DockLeft(),
                contentArea
            );

        window.Content(dockPanel);

        return window;
    }

    // =====================================================
    // 导航数据类型
    // =====================================================

    record NavItem(string Title, string Key);
}