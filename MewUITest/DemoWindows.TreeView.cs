using System;
using System.Collections.Generic;
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUIDemo;

public class TreeItem
{
    public required string Name { get; init; }
    public List<TreeItem> Children { get; init; } = new();
    public bool IsExpanded { get; init; }

    public static string BuildPath(TreeItem target, List<TreeItem> roots)
    {
        var path = new List<string>();
        if (FindPath(target, roots, path))
            return string.Join(" / ", path);
        return target.Name;
    }

    private static bool FindPath(TreeItem target, List<TreeItem> items, List<string> path)
    {
        foreach (var item in items)
        {
            path.Add(item.Name);
            if (item == target) return true;
            if (FindPath(target, item.Children, path)) return true;
            path.RemoveAt(path.Count - 1);
        }
        return false;
    }
}

static partial class DemoWindows
{
    public static Window CreateTreeViewWindow()
    {
        var window = new Window();
        window.Title("TreeView 示例");
        window.Resizable(480, 420);
        window.Padding(0);

        var rootItems = new List<TreeItem>
        {
            new()
            {
                Name = "Documents",
                IsExpanded = true,
                Children = new()
                {
                    new() { Name = "Resume.pdf" },
                    new() { Name = "Notes.txt" }
                }
            },
            new()
            {
                Name = "Pictures",
                Children = new()
                {
                    new() { Name = "Vacation.jpg" },
                    new()
                    {
                        Name = "Screenshots",
                        Children = new()
                        {
                            new() { Name = "shot-01.png" },
                            new() { Name = "shot-02.png" }
                        }
                    }
                }
            }
        };

        var treeView = new TreeView();
        treeView.Background = Color.FromArgb(255, 220, 220, 220);

        // 样式表
        treeView.StyleSheet = new StyleSheet();
        var hotTreeItemStyle = new Style(typeof(ItemContainer))
        {
            Triggers =
            [
                new StateTrigger
                {
                    Match = VisualStateFlags.Hot,
                    Setters =
                    [
                        Setter.Create(
                            Control.BackgroundProperty,
                            (Theme t) => Color.FromArgb(255, 134, 134, 146))
                    ],
                },
                new StateTrigger
                {
                    Match = VisualStateFlags.Selected,
                    Setters =
                    [
                        Setter.Create(
                            Control.BackgroundProperty,
                            (Theme t) => Color.FromArgb(255, 166, 166, 168))
                    ],
                }
            ]
        };
        treeView.StyleSheet.Define<ItemContainer>(hotTreeItemStyle);

        treeView.CanvasPosition(20, 20);
        treeView.Size(420, 200);
        treeView.Items(
            roots: rootItems,
            childrenSelector: item => item.Children,
            textSelector: item => item.Name,
            isExpandableSelector: item => item.Children.Count > 0
        );

        treeView.SelectionChanged += item =>
        {
            if (item is TreeItem selected)
            {
                var path = TreeItem.BuildPath(selected, rootItems);
                Console.WriteLine($"选中：{path}");
            }
            else
            {
                Console.WriteLine("请选择节点");
            }
        };

        treeView.ItemTemplate(new DelegateTemplate<TreeItem>(
            build: ctx =>
            {
                var icon = new Label();
                var name = new Label();
                var panel = new StackPanel()
                    .Horizontal()
                    .Spacing(6)
                    .Children(icon, name);
                panel.Register(ctx, "Root");
                icon.Register(ctx, "Icon");
                name.Register(ctx, "Name");
                return panel;
            },
            bind: (view, item, index, ctx) =>
            {
                var treeItem = (TreeItem)item;
                ctx.Get<Label>("Icon").Text = treeItem.Children.Count > 0 ? "📁" : "📄";
                ctx.Get<Label>("Name").Text = treeItem.Name;
            }
        ));

        var canvas = new Canvas();
        canvas.Add(treeView);

        window.Content(canvas);
        return window;
    }
}