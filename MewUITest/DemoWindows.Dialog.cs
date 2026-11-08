
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using System;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateFileDialogWindow()
    {
        var window = new Window();
        window.Title("FileDialog 示例");
        window.Resizable(520, 360);
        window.Padding(0);

        // ---- 示例 1：打开单个文件 ----
        var openFileButton = new Button()
            .Content("打开单个文件")
            .Width(180)
            .Height(36)
            .OnClick(() =>
            {
                var path = FileDialog.OpenFile(
                    new OpenFileDialogOptions
                    {
                        Owner = window,
                        Title = "选择要打开的文件",
                        Filters = FileFilter.Parse(
                            "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*"
                        )
                    }
                );

                if (path != null)
                {
                    NativeMessageBox.Show(
                        window.Handle,
                        $"已选择文件：\n{path}",
                        "打开成功",
                        NativeMessageBoxButtons.Ok,
                        NativeMessageBoxIcon.Information
                    );
                }
                else
                {
                    Console.WriteLine("用户取消了打开文件。");
                }
            });

        // ---- 示例 2：打开多个文件 ----
        var openFilesButton = new Button()
            .Content("打开多个文件")
            .Width(180)
            .Height(36)
            .OnClick(() =>
            {
                var paths = FileDialog.OpenFiles(
                    new OpenFileDialogOptions
                    {
                        Owner = window,
                        Title = "选择多个文件",
                        Filters = FileFilter.Parse(
                            "图片文件 (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|所有文件 (*.*)|*.*"
                        )
                    }
                );

                if (paths != null && paths.Length > 0)
                {
                    var message =
                        $"共选择了 {paths.Length} 个文件：\n\n" +
                        string.Join("\n", paths);

                    NativeMessageBox.Show(
                        window.Handle,
                        message,
                        "打开成功",
                        NativeMessageBoxButtons.Ok,
                        NativeMessageBoxIcon.Information
                    );
                }
                else
                {
                    Console.WriteLine("用户取消了多选文件。");
                }
            });

        // ---- 示例 3：保存文件 ----
        var saveFileButton = new Button()
            .Content("保存文件")
            .Width(180)
            .Height(36)
            .OnClick(() =>
            {
                var savePath = FileDialog.SaveFile(
                    new SaveFileDialogOptions
                    {
                        Owner = window,
                        Title = "另存为",
                        Filters = FileFilter.Parse(
                            "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*"
                        ),
                        FileName = "新建文档.txt"
                    }
                );

                if (savePath != null)
                {
                    NativeMessageBox.Show(
                        window.Handle,
                        $"保存路径：\n{savePath}",
                        "保存成功",
                        NativeMessageBoxButtons.Ok,
                        NativeMessageBoxIcon.Information
                    );
                }
                else
                {
                    Console.WriteLine("用户取消了保存。");
                }
            });

        // ---- 示例 4：选择文件夹 ----
        var selectFolderButton = new Button()
            .Content("选择文件夹")
            .Width(180)
            .Height(36)
            .OnClick(() =>
            {
                var folder = FileDialog.SelectFolder(
                    new FolderDialogOptions
                    {
                        Owner = window,
                        Title = "选择文件夹"
                    }
                );

                if (folder != null)
                {
                    NativeMessageBox.Show(
                        window.Handle,
                        $"已选择文件夹：\n{folder}",
                        "选择成功",
                        NativeMessageBoxButtons.Ok,
                        NativeMessageBoxIcon.Information
                    );
                }
                else
                {
                    Console.WriteLine("用户取消了文件夹选择。");
                }
            });

        // ---- 布局 ----
        var panel = new StackPanel()
            .Vertical()
            .Spacing(14)
            .Padding(24)
            .Children(
                new TextBlock()
                    .Text("文件对话框示例")
                    .Bold(),

                openFileButton,
                openFilesButton,
                saveFileButton,
                selectFolderButton
            );

        window.Content(panel);

        return window;
    }
}
