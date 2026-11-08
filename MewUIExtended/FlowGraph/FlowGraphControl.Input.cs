using Aprillz.MewUI;
using MewUIExtended.FlowGraph;

namespace MewUI.Extended;

public sealed partial class FlowGraphControl
{
    private int _resizingWidgetNodeId = -1;
    private int _resizingWidgetPortIndex = -1;
    private Point _resizeStartWorld;
    private double _resizeStartWidth;
    private double _resizeStartHeight;
    private Point _controlDragPointerOffset;

    // ==================== 鼠标按下 ====================
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!IsEffectivelyEnabled) return;
        Focus();

        var local = e.GetPosition(this);
        _lastPointerLocal = local;
        UpdateScrollBounds();

        GraphPointerPressed?.Invoke(this, EventArgs.Empty);

        // 右键
        if (e.Button == MouseButton.Right)
        {
            CloseComboPopup();
            var hitNode = HitTestNode(ToWorld(local));
            if (hitNode != null)
            {
                SelectNode(hitNode.Id);
                _contextMenuNodeId = hitNode.Id;
                _contextMenuPosition = local;
                _contextMenuOpen = true;
                _contextMenuHover = -1;
            }
            else
            {
                CloseContextMenu();
            }
            InvalidateGraphVisual();
            e.Handled = true;
            return;
        }

        // 中键 - 平移
        if (e.Button == MouseButton.Middle)
        {
            CloseContextMenu();
            CloseComboPopup();
            _isPanning = true;
            _panStartPointer = local;
            _panStartX = _panX;
            _panStartY = _panY;
            CaptureMouse();
            e.Handled = true;
            return;
        }

        if (e.Button != MouseButton.Left) return;

        // ★★★ 滚动条交互 ★★★
        if (IsPointInScrollBarArea(local))
        {
            if (TryHitHScrollBarThumb(local, out double thumbX, out double thumbW, out double barX, out double barW))
            {
                _draggingHScrollThumb = true;
                _scrollDragOffset = local.X - thumbX;
                CaptureMouse();
                e.Handled = true;
                return;
            }

            if (TryHitVScrollBarThumb(local, out double thumbY, out double thumbH, out double barY2, out double barH))
            {
                _draggingVScrollThumb = true;
                _scrollDragOffset = local.Y - thumbY;
                CaptureMouse();
                e.Handled = true;
                return;
            }

            // 点击轨道：横向。轨道与滑块尺寸使用绘制时的同一套安全计算。
            if (GetHorizontalScrollMetrics(out double hStart, out double hTrack,
                    out double hThumb, out double hThumbLength, out double hMaxScroll) &&
                local.Y >= Bounds.Height - ScrollBarThickness &&
                local.X >= hStart && local.X <= hStart + hTrack)
            {
                double travel = Math.Max(0, hTrack - hThumbLength);
                double ratio = travel > 0
                    ? (local.X - hStart - hThumbLength / 2) / travel
                    : 0;
                _panX = Math.Clamp(ratio * hMaxScroll, 0, hMaxScroll);
                InvalidateGraphVisual();
                e.Handled = true;
                return;
            }

            // 点击轨道：纵向
            if (GetVerticalScrollMetrics(out double vStart, out double vTrack,
                    out double vThumb, out double vThumbLength, out double vMaxScroll) &&
                local.X >= Bounds.Width - ScrollBarThickness &&
                local.Y >= vStart && local.Y <= vStart + vTrack)
            {
                double travel = Math.Max(0, vTrack - vThumbLength);
                double ratio = travel > 0
                    ? (local.Y - vStart - vThumbLength / 2) / travel
                    : 0;
                _panY = Math.Clamp(ratio * vMaxScroll, 0, vMaxScroll);
                InvalidateGraphVisual();
                e.Handled = true;
                return;
            }
        }

        // 侧边栏
        if (local.X >= 0 && local.X < FlowGraphConstants.SidebarWidth ||
            IsPointInsideOpenSidebarPanel(local))
        {
            HandleSidebarClick(local);
            e.Handled = true;
            return;
        }

        if (HasOpenSidebarPanel() || _sidebarHoverBtn != -1)
        {
            _sidebarHoverBtn = -1;
            CloseSidebarPanels();
            InvalidateGraphVisual();
        }

        if (_contextMenuOpen)
        {
            HandleContextMenuClick(local);
            e.Handled = true;
            return;
        }

        if (_expandedComboNodeId != -1)
        {
            int optionIdx = HitTestComboPopup(local);
            if (optionIdx >= 0)
            {
                var node = FindNode(_expandedComboNodeId);
                if (node != null && IsValidPortIndex(node, _expandedComboPortIndex))
                    SetComboSelection(node.Id, _expandedComboPortIndex, optionIdx);
            }
            CloseComboPopup();
            InvalidateGraphVisual();
            e.Handled = true;
            return;
        }

        if (_showEditPanel)
        {
            double pw = FlowGraphConstants.EditPanelWidth;
            double ph = FlowGraphConstants.EditPanelHeight;
            double px = Bounds.Width - pw - 20;
            double py = Bounds.Height - ph - 20;
            if (local.X >= px && local.X <= px + pw && local.Y >= py && local.Y <= py + ph)
            {
                e.Handled = true;
                return;
            }
        }

        CloseContextMenu();

        var world = ToWorld(local);

        if (TryHitWidgetResizeHandle(local, out var resizeNode, out int resizePortIndex))
        {
            var resizePort = resizeNode.Ports[resizePortIndex];
            _resizingWidgetNodeId = resizeNode.Id;
            _resizingWidgetPortIndex = resizePortIndex;
            _resizeStartWorld = world;
            double minimumWidth = resizePort.Widget is FlowGraphWidgetKind.ImagePreview
                or FlowGraphWidgetKind.Edit or FlowGraphWidgetKind.Text or FlowGraphWidgetKind.MultilineText
                ? 300 : 120;
            double minimumHeight = resizePort.Widget == FlowGraphWidgetKind.ImagePreview ? 180 : 45;
            var currentWidgetRect = GetWidgetRect(resizeNode, resizePortIndex);
            _resizeStartWidth = Math.Max(minimumWidth,
                resizePort.WidgetWidth > 0 ? resizePort.WidgetWidth : currentWidgetRect.Width);
            _resizeStartHeight = Math.Max(minimumHeight, currentWidgetRect.Height);
            CaptureMouse();
            InvalidateGraphVisual();
            e.Handled = true;
            return;
        }

        if (TryHitWidget(local, out var widgetNode, out int widgetPortIdx))
        {
            var port = widgetNode.Ports[widgetPortIdx];
            SelectNode(widgetNode.Id);

            if (port.Widget == FlowGraphWidgetKind.Combo)
            {
                OpenComboPopup(widgetNode.Id, widgetPortIdx);
            }
            else if (port.Widget == FlowGraphWidgetKind.Button)
            {
                ButtonClicked?.Invoke(this, new FlowGraphButtonClickedEventArgs(
                    widgetNode.Id, widgetNode.CardType, widgetPortIdx, port.Id));
            }
            else if (port.Widget == FlowGraphWidgetKind.DualButton)
            {
                var widgetRect = GetWidgetRect(widgetNode, widgetPortIdx);
                double halfW = (widgetRect.Width - 6) / 2;
                double localX = world.X - widgetRect.X;
                int which = localX <= halfW ? 1 : 2;
                ButtonClicked?.Invoke(this, new FlowGraphButtonClickedEventArgs(
                    widgetNode.Id, widgetNode.CardType, widgetPortIdx, port.Id, which));
            }

            PortWidgetClicked?.Invoke(this, new FlowGraphPortEventArgs(widgetNode, port, widgetPortIdx));
            InvalidateGraphVisual();
            e.Handled = true;
            return;
        }

        if (TryHitPort(local, out var portNode, out int portIndex))
        {
            _connectingNodeId = portNode.Id;
            _connectingPortIndex = portIndex;
            _connectingPointer = local;
            CaptureMouse();
            InvalidateGraphVisual();
            e.Handled = true;
            return;
        }

        if (TryHitSelectedConnectionHandle(local, out int handle, out Point handleScreen))
        {
            _draggingControlPoint = handle;
            _draggingConnectionId = _selectedConnectionId;
            var pointerWorld = ToWorld(local);
            var handleWorld = ToWorld(handleScreen);
            _controlDragPointerOffset = new Point(
                handleWorld.X - pointerWorld.X,
                handleWorld.Y - pointerWorld.Y);
            CaptureMouse();
            e.Handled = true;
            return;
        }

        var hitConn = HitTestConnection(local);
        if (hitConn != null)
        {
            _selectedConnectionId = hitConn.Id;
            _selectedNodeIds.Clear();
            NotifySelectionChanged();
            ConnectionSelected?.Invoke(this, new FlowGraphConnectionEventArgs(hitConn));
            InvalidateGraphVisual();
            e.Handled = true;
            return;
        }

        var node2 = HitTestNode(world);
        if (node2 != null)
        {
            bool ctrl = e.Modifiers.HasFlag(ModifierKeys.Control);
            if (!_selectedNodeIds.Contains(node2.Id))
            {
                if (!ctrl) _selectedNodeIds.Clear();
                _selectedNodeIds.Add(node2.Id);
            }

            _selectedConnectionId = -1;
            _isDraggingNodes = true;
            _lastDragWorld = world;
            CaptureMouse();
            NotifySelectionChanged();
            NodeSelected?.Invoke(this, new FlowGraphNodeEventArgs(node2));
            InvalidateGraphVisual();
            e.Handled = true;
            return;
        }

        if (!e.Modifiers.HasFlag(ModifierKeys.Control) && !e.Modifiers.HasFlag(ModifierKeys.Shift))
            _selectedNodeIds.Clear();

        _selectedConnectionId = -1;
        _isSelecting = true;
        _selectionStartWorld = world;
        _selectionEndWorld = world;
        CaptureMouse();
        NotifySelectionChanged();
        InvalidateGraphVisual();
        e.Handled = true;
    }

    // ==================== 鼠标移动 ====================
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!IsEffectivelyEnabled) return;

        var local = e.GetPosition(this);
        var previousPointerLocal = _lastPointerLocal;
        _lastPointerLocal = local;

        // ★★★ 滚动条拖动 ★★★
        if (_draggingHScrollThumb)
        {
            UpdateScrollBounds();
            if (GetHorizontalScrollMetrics(out double hStart, out double hTrack,
                    out _, out double hThumbLength, out double hMaxScroll))
            {
                double travel = Math.Max(0, hTrack - hThumbLength);
                double pos = local.X - hStart - _scrollDragOffset;
                double ratio = travel > 0 ? pos / travel : 0;
                _panX = Math.Clamp(ratio * hMaxScroll, 0, hMaxScroll);
            }
            InvalidateGraphVisual();
            e.Handled = true;
            return;
        }

        if (_draggingVScrollThumb)
        {
            UpdateScrollBounds();
            if (GetVerticalScrollMetrics(out double vStart, out double vTrack,
                    out _, out double vThumbLength, out double vMaxScroll))
            {
                double travel = Math.Max(0, vTrack - vThumbLength);
                double pos = local.Y - vStart - _scrollDragOffset;
                double ratio = travel > 0 ? pos / travel : 0;
                _panY = Math.Clamp(ratio * vMaxScroll, 0, vMaxScroll);
            }
            InvalidateGraphVisual();
            e.Handled = true;
            return;
        }

        // 侧边栏悬停
        if (!_isPanning && !_isDraggingNodes && !_isSelecting)
        {
            bool overSidebarButtonArea = local.X >= 0 && local.X < FlowGraphConstants.SidebarWidth;
            bool overSidebarPanelArea = IsPointInsideOpenSidebarPanel(local);

            if (overSidebarButtonArea || overSidebarPanelArea)
            {
                UpdateSidebarHover(local);
                e.Handled = true;
                return;
            }

            if (HasOpenSidebarPanel() || _sidebarHoverBtn != -1)
            {
                _sidebarHoverBtn = -1;
                CloseSidebarPanels();
                InvalidateGraphVisual();
            }
        }

        if (_contextMenuOpen)
        {
            _hoverTitleNodeId = -1;
            int oldHover = _contextMenuHover;
            _contextMenuHover = HitTestContextMenu(local);
            if (oldHover != _contextMenuHover) InvalidateGraphVisual();
            return;
        }

        if (_expandedComboNodeId != -1)
        {
            _hoverTitleNodeId = -1;
            int oldHover = _comboHoverIndex;
            _comboHoverIndex = HitTestComboPopup(local);
            if (oldHover != _comboHoverIndex) InvalidateGraphVisual();
            return;
        }

        if (_isPanning)
        {
            double maxPanX = _showHScrollBar ? Math.Max(0, _scrollContentWidth - (Bounds.Width - FlowGraphConstants.SidebarWidth)) : 0;
            double maxPanY = _showVScrollBar ? Math.Max(0, _scrollContentHeight - Bounds.Height) : 0;

            _panX = Math.Clamp(_panStartX - (local.X - _panStartPointer.X), 0, maxPanX);
            _panY = Math.Clamp(_panStartY - (local.Y - _panStartPointer.Y), 0, maxPanY);
            InvalidateGraphVisual();
            e.Handled = true;
            return;
        }

        if (_resizingWidgetNodeId != -1)
        {
            var node = FindNode(_resizingWidgetNodeId);
            if (node != null && IsValidPortIndex(node, _resizingWidgetPortIndex))
            {
                var port = node.Ports[_resizingWidgetPortIndex];
                var world = ToWorld(local);
                double minWidth = port.Widget is FlowGraphWidgetKind.ImagePreview
                    or FlowGraphWidgetKind.Edit or FlowGraphWidgetKind.Text or FlowGraphWidgetKind.MultilineText
                    ? 300 : 120;
                double minHeight = port.Widget == FlowGraphWidgetKind.ImagePreview ? 180 : 45;

                port.WidgetWidth = Math.Clamp(_resizeStartWidth + world.X - _resizeStartWorld.X, minWidth, 1600);
                port.WidgetHeight = Math.Clamp(_resizeStartHeight + world.Y - _resizeStartWorld.Y, minHeight, 1200);

                FitNodeWidthToWidgets(node);
                RecalculateNodeSize(node);
                UpdateScrollBounds();
                InvalidateGraphVisual();
            }
            e.Handled = true;
            return;
        }

        if (_connectingNodeId != -1)
        {
            _connectingPointer = local;
            InvalidateGraphVisual();
            e.Handled = true;
            return;
        }

        if (_draggingControlPoint != 0)
        {
            var conn = FindConnection(_draggingConnectionId);
            if (conn != null)
            {
                var pointerWorld = ToWorld(local);
                var world = new Point(
                    pointerWorld.X + _controlDragPointerOffset.X,
                    pointerWorld.Y + _controlDragPointerOffset.Y);
                SetCurveHandleOnCurve(conn, _draggingControlPoint, world);
                InvalidateGraphVisual();
            }
            e.Handled = true;
            return;
        }

        if (_isDraggingNodes)
        {
            var world = ToWorld(local);
            double dx = world.X - _lastDragWorld.X;
            double dy = world.Y - _lastDragWorld.Y;

            if (dx != 0 || dy != 0)
            {
                foreach (var node in _nodes.Where(n => _selectedNodeIds.Contains(n.Id)))
                {
                    node.X = Math.Max(0, node.X + dx);
                    node.Y = Math.Max(0, node.Y + dy);

                    foreach (var conn in _connections)
                    {
                        if (conn.FromNodeId == node.Id)
                            conn.ControlPoint1 = new Point(conn.ControlPoint1.X + dx, conn.ControlPoint1.Y + dy);
                        if (conn.ToNodeId == node.Id)
                            conn.ControlPoint2 = new Point(conn.ControlPoint2.X + dx, conn.ControlPoint2.Y + dy);
                    }

                    NodeMoved?.Invoke(this, new FlowGraphNodeEventArgs(node));
                }

                _lastDragWorld = world;
                UpdateScrollBounds();
                InvalidateGraphVisual();
            }
            e.Handled = true;
            return;
        }

        if (_isSelecting)
        {
            _selectionEndWorld = ToWorld(local);
            UpdateBoxSelection();
            InvalidateGraphVisual();
            e.Handled = true;
            return;
        }

        // 悬停检测
        int oldHoverNode = _hoverNodeId;
        int oldPortNode = _hoverPortNodeId;
        int oldPortIdx = _hoverPortIndex;
        var worldPt = ToWorld(local);

        _hoverNodeId = HitTestNode(worldPt)?.Id ?? -1;

        if (TryHitPort(local, out var hpNode, out int hpIdx))
        {
            _hoverPortNodeId = hpNode.Id;
            _hoverPortIndex = hpIdx;
        }
        else
        {
            _hoverPortNodeId = -1;
            _hoverPortIndex = -1;
        }

        int oldTitleNode = _hoverTitleNodeId;
        _hoverTitleNodeId = -1;
        for (int i = _nodes.Count - 1; i >= 0; i--)
        {
            var candidate = _nodes[i];
            if (worldPt.X >= candidate.X && worldPt.X <= candidate.X + candidate.Width &&
                worldPt.Y >= candidate.Y && worldPt.Y <= candidate.Y + FlowGraphConstants.TitleHeight)
            {
                _hoverTitleNodeId = candidate.Id;
                break;
            }
        }

        bool tooltipPointerMoved = _hoverTitleNodeId >= 0 &&
                                   (Math.Abs(local.X - previousPointerLocal.X) > 0.25 ||
                                    Math.Abs(local.Y - previousPointerLocal.Y) > 0.25);

        if (oldHoverNode != _hoverNodeId || oldPortNode != _hoverPortNodeId ||
            oldPortIdx != _hoverPortIndex || oldTitleNode != _hoverTitleNodeId ||
            tooltipPointerMoved)
            InvalidateGraphVisual();
    }

    // ==================== 鼠标释放 ====================
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!IsEffectivelyEnabled) return;

        var local = e.GetPosition(this);

        if (e.Button == MouseButton.Left)
        {
            if (_draggingHScrollThumb || _draggingVScrollThumb)
            {
                _draggingHScrollThumb = false;
                _draggingVScrollThumb = false;
                ReleaseMouseCapture();
                e.Handled = true;
                return;
            }

            if (_connectingNodeId != -1)
            {
                if (TryHitPort(local, out var targetNode, out int targetPort))
                    AddConnection(_connectingNodeId, _connectingPortIndex, targetNode.Id, targetPort);

                _connectingNodeId = -1;
                _connectingPortIndex = -1;
                InvalidateGraphVisual();
            }

            if (_isSelecting)
            {
                _selectionEndWorld = ToWorld(local);
                UpdateBoxSelection();
                _isSelecting = false;
                NotifySelectionChanged();
                InvalidateGraphVisual();
            }

            _isDraggingNodes = false;
            _draggingControlPoint = 0;
            _draggingConnectionId = -1;
            _controlDragPointerOffset = new Point(0, 0);
            _resizingWidgetNodeId = -1;
            _resizingWidgetPortIndex = -1;
            ReleaseMouseCapture();
            e.Handled = true;
        }
        else if (e.Button == MouseButton.Middle)
        {
            _isPanning = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    // ==================== 鼠标滚轮 ====================
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!IsEffectivelyEnabled || e.Delta.Y == 0) return;

        if (_expandedComboNodeId != -1)
            return;

        // ★ 无Ctrl时，滚轮控制纵向滚动
        if (!e.Modifiers.HasFlag(ModifierKeys.Control))
        {
            if (_showVScrollBar)
            {
                double viewH = Bounds.Height;
                double maxScroll = Math.Max(0, _scrollContentHeight - viewH);
                double step = 40;
                _panY = Math.Clamp(_panY - Math.Sign(e.Delta.Y) * step, 0, maxScroll);
                InvalidateGraphVisual();
                e.Handled = true;
                return;
            }
            return;
        }

        CloseComboPopup();

        var local = e.GetPosition(this);
        double oldZoom = _zoom;
        double factor = e.Delta.Y > 0 ? 1.1 : 0.9;
        double newZoom = Clamp(oldZoom * factor, FlowGraphConstants.MinZoom, FlowGraphConstants.MaxZoom);

        if (Math.Abs(newZoom - oldZoom) < 0.0001) return;

        double worldX = (local.X + _panX) / oldZoom;
        double worldY = (local.Y + _panY) / oldZoom;

        _zoom = newZoom;
        _panX = worldX * newZoom - local.X;
        _panY = worldY * newZoom - local.Y;

        UpdateScrollBounds();
        InvalidateGraphVisual();
        e.Handled = true;
    }

    // ==================== 键盘 ====================
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsEffectivelyEnabled) return;

        bool ctrl = e.Modifiers.HasFlag(ModifierKeys.Control);

        if (e.Key == Key.Delete)
        {
            bool shift = e.Modifiers.HasFlag(ModifierKeys.Shift);
            if (shift && _selectedNodeIds.Count > 0)
            {
                foreach (int id in _selectedNodeIds.ToArray())
                    RemoveNode(id);
                _selectedNodeIds.Clear();
                NotifySelectionChanged();
            }
            else if (_selectedConnectionId != -1)
            {
                RemoveConnection(_selectedConnectionId);
            }

            InvalidateGraphVisual();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CloseComboPopup();
            CloseContextMenu();
            _connectingNodeId = -1;
            _connectingPortIndex = -1;
            _isSelecting = false;
            _isDraggingNodes = false;
            _draggingControlPoint = 0;
            _controlDragPointerOffset = new Point(0, 0);
            _resizingWidgetNodeId = -1;
            _resizingWidgetPortIndex = -1;
            InvalidateGraphVisual();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.C)
        {
            CopySelectedNodesToClipboard();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.X)
        {
            CutSelectedNodesToClipboard();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.V)
        {
            PasteClipboardNodes();
            e.Handled = true;
        }
    }

    protected override void OnMouseLeave()
    {
        base.OnMouseLeave();
        if (!IsMouseCaptured)
        {
            _hoverNodeId = -1;
            _hoverPortNodeId = -1;
            _hoverPortIndex = -1;
            _hoverTitleNodeId = -1;
            _sidebarHoverBtn = -1;
            CloseSidebarPanels();
            InvalidateGraphVisual();
        }
    }

    protected override void OnMewPropertyChanged(MewProperty property)
    {
        base.OnMewPropertyChanged(property);

        if (property == IsMouseCapturedProperty && !IsMouseCaptured)
        {
            _isPanning = false;
            _isDraggingNodes = false;
            _isSelecting = false;
            _connectingNodeId = -1;
            _connectingPortIndex = -1;
            _draggingControlPoint = 0;
            _draggingConnectionId = -1;
            _controlDragPointerOffset = new Point(0, 0);
            _draggingHScrollThumb = false;
            _draggingVScrollThumb = false;
            InvalidateGraphVisual();
        }
    }

    // ==================== 侧边栏交互 ====================
    private bool HasOpenSidebarPanel()
        => _showAddPanel || _showChainPanel || _showCancelPanel || _showCustomPanel;

    private void CloseSidebarPanels()
    {
        _showAddPanel = false;
        _showChainPanel = false;
        _showCancelPanel = false;
        _showCustomPanel = false;
        _addPanelHover = -1;
        _chainPanelHover = -1;
        _cancelPanelHover = -1;
        _customPanelHover = -1;
    }

    private static bool IsInsideSidebarPanel(Point local, double panelY, double panelWidth, double panelHeight)
    {
        double left = FlowGraphConstants.SidebarWidth;
        return local.X >= left && local.X < left + panelWidth &&
               local.Y >= panelY && local.Y < panelY + panelHeight;
    }

    private bool IsPointInsideOpenSidebarPanel(Point local)
    {
        if (_showAddPanel)
        {
            int count = _cardRegistry.Count;
            double h = FlowGraphConstants.AddPanelHeader +
                       (count > 0 ? count * FlowGraphConstants.AddPanelItemH + 4 : 36);
            if (IsInsideSidebarPanel(local, FlowGraphConstants.SidebarBtnY0,
                    FlowGraphConstants.AddPanelWidth, h)) return true;
        }

        if (_showChainPanel && _nodes.Count > 0)
        {
            int visibleCount = Math.Min(_nodes.Count, FlowGraphConstants.ChainPanelMaxVisible);
            double y = _chainPanelMode == 0 ? FlowGraphConstants.SidebarBtnY1 : FlowGraphConstants.SidebarBtnY2;
            double h = FlowGraphConstants.ChainPanelHeader +
                       visibleCount * FlowGraphConstants.ChainPanelItemH + 4;
            if (IsInsideSidebarPanel(local, y, FlowGraphConstants.ChainPanelWidth, h)) return true;
        }

        if (_showCancelPanel)
        {
            int busyCount = _nodes.Count(n =>
                n.ExecutionStatus == FlowGraphExecutionStatus.Running ||
                n.ExecutionStatus == FlowGraphExecutionStatus.Pending);
            if (busyCount > 0)
            {
                int visibleCount = Math.Min(busyCount, FlowGraphConstants.CancelPanelMaxVisible);
                double h = FlowGraphConstants.CancelPanelHeader +
                           visibleCount * FlowGraphConstants.CancelPanelItemH + 4;
                if (IsInsideSidebarPanel(local, FlowGraphConstants.SidebarBtnY3,
                        FlowGraphConstants.CancelPanelWidth, h)) return true;
            }
        }

        if (_showCustomPanel)
        {
            int count = _customItems.Length;
            double h = FlowGraphConstants.CustomPanelHeader +
                       (count > 0 ? Math.Min(count, FlowGraphConstants.CustomPanelMaxVisible) *
                           FlowGraphConstants.CustomPanelItemH + 4 : 36);
            if (IsInsideSidebarPanel(local, FlowGraphConstants.SidebarBtnY6,
                    FlowGraphConstants.CustomPanelWidth, h)) return true;
        }

        return false;
    }

    private static int GetSidebarPanelItemIndex(
        double pointerY, double panelY, double headerHeight, double itemHeight, int itemCount)
    {
        if (itemCount <= 0 || itemHeight <= 0) return -1;
        double itemY = pointerY - panelY - headerHeight;
        if (itemY < 0 || itemY >= itemCount * itemHeight) return -1;
        return (int)(itemY / itemHeight);
    }

    private void UpdateSidebarHover(Point local)
    {
        if (IsPointInsideOpenSidebarPanel(local))
        {
            _sidebarHoverBtn = -1;
            _addPanelHover = -1;
            _chainPanelHover = -1;
            _cancelPanelHover = -1;
            _customPanelHover = -1;

            if (_showAddPanel)
            {
                _addPanelHover = GetSidebarPanelItemIndex(local.Y,
                    FlowGraphConstants.SidebarBtnY0, FlowGraphConstants.AddPanelHeader,
                    FlowGraphConstants.AddPanelItemH, _cardRegistry.Count);
            }
            else if (_showChainPanel)
            {
                double panelY = _chainPanelMode == 0 ? FlowGraphConstants.SidebarBtnY1 : FlowGraphConstants.SidebarBtnY2;
                int visibleCount = Math.Min(_nodes.Count, FlowGraphConstants.ChainPanelMaxVisible);
                _chainPanelHover = GetSidebarPanelItemIndex(local.Y, panelY,
                    FlowGraphConstants.ChainPanelHeader, FlowGraphConstants.ChainPanelItemH, visibleCount);
            }
            else if (_showCancelPanel)
            {
                int busyCount = _nodes.Count(n =>
                    n.ExecutionStatus == FlowGraphExecutionStatus.Running ||
                    n.ExecutionStatus == FlowGraphExecutionStatus.Pending);
                int visibleCount = Math.Min(busyCount, FlowGraphConstants.CancelPanelMaxVisible);
                _cancelPanelHover = GetSidebarPanelItemIndex(local.Y,
                    FlowGraphConstants.SidebarBtnY3, FlowGraphConstants.CancelPanelHeader,
                    FlowGraphConstants.CancelPanelItemH, visibleCount);
            }
            else if (_showCustomPanel)
            {
                int visibleCount = Math.Min(_customItems.Length, FlowGraphConstants.CustomPanelMaxVisible);
                _customPanelHover = GetSidebarPanelItemIndex(local.Y,
                    FlowGraphConstants.SidebarBtnY6, FlowGraphConstants.CustomPanelHeader,
                    FlowGraphConstants.CustomPanelItemH, visibleCount);
            }

            InvalidateGraphVisual();
            return;
        }

        if (local.X >= 0 && local.X < FlowGraphConstants.SidebarWidth)
        {
            double[] btnYs = GetSidebarButtonYs();
            _sidebarHoverBtn = -1;

            for (int i = 0; i < FlowGraphConstants.SidebarBtnCount; i++)
            {
                if (local.Y >= btnYs[i] && local.Y < btnYs[i] + FlowGraphConstants.SidebarBtnHeight)
                {
                    _sidebarHoverBtn = i;
                    break;
                }
            }

            _addPanelHover = -1;
            _chainPanelHover = -1;
            _cancelPanelHover = -1;
            _customPanelHover = -1;

            switch (_sidebarHoverBtn)
            {
                case 0:
                    CloseSidebarPanels();
                    _showAddPanel = true;
                    break;
                case 1:
                    CloseSidebarPanels();
                    _chainPanelMode = 0;
                    _showChainPanel = _nodes.Count > 0;
                    break;
                case 2:
                    CloseSidebarPanels();
                    _chainPanelMode = 1;
                    _showChainPanel = _nodes.Count > 0;
                    break;
                case 3:
                    CloseSidebarPanels();
                    _showCancelPanel = _nodes.Any(n =>
                        n.ExecutionStatus == FlowGraphExecutionStatus.Running ||
                        n.ExecutionStatus == FlowGraphExecutionStatus.Pending);
                    break;
                case 6:
                    CloseSidebarPanels();
                    _showCustomPanel = true;
                    break;
                default:
                    CloseSidebarPanels();
                    break;
            }

            InvalidateGraphVisual();
            return;
        }

        _sidebarHoverBtn = -1;
        CloseSidebarPanels();
        InvalidateGraphVisual();
    }

    private void HandleSidebarClick(Point local)
    {
        double[] btnYs = GetSidebarButtonYs();

        if (_showAddPanel && local.X >= FlowGraphConstants.SidebarWidth)
        {
            double panelY = FlowGraphConstants.SidebarBtnY0;
            double itemStartY = panelY + FlowGraphConstants.AddPanelHeader;
            int idx = (int)((local.Y - itemStartY) / FlowGraphConstants.AddPanelItemH);
            if (idx >= 0 && idx < _cardRegistry.Count)
            {
                var desc = _cardRegistry.Values.ElementAt(idx);
                SidebarAddCard?.Invoke(this, new FlowGraphAddCardEventArgs(desc.CardType));
            }
            _showAddPanel = false;
            InvalidateGraphVisual();
            return;
        }

        if (_showChainPanel && local.X >= FlowGraphConstants.SidebarWidth)
        {
            double panelY = _chainPanelMode == 0 ? FlowGraphConstants.SidebarBtnY1 : FlowGraphConstants.SidebarBtnY2;
            double itemStartY = panelY + FlowGraphConstants.ChainPanelHeader;
            int idx = (int)((local.Y - itemStartY) / FlowGraphConstants.ChainPanelItemH);
            int visibleCount = Math.Min(_nodes.Count, FlowGraphConstants.ChainPanelMaxVisible);
            if (idx >= 0 && idx < visibleCount)
            {
                var node = _nodes[idx];
                var mode = _chainPanelMode == 0 ? FlowGraphExecutionMode.Fresh : FlowGraphExecutionMode.Continue;
                _ = ExecuteChainAsync(node.Id, mode);
            }
            _showChainPanel = false;
            InvalidateGraphVisual();
            return;
        }

        if (_showCancelPanel && local.X >= FlowGraphConstants.SidebarWidth)
        {
            var busyNodes = _nodes.Where(n =>
                n.ExecutionStatus == FlowGraphExecutionStatus.Running ||
                n.ExecutionStatus == FlowGraphExecutionStatus.Pending).ToList();

            double panelY = FlowGraphConstants.SidebarBtnY3;
            double itemStartY = panelY + FlowGraphConstants.CancelPanelHeader;
            int idx = (int)((local.Y - itemStartY) / FlowGraphConstants.CancelPanelItemH);
            if (idx >= 0 && idx < busyNodes.Count)
                CancelNode(busyNodes[idx].Id);

            _showCancelPanel = false;
            InvalidateGraphVisual();
            return;
        }

        if (_showCustomPanel && local.X >= FlowGraphConstants.SidebarWidth)
        {
            double panelY = FlowGraphConstants.SidebarBtnY6;
            double itemStartY = panelY + FlowGraphConstants.CustomPanelHeader;
            int idx = (int)((local.Y - itemStartY) / FlowGraphConstants.CustomPanelItemH);
            if (idx >= 0 && idx < _customItems.Length)
                CustomItemClicked?.Invoke(this, new FlowGraphCustomItemClickedEventArgs(idx, _customItems[idx]));

            _showCustomPanel = false;
            InvalidateGraphVisual();
            return;
        }

        for (int i = 0; i < FlowGraphConstants.SidebarBtnCount; i++)
        {
            if (local.Y >= btnYs[i] && local.Y < btnYs[i] + FlowGraphConstants.SidebarBtnHeight)
            {
                if (i == 4)
                    YamlExportRequested?.Invoke(this, EventArgs.Empty);
                else if (i == 5)
                    YamlImportRequested?.Invoke(this, EventArgs.Empty);

                _sidebarHoverBtn = -1;
                CloseSidebarPanels();
                InvalidateGraphVisual();
                return;
            }
        }
    }

    // ==================== 右键菜单交互 ====================
    private void HandleContextMenuClick(Point local)
    {
        int index = HitTestContextMenu(local);
        int nodeId = _contextMenuNodeId;
        CloseContextMenu();

        if (index >= 0)
        {
            var node = FindNode(nodeId);
            if (node != null)
            {
                switch (index)
                {
                    case 0:
                        _ = ExecuteChainAsync(nodeId, FlowGraphExecutionMode.Fresh);
                        break;
                    case 1:
                        _ = ExecuteChainAsync(nodeId, FlowGraphExecutionMode.Continue);
                        break;
                    case 2:
                        CancelNode(nodeId);
                        break;
                    case 3:
                        ClearNodeOutputs(node);
                        break;
                }
            }
        }

        InvalidateGraphVisual();
    }

    private int HitTestContextMenu(Point local)
    {
        double menuW = FlowGraphConstants.ContextMenuWidth;
        double menuH = FlowGraphConstants.ContextMenuHeader * 2 + FlowGraphConstants.ContextMenuItemH * 4;
        double mx = Math.Clamp(_contextMenuPosition.X, 0, Bounds.Width - menuW);
        double my = Math.Clamp(_contextMenuPosition.Y, 0, Bounds.Height - menuH);

        if (local.X < mx || local.X > mx + menuW || local.Y < my || local.Y > my + menuH)
            return -1;

        int idx = (int)((local.Y - my - FlowGraphConstants.ContextMenuHeader) / FlowGraphConstants.ContextMenuItemH);
        return idx >= 0 && idx < 4 ? idx : -1;
    }

    private void CloseContextMenu()
    {
        _contextMenuOpen = false;
        _contextMenuNodeId = -1;
        _contextMenuHover = -1;
    }

    // ==================== 组合框交互 ====================
    private void OpenComboPopup(int nodeId, int portIndex)
    {
        var node = FindNode(nodeId);
        if (node is null || !IsValidPortIndex(node, portIndex)) return;

        var port = node.Ports[portIndex];
        if (port.Widget != FlowGraphWidgetKind.Combo || port.ComboData is null || port.ComboData.Options.Count == 0) return;

        if (_expandedComboNodeId == nodeId && _expandedComboPortIndex == portIndex)
        {
            CloseComboPopup();
            return;
        }

        _expandedComboNodeId = nodeId;
        _expandedComboPortIndex = portIndex;
        _comboHoverIndex = -1;
        InvalidateGraphVisual();
    }

    private void CloseComboPopup()
    {
        _expandedComboNodeId = -1;
        _expandedComboPortIndex = -1;
        _comboHoverIndex = -1;
    }

    private int HitTestComboPopup(Point local)
    {
        var node = FindNode(_expandedComboNodeId);
        if (node is null || !IsValidPortIndex(node, _expandedComboPortIndex)) return -1;

        var port = node.Ports[_expandedComboPortIndex];
        if (port.ComboData is null || port.ComboData.Options.Count == 0) return -1;

        var widgetRect = WorldToScreen(GetWidgetRect(node, _expandedComboPortIndex));
        double itemH = FlowGraphConstants.ComboItemHeight * _zoom;
        double panelX = widgetRect.X;
        double panelY = widgetRect.Y + widgetRect.Height;
        double panelW = widgetRect.Width;
        double panelH = port.ComboData.Options.Count * itemH;

        if (local.X < panelX || local.X > panelX + panelW || local.Y < panelY || local.Y > panelY + panelH)
            return -1;

        int idx = (int)((local.Y - panelY) / itemH);
        return idx >= 0 && idx < port.ComboData.Options.Count ? idx : -1;
    }

    // ==================== HitTest ====================
    private bool TryHitPort(Point local, out FlowGraphNode node, out int portIndex)
    {
        var world = ToWorld(local);
        var bodyNode = HitTestNode(world);

        if (bodyNode is not null)
            return TryHitPortOnNode(bodyNode, world, out node, out portIndex);

        for (int pass = 0; pass < 2; pass++)
        {
            bool selectedPass = pass == 0;
            for (int n = _nodes.Count - 1; n >= 0; n--)
            {
                var candidate = _nodes[n];
                if (_selectedNodeIds.Contains(candidate.Id) != selectedPass) continue;
                if (TryHitPortOnNode(candidate, world, out node, out portIndex))
                    return true;
            }
        }

        node = null!;
        portIndex = -1;
        return false;
    }

    private bool TryHitPortOnNode(FlowGraphNode candidate, Point world,
        out FlowGraphNode node, out int portIndex)
    {
        for (int i = 0; i < candidate.Ports.Count; i++)
        {
            var port = candidate.Ports[i];
            if (port.Direction == FlowGraphPortDirection.Inline) continue;

            var anchor = GetPortAnchor(candidate, i);
            double dx = (world.X - anchor.X) * _zoom;
            double dy = (world.Y - anchor.Y) * _zoom;
            double hitRadius = FlowGraphConstants.PortHoverRadius + 4;

            if (dx * dx + dy * dy <= hitRadius * hitRadius)
            {
                node = candidate;
                portIndex = i;
                return true;
            }
        }

        node = null!;
        portIndex = -1;
        return false;
    }

    private bool TryHitWidgetResizeHandle(Point local, out FlowGraphNode node, out int portIndex)
    {
        var world = ToWorld(local);
        var bodyNode = HitTestNode(world);

        if (bodyNode is not null)
        {
            if (TryHitWidgetResizeHandleOnNode(bodyNode, local, out node, out portIndex))
                return true;
            node = null!;
            portIndex = -1;
            return false;
        }

        for (int pass = 0; pass < 2; pass++)
        {
            bool selectedPass = pass == 0;
            for (int n = _nodes.Count - 1; n >= 0; n--)
            {
                var candidate = _nodes[n];
                if (_selectedNodeIds.Contains(candidate.Id) != selectedPass) continue;
                if (TryHitWidgetResizeHandleOnNode(candidate, local, out node, out portIndex))
                    return true;
            }
        }

        node = null!;
        portIndex = -1;
        return false;
    }

    private bool TryHitWidgetResizeHandleOnNode(FlowGraphNode candidate, Point local,
        out FlowGraphNode node, out int portIndex)
    {
        for (int i = candidate.Ports.Count - 1; i >= 0; i--)
        {
            var port = candidate.Ports[i];
            if (!HasVisibleWidget(port) || !IsResizableWidget(port.Widget))
                continue;

            var rect = WorldToScreen(GetWidgetRect(candidate, i));
            if (local.X >= rect.Right - 12 && local.X <= rect.Right + 3 &&
                local.Y >= rect.Bottom - 12 && local.Y <= rect.Bottom + 3)
            {
                node = candidate;
                portIndex = i;
                return true;
            }
        }

        node = null!;
        portIndex = -1;
        return false;
    }

    private bool TryHitWidget(Point local, out FlowGraphNode node, out int portIndex)
    {
        var world = ToWorld(local);
        var candidate = HitTestNode(world);

        if (candidate is not null)
        {
            for (int i = 0; i < candidate.Ports.Count; i++)
            {
                var port = candidate.Ports[i];
                if (!HasVisibleWidget(port)) continue;

                var rect = GetWidgetRect(candidate, i);
                if (world.X >= rect.X && world.X <= rect.Right &&
                    world.Y >= rect.Y && world.Y <= rect.Bottom)
                {
                    node = candidate;
                    portIndex = i;
                    return true;
                }
            }
        }

        node = null!;
        portIndex = -1;
        return false;
    }

    private bool TryHitSelectedConnectionHandle(Point local, out int handle, out Point handleScreen)
    {
        handle = 0;
        handleScreen = new Point(0, 0);

        var conn = FindConnection(_selectedConnectionId);
        if (conn is null) return false;

        var fromNode = FindNode(conn.FromNodeId);
        var toNode = FindNode(conn.ToNodeId);
        if (fromNode is null || toNode is null ||
            !IsValidPortIndex(fromNode, conn.FromPortIndex) ||
            !IsValidPortIndex(toNode, conn.ToPortIndex))
            return false;

        var p0 = WorldToScreen(GetPortAnchor(fromNode, conn.FromPortIndex));
        var p3 = WorldToScreen(GetPortAnchor(toNode, conn.ToPortIndex));
        var cp1 = WorldToScreen(conn.ControlPoint1);
        var cp2 = WorldToScreen(conn.ControlPoint2);

        var p1 = CubicBezier(p0, cp1, cp2, p3, 1.0 / 3.0);
        var p2 = CubicBezier(p0, cp1, cp2, p3, 2.0 / 3.0);

        if (Distance(local, p1) <= 9) { handle = 1; handleScreen = p1; return true; }
        if (Distance(local, p2) <= 9) { handle = 2; handleScreen = p2; return true; }

        return false;
    }

    private FlowGraphNode? HitTestNode(Point world)
    {
        for (int i = _nodes.Count - 1; i >= 0; i--)
        {
            var node = _nodes[i];
            if (_selectedNodeIds.Contains(node.Id) && ContainsNode(node, world))
                return node;
        }

        for (int i = _nodes.Count - 1; i >= 0; i--)
        {
            var node = _nodes[i];
            if (!_selectedNodeIds.Contains(node.Id) && ContainsNode(node, world))
                return node;
        }

        return null;
    }

    private static bool ContainsNode(FlowGraphNode node, Point world)
        => world.X >= node.X && world.X <= node.X + node.Width &&
           world.Y >= node.Y && world.Y <= node.Y + node.Height;

    private FlowGraphConnection? HitTestConnection(Point local)
    {
        FlowGraphConnection? best = null;
        double bestDist = 8;

        foreach (var conn in _connections)
        {
            var from = FindNode(conn.FromNodeId);
            var to = FindNode(conn.ToNodeId);
            if (from is null || to is null) continue;
            if (!IsValidPortIndex(from, conn.FromPortIndex) || !IsValidPortIndex(to, conn.ToPortIndex)) continue;

            var p0 = WorldToScreen(GetPortAnchor(from, conn.FromPortIndex));
            var p1 = WorldToScreen(conn.ControlPoint1);
            var p2 = WorldToScreen(conn.ControlPoint2);
            var p3 = WorldToScreen(GetPortAnchor(to, conn.ToPortIndex));

            var prev = p0;
            for (int i = 1; i <= 28; i++)
            {
                var cur = CubicBezier(p0, p1, p2, p3, i / 28.0);
                double d = DistanceToSegment(local, prev, cur);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = conn;
                }
                prev = cur;
            }
        }

        return best;
    }

    private void UpdateBoxSelection()
    {
        var rect = NormalizeRect(_selectionStartWorld, _selectionEndWorld);
        _selectedNodeIds.Clear();

        foreach (var node in _nodes)
        {
            if (node.X + node.Width >= rect.X && node.X <= rect.Right &&
                node.Y + node.Height >= rect.Y && node.Y <= rect.Bottom)
                _selectedNodeIds.Add(node.Id);
        }
    }

    // ==================== 鼠标捕获 ====================
    private void CaptureMouse()
    {
        if (FindVisualRoot() is Window window)
            window.CaptureMouse(this);
    }

    private void ReleaseMouseCapture()
    {
        if (FindVisualRoot() is Window window)
            window.ReleaseMouseCapture();
    }

    private void NotifySelectionChanged() => SelectionChanged?.Invoke(this, EventArgs.Empty);
}
