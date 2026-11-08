using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using MewUI.Extended;
using MewUIExtended.FlowGraph;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateFlowGraphWindow()
    {
        var window = new Window();
        window.Title("MewUI 节点图");
        window.Resizable(1400, 1000);
        window.Content(FlowGraphDemo.CreateWorkspace());
        return window;
    }
}

public static class FlowGraphDemo
{
    private const int CardLocalText = 1;
    private const int CardLLMText = 2;
    private const int CardTextToImage = 3;
    private const int CardLocalImage = 4;
    private const int CardRefImage = 5;
    private const int CardTextRender = 6;

    public static FlowGraphWorkspace CreateWorkspace()
    {
        var graph = new FlowGraphControl
        {
            Background = Color.FromRgb(80, 80, 80)
        };

        RegisterCards(graph);

        // 创建示例节点
        var localText = graph.CreateNodeFromCard(CardLocalText, 100, 100);
        var llmText = graph.CreateNodeFromCard(CardLLMText, 100, 400);
        var textToImage = graph.CreateNodeFromCard(CardTextToImage, 500, 200);
        var localImage = graph.CreateNodeFromCard(CardLocalImage, 500, 500);
        var refImage = graph.CreateNodeFromCard(CardRefImage, 900, 300);

        // 创建连线
        graph.AddConnection(localText.Id, 1, llmText.Id, 0);
        graph.AddConnection(llmText.Id, 3, textToImage.Id, 0);
        graph.AddConnection(localImage.Id, 2, refImage.Id, 7);

        // 按钮事件
        graph.ButtonClicked += (_, args) =>
        {
            // 本地图卡片的第 0 号端口是“选择图片”按钮。
            if (args.CardType == CardLocalImage && args.PortIndex == 0)
            {
                string? imagePath = null;
                try
                {
                    imagePath = FileDialog.OpenFile(
                        new OpenFileDialogOptions
                        {
                            Owner = graph.OwnerWindow,
                            Title = "选择本地图片",
                            Filters = FileFilter.Parse(
                                "图片文件 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|所有文件 (*.*)|*.*"
                            )
                        }
                    );

                    // 用户取消选择时不修改节点数据。
                    if (string.IsNullOrWhiteSpace(imagePath))
                        return;

                    // 图片预览端口为 1，图片输出端口为 2。
                    // 同一份图片数据同时作为预览内容和节点输出，供下游节点读取。
                    var imageData = FlowGraphImageData.FromFile(imagePath, 512, 512);
                    graph.SetPortImage(args.NodeId, 1, imageData);
                    graph.SetPortImage(args.NodeId, 2, imageData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"选择或加载图片失败：{imagePath ?? string.Empty}{Environment.NewLine}{ex}");
                }

                return;
            }
            // 文生图卡片：保存图片按钮
            if (args.CardType == CardTextToImage && args.PortIndex == 3)
            {
                string? savePath = null;

                try
                {
                    // 图片预览端口索引为 2
                    if (!graph.TryGetPortData(args.NodeId, 2, out var data) ||
                        data is not FlowGraphImageData imageData)
                    {
                        Console.WriteLine("当前没有可保存的图片。");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(imageData.SourcePath) ||
                        !File.Exists(imageData.SourcePath))
                    {
                        Console.WriteLine("图片没有有效的源文件路径，无法保存。");
                        return;
                    }

                    savePath = FileDialog.SaveFile(
                        new SaveFileDialogOptions
                        {
                            Owner = graph.OwnerWindow,
                            Title = "保存生成图片",
                            FileName = "GeneratedImage.png",
                            Filters = FileFilter.Parse(
                                "PNG 图片 (*.png)|*.png|" +
                                "JPEG 图片 (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                                "BMP 图片 (*.bmp)|*.bmp"
                            )
                        });

                    // 用户取消保存
                    if (string.IsNullOrWhiteSpace(savePath))
                        return;

                    // 复制原始图片文件
                    File.Copy(imageData.SourcePath, savePath, overwrite: true);

                    Console.WriteLine($"图片保存成功：{savePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"保存图片失败：{savePath ?? string.Empty}{Environment.NewLine}{ex}");
                }

                return;
            }
            if (args.WhichButton == 1) // 双按钮左（+）
            {
                graph.AddDynamicPort(args.NodeId, 1);
            }
            else if (args.WhichButton == 2) // 双按钮右（-）
            {
                graph.RemoveDynamicPort(args.NodeId, 1);
            }
        };

        // 侧边栏添加卡片
        graph.SidebarAddCard += (_, args) =>
        {
            graph.CreateNodeFromCard(args.CardType);
        };

        var workspace = new FlowGraphWorkspace(graph);
        return workspace;
    }

    private static void RegisterCards(FlowGraphControl graph)
    {
        // 1. 本地文本
        var localText = new FlowGraphCardDescriptor(CardLocalText, "本地文本")
        {
            HeaderColor = Color.FromRgb(80, 130, 80),
            Executor = async (ctx, ct) =>
            {
                var text = ctx.GetInput<string>("内容") ?? "";
                ctx.SetOutput("输出文本", text);
                await Task.CompletedTask;
            }
        };
        localText.Ports.Add(new FlowGraphPortDescriptor("内容", FlowGraphPortDirection.Inline, FlowGraphDataType.Text)
        {
            Widget = FlowGraphWidgetKind.Text,
            WidgetWidth = 300,
            WidgetHeight = 60
        });
        localText.Ports.Add(new FlowGraphPortDescriptor("输出文本", FlowGraphPortDirection.Output, FlowGraphDataType.Text));
        graph.RegisterCardType(localText);

        // 2. 大模型文本
        var llmText = new FlowGraphCardDescriptor(CardLLMText, "大模型文本")
        {
            HeaderColor = Color.FromRgb(80, 80, 180),
            Executor = async (ctx, ct) =>
            {
                var input = ctx.GetInput<string>("输入文本") ?? "";
                await Task.Delay(2000, ct); // 模拟异步
                ctx.SetOutput("输出文本", $"{input}");
            }
        };
        llmText.Ports.Add(new FlowGraphPortDescriptor("输入文本", FlowGraphPortDirection.Input, FlowGraphDataType.Text));
        var modelPort = new FlowGraphPortDescriptor("模型", FlowGraphPortDirection.Inline, FlowGraphDataType.Combo)
        {
            Widget = FlowGraphWidgetKind.Combo,
            WidgetWidth = 300,
            WidgetHeight = 30
        };
        modelPort.Options.AddRange(new[] { "deepseek", "豆包", "GPT-4" });
        llmText.Ports.Add(modelPort);
        llmText.Ports.Add(new FlowGraphPortDescriptor("提示词", FlowGraphPortDirection.Inline, FlowGraphDataType.Text)
        {
            Widget = FlowGraphWidgetKind.Text,
            WidgetWidth = 300,
            WidgetHeight = 60
        });
        llmText.Ports.Add(new FlowGraphPortDescriptor("输出文本", FlowGraphPortDirection.Output, FlowGraphDataType.Text));
        graph.RegisterCardType(llmText);

        // 3. 文生图
        var textToImage = new FlowGraphCardDescriptor(CardTextToImage, "文生图")
        {
            HeaderColor = Color.FromRgb(180, 80, 80),
            Executor = async (ctx, ct) =>
            {
                var prompt = ctx.GetInput<string>("输入文本") ?? "";
                if (string.IsNullOrEmpty(prompt)) { ctx.Fail("输入文本为空"); return; }
                await Task.Delay(2000, ct);
                // 实际项目中这里调用生图API
            }
        };
        textToImage.Ports.Add(new FlowGraphPortDescriptor("输入文本", FlowGraphPortDirection.Input, FlowGraphDataType.Text));
        var resPort = new FlowGraphPortDescriptor("分辨率", FlowGraphPortDirection.Inline, FlowGraphDataType.Combo)
        {
            Widget = FlowGraphWidgetKind.Combo,
            WidgetWidth = 300,
            WidgetHeight = 30
        };
        resPort.Options.AddRange(new[] { "1K", "2K", "4K" });
        resPort.SelectedIndex = 1;
        textToImage.Ports.Add(resPort);
        textToImage.Ports.Add(new FlowGraphPortDescriptor("预览", FlowGraphPortDirection.Inline, FlowGraphDataType.Image)
        {
            Widget = FlowGraphWidgetKind.ImagePreview,
            WidgetWidth = 300,
            WidgetHeight = 200
        });
        textToImage.Ports.Add(new FlowGraphPortDescriptor("保存图片",
            FlowGraphPortDirection.Inline, FlowGraphDataType.Object)
        {
            Widget = FlowGraphWidgetKind.Button,
            WidgetWidth = 300,
            WidgetHeight = 30,
            DefaultButtonData = new FlowGraphButtonData
            {
                Caption = "保存图片"
            }
        });
        textToImage.Ports.Add(new FlowGraphPortDescriptor("输出图片", FlowGraphPortDirection.Output, FlowGraphDataType.Image));

        graph.RegisterCardType(textToImage);

        // 4. 本地图
        var localImage = new FlowGraphCardDescriptor(CardLocalImage, "本地图")
        {
            HeaderColor = Color.FromRgb(80, 150, 130)
        };
        localImage.Ports.Add(new FlowGraphPortDescriptor("选择图片", FlowGraphPortDirection.Inline, FlowGraphDataType.Object)
        {
            Widget = FlowGraphWidgetKind.Button,
            WidgetWidth = 300,
            WidgetHeight = 30,
            DefaultButtonData = new FlowGraphButtonData { Caption = "选择图片" }
        });
        localImage.Ports.Add(new FlowGraphPortDescriptor("预览", FlowGraphPortDirection.Inline, FlowGraphDataType.Image)
        {
            Widget = FlowGraphWidgetKind.ImagePreview,
            WidgetWidth = 300,
            WidgetHeight = 200
        });
        localImage.Ports.Add(new FlowGraphPortDescriptor("输出图片", FlowGraphPortDirection.Output, FlowGraphDataType.Image));
        graph.RegisterCardType(localImage);

        // 5. 参考生图（带动态端口组）
        var refImage = new FlowGraphCardDescriptor(CardRefImage, "参考生图")
        {
            HeaderColor = Color.FromRgb(150, 80, 150),
            Executor = async (ctx, ct) =>
            {
                var prompt = ctx.GetInput<string>("文本") ?? "";
                if (string.IsNullOrEmpty(prompt)) { ctx.Fail("文本为空"); return; }
                await Task.Delay(3000, ct);
            }
        };
        refImage.Ports.Add(new FlowGraphPortDescriptor("文本", FlowGraphPortDirection.Input, FlowGraphDataType.Text));
        refImage.Ports.Add(new FlowGraphPortDescriptor("参考图控制", FlowGraphPortDirection.Inline, FlowGraphDataType.Any)
        {
            Widget = FlowGraphWidgetKind.DualButton,
            WidgetWidth = 300,
            WidgetHeight = 30,
            DefaultDualButtonData = new FlowGraphDualButtonData { Caption1 = "+参考图", Caption2 = "-参考图" }
        });
        var modelPort2 = new FlowGraphPortDescriptor("模型", FlowGraphPortDirection.Inline, FlowGraphDataType.Combo)
        {
            Widget = FlowGraphWidgetKind.Combo,
            WidgetWidth = 300,
            WidgetHeight = 30
        };
        modelPort2.Options.Add("豆包");
        refImage.Ports.Add(modelPort2);
        refImage.Ports.Add(new FlowGraphPortDescriptor("预览", FlowGraphPortDirection.Inline, FlowGraphDataType.Image)
        {
            Widget = FlowGraphWidgetKind.ImagePreview,
            WidgetWidth = 300,
            WidgetHeight = 200
        });
        refImage.Ports.Add(new FlowGraphPortDescriptor("输出图片", FlowGraphPortDirection.Output, FlowGraphDataType.Image));

        // ★ 动态端口组：参考图片输入端口，最少1个，最多10个
        refImage.DynamicPorts = new FlowGraphDynamicPortGroup(
            namePrefix: "参考图片",
            direction: FlowGraphPortDirection.Input,
            dataType: FlowGraphDataType.Image,
            minCount: 1,
            maxCount: 10,
            baseIndex: 4); // 在"预览"端口之后插入

        graph.RegisterCardType(refImage);

        // 6. 文本显示
        var textRender = new FlowGraphCardDescriptor(CardTextRender, "文本显示")
        {
            HeaderColor = Color.FromRgb(60, 120, 160)
        };
        textRender.Ports.Add(new FlowGraphPortDescriptor("输入文本", FlowGraphPortDirection.Input, FlowGraphDataType.Text)
        {
            Widget = FlowGraphWidgetKind.Display,
            WidgetWidth = 500,
            WidgetHeight = 120
        });
        graph.RegisterCardType(textRender);


    }
}
