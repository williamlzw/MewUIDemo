// FlowGraphControl.Api.cs
using Aprillz.MewUI;
using MewUIExtended.FlowGraph;

namespace MewUI.Extended;

public sealed partial class FlowGraphControl
{
    // ==================== 节点管理 ====================
    public int AddNode(FlowGraphNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (IsExecuting) throw new InvalidOperationException("执行期间不能添加节点。");

        if (node.Id <= 0 || _nodes.Any(n => n.Id == node.Id))
            node.Id = _nextNodeId++;
        else
            _nextNodeId = Math.Max(_nextNodeId, node.Id + 1);

        node.Width = Math.Max(FlowGraphConstants.NodeMinWidth, node.Width);

        foreach (var port in node.Ports) AssignPortId(port);

        RecalculateNodeSize(node);
        _nodes.Add(node);
        UpdateScrollBounds();
        InvalidateGraphVisual();
        return node.Id;
    }

    public bool RemoveNode(int nodeId)
    {
        if (IsExecuting) return false;
        var node = FindNode(nodeId);
        if (node is null) return false;

        for (int i = _connections.Count - 1; i >= 0; i--)
        {
            if (_connections[i].FromNodeId == nodeId || _connections[i].ToNodeId == nodeId)
                RemoveConnection(_connections[i].Id);
        }

        _nodes.Remove(node);
        _selectedNodeIds.Remove(nodeId);
        if (_contextMenuNodeId == nodeId) CloseContextMenu();
        if (_expandedComboNodeId == nodeId) CloseComboPopup();

        NotifySelectionChanged();
        UpdateScrollBounds();
        InvalidateGraphVisual();
        return true;
    }

    // ==================== 连线管理 ====================
    public bool AddConnection(int fromNodeId, int fromPortIndex, int toNodeId, int toPortIndex)
    {
        if (IsExecuting) return false;

        var fromNode = FindNode(fromNodeId);
        var toNode = FindNode(toNodeId);
        if (fromNode is null || toNode is null || fromNodeId == toNodeId) return false;

        if (!IsValidPortIndex(fromNode, fromPortIndex) || !IsValidPortIndex(toNode, toPortIndex)) return false;

        var fromPort = fromNode.Ports[fromPortIndex];
        var toPort = toNode.Ports[toPortIndex];

        if (fromPort.Direction == FlowGraphPortDirection.Input && toPort.Direction == FlowGraphPortDirection.Output)
        {
            (fromNode, toNode) = (toNode, fromNode);
            (fromPortIndex, toPortIndex) = (toPortIndex, fromPortIndex);
            (fromPort, toPort) = (toPort, fromPort);
        }

        if (fromPort.Direction != FlowGraphPortDirection.Output || toPort.Direction != FlowGraphPortDirection.Input)
            return false;

        if (!IsCompatible(fromPort.DataType, toPort.DataType)) return false;

        for (int i = _connections.Count - 1; i >= 0; i--)
        {
            if (_connections[i].ToNodeId == toNode.Id && _connections[i].ToPortIndex == toPortIndex)
                RemoveConnection(_connections[i].Id);
        }

        var conn = new FlowGraphConnection
        {
            Id = _nextConnectionId++,
            FromNodeId = fromNode.Id,
            FromPortIndex = fromPortIndex,
            ToNodeId = toNode.Id,
            ToPortIndex = toPortIndex
        };

        InitializeControlPoints(conn, GetPortAnchor(fromNode, fromPortIndex), GetPortAnchor(toNode, toPortIndex));
        _connections.Add(conn);

        toPort.IsConnected = true;
        RecalculateNodeSize(toNode);

        if (toNode.ExecutionStatus is not FlowGraphExecutionStatus.Running and not FlowGraphExecutionStatus.Pending)
        {
            toNode.ExecutionStatus = FlowGraphExecutionStatus.Idle;
            InvalidateDownstream(toNode.Id);
        }

        ConnectionCreated?.Invoke(this, new FlowGraphConnectionEventArgs(conn));
        InvalidateGraphVisual();
        return true;
    }

    public bool RemoveConnection(int connectionId)
    {
        if (IsExecuting) return false;

        int index = _connections.FindIndex(c => c.Id == connectionId);
        if (index < 0) return false;

        var conn = _connections[index];
        _connections.RemoveAt(index);

        var toNode = FindNode(conn.ToNodeId);
        if (toNode != null && IsValidPortIndex(toNode, conn.ToPortIndex))
        {
            var input = toNode.Ports[conn.ToPortIndex];
            input.IsConnected = _connections.Any(c => c.ToNodeId == toNode.Id && c.ToPortIndex == conn.ToPortIndex);
            RecalculateNodeSize(toNode);

            if (toNode.ExecutionStatus is not FlowGraphExecutionStatus.Running and not FlowGraphExecutionStatus.Pending)
            {
                toNode.ExecutionStatus = FlowGraphExecutionStatus.Idle;
                InvalidateDownstream(toNode.Id);
            }
        }

        if (_selectedConnectionId == connectionId) _selectedConnectionId = -1;

        ConnectionRemoved?.Invoke(this, new FlowGraphConnectionEventArgs(conn));
        InvalidateGraphVisual();
        return true;
    }

    // ==================== 动态端口 ====================
    public bool AddDynamicPort(int nodeId, int group = 1)
    {
        if (IsExecuting) return false;
        var node = FindNode(nodeId);
        if (node is null || !_cardRegistry.TryGetValue(node.CardType, out var desc)) return false;

        var config = group == 1 ? desc.DynamicPorts : desc.DynamicPorts2;
        if (config is null || config.BaseIndex < 0) return false;

        int count = group == 1 ? node.DynamicPortCount : node.DynamicPort2Count;
        if (count >= config.MaxCount) return false;

        int insertIdx = ResolveDynamicInsertIndex(node, desc, group);
        var port = config.CreatePort(count);
        node.Ports.Insert(insertIdx, port);
        AssignPortId(port);

        if (group == 1) node.DynamicPortCount++;
        else node.DynamicPort2Count++;

        ReindexConnectionsAfterInsert(nodeId, insertIdx);
        RecalculateNodeSize(node);
        InvalidateDownstream(nodeId);
        InvalidateGraphVisual();
        return true;
    }

    public bool RemoveDynamicPort(int nodeId, int group = 1)
    {
        if (IsExecuting) return false;
        var node = FindNode(nodeId);
        if (node is null || !_cardRegistry.TryGetValue(node.CardType, out var desc)) return false;

        var config = group == 1 ? desc.DynamicPorts : desc.DynamicPorts2;
        if (config is null || config.BaseIndex < 0) return false;

        int count = group == 1 ? node.DynamicPortCount : node.DynamicPort2Count;
        if (count <= config.MinCount) return false;

        int removeIdx = ResolveDynamicInsertIndex(node, desc, group) - 1;
        if (removeIdx < 0 || removeIdx >= node.Ports.Count) return false;

        if (group == 1) node.DynamicPortCount--;
        else node.DynamicPort2Count--;

        return RemovePort(nodeId, removeIdx);
    }

    public bool RemovePort(int nodeId, int portIndex)
    {
        if (IsExecuting) return false;
        var node = FindNode(nodeId);
        if (node is null || !IsValidPortIndex(node, portIndex)) return false;

        for (int i = _connections.Count - 1; i >= 0; i--)
        {
            var c = _connections[i];
            if ((c.FromNodeId == nodeId && c.FromPortIndex == portIndex) ||
                (c.ToNodeId == nodeId && c.ToPortIndex == portIndex))
                RemoveConnection(c.Id);
        }

        node.Ports.RemoveAt(portIndex);

        foreach (var c in _connections)
        {
            if (c.FromNodeId == nodeId && c.FromPortIndex > portIndex) c.FromPortIndex--;
            if (c.ToNodeId == nodeId && c.ToPortIndex > portIndex) c.ToPortIndex--;
        }

        RecalculateNodeSize(node);
        InvalidateDownstream(nodeId);
        InvalidateGraphVisual();
        return true;
    }

    private int ResolveDynamicInsertIndex(FlowGraphNode node, FlowGraphCardDescriptor desc, int group)
    {
        if (group == 1)
        {
            var cfg = desc.DynamicPorts!;
            return Math.Clamp(cfg.BaseIndex + node.DynamicPortCount, 0, node.Ports.Count);
        }

        var cfg2 = desc.DynamicPorts2!;
        int idx = cfg2.BaseIndex;
        if (desc.DynamicPorts is { BaseIndex: >= 0 })
            idx += node.DynamicPortCount;

        return Math.Clamp(idx + node.DynamicPort2Count, 0, node.Ports.Count);
    }

    private void ReindexConnectionsAfterInsert(int nodeId, int insertedIndex)
    {
        foreach (var conn in _connections)
        {
            if (conn.FromNodeId == nodeId && conn.FromPortIndex >= insertedIndex) conn.FromPortIndex++;
            if (conn.ToNodeId == nodeId && conn.ToPortIndex >= insertedIndex) conn.ToPortIndex++;
        }
    }

    // ==================== 卡片注册 ====================
    public bool RegisterCardType(FlowGraphCardDescriptor descriptor, bool replaceExisting = true)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (IsExecuting) return false;
        if (_cardRegistry.ContainsKey(descriptor.CardType) && !replaceExisting) return false;
        _cardRegistry[descriptor.CardType] = descriptor.Clone();
        return true;
    }

    public bool UnregisterCardType(int cardType) => !IsExecuting && _cardRegistry.Remove(cardType);

    public FlowGraphNode CreateNodeFromCard(int cardType, double x = -1, double y = -1, string? title = null)
    {
        if (!_cardRegistry.TryGetValue(cardType, out var desc))
            throw new KeyNotFoundException($"卡片类型 {cardType} 未注册。");

        var node = new FlowGraphNode(title ?? desc.Name)
        {
            CardType = desc.CardType,
            Category = desc.Name,
            HeaderColor = desc.HeaderColor,
            X = x >= 0 ? x : Math.Max(30, (Bounds.Width / 2 + _panX) / _zoom - 130),
            Y = y >= 0 ? y : Math.Max(30, (Bounds.Height / 2 + _panY) / _zoom - 80),
            Width = 260,
            Tag = desc.Tag
        };

        foreach (var pd in desc.Ports)
            node.Ports.Add(pd.CreatePort());

        if (desc.DynamicPorts is { BaseIndex: >= 0 } g1)
        {
            node.DynamicPortCount = g1.MinCount;
            int idx = Math.Clamp(g1.BaseIndex, 0, node.Ports.Count);
            for (int i = 0; i < g1.MinCount; i++)
                node.Ports.Insert(idx + i, g1.CreatePort(i));
        }

        if (desc.DynamicPorts2 is { BaseIndex: >= 0 } g2)
        {
            node.DynamicPort2Count = g2.MinCount;
            int baseIdx = g2.BaseIndex;
            if (desc.DynamicPorts is { BaseIndex: >= 0 }) baseIdx += node.DynamicPortCount;
            int idx = Math.Clamp(baseIdx, 0, node.Ports.Count);
            for (int i = 0; i < g2.MinCount; i++)
                node.Ports.Insert(idx + i, g2.CreatePort(i));
        }

        AddNode(node);
        return node;
    }

    // ==================== 端口数据 ====================
    public bool SetPortData(int nodeId, int portIndex, object? data)
    {
        if (IsExecuting) return false;
        var node = FindNode(nodeId);
        if (node is null || !IsValidPortIndex(node, portIndex)) return false;

        var port = node.Ports[portIndex];
        port.Data = data;
        port.Value = ToDisplayText(data);
        port.ImagePath = data is FlowGraphImageData imageData
            ? imageData.SourcePath
            : null;

        if (node.CardType == LocalImageCardType &&
            port.Widget == FlowGraphWidgetKind.ImagePreview &&
            port.DataType == FlowGraphDataType.Image)
        {
            SynchronizeLocalImageOutput(node);
        }

        RecalculateNodeSize(node);

        if (node.ExecutionStatus is not FlowGraphExecutionStatus.Running and not FlowGraphExecutionStatus.Pending)
        {
            node.ExecutionStatus = FlowGraphExecutionStatus.Idle;
            InvalidateDownstream(nodeId);
        }

        PortValueChanged?.Invoke(this, new FlowGraphPortEventArgs(node, port, portIndex));
        InvalidateGraphVisual();
        return true;
    }

    public bool SetPortImage(int nodeId, int portIndex, FlowGraphImageData? image)
        => SetPortData(nodeId, portIndex, image);

    public bool SetComboSelection(int nodeId, int portIndex, int selectedIndex)
    {
        if (IsExecuting) return false;
        var node = FindNode(nodeId);
        if (node is null || !IsValidPortIndex(node, portIndex)) return false;

        var port = node.Ports[portIndex];
        if (port.Widget != FlowGraphWidgetKind.Combo || port.ComboData is null || port.ComboData.Options.Count == 0)
            return false;

        port.ComboData.Current = Math.Clamp(selectedIndex, 0, port.ComboData.Options.Count - 1);
        port.Value = port.ComboData.Options[port.ComboData.Current];
        port.Data = port.Value;

        if (node.ExecutionStatus is not FlowGraphExecutionStatus.Running and not FlowGraphExecutionStatus.Pending)
        {
            node.ExecutionStatus = FlowGraphExecutionStatus.Idle;
            InvalidateDownstream(nodeId);
        }

        PortValueChanged?.Invoke(this, new FlowGraphPortEventArgs(node, port, portIndex));
        InvalidateGraphVisual();
        return true;
    }

    internal bool SetOutputDataFromExecution(int nodeId, int portIndex, object? data)
    {
        var node = FindNode(nodeId);
        if (node is null || !IsValidPortIndex(node, portIndex)) return false;

        var port = node.Ports[portIndex];
        if (port.Direction != FlowGraphPortDirection.Output) return false;

        port.Data = data;
        port.Value = ToDisplayText(data);
        RecalculateNodeSize(node);
        InvalidateDownstream(nodeId);

        PortValueChanged?.Invoke(this, new FlowGraphPortEventArgs(node, port, portIndex));
        InvalidateGraphVisual();
        return true;
    }

    public bool SetExecutionStatus(int nodeId, FlowGraphExecutionStatus status)
    {
        var node = FindNode(nodeId);
        if (node is null) return false;
        node.ExecutionStatus = status;
        InvalidateGraphVisual();
        return true;
    }

    // ==================== 选择 ====================
    public bool SelectNode(int nodeId, bool additive = false)
    {
        if (FindNode(nodeId) is null) return false;
        if (!additive) _selectedNodeIds.Clear();
        _selectedNodeIds.Add(nodeId);
        _selectedConnectionId = -1;
        NotifySelectionChanged();
        NodeSelected?.Invoke(this, new FlowGraphNodeEventArgs(FindNode(nodeId)!));
        InvalidateGraphVisual();
        return true;
    }

    public void ClearSelection()
    {
        _selectedNodeIds.Clear();
        _selectedConnectionId = -1;
        NotifySelectionChanged();
        InvalidateGraphVisual();
    }

    // ==================== 清空 ====================
    public void Clear()
    {
        if (IsExecuting) throw new InvalidOperationException("执行期间不能清空画布。");

        _nodes.Clear();
        _connections.Clear();
        _selectedNodeIds.Clear();
        _selectedConnectionId = -1;
        _connectingNodeId = -1;
        _isSelecting = false;
        _isDraggingNodes = false;

        CloseContextMenu();
        CloseComboPopup();

        _projectName = string.Empty;
        NotifySelectionChanged();
        UpdateScrollBounds();
        InvalidateGraphVisual();
    }

    public void RefreshGraph()
    {
        foreach (var node in _nodes) RecalculateNodeSize(node);
        UpdateScrollBounds();
        InvalidateGraphVisual();
    }

    public void SetCustomItems(string[] items)
    {
        _customItems = items ?? Array.Empty<string>();
        InvalidateGraphVisual();
    }

    public void SetProjectName(string name)
    {
        _projectName = name ?? string.Empty;
        InvalidateGraphVisual();
    }

    // ==================== 执行引擎 ====================
    public async Task<bool> ExecuteChainAsync(int terminalNodeId,
        FlowGraphExecutionMode mode = FlowGraphExecutionMode.Fresh,
        CancellationToken cancellationToken = default)
    {
        var terminal = FindNode(terminalNodeId);
        if (terminal is null) return false;

        var ordered = BuildUpstreamOrder(terminalNodeId);
        return await ExecuteOrderAsync(ordered, mode, cancellationToken);
    }

    public async Task<bool> ExecuteAllAsync(
        FlowGraphExecutionMode mode = FlowGraphExecutionMode.Fresh,
        CancellationToken cancellationToken = default)
    {
        if (_nodes.Count == 0) return true;
        var ordered = BuildAllTopologicalOrder();
        return await ExecuteOrderAsync(ordered, mode, cancellationToken);
    }

    public void CancelExecution()
    {
        try { _executionCancellation?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    public void CancelNode(int nodeId)
    {
        var node = FindNode(nodeId);
        if (node is null) return;

        bool wasBusy = node.ExecutionStatus is FlowGraphExecutionStatus.Running or FlowGraphExecutionStatus.Pending;
        node.ExecutionStatus = FlowGraphExecutionStatus.Idle;
        InvalidateDownstream(nodeId);

        if (wasBusy)
            NodeCanceled?.Invoke(this, new FlowGraphExecutionEventArgs(node));

        InvalidateGraphVisual();
    }

    public void RequestExecuteNode(int nodeId, FlowGraphExecutionMode mode = FlowGraphExecutionMode.Fresh)
        => _ = ExecuteChainAsync(nodeId, mode);

    public void RequestExecuteAll(FlowGraphExecutionMode mode = FlowGraphExecutionMode.Fresh)
        => _ = ExecuteAllAsync(mode);

    private async Task<bool> ExecuteOrderAsync(
        IReadOnlyList<FlowGraphNode> ordered,
        FlowGraphExecutionMode mode,
        CancellationToken cancellationToken)
    {
        if (ordered.Count == 0) return true;

        bool entered;
        try { entered = await _executionGate.WaitAsync(0, cancellationToken); }
        catch (OperationCanceledException) { return false; }

        if (!entered) return false;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executionCancellation = linked;
        _isExecuting = true;

        try
        {
            _executionPass++;

            // ★★★ Continue模式下，不重置已完成的节点 ★★★
            foreach (var node in ordered)
            {
                if (mode == FlowGraphExecutionMode.Fresh)
                {
                    node.ExecutionStatus = FlowGraphExecutionStatus.Idle;
                    node.LastError = string.Empty;
                    node.ExecutionStatus = FlowGraphExecutionStatus.Pending;
                }
                else
                {
                    // Continue模式：只将非Completed节点设为Pending
                    if (node.ExecutionStatus != FlowGraphExecutionStatus.Completed)
                    {
                        node.ExecutionStatus = FlowGraphExecutionStatus.Pending;
                    }
                }
            }

            GraphExecutionStarted?.Invoke(this, EventArgs.Empty);
            InvalidateGraphVisual();

            foreach (var node in ordered)
            {
                linked.Token.ThrowIfCancellationRequested();

                // ★★★ Continue模式跳过已完成的节点 ★★★
                if (mode == FlowGraphExecutionMode.Continue &&
                    node.ExecutionStatus == FlowGraphExecutionStatus.Completed)
                    continue;

                CopyConnectedInputs(node);

                node.LastExecutedPass = _executionPass;
                node.ExecutionStatus = FlowGraphExecutionStatus.Running;
                node.LastError = string.Empty;

                NodeExecutionStarted?.Invoke(this, new FlowGraphExecutionEventArgs(node));
                InvalidateGraphVisual();

                try
                {
                    var context = new FlowGraphExecutionContext(this, node, linked.Token);
                    ExecuteNodeRequested?.Invoke(this, new FlowGraphNodeEventArgs(node));

                    if (_cardRegistry.TryGetValue(node.CardType, out var desc) && desc.Executor != null)
                        await desc.Executor(context, linked.Token);

                    linked.Token.ThrowIfCancellationRequested();

                    if (node.ExecutionStatus == FlowGraphExecutionStatus.Failed)
                    {
                        NodeExecutionFailed?.Invoke(this, new FlowGraphExecutionEventArgs(node, message: node.LastError));
                        foreach (var w in ordered.Where(n => n.ExecutionStatus == FlowGraphExecutionStatus.Pending))
                            w.ExecutionStatus = FlowGraphExecutionStatus.Idle;
                        InvalidateGraphVisual();
                        return false;
                    }

                    SynchronizeLocalImageOutput(node);
                    node.ExecutionStatus = FlowGraphExecutionStatus.Completed;
                    PropagateOutputs(node);

                    NodeExecutionCompleted?.Invoke(this, new FlowGraphExecutionEventArgs(node));
                    InvalidateGraphVisual();
                }
                catch (OperationCanceledException)
                {
                    node.ExecutionStatus = FlowGraphExecutionStatus.Idle;
                    throw;
                }
                catch (Exception ex)
                {
                    node.ExecutionStatus = FlowGraphExecutionStatus.Failed;
                    node.LastError = ex.Message;
                    NodeExecutionFailed?.Invoke(this, new FlowGraphExecutionEventArgs(node, ex));
                    foreach (var w in ordered.Where(n => n.ExecutionStatus == FlowGraphExecutionStatus.Pending))
                        w.ExecutionStatus = FlowGraphExecutionStatus.Idle;
                    InvalidateGraphVisual();
                    return false;
                }
            }

            GraphExecutionCompleted?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (OperationCanceledException)
        {
            GraphExecutionCanceled?.Invoke(this, EventArgs.Empty);
            foreach (var node in ordered.Where(n =>
                         n.ExecutionStatus is FlowGraphExecutionStatus.Pending or FlowGraphExecutionStatus.Running))
                node.ExecutionStatus = FlowGraphExecutionStatus.Idle;
            InvalidateGraphVisual();
            return false;
        }
        finally
        {
            _isExecuting = false;
            _executionCancellation = null;
            _executionGate.Release();
        }
    }

    // ==================== 执行辅助 ====================
    private const int LocalImageCardType = 4;

    private static void SynchronizeLocalImageOutput(FlowGraphNode node)
    {
        if (node.CardType != LocalImageCardType)
            return;

        var preview = node.Ports.FirstOrDefault(p =>
            p.Widget == FlowGraphWidgetKind.ImagePreview &&
            p.DataType == FlowGraphDataType.Image);
        var output = node.Ports.FirstOrDefault(p =>
            p.Direction == FlowGraphPortDirection.Output &&
            p.DataType == FlowGraphDataType.Image);

        if (preview is null || output is null)
            return;

        output.Data = preview.Data;
        output.Value = ToDisplayText(preview.Data);
        output.ImagePath = preview.Data is FlowGraphImageData image
            ? image.SourcePath
            : preview.ImagePath;
    }

    private List<FlowGraphNode> BuildUpstreamOrder(int terminalNodeId)
    {
        var result = new List<FlowGraphNode>();
        var permanent = new HashSet<int>();
        var temporary = new HashSet<int>();
        VisitUpstream(terminalNodeId, permanent, temporary, result);
        return result;
    }

    private List<FlowGraphNode> BuildAllTopologicalOrder()
    {
        var result = new List<FlowGraphNode>();
        var permanent = new HashSet<int>();
        var temporary = new HashSet<int>();

        foreach (var node in _nodes)
            VisitUpstream(node.Id, permanent, temporary, result);

        return result;
    }

    private void VisitUpstream(int nodeId, HashSet<int> permanent, HashSet<int> temporary, List<FlowGraphNode> result)
    {
        if (permanent.Contains(nodeId)) return;
        if (!temporary.Add(nodeId))
            throw new InvalidOperationException("图中存在循环依赖。");

        foreach (var conn in _connections.Where(c => c.ToNodeId == nodeId))
            VisitUpstream(conn.FromNodeId, permanent, temporary, result);

        temporary.Remove(nodeId);
        permanent.Add(nodeId);

        var node = FindNode(nodeId);
        if (node != null) result.Add(node);
    }

    private void CopyConnectedInputs(FlowGraphNode node)
    {
        foreach (var conn in _connections.Where(c => c.ToNodeId == node.Id))
        {
            var source = FindNode(conn.FromNodeId);
            if (source is null) continue;
            if (!IsValidPortIndex(source, conn.FromPortIndex) || !IsValidPortIndex(node, conn.ToPortIndex)) continue;

            var srcPort = source.Ports[conn.FromPortIndex];
            var dstPort = node.Ports[conn.ToPortIndex];

            object? transferData = srcPort.Data;
            if (transferData is null && !string.IsNullOrEmpty(srcPort.Value))
                transferData = srcPort.Value;

            dstPort.Data = transferData;
            dstPort.ImagePath = transferData is FlowGraphImageData image
                ? image.SourcePath
                : srcPort.ImagePath;
            dstPort.Value = ToDisplayText(transferData);
        }
    }

    private void PropagateOutputs(FlowGraphNode node)
    {
        foreach (var conn in _connections.Where(c => c.FromNodeId == node.Id))
        {
            var target = FindNode(conn.ToNodeId);
            if (target is null) continue;
            if (!IsValidPortIndex(node, conn.FromPortIndex) || !IsValidPortIndex(target, conn.ToPortIndex)) continue;

            var srcPort = node.Ports[conn.FromPortIndex];
            var dstPort = target.Ports[conn.ToPortIndex];

            if (dstPort.Widget == FlowGraphWidgetKind.Display)
                continue;

            object? transferData = srcPort.Data;
            if (transferData is null && !string.IsNullOrEmpty(srcPort.Value))
                transferData = srcPort.Value;

            dstPort.Data = transferData;
            dstPort.ImagePath = transferData is FlowGraphImageData image
                ? image.SourcePath
                : srcPort.ImagePath;
            dstPort.Value = ToDisplayText(transferData);

            RecalculateNodeSize(target);
            PortValueChanged?.Invoke(this, new FlowGraphPortEventArgs(target, dstPort, conn.ToPortIndex));
        }
    }

    private void InvalidateDownstream(int nodeId)
    {
        var visited = new HashSet<int>();
        InvalidateDownstreamCore(nodeId, visited);
    }

    private void InvalidateDownstreamCore(int nodeId, HashSet<int> visited)
    {
        if (!visited.Add(nodeId)) return;

        foreach (var conn in _connections.Where(c => c.FromNodeId == nodeId))
        {
            var child = FindNode(conn.ToNodeId);
            if (child is null || !IsValidPortIndex(child, conn.ToPortIndex)) continue;

            var input = child.Ports[conn.ToPortIndex];

            if (input.Widget == FlowGraphWidgetKind.Display)
                ClearPortValue(input);

            if (child.ExecutionStatus is not FlowGraphExecutionStatus.Running and not FlowGraphExecutionStatus.Pending)
            {
                child.ExecutionStatus = FlowGraphExecutionStatus.Idle;
                child.LastError = string.Empty;
            }

            InvalidateDownstreamCore(child.Id, visited);
        }
    }

    private void ClearNodeOutputs(FlowGraphNode node)
    {
        if (node is null || IsExecuting) return;
        var visited = new HashSet<int>();
        ClearNodeOutputsCore(node, visited);
        InvalidateGraphVisual();
    }

    private void ClearNodeOutputsCore(FlowGraphNode node, HashSet<int> visited)
    {
        if (!visited.Add(node.Id)) return;

        bool changed = false;
        for (int i = 0; i < node.Ports.Count; i++)
        {
            var port = node.Ports[i];
            if (port.Direction == FlowGraphPortDirection.Output ||
                port.Widget == FlowGraphWidgetKind.Display)
            {
                changed |= ClearPortValue(port);
            }
        }

        if (changed || node.ExecutionStatus != FlowGraphExecutionStatus.Idle ||
            !string.IsNullOrEmpty(node.LastError))
        {
            node.ExecutionStatus = FlowGraphExecutionStatus.Idle;
            node.LastError = string.Empty;
            RecalculateNodeSize(node);
        }

        foreach (var conn in _connections.Where(c => c.FromNodeId == node.Id).ToArray())
        {
            var target = FindNode(conn.ToNodeId);
            if (target is null || !IsValidPortIndex(target, conn.ToPortIndex)) continue;

            var input = target.Ports[conn.ToPortIndex];
            bool inputChanged = input.Direction == FlowGraphPortDirection.Input && ClearPortValue(input);
            if (inputChanged)
                RecalculateNodeSize(target);

            ClearNodeOutputsCore(target, visited);
        }
    }

    private static bool ClearPortValue(FlowGraphPort port)
    {
        bool changed = port.Data is not null || !string.IsNullOrEmpty(port.Value);
        port.Data = null;
        port.Value = string.Empty;
        return changed;
    }

    private static void InitializeControlPoints(FlowGraphConnection conn, Point from, Point to)
    {
        double dx = to.X - from.X, dy = to.Y - from.Y;
        conn.ControlPoint1 = new Point(from.X + dx * 0.25, from.Y + dy * 0.25);
        conn.ControlPoint2 = new Point(to.X - dx * 0.25, to.Y - dy * 0.25);
    }

    private void AssignPortId(FlowGraphPort port)
    {
        if (port.Id <= 0) port.Id = _nextPortId++;
        else _nextPortId = Math.Max(_nextPortId, port.Id + 1);
    }

    /// <summary>
    /// 获取指定节点端口的数据。
    /// </summary>
    public bool TryGetPortData(
        int nodeId,
        int portIndex,
        out object? data)
    {
        var node = FindNode(nodeId);

        if (node == null ||
            portIndex < 0 ||
            portIndex >= node.Ports.Count)
        {
            data = null;
            return false;
        }

        data = node.Ports[portIndex].Data;
        return true;
    }
}