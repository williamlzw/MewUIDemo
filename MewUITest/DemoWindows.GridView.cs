
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;
using System.Collections.Generic;

namespace MewUIDemo;

static partial class DemoWindows
{
    // 数据模型
    public class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string City { get; set; } = "";
    }

    public static Window CreateGridViewWindow()
    {
        var window = new Window();
        window.Title("GridView 示例");
        window.Resizable(600, 400);
        window.Padding(20);

        // 1. 准备数据源
        var people = new List<Person>
        {
            new() { Name = "张三", Age = 28, City = "北京" },
            new() { Name = "李四", Age = 34, City = "上海" },
            new() { Name = "王五", Age = 25, City = "广州" },
            new() { Name = "赵六", Age = 42, City = "深圳" },
            new() { Name = "孙七", Age = 19, City = "成都" },
        };

        // 2. 创建 GridView
        var gridView = new GridView();

        // 3. 设置数据源
        gridView.ItemsSource(people);

        // 4. 设置列
        gridView.Columns(

            // 第一列：姓名
            new GridViewColumn<Person>()
                .Header("姓名")
                .Width(180)
                .Bind(
                    build: _ => new TextBlock()
                        .FontSize(14),

                    bind: (TextBlock cell, Person item) =>
                    {
                        cell.Text = item.Name;
                    }
                ),

            // 第二列：年龄
            new GridViewColumn<Person>()
                .Header("年龄")
                .Width(120)
                .Bind(
                    build: _ => new TextBlock()
                        .FontSize(14),

                    bind: (TextBlock cell, Person item) =>
                    {
                        cell.Text = item.Age.ToString();
                    }
                ),

            // 第三列：城市
            new GridViewColumn<Person>()
                .Header("城市")
                .Width(180)
                .Bind(
                    build: _ => new TextBlock()
                        .FontSize(14),

                    bind: (TextBlock cell, Person item) =>
                    {
                        cell.Text = item.City;
                    }
                )
        );

        // 5. 设置行高
        gridView.RowHeight(36);

        // 6. 显示网格线
        gridView.ShowGridLines(true);

        // 7. 选中行事件
        gridView.OnSelectionChanged(obj =>
        {
            if (obj is Person person)
            {
                Console.WriteLine(
                    $"选中：{person.Name}，年龄：{person.Age}，城市：{person.City}"
                );
            }
        });

        window.Content(gridView);

        return window;
    }
}
