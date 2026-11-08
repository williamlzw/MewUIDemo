using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using MewUI.Extended;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateExDrawingBoardWindow()
    {
        var window = new Window();
        window.Title("ExDrawingBoard 示例");
        window.Resizable(560, 480);
        window.Padding(0);

        var board = new ExDrawingBoard
        {
            PenColor = Color.FromRgb(30, 30, 30),
            PenWidth = 3,
            EraserWidth = 15,
            CanvasBackground = Color.FromRgb(255, 255, 255)
        };

        // 工具栏。
        var toolbar = new StackPanel()
            .Horizontal()
            .Spacing(8)
            .Children(
                new Button()
                    .Content("画笔")
                    .OnClick(() => {board.SetPen(); board.SetPenWidth(2); board.SetPenColor(
                            Color.FromRgb(30, 30, 30)); }),

                new Button()
                    .Content("橡皮擦")
                    .OnClick(() => board.SetEraser()),

                new Button()
                    .Content("细画笔")
                    .OnClick(() => board.SetPenWidth(2)),

                new Button()
                    .Content("粗画笔")
                    .OnClick(() => board.SetPenWidth(6)),

                new Button()
                    .Content("红色")
                    .OnClick(() =>
                    {
                        board.SetPen();
                        board.SetPenColor(
                            Color.FromRgb(255, 0, 0));
                    }),

                new Button()
                    .Content("蓝色")
                    .OnClick(() =>
                    {
                        board.SetPen();
                        board.SetPenColor(
                            Color.FromRgb(0, 100, 255));
                    }),

                new Button()
                    .Content("清空")
                    .OnClick(() => board.Clear()),

                new Button()
                    .Content("保存 PNG")
                    .OnClick(() =>
                    {
                        string path = Path.Combine(
                            AppContext.BaseDirectory,
                            "DrawingBoard.png");

                        board.SavePng(path, 2.0);
                    })
            );

        var layout = new Grid()
            .Rows("Auto,*")
            .Columns("*")
            .Children(
                toolbar.Row(0),
                board.Row(1)
            );

        window.Content(layout);

        return window;
    }
}