//FlowGraphCards.cs
using Aprillz.MewUI;
using MewUI.Extended;

namespace MewUIExtended.FlowGraph;

// ==================== 端口描述符 ====================
public sealed class FlowGraphPortDescriptor
{
    public FlowGraphPortDescriptor(
        string name,
        FlowGraphPortDirection direction,
        FlowGraphDataType dataType = FlowGraphDataType.Any)
    {
        Name = name ?? string.Empty;
        Direction = direction;
        DataType = dataType;
    }

    public string Name { get; set; }
    public FlowGraphPortDirection Direction { get; set; }
    public FlowGraphDataType DataType { get; set; }
    public FlowGraphWidgetKind Widget { get; set; }
    public int WidgetId { get; set; }
    public double WidgetWidth { get; set; }
    public double WidgetHeight { get; set; } = 30;
    public string DefaultValue { get; set; } = string.Empty;
    public object? DefaultData { get; set; }
    public List<string> Options { get; } = new();
    public int SelectedIndex { get; set; }
    public FlowGraphButtonData? DefaultButtonData { get; set; }
    public FlowGraphDualButtonData? DefaultDualButtonData { get; set; }
    public Func<object?, object?>? CloneDefaultData { get; set; }

    internal FlowGraphPort CreatePort()
    {
        var port = new FlowGraphPort(Name, Direction, DataType)
        {
            Widget = Widget,
            WidgetId = WidgetId,
            WidgetWidth = WidgetWidth,
            WidgetHeight = WidgetHeight,
            Value = DefaultValue ?? string.Empty,
            CloneData = CloneDefaultData
        };

        if (DefaultData != null)
            port.Data = CloneDefaultData != null ? CloneDefaultData(DefaultData) : DefaultData;
        else if (!string.IsNullOrEmpty(DefaultValue))
            port.Data = DefaultValue;

        // 组合框
        if (Widget == FlowGraphWidgetKind.Combo && Options.Count > 0)
        {
            port.ComboData = new FlowGraphComboData();
            foreach (var opt in Options) port.ComboData.Options.Add(opt);
            port.ComboData.Current = Math.Clamp(SelectedIndex, 0, Options.Count - 1);
            port.Value = Options[port.ComboData.Current];
            port.Data = port.Value;
        }

        // 按钮
        if (Widget == FlowGraphWidgetKind.Button)
        {
            port.ButtonData = new FlowGraphButtonData
            {
                Caption = DefaultButtonData?.Caption ?? DefaultValue ?? Name
            };
        }

        // 双按钮
        if (Widget == FlowGraphWidgetKind.DualButton)
        {
            port.DualButtonData = new FlowGraphDualButtonData
            {
                Caption1 = DefaultDualButtonData?.Caption1 ?? "+",
                Caption2 = DefaultDualButtonData?.Caption2 ?? "-"
            };
        }

        return port;
    }

    internal FlowGraphPortDescriptor Clone()
    {
        var clone = new FlowGraphPortDescriptor(Name, Direction, DataType)
        {
            Widget = Widget,
            WidgetId = WidgetId,
            WidgetWidth = WidgetWidth,
            WidgetHeight = WidgetHeight,
            DefaultValue = DefaultValue,
            DefaultData = DefaultData,
            SelectedIndex = SelectedIndex,
            DefaultButtonData = DefaultButtonData,
            DefaultDualButtonData = DefaultDualButtonData,
            CloneDefaultData = CloneDefaultData
        };
        foreach (var opt in Options) clone.Options.Add(opt);
        return clone;
    }
}

// ==================== 动态端口组 ====================
public sealed class FlowGraphDynamicPortGroup
{
    public FlowGraphDynamicPortGroup(
        string namePrefix,
        FlowGraphPortDirection direction,
        FlowGraphDataType dataType,
        int minCount = 0,
        int maxCount = 8,
        int baseIndex = -1)
    {
        NamePrefix = namePrefix ?? string.Empty;
        Direction = direction;
        DataType = dataType;
        MinCount = minCount;
        MaxCount = maxCount;
        BaseIndex = baseIndex;
    }

    public string NamePrefix { get; set; }
    public FlowGraphPortDirection Direction { get; set; }
    public FlowGraphDataType DataType { get; set; }
    public int MinCount { get; set; }
    public int MaxCount { get; set; }
    public int BaseIndex { get; set; }

    internal FlowGraphDynamicPortGroup Clone() => new(
        NamePrefix, Direction, DataType, MinCount, MaxCount, BaseIndex);

    internal FlowGraphPort CreatePort(int ordinal)
        => new($"{NamePrefix}{ordinal}", Direction, DataType);
}

// ==================== 卡片描述符 ====================
public sealed class FlowGraphCardDescriptor
{
    public FlowGraphCardDescriptor(int cardType, string name)
    {
        if (cardType < 1)
            throw new ArgumentOutOfRangeException(nameof(cardType), "CardType 必须大于 0。");
        CardType = cardType;
        Name = name ?? string.Empty;
    }

    public int CardType { get; }
    public string Name { get; set; }
    public Color HeaderColor { get; set; } = Color.FromRgb(100, 100, 100);
    public List<FlowGraphPortDescriptor> Ports { get; } = new();
    public FlowGraphDynamicPortGroup? DynamicPorts { get; set; }
    public FlowGraphDynamicPortGroup? DynamicPorts2 { get; set; }
    public Func<FlowGraphExecutionContext, CancellationToken, Task>? Executor { get; set; }
    public object? Tag { get; set; }

    internal FlowGraphCardDescriptor Clone()
    {
        var clone = new FlowGraphCardDescriptor(CardType, Name)
        {
            HeaderColor = HeaderColor,
            DynamicPorts = DynamicPorts?.Clone(),
            DynamicPorts2 = DynamicPorts2?.Clone(),
            Executor = Executor,
            Tag = Tag
        };
        foreach (var port in Ports) clone.Ports.Add(port.Clone());
        return clone;
    }
}

// ==================== 执行上下文 ====================
public sealed class FlowGraphExecutionContext
{
    private readonly FlowGraphControl _graph;

    internal FlowGraphExecutionContext(FlowGraphControl graph, FlowGraphNode node, CancellationToken ct)
    {
        _graph = graph;
        Node = node;
        CancellationToken = ct;
    }

    public FlowGraphNode Node { get; }
    public CancellationToken CancellationToken { get; }

    public object? GetInput(string portName)
    {
        var port = Node.Ports.FirstOrDefault(p =>
            p.Direction != FlowGraphPortDirection.Output &&
            string.Equals(p.Name, portName, StringComparison.OrdinalIgnoreCase));
        return port?.Data ?? port?.Value;
    }

    public object? GetInput(int portIndex)
    {
        if (portIndex < 0 || portIndex >= Node.Ports.Count) return null;
        var port = Node.Ports[portIndex];
        return port.Direction == FlowGraphPortDirection.Output ? null : (port.Data ?? port.Value);
    }

    public T? GetInput<T>(string portName)
    {
        var value = GetInput(portName);
        if (value is T typed) return typed;
        if (value is null) return default;
        try { return (T?)Convert.ChangeType(value, typeof(T)); }
        catch { return default; }
    }

    public FlowGraphImageData? GetInputImage(string portName) => GetInput<FlowGraphImageData>(portName);

    public bool SetOutput(string portName, object? value)
    {
        int index = Node.Ports.FindIndex(p =>
            p.Direction == FlowGraphPortDirection.Output &&
            string.Equals(p.Name, portName, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && SetOutputPort(index, value);
    }

    public bool SetOutputPort(int portIndex, object? value)
    {
        if (portIndex < 0 || portIndex >= Node.Ports.Count) return false;
        var port = Node.Ports[portIndex];
        if (port.Direction != FlowGraphPortDirection.Output) return false;
        return _graph.SetOutputDataFromExecution(Node.Id, portIndex, value);
    }

    public bool SetOutput(int outputOrdinal, object? value)
    {
        int seen = 0;
        for (int i = 0; i < Node.Ports.Count; i++)
        {
            if (Node.Ports[i].Direction != FlowGraphPortDirection.Output) continue;
            if (seen++ != outputOrdinal) continue;
            return _graph.SetOutputDataFromExecution(Node.Id, i, value);
        }
        return false;
    }

    public void Fail(string message)
    {
        Node.LastError = message ?? "节点执行失败。";
        Node.ExecutionStatus = FlowGraphExecutionStatus.Failed;
        _graph.SetExecutionStatus(Node.Id, FlowGraphExecutionStatus.Failed);
    }
}
