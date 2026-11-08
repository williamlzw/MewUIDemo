using Aprillz.MewUI;
using Aprillz.MewUI.Resources;

namespace MewUIExtended.FlowGraph;

// ==================== 图片数据 ====================
public sealed class FlowGraphImageData
{
    public FlowGraphImageData(
        IImageSource source,
        double pixelWidth,
        double pixelHeight,
        string? sourcePath = null)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        PixelWidth = pixelWidth > 0 ? pixelWidth : throw new ArgumentOutOfRangeException(nameof(pixelWidth));
        PixelHeight = pixelHeight > 0 ? pixelHeight : throw new ArgumentOutOfRangeException(nameof(pixelHeight));
        SourcePath = sourcePath ?? string.Empty;
    }

    public static FlowGraphImageData FromFile(string path, double pixelWidth, double pixelHeight)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("图片路径不能为空。", nameof(path));
        return new FlowGraphImageData(ImageSource.FromFile(path), pixelWidth, pixelHeight, path);
    }

    /// <summary>
    /// MewUI 图像源。节点图通过 IGraphicsFactory 将它创建为后端 IImage，
    /// 再在节点自身的 OnRender 绘制顺序中调用 DrawImageOriented。
    /// </summary>
    public IImageSource Source { get; }
    public double PixelWidth { get; }
    public double PixelHeight { get; }
    public string SourcePath { get; }

    /// <summary>
    /// 图片的 EXIF/显示方向。IImageSource 创建的 IImage 不携带方向信息，
    /// 因此由数据提供方显式设置；默认按原始方向绘制。
    /// </summary>
    public ImageOrientation Orientation { get; set; } = ImageOrientation.Normal;
}

/// <summary>
/// FlowGraphWorkspace 使用的图片覆盖层布局信息。
/// Bounds 是相对于 Graph 控件左上角的坐标。
/// </summary>
internal readonly record struct FlowGraphImagePlacement(
    int NodeId,
    int PortId,
    bool IsTooltip,
    IImageSource Source,
    Rect Bounds);

// ==================== 组合框数据 ====================
public sealed class FlowGraphComboData
{
    public List<string> Options { get; } = new();
    public int Current { get; set; }
}

// ==================== 按钮数据 ====================
public sealed class FlowGraphButtonData
{
    public string Caption { get; set; } = string.Empty;
}

// ==================== 双按钮数据 ====================
public sealed class FlowGraphDualButtonData
{
    public string Caption1 { get; set; } = string.Empty;
    public string Caption2 { get; set; } = string.Empty;
}

// ==================== 端口 ====================
public sealed class FlowGraphPort
{
    public FlowGraphPort(string name, FlowGraphPortDirection direction,
        FlowGraphDataType dataType = FlowGraphDataType.Any)
    {
        Name = name ?? string.Empty;
        Direction = direction;
        DataType = dataType;
    }

    public int Id { get; internal set; }
    public string Name { get; set; }
    public FlowGraphPortDirection Direction { get; set; }
    public FlowGraphDataType DataType { get; set; }

    // 子组件
    public FlowGraphWidgetKind Widget { get; set; }
    public int WidgetId { get; set; }
    public double WidgetWidth { get; set; }
    public double WidgetHeight { get; set; } = 30;

    // 数据
    public object? Data { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? ImagePath { get; set; }

    // 组合框
    public FlowGraphComboData? ComboData { get; set; }
    // 按钮
    public FlowGraphButtonData? ButtonData { get; set; }
    // 双按钮
    public FlowGraphDualButtonData? DualButtonData { get; set; }

    public bool IsConnected { get; internal set; }

    // 布局（相对节点）
    internal Rect PortRect { get; set; }
    internal Rect WidgetRect { get; set; }

    public object? Tag { get; set; }
    public Func<object?, object?>? CloneData { get; set; }
}

// ==================== 节点 ====================
public sealed class FlowGraphNode
{
    public FlowGraphNode(string title)
    {
        Title = title ?? string.Empty;
    }

    public int Id { get; internal set; }
    public int CardType { get; internal set; }
    public string Title { get; set; }
    public string Category { get; set; } = string.Empty;
    public double X { get; set; } = 80;
    public double Y { get; set; } = 80;
    public double Width { get; set; } = 260;
    public double Height { get; internal set; } = 120;
    public Color HeaderColor { get; set; } = Color.FromRgb(100, 100, 100);
    public FlowGraphExecutionStatus ExecutionStatus { get; set; }
    public string LastError { get; set; } = string.Empty;
    internal int LastExecutedPass { get; set; }

    public List<FlowGraphPort> Ports { get; } = new();

    // 动态端口计数
    public int DynamicPortCount { get; internal set; }
    public int DynamicPort2Count { get; internal set; }

    public object? Tag { get; set; }
}

// ==================== 连线 ====================
public sealed class FlowGraphConnection
{
    public int Id { get; internal set; }
    public int FromNodeId { get; internal set; }
    public int FromPortIndex { get; internal set; }
    public int ToNodeId { get; internal set; }
    public int ToPortIndex { get; internal set; }
    public Point ControlPoint1 { get; set; }
    public Point ControlPoint2 { get; set; }
    public bool HasCustomControlPoints { get; set; }
}

// ==================== 链式执行上下文 ====================
internal sealed class FlowGraphChainContext
{
    public int ChainEndNode { get; set; }
    public int[] NodeOrder { get; set; } = Array.Empty<int>();
    public int NodeOrderCount { get; set; }
    public int CurrentIndex { get; set; }
    public int ExecutionPass { get; set; }
    public FlowGraphExecutionMode ExecuteMode { get; set; }
}

// ==================== ★ 图片拖放事件参数 ====================
public sealed class FlowGraphImageDroppedEventArgs : EventArgs
{
    public FlowGraphImageDroppedEventArgs(int nodeId, int cardType, string filePath)
    {
        NodeId = nodeId;
        CardType = cardType;
        FilePath = filePath;
    }

    public int NodeId { get; }
    public int CardType { get; }
    public string FilePath { get; }
}
