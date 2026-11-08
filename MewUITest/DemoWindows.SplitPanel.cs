
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateSplitPanelWindow()
    {
        var window = new Window();
        window.Title("SplitPanel 示例");
        window.Resizable(760, 520);
        window.Padding(0);

        // ---- 左侧：项目导航区域 ----
        var projectPanel = new GroupBox()
            .Header("项目资源管理器")
            .Content(
                new Grid()
                    .Rows("Auto,*,Auto")
                    .Spacing(8)
                    .Padding(12)
                    .Children(
                        new TextBlock()
                            .Text("项目目录")
                            .Bold()
                            .Row(0),

                        new ListBox()
                            .Items(
                                "Solution",
                                "  ├─ Main.cs",
                                "  ├─ Program.cs",
                                "  ├─ Models",
                                "  │   └─ Player.cs",
                                "  ├─ Views",
                                "  │   └─ MainView.cs",
                                "  └─ Assets"
                            )
                            .SelectedIndex(1)
                            .StretchHorizontal()
                            .StretchVertical()
                            .Row(1),

                        new TextBlock()
                            .Text("项目资源管理器")
                            .Row(2)
                    )
            );

        // ---- 右上：代码编辑区域 ----
        var editorPanel = new GroupBox()
            .Header("代码编辑器")
            .Content(
                new Grid()
                    .Rows("Auto,*")
                    .Spacing(8)
                    .Padding(12)
                    .Children(
                        new TextBlock()
                            .Text("Main.cs")
                            .Bold()
                            .Row(0),

                        new MultiLineTextBox()
                            .Text(
                                "using System;\n\n" +
                                "class Program\n" +
                                "{\n" +
                                "    static void Main()\n" +
                                "    {\n" +
                                "        Console.WriteLine(\"Hello MewUI!\");\n" +
                                "    }\n" +
                                "}"
                            )
                            .AcceptTab(true)
                            .Wrap(false)
                            .StretchHorizontal()
                            .StretchVertical()
                            .Row(1)
                    )
            );

        // ---- 右下：输出区域 ----
        var outputPanel = new GroupBox()
            .Header("输出")
            .Content(
                new Grid()
                    .Rows("Auto,*")
                    .Spacing(8)
                    .Padding(12)
                    .Children(
                        new TextBlock()
                            .Text("程序运行输出：")
                            .Bold()
                            .Row(0),

                        new MultiLineTextBox()
                            .Text(
                                "Build started...\n" +
                                "Build succeeded.\n\n" +
                                "Hello MewUI!\n\n" +
                                "Process exited with code 0."
                            )
                            .IsReadOnly(true)
                            .Wrap(true)
                            .StretchHorizontal()
                            .StretchVertical()
                            .Row(1)
                    )
            );

        // ---- 右侧：上下分割 ----
        var rightSplitPanel = new SplitPanel()
            .Vertical()
            .FirstLength(new GridLength(2, GridUnitType.Star))
            .SecondLength(new GridLength(1, GridUnitType.Star))
            .SplitterThickness(6)
            .MinFirst(120)
            .MinSecond(80)
            .First(editorPanel)
            .Second(outputPanel);

        // ---- 主区域：左右分割 ----
        var mainSplitPanel = new SplitPanel()
            .Horizontal()
            .FirstLength(new GridLength(1, GridUnitType.Star))
            .SecondLength(new GridLength(3, GridUnitType.Star))
            .SplitterThickness(6)
            .MinFirst(140)
            .MaxFirst(320)
            .MinSecond(250)
            .First(projectPanel)
            .Second(rightSplitPanel);

        window.Content(mainSplitPanel);

        return window;
    }
}
