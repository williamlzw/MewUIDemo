using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateItemsControlWindow()
    {
        var window = new Window();
        window.Title("ItemsControl 示例");
        window.Resizable(500, 400);
        window.Padding(20);

        // 1. 准备数据
        var items = new List<string> { "苹果", "香蕉", "橙子", "葡萄", "西瓜", "草莓" };

        // 2. 创建 ItemsControl
        var itemsControl = new ItemsControl();

        // 3. 设置数据源
        itemsControl.ItemsSource(ItemsView.Create(items));

        // 4. 设置项模板 (ItemTemplate)
        // 这里使用 DelegateTemplate，为每个数据项创建一个简单的 TextBlock
        itemsControl.ItemTemplate(new DelegateTemplate<string>(
            build: _ => new TextBlock().FontSize(16).Margin(new Thickness(0, 4)),
            bind: (view, item, index, _) =>
            {
                ((TextBlock)view).Text = $"【{index}】 {item}";
            }
        ));

        // 这里设定每个项的固定宽度和高度，超过窗口宽度时会自动换行
        itemsControl.WrapPresenter(100, 40);

        window.Content(itemsControl);
        return window;
    }
}
