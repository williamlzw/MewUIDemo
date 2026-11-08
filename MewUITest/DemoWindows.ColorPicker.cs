using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateColorPickerWindow()
    {
        var window = new Window();
        window.Title("ColorPicker 示例");
        window.Resizable(400, 300);
        window.Padding(0);

        // 1. 创建一个用于实时显示当前颜色的 Label
        var color = new Color(255, 100, 50);
        var colorLabel = new Label();
        colorLabel.Text(color.ToString()); // 初始文本
        colorLabel.CanvasPosition(50, 120);
        colorLabel.Size(250, 24);

        // 2. 创建 ColorPicker 并设置属性
        var colorPicker = new ColorPicker();
        // 设置初始颜色（也可以不设，默认使用主题色）
        colorPicker.SelectedColor(color);
        // 开启 Alpha 透明度编辑（可选）
        colorPicker.ShowAlpha(true);
        // 订阅颜色变更事件，同步更新 Label 文本
        colorPicker.OnSelectedColorChanged(color =>
        {
            colorLabel.Text(color.ToString()); // 假设 Color 有 ToHex() 方法，若没有可改用 color.ToString()
            //Console.WriteLine($"颜色已变更: {color}");
        });

        colorPicker.CanvasPosition(50, 60);
        colorPicker.Size(200, 32);

        // 3. 将控件添加到 Canvas
        var canvas = new Canvas();
        canvas.Add(colorPicker);
        canvas.Add(colorLabel);

        window.Content(canvas);
        return window;
    }
};