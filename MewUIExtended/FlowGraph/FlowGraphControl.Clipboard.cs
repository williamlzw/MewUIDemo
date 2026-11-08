//FlowGraphControl.Clipboard.cs
using Aprillz.MewUI;
using MewUIExtended.FlowGraph;
namespace MewUI.Extended;

public sealed partial class FlowGraphControl
{
    private readonly List<FlowGraphNode> _clipboardNodes = new();
    private readonly List<ClipboardConnection> _clipboardConnections = new();
    private Point _clipboardCenter;

    private sealed class ClipboardConnection
    {
        public int FromNodeId { get; init; }
        public int FromPortIndex { get; init; }
        public int ToNodeId { get; init; }
        public int ToPortIndex { get; init; }
        public Point ControlPoint1 { get; init; }
        public Point ControlPoint2 { get; init; }
        public bool HasCustomControlPoints { get; init; }
    }

    private void CopySelectedNodesToClipboard()
    {
        _clipboardNodes.Clear();
        _clipboardConnections.Clear();

        var selected = _nodes.Where(n => _selectedNodeIds.Contains(n.Id)).ToArray();
        if (selected.Length == 0)
            return;

        double minX = selected.Min(n => n.X);
        double minY = selected.Min(n => n.Y);
        double maxX = selected.Max(n => n.X + n.Width);
        double maxY = selected.Max(n => n.Y + n.Height);
        _clipboardCenter = new Point((minX + maxX) / 2.0, (minY + maxY) / 2.0);

        var selectedIds = selected.Select(n => n.Id).ToHashSet();
        foreach (var node in selected)
            _clipboardNodes.Add(CloneNodeForClipboard(node));

        // 只复制选中节点彼此之间的连线，不复制指向未选中节点的外部连线。
        foreach (var connection in _connections.Where(c =>
                     selectedIds.Contains(c.FromNodeId) && selectedIds.Contains(c.ToNodeId)))
        {
            _clipboardConnections.Add(new ClipboardConnection
            {
                FromNodeId = connection.FromNodeId,
                FromPortIndex = connection.FromPortIndex,
                ToNodeId = connection.ToNodeId,
                ToPortIndex = connection.ToPortIndex,
                ControlPoint1 = connection.ControlPoint1,
                ControlPoint2 = connection.ControlPoint2,
                HasCustomControlPoints = connection.HasCustomControlPoints
            });
        }
    }

    private void CutSelectedNodesToClipboard()
    {
        if (IsExecuting || _selectedNodeIds.Count == 0)
            return;

        // C++ 行为：运行中/等待中的节点不能剪切，避免执行上下文引用已删除节点。
        if (_nodes.Any(n => _selectedNodeIds.Contains(n.Id) &&
            n.ExecutionStatus is FlowGraphExecutionStatus.Running or FlowGraphExecutionStatus.Pending))
            return;

        CopySelectedNodesToClipboard();
        var ids = _selectedNodeIds.ToArray();
        foreach (int id in ids)
            RemoveNode(id);

        _selectedNodeIds.Clear();
        _selectedConnectionId = -1;
        NotifySelectionChanged();
        InvalidateGraphVisual();
    }

    private void PasteClipboardNodes()
    {
        if (IsExecuting || _clipboardNodes.Count == 0)
            return;

        Point target;
        if (Math.Abs(_lastPointerLocal.X) < 0.001 && Math.Abs(_lastPointerLocal.Y) < 0.001)
        {
            target = new Point(
                (Bounds.Width / 2.0 + _panX) / _zoom,
                (Bounds.Height / 2.0 + _panY) / _zoom);
        }
        else
        {
            target = ToWorld(_lastPointerLocal);
        }

        double dx = target.X - _clipboardCenter.X;
        double dy = target.Y - _clipboardCenter.Y;

        // 避免粘贴后的节点落到画布左上角以外。
        double minX = _clipboardNodes.Min(n => n.X + dx);
        double minY = _clipboardNodes.Min(n => n.Y + dy);
        if (minX < 0) dx -= minX;
        if (minY < 0) dy -= minY;

        var idMap = new Dictionary<int, int>();
        _selectedNodeIds.Clear();

        foreach (var source in _clipboardNodes)
        {
            var clone = CloneNodeForClipboard(source);
            clone.Id = 0;
            clone.X = Math.Max(0, source.X + dx);
            clone.Y = Math.Max(0, source.Y + dy);
            foreach (var port in clone.Ports)
            {
                port.Id = 0;
                port.IsConnected = false;
            }

            int newId = AddNode(clone);
            idMap[source.Id] = newId;
            _selectedNodeIds.Add(newId);
        }

        foreach (var source in _clipboardConnections)
        {
            if (!idMap.TryGetValue(source.FromNodeId, out int fromId) ||
                !idMap.TryGetValue(source.ToNodeId, out int toId))
                continue;

            if (!AddConnection(fromId, source.FromPortIndex, toId, source.ToPortIndex))
                continue;

            var created = _connections.LastOrDefault(c =>
                c.FromNodeId == fromId && c.FromPortIndex == source.FromPortIndex &&
                c.ToNodeId == toId && c.ToPortIndex == source.ToPortIndex);
            if (created != null && source.HasCustomControlPoints)
            {
                created.ControlPoint1 = new Point(source.ControlPoint1.X + dx, source.ControlPoint1.Y + dy);
                created.ControlPoint2 = new Point(source.ControlPoint2.X + dx, source.ControlPoint2.Y + dy);
                created.HasCustomControlPoints = true;
            }
        }

        _selectedConnectionId = -1;
        NotifySelectionChanged();
        InvalidateGraphVisual();
    }

    private static FlowGraphNode CloneNodeForClipboard(FlowGraphNode source)
    {
        var clone = new FlowGraphNode(source.Title)
        {
            CardType = source.CardType,
            Category = source.Category,
            X = source.X,
            Y = source.Y,
            Width = source.Width,
            Height = source.Height,
            HeaderColor = source.HeaderColor,
            ExecutionStatus = FlowGraphExecutionStatus.Idle,
            LastError = string.Empty,
            DynamicPortCount = source.DynamicPortCount,
            DynamicPort2Count = source.DynamicPort2Count,
            Tag = source.Tag
        };

        foreach (var sourcePort in source.Ports)
        {
            var port = new FlowGraphPort(sourcePort.Name, sourcePort.Direction, sourcePort.DataType)
            {
                Id = sourcePort.Id,
                Widget = sourcePort.Widget,
                WidgetId = sourcePort.WidgetId,
                WidgetWidth = sourcePort.WidgetWidth,
                WidgetHeight = sourcePort.WidgetHeight,
                Data = ClonePortData(sourcePort),
                Value = sourcePort.Value,
                ImagePath = sourcePort.ImagePath,
                IsConnected = false,
                Tag = sourcePort.Tag,
                CloneData = sourcePort.CloneData
            };

            if (sourcePort.ComboData != null)
            {
                port.ComboData = new FlowGraphComboData
                {
                    Current = sourcePort.ComboData.Current
                };
                port.ComboData.Options.AddRange(sourcePort.ComboData.Options);
            }
            if (sourcePort.ButtonData != null)
                port.ButtonData = new FlowGraphButtonData { Caption = sourcePort.ButtonData.Caption };
            if (sourcePort.DualButtonData != null)
                port.DualButtonData = new FlowGraphDualButtonData
                {
                    Caption1 = sourcePort.DualButtonData.Caption1,
                    Caption2 = sourcePort.DualButtonData.Caption2
                };

            clone.Ports.Add(port);
        }

        return clone;
    }

    private static object? ClonePortData(FlowGraphPort source)
    {
        if (source.Data == null)
            return null;

        if (source.CloneData != null)
            return source.CloneData(source.Data);

        // 这些类型要么不可变，要么在这里做显式复制。
        if (source.Data is FlowGraphImageData)
            return source.Data;
        if (source.Data is FlowGraphComboData combo)
        {
            var copy = new FlowGraphComboData { Current = combo.Current };
            copy.Options.AddRange(combo.Options);
            return copy;
        }
        if (source.Data is FlowGraphButtonData button)
            return new FlowGraphButtonData { Caption = button.Caption };
        if (source.Data is FlowGraphDualButtonData dual)
            return new FlowGraphDualButtonData { Caption1 = dual.Caption1, Caption2 = dual.Caption2 };
        if (source.Data is ICloneable cloneable)
            return cloneable.Clone();

        // 字符串、数值、布尔等值类型可安全直接复制；自定义引用类型沿用引用，
        // 需要深复制的自定义数据请在端口注册时提供 CloneData。
        return source.Data;
    }
}
