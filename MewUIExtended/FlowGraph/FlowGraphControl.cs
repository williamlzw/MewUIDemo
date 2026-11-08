using System.Text;
using System.Diagnostics;
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Input;
using Aprillz.MewUI.Rendering;
using Aprillz.MewUI.Resources;
using MewUIExtended.FlowGraph;

namespace MewUI.Extended;

public sealed partial class FlowGraphControl : Control
{
    // ==================== 状态 ====================
    private readonly List<FlowGraphNode> _nodes = new();
    private readonly List<FlowGraphConnection> _connections = new();
    private readonly HashSet<int> _selectedNodeIds = new();
    private readonly Dictionary<int, FlowGraphCardDescriptor> _cardRegistry = new();
    private readonly List<FlowGraphChainContext> _chainContexts = new();

    private int _nextNodeId = 1;
    private int _nextConnectionId = 1;
    private int _nextPortId = 1;
    private int _executionPass;
    private bool _isExecuting;
    private CancellationTokenSource? _executionCancellation;
    private readonly SemaphoreSlim _executionGate = new(1, 1);

    // IImageSource 不是后端 IImage。缓存已创建的后端图像，避免每帧重复解码/上传。
    private readonly Dictionary<FlowGraphImageData, IImage> _renderImages = new();

    // 视口
    private double _zoom = 1.0;
    private double _panX, _panY;

    // ★ 滚动条状态
    private bool _showHScrollBar;
    private bool _showVScrollBar;
    private double _scrollContentWidth;   // 内容总宽(世界坐标)
    private double _scrollContentHeight;  // 内容总高(世界坐标)
    private bool _draggingHScrollThumb;
    private bool _draggingVScrollThumb;
    private double _scrollDragOffset;
    private const double ScrollBarThickness = 14;
    private const double ScrollBarMinThumbSize = 30;

    // 交互状态
    private bool _isPanning;
    private Point _panStartPointer;
    private double _panStartX, _panStartY;
    private bool _isDraggingNodes;
    private Point _lastDragWorld;
    private bool _isSelecting;
    private Point _selectionStartWorld, _selectionEndWorld;
    private int _connectingNodeId = -1;
    private int _connectingPortIndex = -1;
    private Point _connectingPointer;
    private int _draggingControlPoint;
    private int _draggingConnectionId = -1;
    private int _selectedConnectionId = -1;
    private int _hoverNodeId = -1;
    private int _hoverPortNodeId = -1;
    private int _hoverPortIndex = -1;
    private int _hoverTitleNodeId = -1;
    private Point _lastPointerLocal;

    // 侧边栏
    private int _sidebarHoverBtn = -1;
    private bool _showAddPanel;
    private int _addPanelHover = -1;
    private bool _showChainPanel;
    private int _chainPanelHover = -1;
    private int _chainPanelMode;
    private bool _showCancelPanel;
    private int _cancelPanelHover = -1;
    private bool _showCustomPanel;
    private int _customPanelHover = -1;
    private string[] _customItems = Array.Empty<string>();

    // 右键菜单
    private bool _contextMenuOpen;
    private int _contextMenuNodeId = -1;
    private Point _contextMenuPosition;
    private int _contextMenuHover = -1;

    // 组合框下拉
    private int _expandedComboNodeId = -1;
    private int _expandedComboPortIndex = -1;
    private int _comboHoverIndex = -1;

    // 编辑面板
    private bool _showEditPanel;
    private int _editPanelTargetNode = -1;

    // 工程名
    private string _projectName = string.Empty;

    private bool _hasExternalFloatingOverlay;
    private FloatingOverlayControl? _tooltipBackgroundOverlayControl;
    private FloatingOverlayControl? _floatingOverlayControl;
    private FloatingOverlayControl? _topmostOverlayControl;

    // ==================== 颜色主题 ====================
    private Color _canvasBackground = Color.FromRgb(80, 80, 80);
    private Color _gridColor = Color.FromRgb(29, 29, 29);
    private Color _nodeBackground = Color.FromRgb(23, 23, 23);
    private Color _nodeBorder = Color.FromRgb(0, 0, 0);
    private Color _nodeTitleForeground = Color.FromRgb(217, 217, 217);
    private Color _selectedColor = Color.FromRgb(255, 255, 255);

    // ==================== 构造函数 ====================
    static FlowGraphControl()
    {
        FocusableProperty.OverrideDefaultValue<FlowGraphControl>(true);
    }

    public FlowGraphControl()
    {
        Background = _canvasBackground;
        BorderBrush = Color.FromRgb(58, 61, 68);
        BorderThickness = 1;
        IsTabStop = true;
    }

    // ==================== 公共属性 ====================
    public IReadOnlyList<FlowGraphNode> Nodes => _nodes;
    public IReadOnlyList<FlowGraphConnection> Connections => _connections;
    public IReadOnlyCollection<int> SelectedNodeIds => _selectedNodeIds;
    public int SelectedConnectionId => _selectedConnectionId;
    public double Zoom => _zoom;
    public Point PanOffset => new(_panX, _panY);
    public bool IsExecuting => _isExecuting;

    public IReadOnlyCollection<FlowGraphCardDescriptor> RegisteredCardTypes
        => _cardRegistry.Values.Select(d => d.Clone()).ToArray();

    // ==================== 公共事件 ====================
    public event EventHandler<FlowGraphNodeEventArgs>? NodeSelected;
    public event EventHandler<FlowGraphNodeEventArgs>? NodeMoved;
    public event EventHandler<FlowGraphNodeEventArgs>? ExecuteNodeRequested;
    public event EventHandler<FlowGraphConnectionEventArgs>? ConnectionCreated;
    public event EventHandler<FlowGraphConnectionEventArgs>? ConnectionRemoved;
    public event EventHandler<FlowGraphConnectionEventArgs>? ConnectionSelected;
    public event EventHandler<FlowGraphPortEventArgs>? PortWidgetClicked;
    public event EventHandler<FlowGraphPortEventArgs>? PortValueChanged;
    public event EventHandler? SelectionChanged;
    public event EventHandler<FlowGraphButtonClickedEventArgs>? ButtonClicked;
    public event EventHandler<FlowGraphAddCardEventArgs>? SidebarAddCard;
    public event EventHandler<FlowGraphCustomItemClickedEventArgs>? CustomItemClicked;
    public event EventHandler<FlowGraphExecutionEventArgs>? NodeExecutionStarted;
    public event EventHandler<FlowGraphExecutionEventArgs>? NodeExecutionCompleted;
    public event EventHandler<FlowGraphExecutionEventArgs>? NodeExecutionFailed;
    public event EventHandler? GraphExecutionStarted;
    public event EventHandler? GraphExecutionCompleted;
    public event EventHandler? GraphExecutionCanceled;
    public event EventHandler<FlowGraphExecutionEventArgs>? NodeCanceled;

    internal event EventHandler? GraphVisualInvalidated;
    internal event EventHandler? GraphPointerPressed;

    public Window? OwnerWindow => FindVisualRoot() as Window;

    // ==================== 渲染 ====================
    protected override Size MeasureContent(Size availableSize)
    {
        double w = double.IsFinite(availableSize.Width) ? availableSize.Width : 800;
        double h = double.IsFinite(availableSize.Height) ? availableSize.Height : 600;
        return new Size(Math.Max(1, w), Math.Max(1, h)).Inflate(GetBorderVisualInset());
    }

    protected override void OnRender(IGraphicsContext context)
    {
        base.OnRender(context);

        // 外部应用文件拖放需要 Window 也参与平台级注册；Graph.AllowDrop
        // 负责将窗口收到的 Drop 路由到节点图控件。
        if (FindVisualRoot() is Window ownerWindow && !ownerWindow.AllowDrop)
            ownerWindow.AllowDrop = true;

        PruneRenderImageCache();
        context.Save();
        context.SetClip(LayoutRounding.MakeClipRect(Bounds, context.DpiScale));

        DrawGrid(context);
        DrawConnections(context);
        DrawNodes(context);
        DrawConnectingLine(context);

        // ★ 绘制滚动条
        DrawScrollBars(context);

        if (!_hasExternalFloatingOverlay)
        {
            DrawTooltipBackground(context);
            RenderFloatingOverlays(context);
        }

        context.Restore();
    }

    internal bool CanAcceptImageDrop(Point windowPosition)
    {
        var local = new Point(windowPosition.X - Bounds.X, windowPosition.Y - Bounds.Y);
        if (local.X < FlowGraphConstants.SidebarWidth || local.Y < 0 ||
            local.X > Bounds.Width || local.Y > Bounds.Height)
            return false;

        var node = HitTestNode(ToWorld(local));
        return node?.CardType == LocalImageCardType &&
            node.Ports.Any(port => port.Widget == FlowGraphWidgetKind.ImagePreview &&
                                   port.DataType == FlowGraphDataType.Image);
    }

    internal bool TryDropImageFile(string path, Point windowPosition)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        var local = new Point(windowPosition.X - Bounds.X, windowPosition.Y - Bounds.Y);
        var node = HitTestNode(ToWorld(local));
        if (node?.CardType != LocalImageCardType)
            return false;

        int previewIndex = node.Ports.FindIndex(port =>
            port.Widget == FlowGraphWidgetKind.ImagePreview &&
            port.DataType == FlowGraphDataType.Image);
        if (previewIndex < 0)
            return false;

        var fullPath = Path.GetFullPath(path);
        var imageData = FlowGraphImageData.FromFile(fullPath, 512, 512);
        return SetPortImage(node.Id, previewIndex, imageData);
    }

    internal Control CreateTooltipBackgroundOverlay()
    {
        _hasExternalFloatingOverlay = true;
        _tooltipBackgroundOverlayControl = new FloatingOverlayControl(this, FloatingOverlayLayer.TooltipBackground);
        return _tooltipBackgroundOverlayControl;
    }

    internal Control CreateFloatingOverlay()
    {
        _hasExternalFloatingOverlay = true;
        _floatingOverlayControl = new FloatingOverlayControl(this, FloatingOverlayLayer.Content);
        return _floatingOverlayControl;
    }

    internal Control CreateTopmostOverlay()
    {
        _hasExternalFloatingOverlay = true;
        _topmostOverlayControl = new FloatingOverlayControl(this, FloatingOverlayLayer.Topmost);
        return _topmostOverlayControl;
    }

    internal void InvalidateFloatingOverlay()
    {
        _tooltipBackgroundOverlayControl?.RequestInvalidate();
        _floatingOverlayControl?.RequestInvalidate();
        _topmostOverlayControl?.RequestInvalidate();
    }

    private void RenderTooltipBackgroundOverlay(IGraphicsContext context)
    {
        context.Save();
        context.SetClip(LayoutRounding.MakeClipRect(Bounds, context.DpiScale));
        DrawTooltipBackground(context);
        context.Restore();
    }

    private void RenderFloatingOverlays(IGraphicsContext context)
    {
        RenderFloatingContentOverlay(context);
        RenderTopmostOverlay(context);
    }

    private void RenderFloatingContentOverlay(IGraphicsContext context)
    {
        context.Save();
        context.SetClip(LayoutRounding.MakeClipRect(Bounds, context.DpiScale));
        DrawNodeTooltip(context);
        DrawSidebar(context);
        DrawComboPopup(context);
        DrawSelectionRect(context);
        DrawWidgetResizeHandles(context);
        DrawEditPanel(context);
        context.Restore();
    }

    private void RenderTopmostOverlay(IGraphicsContext context)
    {
        context.Save();
        context.SetClip(LayoutRounding.MakeClipRect(Bounds, context.DpiScale));
        DrawContextMenu(context);
        DrawProjectName(context);
        context.Restore();
    }

    private enum FloatingOverlayLayer
    {
        TooltipBackground,
        Content,
        Topmost
    }

    private sealed class FloatingOverlayControl : Control
    {
        private readonly FlowGraphControl _owner;
        private readonly FloatingOverlayLayer _layer;

        public FloatingOverlayControl(FlowGraphControl owner, FloatingOverlayLayer layer)
        {
            _owner = owner;
            _layer = layer;
            IsHitTestVisible = false;
            IsTabStop = false;
        }

        protected override Size MeasureContent(Size availableSize)
        {
            double width = double.IsFinite(availableSize.Width) ? availableSize.Width : 800;
            double height = double.IsFinite(availableSize.Height) ? availableSize.Height : 600;
            return new Size(Math.Max(1, width), Math.Max(1, height)).Inflate(GetBorderVisualInset());
        }

        public void RequestInvalidate() => InvalidateVisual();

        protected override void OnRender(IGraphicsContext context)
        {
            switch (_layer)
            {
                case FloatingOverlayLayer.TooltipBackground:
                    _owner.RenderTooltipBackgroundOverlay(context);
                    break;
                case FloatingOverlayLayer.Content:
                    _owner.RenderFloatingContentOverlay(context);
                    break;
                case FloatingOverlayLayer.Topmost:
                    _owner.RenderTopmostOverlay(context);
                    break;
            }
        }
    }

    // ==================== 滚动条计算与绘制 ====================

    /// <summary>
    /// 根据所有节点位置计算内容边界，决定是否显示滚动条。
    /// 在节点增删、移动、缩放后调用。
    /// </summary>
    internal void UpdateScrollBounds()
    {
        double maxRight = 0;
        double maxBottom = 0;
        foreach (var node in _nodes)
        {
            double right = node.X + node.Width + 60; // 右侧端口外扩
            double bottom = node.Y + node.Height + 40;
            if (double.IsFinite(right)) maxRight = Math.Max(maxRight, right);
            if (double.IsFinite(bottom)) maxBottom = Math.Max(maxBottom, bottom);
        }

        double zoom = double.IsFinite(_zoom) ? Math.Max(0, _zoom) : 1;
        _scrollContentWidth = Math.Max(0, maxRight * zoom);
        _scrollContentHeight = Math.Max(0, maxBottom * zoom);

        double viewW = Math.Max(0, Bounds.Width - FlowGraphConstants.SidebarWidth);
        double viewH = Math.Max(0, Bounds.Height);

        _showHScrollBar = viewW > 0 && _scrollContentWidth > viewW;
        _showVScrollBar = viewH > 0 && _scrollContentHeight > viewH;

        double maxPanX = _showHScrollBar ? Math.Max(0, _scrollContentWidth - viewW) : 0;
        double maxPanY = _showVScrollBar ? Math.Max(0, _scrollContentHeight - viewH) : 0;
        _panX = double.IsFinite(_panX) ? Math.Clamp(_panX, 0, maxPanX) : 0;
        _panY = double.IsFinite(_panY) ? Math.Clamp(_panY, 0, maxPanY) : 0;
    }

    private bool GetHorizontalScrollMetrics(
        out double trackStart, out double trackLength,
        out double thumbStart, out double thumbLength, out double maxScroll)
    {
        trackStart = FlowGraphConstants.SidebarWidth;
        trackLength = thumbStart = thumbLength = maxScroll = 0;
        if (!_showHScrollBar) return false;

        double viewLength = Math.Max(0, Bounds.Width - FlowGraphConstants.SidebarWidth);
        trackLength = Math.Max(0, viewLength - (_showVScrollBar ? ScrollBarThickness : 0));
        double contentLength = Math.Max(0, _scrollContentWidth);
        if (trackLength <= 0 || viewLength <= 0 || contentLength <= viewLength)
            return false;

        maxScroll = contentLength - viewLength;
        double ratio = Math.Clamp(viewLength / contentLength, 0, 1);
        // 最小滑块长度不能超过轨道长度，否则 Math.Clamp 的上下界会反转。
        thumbLength = Math.Clamp(trackLength * ratio,
            Math.Min(ScrollBarMinThumbSize, trackLength), trackLength);
        double travel = Math.Max(0, trackLength - thumbLength);
        double offset = Math.Clamp(double.IsFinite(_panX) ? _panX : 0, 0, maxScroll);
        thumbStart = trackStart + (maxScroll > 0 ? offset / maxScroll * travel : 0);
        return double.IsFinite(thumbStart) && double.IsFinite(thumbLength);
    }

    private bool GetVerticalScrollMetrics(
        out double trackStart, out double trackLength,
        out double thumbStart, out double thumbLength, out double maxScroll)
    {
        trackStart = 0;
        trackLength = thumbStart = thumbLength = maxScroll = 0;
        if (!_showVScrollBar) return false;

        double viewLength = Math.Max(0, Bounds.Height);
        trackLength = Math.Max(0, viewLength - (_showHScrollBar ? ScrollBarThickness : 0));
        double contentLength = Math.Max(0, _scrollContentHeight);
        if (trackLength <= 0 || viewLength <= 0 || contentLength <= viewLength)
            return false;

        maxScroll = contentLength - viewLength;
        double ratio = Math.Clamp(viewLength / contentLength, 0, 1);
        thumbLength = Math.Clamp(trackLength * ratio,
            Math.Min(ScrollBarMinThumbSize, trackLength), trackLength);
        double travel = Math.Max(0, trackLength - thumbLength);
        double offset = Math.Clamp(double.IsFinite(_panY) ? _panY : 0, 0, maxScroll);
        thumbStart = trackStart + (maxScroll > 0 ? offset / maxScroll * travel : 0);
        return double.IsFinite(thumbStart) && double.IsFinite(thumbLength);
    }

    private void DrawScrollBars(IGraphicsContext ctx)
    {
        UpdateScrollBounds();

        if (GetHorizontalScrollMetrics(out double hStart, out double hTrack,
                out double hThumb, out double hThumbLength, out _))
        {
            double barY = Bounds.Bottom - ScrollBarThickness;
            FillRect(ctx, new Rect(Bounds.X + hStart, barY, hTrack, ScrollBarThickness),
                Color.FromRgb(40, 40, 48));
            double thumbHeight = Math.Max(0, ScrollBarThickness - 4);
            if (hThumbLength > 0 && thumbHeight > 0)
                FillRect(ctx, new Rect(Bounds.X + hThumb, barY + 2, hThumbLength, thumbHeight),
                    Color.FromRgb(100, 100, 115));
        }

        if (GetVerticalScrollMetrics(out double vStart, out double vTrack,
                out double vThumb, out double vThumbLength, out _))
        {
            double barX = Bounds.Right - ScrollBarThickness;
            FillRect(ctx, new Rect(barX, Bounds.Y + vStart, ScrollBarThickness, vTrack),
                Color.FromRgb(40, 40, 48));
            double thumbWidth = Math.Max(0, ScrollBarThickness - 4);
            if (vThumbLength > 0 && thumbWidth > 0)
                FillRect(ctx, new Rect(barX + 2, Bounds.Y + vThumb, thumbWidth, vThumbLength),
                    Color.FromRgb(100, 100, 115));
        }
    }

    private bool TryHitHScrollBarThumb(Point local, out double thumbX, out double thumbW, out double barStartX, out double barW)
    {
        thumbX = thumbW = barStartX = barW = 0;
        if (!GetHorizontalScrollMetrics(out barStartX, out barW,
                out thumbX, out thumbW, out _))
            return false;

        double barY = Bounds.Height - ScrollBarThickness;
        return local.Y >= barY && local.Y <= Bounds.Height &&
               local.X >= thumbX && local.X <= thumbX + thumbW;
    }

    private bool TryHitVScrollBarThumb(Point local, out double thumbY, out double thumbH, out double barStartY, out double barH)
    {
        thumbY = thumbH = barStartY = barH = 0;
        if (!GetVerticalScrollMetrics(out barStartY, out barH,
                out thumbY, out thumbH, out _))
            return false;

        double barX = Bounds.Width - ScrollBarThickness;
        return local.X >= barX && local.X <= Bounds.Width &&
               local.Y >= thumbY && local.Y <= thumbY + thumbH;
    }

    private bool IsPointInScrollBarArea(Point local)
    {
        if (_showHScrollBar && local.Y >= Bounds.Height - ScrollBarThickness &&
            local.Y <= Bounds.Height && local.X >= FlowGraphConstants.SidebarWidth &&
            local.X <= Bounds.Width - (_showVScrollBar ? ScrollBarThickness : 0))
            return true;
        if (_showVScrollBar && local.X >= Bounds.Width - ScrollBarThickness &&
            local.X <= Bounds.Width && local.Y >= 0 &&
            local.Y <= Bounds.Height - (_showHScrollBar ? ScrollBarThickness : 0))
            return true;
        return false;
    }

    // ==================== 绘制网格 ====================
    private void DrawGrid(IGraphicsContext ctx)
    {
        double spacing = Math.Max(5, FlowGraphConstants.GridSpacing * _zoom);
        double startX = -PositiveModulo(_panX, spacing);
        double startY = -PositiveModulo(_panY, spacing);

        for (double x = startX; x < Bounds.Width; x += spacing)
            DrawLine(ctx, new Point(Bounds.X + x, Bounds.Y), new Point(Bounds.X + x, Bounds.Bottom), _gridColor, 1);

        for (double y = startY; y < Bounds.Height; y += spacing)
            DrawLine(ctx, new Point(Bounds.X, Bounds.Y + y), new Point(Bounds.Right, Bounds.Y + y), _gridColor, 1);
    }

    // ==================== 绘制连线 ====================
    private void DrawConnections(IGraphicsContext ctx)
    {
        foreach (var conn in _connections)
        {
            var fromNode = FindNode(conn.FromNodeId);
            var toNode = FindNode(conn.ToNodeId);
            if (fromNode is null || toNode is null) continue;
            if (!IsValidPortIndex(fromNode, conn.FromPortIndex) || !IsValidPortIndex(toNode, conn.ToPortIndex)) continue;

            var start = WorldToScreen(GetPortAnchor(fromNode, conn.FromPortIndex));
            var end = WorldToScreen(GetPortAnchor(toNode, conn.ToPortIndex));
            var cp1 = WorldToScreen(conn.ControlPoint1);
            var cp2 = WorldToScreen(conn.ControlPoint2);

            bool selected = conn.Id == _selectedConnectionId;
            var color = selected ? Color.FromRgb(255, 165, 0) : GetPortColor(fromNode.Ports[conn.FromPortIndex].DataType);
            double thickness = selected ? 3 : 2;

            DrawBezier(ctx, start, cp1, cp2, end, color, thickness);

            if (selected)
            {
                DrawHandle(ctx, CubicBezier(start, cp1, cp2, end, 1.0 / 3.0));
                DrawHandle(ctx, CubicBezier(start, cp1, cp2, end, 2.0 / 3.0));
            }
        }
    }

    // ==================== 绘制节点 ====================
    private void DrawNodes(IGraphicsContext ctx)
    {
        foreach (var node in _nodes)
            if (!_selectedNodeIds.Contains(node.Id))
                DrawNode(ctx, node, false);

        foreach (var node in _nodes)
            if (_selectedNodeIds.Contains(node.Id))
                DrawNode(ctx, node, true);
    }

    private void DrawNode(IGraphicsContext ctx, FlowGraphNode node, bool selected)
    {
        var rect = NodeScreenRect(node);
        double zoom = _zoom;

        FillRect(ctx, rect, _nodeBackground);
        var border = selected ? _selectedColor : _nodeBorder;
        DrawRectOutline(ctx, rect, border, selected ? 2 : 1);

        if (node.ExecutionStatus == FlowGraphExecutionStatus.Running)
        {
            var glowRect = new Rect(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
            DrawRectOutline(ctx, glowRect, Color.FromArgb(120, 0, 120, 255), 2);
        }

        double titleH = FlowGraphConstants.TitleHeight * zoom;
        var titleRect = new Rect(rect.X + 2 * zoom, rect.Y + 2 * zoom,
            rect.Width - 4 * zoom, titleH - 2 * zoom);

        FillRect(ctx, new Rect(titleRect.X, titleRect.Y, 4 * zoom, titleRect.Height), node.HeaderColor);

        var statusColor = GetStatusColor(node.ExecutionStatus);
        double dotR = 4 * zoom;
        double dotX = titleRect.Right - 10 * zoom;
        double dotY = titleRect.Y + titleRect.Height / 2;
        FillEllipse(ctx, dotX, dotY, dotR, statusColor);

        string titleText = $"{node.Title}(Id:{node.Id})";
        var textRect = new Rect(titleRect.X + 6 * zoom, titleRect.Y,
            titleRect.Width - 24 * zoom, titleRect.Height);
        DrawText(ctx, titleText, textRect, _nodeTitleForeground,
            TextAlignment.Center, TextAlignment.Center, TextWrapping.NoWrap);

        DrawWidgets(ctx, node, rect, zoom);
        DrawPorts(ctx, node, rect, zoom);
    }

    // ==================== 绘制端口圆圈 ====================
    private void DrawPorts(IGraphicsContext ctx, FlowGraphNode node, Rect nodeRect, double zoom)
    {
        for (int i = 0; i < node.Ports.Count; i++)
        {
            var port = node.Ports[i];
            if (port.Direction == FlowGraphPortDirection.Inline) continue;

            var anchor = WorldToScreen(GetPortAnchor(node, i));
            var screenPt = ToWindow(anchor);

            bool isHover = _hoverPortNodeId == node.Id && _hoverPortIndex == i;
            double radius = (isHover ? FlowGraphConstants.PortHoverRadius : FlowGraphConstants.PortRadius) * zoom;
            var portColor = GetPortColor(port.DataType);

            FillEllipse(ctx, screenPt.X, screenPt.Y, radius,
                isHover ? Color.FromRgb(255, 255, 255) : portColor);

            var portBorder = isHover ? _selectedColor : Color.FromRgb(12, 12, 15);
            DrawEllipse(ctx, screenPt.X, screenPt.Y, radius, portBorder, Math.Max(1.0, 1.25 * zoom));

            if (isHover)
            {
                var nameColor = Color.FromRgb(240, 240, 240);
                if (port.Direction == FlowGraphPortDirection.Input)
                {
                    var labelRect = new Rect(screenPt.X - 160 * zoom, screenPt.Y - 10 * zoom,
                        148 * zoom, 20 * zoom);
                    DrawText(ctx, port.Name, labelRect, nameColor,
                        TextAlignment.Right, TextAlignment.Center, TextWrapping.NoWrap);
                }
                else
                {
                    var labelRect = new Rect(screenPt.X + 12 * zoom, screenPt.Y - 10 * zoom,
                        160 * zoom, 20 * zoom);
                    DrawText(ctx, port.Name, labelRect, nameColor,
                        TextAlignment.Left, TextAlignment.Center, TextWrapping.NoWrap);
                }
            }
        }
    }

    // ==================== 绘制子组件 ====================
    private void DrawWidgets(IGraphicsContext ctx, FlowGraphNode node, Rect nodeRect, double zoom)
    {
        for (int i = 0; i < node.Ports.Count; i++)
        {
            var port = node.Ports[i];
            if (!HasVisibleWidget(port)) continue;

            var widgetRect = ToWindow(WorldToScreen(GetWidgetRect(node, i)));
            if (widgetRect.Width <= 0 || widgetRect.Height <= 0) continue;

            switch (port.Widget)
            {
                case FlowGraphWidgetKind.Edit:
                case FlowGraphWidgetKind.Text:
                case FlowGraphWidgetKind.MultilineText:
                case FlowGraphWidgetKind.Display:
                    DrawTextWidget(ctx, port, widgetRect, zoom);
                    break;
                case FlowGraphWidgetKind.Combo:
                    DrawComboWidget(ctx, node, port, i, widgetRect, zoom);
                    break;
                case FlowGraphWidgetKind.Button:
                    DrawButtonWidget(ctx, port, widgetRect, zoom);
                    break;
                case FlowGraphWidgetKind.DualButton:
                    DrawDualButtonWidget(ctx, port, widgetRect, zoom);
                    break;
                case FlowGraphWidgetKind.ImagePreview:
                    DrawImagePreviewWidget(ctx, port, widgetRect, zoom);
                    break;
            }
        }
    }

    private void DrawTextWidget(IGraphicsContext ctx, FlowGraphPort port, Rect rect, double zoom)
    {
        FillRect(ctx, rect, Color.FromRgb(30, 30, 35));
        DrawRectOutline(ctx, rect, _nodeBorder, 1 * zoom);

        string text = ToDisplayText(port.Data ?? port.Value);
        if (!string.IsNullOrEmpty(text))
        {
            var textRect = new Rect(rect.X + 5 * zoom, rect.Y + 5 * zoom,
                rect.Width - 10 * zoom, rect.Height - 10 * zoom);
            DrawText(ctx, text, textRect, _nodeTitleForeground,
                TextAlignment.Left, TextAlignment.Top, TextWrapping.Wrap);
        }
    }

    private void DrawComboWidget(IGraphicsContext ctx, FlowGraphNode node, FlowGraphPort port,
        int portIndex, Rect rect, double zoom)
    {
        FillRect(ctx, rect, Color.FromRgb(34, 34, 34));

        bool isExpanded = _expandedComboNodeId == node.Id && _expandedComboPortIndex == portIndex;
        var border = isExpanded ? Color.FromRgb(100, 150, 255) : _nodeBorder;
        DrawRectOutline(ctx, rect, border, 1 * zoom);

        double btnW = 24 * zoom;
        var btnRect = new Rect(rect.Right - btnW, rect.Y, btnW, rect.Height);
        FillRect(ctx, btnRect, Color.FromRgb(45, 45, 50));
        DrawLine(ctx, new Point(btnRect.X, rect.Y), new Point(btnRect.X, rect.Bottom), _nodeBorder, 1 * zoom);

        double cx = btnRect.X + btnW / 2;
        double cy = rect.Y + rect.Height / 2;
        double triSize = 5 * zoom;
        DrawLine(ctx, new Point(cx - triSize, cy - triSize / 2), new Point(cx, cy + triSize / 2), _nodeTitleForeground, 1.5);
        DrawLine(ctx, new Point(cx, cy + triSize / 2), new Point(cx + triSize, cy - triSize / 2), _nodeTitleForeground, 1.5);

        if (port.ComboData != null && port.ComboData.Options.Count > 0)
        {
            int idx = Math.Clamp(port.ComboData.Current, 0, port.ComboData.Options.Count - 1);
            var textRect = new Rect(rect.X + 8 * zoom, rect.Y, rect.Width - btnW - 13 * zoom, rect.Height);
            DrawText(ctx, port.ComboData.Options[idx], textRect, _nodeTitleForeground,
                TextAlignment.Left, TextAlignment.Center, TextWrapping.NoWrap);
        }
    }

    private void DrawButtonWidget(IGraphicsContext ctx, FlowGraphPort port, Rect rect, double zoom)
    {
        FillRect(ctx, rect, Color.FromRgb(50, 50, 60));
        DrawRectOutline(ctx, rect, _nodeBorder, 1 * zoom);
        string caption = port.ButtonData?.Caption ?? port.Value ?? "按钮";
        DrawText(ctx, caption, rect, _nodeTitleForeground,
            TextAlignment.Center, TextAlignment.Center, TextWrapping.NoWrap);
    }

    private void DrawDualButtonWidget(IGraphicsContext ctx, FlowGraphPort port, Rect rect, double zoom)
    {
        double gap = 6 * zoom;
        double halfW = (rect.Width - gap) / 2;
        var leftRect = new Rect(rect.X, rect.Y, halfW, rect.Height);
        var rightRect = new Rect(rect.X + halfW + gap, rect.Y, halfW, rect.Height);

        FillRect(ctx, leftRect, Color.FromRgb(50, 50, 60));
        DrawRectOutline(ctx, leftRect, _nodeBorder, 1 * zoom);
        FillRect(ctx, rightRect, Color.FromRgb(50, 50, 60));
        DrawRectOutline(ctx, rightRect, _nodeBorder, 1 * zoom);

        string cap1 = port.DualButtonData?.Caption1 ?? "+";
        string cap2 = port.DualButtonData?.Caption2 ?? "-";
        DrawText(ctx, cap1, leftRect, _nodeTitleForeground, TextAlignment.Center, TextAlignment.Center);
        DrawText(ctx, cap2, rightRect, _nodeTitleForeground, TextAlignment.Center, TextAlignment.Center);
    }

    private void DrawImagePreviewWidget(IGraphicsContext ctx, FlowGraphPort port, Rect rect, double zoom)
    {
        FillRect(ctx, rect, Color.FromRgb(28, 28, 31));

        if (port.Data is not FlowGraphImageData imageData)
        {
            DrawText(ctx, "暂无图片", rect, Color.FromRgb(115, 115, 125),
                TextAlignment.Center, TextAlignment.Center, TextWrapping.NoWrap);
            DrawRectOutline(ctx, rect, _nodeBorder, 1 * zoom);
            return;
        }

        try
        {
            if (!_renderImages.TryGetValue(imageData, out var image))
            {
                image = imageData.Source.CreateImage(GetGraphicsFactory());
                _renderImages.Add(imageData, image);
            }

            int rawWidth = image.PixelWidth;
            int rawHeight = image.PixelHeight;
            if (rawWidth <= 0 || rawHeight <= 0)
                throw new InvalidOperationException("图片解码后尺寸无效。");

            bool swapsAxes = imageData.Orientation is ImageOrientation.Transpose
                or ImageOrientation.Rotate90
                or ImageOrientation.Transverse
                or ImageOrientation.Rotate270;
            double orientedWidth = swapsAxes ? rawHeight : rawWidth;
            double orientedHeight = swapsAxes ? rawWidth : rawHeight;

            // 等比缩放并居中，避免把原图拉伸到预览框的宽高比。
            var inner = InsetRect(rect, Math.Max(3, 3 * zoom));
            double scale = Math.Min(inner.Width / orientedWidth, inner.Height / orientedHeight);
            double drawWidth = orientedWidth * scale;
            double drawHeight = orientedHeight * scale;
            var destination = new Rect(
                inner.X + (inner.Width - drawWidth) / 2,
                inner.Y + (inner.Height - drawHeight) / 2,
                drawWidth,
                drawHeight);

            ctx.DrawImageOriented(
                image,
                imageData.Orientation,
                rawWidth,
                rawHeight,
                new Rect(0, 0, orientedWidth, orientedHeight),
                destination);
        }
        catch (Exception ex)
        {
            // 单张图片解码失败不能中断整张节点图的绘制。
            DrawText(ctx, "图片加载失败", rect, Color.FromRgb(220, 110, 110),
                TextAlignment.Center, TextAlignment.Center, TextWrapping.NoWrap);
            Trace.TraceError($"FlowGraph 图片预览失败：{imageData.SourcePath}{Environment.NewLine}{ex}");
        }

        DrawRectOutline(ctx, rect, _nodeBorder, 1 * zoom);
    }

    private void PruneRenderImageCache()
    {
        var referenced = new HashSet<FlowGraphImageData>(ReferenceEqualityComparer.Instance);
        foreach (var port in _nodes.SelectMany(node => node.Ports))
        {
            if (port.Data is FlowGraphImageData imageData)
                referenced.Add(imageData);
        }

        foreach (var stale in _renderImages.Keys.Where(key => !referenced.Contains(key)).ToArray())
        {
            if (_renderImages.Remove(stale, out var image))
                image.Dispose();
        }
    }

    private static Rect InsetRect(Rect rect, double inset)
    {
        inset = Math.Max(0, inset);
        return new Rect(rect.X + inset, rect.Y + inset,
            Math.Max(0, rect.Width - inset * 2),
            Math.Max(0, rect.Height - inset * 2));
    }

    internal IReadOnlyList<FlowGraphImagePlacement> GetImagePlacements()
    {
        var result = new List<FlowGraphImagePlacement>();

        foreach (var node in _nodes)
        {
            for (int i = 0; i < node.Ports.Count; i++)
            {
                var port = node.Ports[i];
                if (port.Widget != FlowGraphWidgetKind.ImagePreview ||
                    !HasVisibleWidget(port) ||
                    port.Data is not FlowGraphImageData imageData)
                    continue;

                var widgetRect = WorldToScreen(GetWidgetRect(node, i));
                var imageRect = InsetRect(widgetRect, 3 * _zoom);
                result.Add(new FlowGraphImagePlacement(
                    node.Id, port.Id, false, imageData.Source, imageRect));
            }
        }

        var hoverNode = _hoverTitleNodeId >= 0 ? FindNode(_hoverTitleNodeId) : null;
        if (hoverNode is not null)
        {
            var layout = CreateNodeTooltipLayout(hoverNode);
            if (layout is not null)
            {
                foreach (var item in layout.Images)
                {
                    var localBounds = new Rect(
                        item.Bounds.X - Bounds.X,
                        item.Bounds.Y - Bounds.Y,
                        item.Bounds.Width,
                        item.Bounds.Height);
                    result.Add(new FlowGraphImagePlacement(
                        hoverNode.Id, item.Port.Id, true,
                        item.ImageData.Source, InsetRect(localBounds, 2)));
                }
            }
        }

        return result;
    }

    private sealed class TooltipImageInfo
    {
        public FlowGraphPort Port { get; set; } = null!;
        public FlowGraphImageData ImageData { get; set; } = null!;
        public Rect Bounds { get; set; }
    }

    private sealed class NodeTooltipLayout
    {
        public Rect PanelBounds { get; set; }
        public Rect TextBounds { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<TooltipImageInfo> Images { get; } = new();
    }

    private NodeTooltipLayout? CreateNodeTooltipLayout(FlowGraphNode node)
    {
        const double outerPadding = 8;
        const double imageGap = 10;
        const double textImageGap = 15;
        const double requestedImageWidth = 200;
        const double requestedImageHeight = 200;
        const double lineHeight = 16;

        var layout = new NodeTooltipLayout
        {
            Text = BuildNodeTooltipText(node)
        };

        for (int i = 0; i < node.Ports.Count && layout.Images.Count < 20; i++)
        {
            var port = node.Ports[i];
            if (port.Direction != FlowGraphPortDirection.Input || port.DataType != FlowGraphDataType.Image)
                continue;

            var imageData = ResolveInputImagePreview(node, i);
            if (imageData is null) continue;

            layout.Images.Add(new TooltipImageInfo
            {
                Port = port,
                ImageData = imageData
            });
        }

        int imageCount = layout.Images.Count;
        int imageColumns = Math.Min(3, imageCount);
        int imageRows = (imageCount + 2) / 3;

        double requestedImageAreaWidth = imageCount == 0
            ? 0
            : imageColumns * requestedImageWidth + Math.Max(0, imageColumns - 1) * imageGap;

        double maxContentWidth = Math.Max(180,
            Bounds.Width - FlowGraphConstants.SidebarWidth - 36);
        double contentWidth = Math.Min(Math.Max(410, requestedImageAreaWidth), maxContentWidth);

        double imageWidth = requestedImageWidth;
        double imageHeight = requestedImageHeight;
        if (imageCount > 0 && requestedImageAreaWidth > contentWidth)
        {
            imageWidth = Math.Max(72, (contentWidth - (imageColumns - 1) * imageGap) / imageColumns);
            imageHeight = imageWidth * requestedImageHeight / requestedImageWidth;
        }

        double imageAreaWidth = imageCount == 0
            ? 0
            : imageColumns * imageWidth + Math.Max(0, imageColumns - 1) * imageGap;

        double panelContentWidth = Math.Max(contentWidth, imageAreaWidth);

        // ★★★ 增加最后一行的额外高度，防止文字被截断 ★★★
        double textHeight = EstimateTooltipTextHeight(layout.Text, contentWidth, lineHeight);

        double imageAreaHeight = imageCount == 0
            ? 0
            : imageRows * imageHeight + Math.Max(0, imageRows - 1) * imageGap;

        double gap = imageCount > 0 ? textImageGap : 0;
        double panelWidth = panelContentWidth + outerPadding * 2;
        double panelHeight = textHeight + imageAreaHeight + gap + outerPadding * 2;

        double mouseX = Bounds.X + _lastPointerLocal.X;
        double mouseY = Bounds.Y + _lastPointerLocal.Y;
        double panelX = mouseX + 15;
        double panelY = mouseY - panelHeight / 2;

        if (panelX + panelWidth + 6 > Bounds.Right)
            panelX = mouseX - panelWidth - 20;

        double minX = Bounds.X + FlowGraphConstants.SidebarWidth + 6;
        double maxX = Math.Max(minX, Bounds.Right - panelWidth - 6);
        panelX = Math.Clamp(panelX, minX, maxX);

        double minY = Bounds.Y + 6;
        double maxY = Math.Max(minY, Bounds.Bottom - panelHeight - 6);
        panelY = Math.Clamp(panelY, minY, maxY);

        layout.PanelBounds = new Rect(panelX, panelY, panelWidth, panelHeight);
        layout.TextBounds = new Rect(
            panelX + outerPadding,
            panelY + outerPadding,
            contentWidth,
            textHeight);

        double imageStartX = panelX + outerPadding;
        double imageStartY = panelY + outerPadding + textHeight + gap;

        for (int i = 0; i < layout.Images.Count; i++)
        {
            int row = i / 3;
            int col = i % 3;
            layout.Images[i].Bounds = new Rect(
                imageStartX + col * (imageWidth + imageGap),
                imageStartY + row * (imageHeight + imageGap),
                imageWidth,
                imageHeight);
        }

        return layout;
    }

    private FlowGraphImageData? ResolveInputImagePreview(FlowGraphNode node, int inputPortIndex)
    {
        if (!IsValidPortIndex(node, inputPortIndex)) return null;
        var inputPort = node.Ports[inputPortIndex];
        if (inputPort.Direction != FlowGraphPortDirection.Input ||
            inputPort.DataType != FlowGraphDataType.Image)
            return null;

        if (inputPort.Data is FlowGraphImageData directImage)
            return directImage;

        var connection = _connections.FirstOrDefault(c =>
            c.ToNodeId == node.Id && c.ToPortIndex == inputPortIndex);
        if (connection is null) return null;

        var sourceNode = FindNode(connection.FromNodeId);
        const int localImageCardType = 4;
        if (sourceNode is null || sourceNode.CardType != localImageCardType)
            return null;

        foreach (var sourcePort in sourceNode.Ports)
        {
            if (sourcePort.DataType == FlowGraphDataType.Image &&
                sourcePort.Data is FlowGraphImageData imageData)
                return imageData;
        }

        return null;
    }

    private string BuildNodeTooltipText(FlowGraphNode node)
    {
        var builder = new StringBuilder(1024);

        string typeName = _cardRegistry.TryGetValue(node.CardType, out var descriptor)
            ? descriptor.Name
            : "自定义";

        string statusText = node.ExecutionStatus switch
        {
            FlowGraphExecutionStatus.Idle => "○ 未执行",
            FlowGraphExecutionStatus.Pending => "● 等待执行",
            FlowGraphExecutionStatus.Running => "● 执行中...",
            FlowGraphExecutionStatus.Completed => "● 已完成",
            FlowGraphExecutionStatus.Failed => "● 执行失败",
            _ => "○ 未知"
        };

        builder.Append("节点: ").Append(node.Title).Append("(nodeId:").Append(node.Id).AppendLine(")");
        builder.Append("类型: ").AppendLine(typeName);
        builder.Append("状态: ").AppendLine(statusText);

        if (!string.IsNullOrWhiteSpace(node.LastError))
            builder.Append("错误: ").AppendLine(node.LastError);

        builder.AppendLine();
        builder.Append("── 输入来源 ──");

        bool hasInputPort = false;
        for (int i = 0; i < node.Ports.Count; i++)
        {
            var inputPort = node.Ports[i];
            if (inputPort.Direction != FlowGraphPortDirection.Input) continue;
            hasInputPort = true;

            var connection = _connections.FirstOrDefault(c =>
                c.ToNodeId == node.Id && c.ToPortIndex == i);

            if (connection is null)
            {
                builder.AppendLine().Append(inputPort.Name)
                    .Append("(portId:").Append(inputPort.Id).Append("): 未连接");
                continue;
            }

            var fromNode = FindNode(connection.FromNodeId);
            if (fromNode is not null && IsValidPortIndex(fromNode, connection.FromPortIndex))
            {
                var fromPort = fromNode.Ports[connection.FromPortIndex];
                builder.AppendLine().Append(inputPort.Name)
                    .Append("(portId:").Append(inputPort.Id).Append(") ← ")
                    .Append(fromNode.Title).Append("(nodeId:").Append(fromNode.Id).Append("):")
                    .Append(fromPort.Name).Append("(portId:").Append(fromPort.Id).Append(')');
            }
            else
            {
                builder.AppendLine().Append(inputPort.Name)
                    .Append("(portId:").Append(inputPort.Id).Append("): 连接目标无效");
            }
        }

        if (!hasInputPort)
            builder.AppendLine().Append("(无输入端口)");

        builder.AppendLine().AppendLine("── 输出目标 ──");

        bool hasOutputPort = false;
        for (int i = 0; i < node.Ports.Count; i++)
        {
            var outputPort = node.Ports[i];
            if (outputPort.Direction != FlowGraphPortDirection.Output) continue;
            hasOutputPort = true;

            var outgoing = _connections.Where(c =>
                c.FromNodeId == node.Id && c.FromPortIndex == i).ToList();

            if (outgoing.Count == 0)
            {
                builder.Append(outputPort.Name)
                    .Append("(portId:").Append(outputPort.Id).Append("): 未连接").AppendLine();
                continue;
            }

            foreach (var connection in outgoing)
            {
                var toNode = FindNode(connection.ToNodeId);
                if (toNode is not null && IsValidPortIndex(toNode, connection.ToPortIndex))
                {
                    var toPort = toNode.Ports[connection.ToPortIndex];
                    builder.Append(outputPort.Name)
                        .Append("(portId:").Append(outputPort.Id).Append(") → ")
                        .Append(toNode.Title).Append("(nodeId:").Append(toNode.Id).Append("):")
                        .Append(toPort.Name).Append("(portId:").Append(toPort.Id).Append(')')
                        .AppendLine();
                }
            }
        }

        if (!hasOutputPort)
            builder.Append("(无输出端口)");

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// ★★★ 增加最后一行额外高度，防止文字被截断 ★★★
    /// </summary>
    private static double EstimateTooltipTextHeight(string text, double maxWidth, double lineHeight)
    {
        if (string.IsNullOrEmpty(text)) return lineHeight;

        int lineCount = 0;
        foreach (string line in text.Replace("\r", string.Empty).Split('\n'))
        {
            if (line.Length == 0)
            {
                lineCount++;
                continue;
            }

            double usedWidth = 0;
            int wrappedLines = 1;

            foreach (char ch in line)
            {
                double charWidth = ch <= 0x7F
                    ? (char.IsWhiteSpace(ch) ? 4.0 : 6.5)
                    : 12.0;

                if (usedWidth > 0 && usedWidth + charWidth > maxWidth)
                {
                    wrappedLines++;
                    usedWidth = 0;
                }

                usedWidth += charWidth;
            }

            lineCount += wrappedLines;
        }

        // ★ 增加一整行高度作为底部余量，确保最后一行不被截断
        return Math.Max(56, lineCount * lineHeight + lineHeight);
    }

    // ==================== 绘制侧边栏 ====================
    private static double[] GetSidebarButtonYs()
    {
        return new[]
        {
            FlowGraphConstants.SidebarBtnY0, FlowGraphConstants.SidebarBtnY1,
            FlowGraphConstants.SidebarBtnY2, FlowGraphConstants.SidebarBtnY3,
            FlowGraphConstants.SidebarBtnY4, FlowGraphConstants.SidebarBtnY5,
            FlowGraphConstants.SidebarBtnY6, FlowGraphConstants.SidebarBtnY7
        };
    }

    private void DrawSidebar(IGraphicsContext ctx)
    {
        double sw = FlowGraphConstants.SidebarWidth;
        double[] btnYs = GetSidebarButtonYs();
        string[] labels = { "添加", "链路", "链路", "取消", "导出", "导入", "自定义", "帮助" };
        Color[] activeColors = {
            Color.FromRgb(100, 150, 255), Color.FromRgb(0, 200, 200),
            Color.FromRgb(200, 150, 0), Color.FromRgb(255, 80, 80),
            Color.FromRgb(0, 180, 200), Color.FromRgb(160, 100, 220),
            Color.FromRgb(220, 160, 60), Color.FromRgb(100, 200, 255)
        };

        FillRect(ctx, new Rect(Bounds.X, Bounds.Y, sw, Bounds.Height), Color.FromRgb(18, 18, 22));
        DrawLine(ctx, new Point(Bounds.X + sw, Bounds.Y), new Point(Bounds.X + sw, Bounds.Bottom),
            Color.FromRgb(50, 50, 60), 1);

        for (int i = 0; i < FlowGraphConstants.SidebarBtnCount; i++)
        {
            double by = btnYs[i];
            bool isHover = _sidebarHoverBtn == i;
            bool isActive = (i == 0 && _showAddPanel) ||
                            (i == 1 && _showChainPanel && _chainPanelMode == 0) ||
                            (i == 2 && _showChainPanel && _chainPanelMode == 1) ||
                            (i == 3 && _showCancelPanel) ||
                            (i == 6 && _showCustomPanel);

            if (isHover || isActive)
            {
                FillRect(ctx, new Rect(Bounds.X + 1, Bounds.Y + by, sw - 1, FlowGraphConstants.SidebarBtnHeight),
                    isActive ? Color.FromRgb(40, 40, 55) : Color.FromRgb(45, 45, 58));
                FillRect(ctx, new Rect(Bounds.X, Bounds.Y + by + 6, 3, FlowGraphConstants.SidebarBtnHeight - 12),
                    activeColors[i]);
            }

            var iconColor = (isHover || isActive) ? Color.FromRgb(230, 230, 235) : Color.FromRgb(140, 140, 155);
            double cx = Bounds.X + sw / 2;
            double cy = Bounds.Y + by + 24;
            DrawSidebarIcon(ctx, i, cx, cy, iconColor);

            var textColor = (isHover || isActive) ? Color.FromRgb(220, 220, 225) : Color.FromRgb(120, 120, 135);
            if (i is 1 or 2 or 6)
            {
                var smallColor = (isHover || isActive)
                    ? Color.FromRgb(220, 220, 225)
                    : Color.FromRgb(110, 110, 125);
                var line1 = i == 6 ? "自定" : "链路";
                var line2 = i == 1 ? "从头" : i == 2 ? "继续" : "义";
                DrawText(ctx, line1,
                    new Rect(Bounds.X, Bounds.Y + by + 36, sw, 18),
                    smallColor, TextAlignment.Center, TextAlignment.Top, TextWrapping.NoWrap);
                DrawText(ctx, line2,
                    new Rect(Bounds.X, Bounds.Y + by + 54, sw, 16),
                    smallColor, TextAlignment.Center, TextAlignment.Top, TextWrapping.NoWrap);
            }
            else
            {
                var labelRect = new Rect(Bounds.X, Bounds.Y + by + 42, sw, FlowGraphConstants.SidebarBtnHeight - 42);
                DrawText(ctx, labels[i], labelRect, textColor,
                    TextAlignment.Center, TextAlignment.Top, TextWrapping.NoWrap);
            }
        }

        if (_showAddPanel) DrawAddPanel(ctx);
        if (_showChainPanel) DrawChainPanel(ctx);
        if (_showCancelPanel) DrawCancelPanel(ctx);
        if (_showCustomPanel) DrawCustomPanel(ctx);
        if (_sidebarHoverBtn == (int)FlowGraphSidebarButton.Help) DrawHelpTooltip(ctx);
    }

    private void DrawHelpTooltip(IGraphicsContext ctx)
    {
        const string helpText =
            """
            操作帮助:
            【快捷键操作】
            Ctrl+C        ：复制选中的节点(自动包含选区内连线)
            Ctrl+X        ：剪切选中的节点(自动包含选区内连线)
            Ctrl+V        ：粘贴节点到鼠标位置(智能防越界)
            Delete        ：删除选中的连接线
            Shift+Delete  ：删除选中的多个节点(忙碌受保护)
            【鼠标与画布控制】
            空白处左键拖拽：框选多个节点
            多选后左键拖拽：整体移动选中的节点组
            鼠标中键拖动  ：平移画布
            Ctrl+鼠标滚轮 ：缩放画布
            鼠标右键节点  ：打开上下文菜单(执行/取消/清空)
            拖拽图片到节点：快速加载本地图片(仅限本地图卡片)
            【节点编辑与交互】
            左键拖动端口  ：手动创建连接线
            双击编辑框    ：原位快速编辑文本内容
            选中特定节点  ：弹出右下侧浮动编辑面板
            左键拖动白点  ：调整连线贝塞尔曲率
            拖动组件右下角：调整编辑/图片/视频框大小
            点击组合框箭头：展开下拉选项
            点击视频控件  ：播放/暂停及拖拽进度条
            点击双按钮(+/-)：动态增减参考图/音频端口
            【节点状态与提示】
            鼠标悬停标题  ：查看详细执行信息、拓扑及输入图片预览
            (支持未执行时透传预览上游本地图，每行3张带端口标识)
            右上角圆点标识状态(执行中带蓝色外发光)：
            ⚪ 灰色 - 未执行  🔵 蓝色 - 执行中
            🟡 黄色 - 排队中  🟢 绿色 - 成功  🔴 红色 - 失败
            【侧边栏功能】
            [+] 添加      ：选择类型，新建节点
            [|▶]链路从头  ：从头执行选定的单链路
            [‖▶]链路继续  ：继续执行选定的单链路
            [■] 取消      ：强制取消执行/排队的节点
            [↑] 导出      ：保存YAML工程(自动打包素材到同名文件夹)
            [↓] 导入      ：从YAML工程恢复(自动从同名文件夹加载素材)
            [田] 自定义   ：显示自定义功能菜单
            [?] 帮助      ：显示本操作面板
            【核心机制与重要提示】
            1.数据缓存：断开连线时输入数据会保留不丢失
            2.级联失效：修改数据/删除连线会使下游重置为未执行
            3.忙碌保护：执行中的节点及链路无法被删除或剪切
            """;

        const double panelPadding = 12;
        const double textLineHeight = 18;

        double contentWidth = Math.Min(500,
            Math.Max(180, Bounds.Width - FlowGraphConstants.SidebarWidth - panelPadding * 2 - 12));
        double textHeight = EstimateTooltipTextHeight(helpText, contentWidth, textLineHeight);
        double panelWidth = contentWidth + panelPadding * 2;
        double panelHeight = textHeight + panelPadding * 2;

        double mouseX = Bounds.X + _lastPointerLocal.X;
        double mouseY = Bounds.Y + _lastPointerLocal.Y;
        double panelX = mouseX + 16;
        double panelY = mouseY - panelHeight / 2;

        if (panelX + panelWidth > Bounds.Right - 6)
            panelX = mouseX - panelWidth - 16;

        double minX = Bounds.X + FlowGraphConstants.SidebarWidth + 6;
        double maxX = Math.Max(minX, Bounds.Right - panelWidth - 6);
        panelX = Math.Clamp(panelX, minX, maxX);

        if (panelY + panelHeight > Bounds.Bottom - 6)
            panelY = Bounds.Bottom - panelHeight - 6;
        if (panelY < Bounds.Y + 6)
            panelY = Bounds.Y + 6;

        var panel = new Rect(panelX, panelY, panelWidth, panelHeight);
        FillRect(ctx, panel, Color.FromArgb(246, 28, 30, 36));
        DrawRectOutline(ctx, panel, Color.FromArgb(230, 85, 105, 135), 1);
        FillRect(ctx, new Rect(panel.X, panel.Y, 4, panel.Height), Color.FromRgb(100, 170, 255));

        DrawText(ctx, helpText,
            new Rect(panel.X + panelPadding, panel.Y + panelPadding,
                contentWidth, textHeight),
            Color.FromRgb(222, 225, 232), TextAlignment.Left,
            TextAlignment.Top, TextWrapping.Wrap);
    }

    // ==================== 标题悬停预览 ====================
    private void DrawTooltipBackground(IGraphicsContext ctx)
    {
        if (_hoverTitleNodeId < 0) return;
        var node = FindNode(_hoverTitleNodeId);
        if (node is null) return;

        var layout = CreateNodeTooltipLayout(node);
        if (layout is null) return;

        FillRect(ctx, layout.PanelBounds, Color.FromArgb(246, 30, 32, 39));
        FillRect(ctx, new Rect(layout.PanelBounds.X, layout.PanelBounds.Y, 4, layout.PanelBounds.Height),
            GetStatusColor(node.ExecutionStatus));

        foreach (var item in layout.Images)
            FillRect(ctx, item.Bounds, Color.FromRgb(19, 20, 24));
    }

    private void DrawNodeTooltip(IGraphicsContext ctx)
    {
        if (_hoverTitleNodeId < 0) return;
        var node = FindNode(_hoverTitleNodeId);
        if (node is null) return;

        var layout = CreateNodeTooltipLayout(node);
        if (layout is null) return;

        DrawRectOutline(ctx, layout.PanelBounds, Color.FromArgb(235, 90, 105, 130), 1);
        DrawText(ctx, layout.Text, layout.TextBounds,
            Color.FromRgb(220, 220, 220), TextAlignment.Left, TextAlignment.Top, TextWrapping.Wrap);

        foreach (var item in layout.Images)
        {
            var imageRect = item.Bounds;
            DrawRectOutline(ctx, imageRect, Color.FromRgb(76, 82, 94), 1);

            const double labelHeight = 22;
            var labelRect = new Rect(imageRect.X, imageRect.Y, imageRect.Width, Math.Min(labelHeight, imageRect.Height));
            FillRect(ctx, labelRect, Color.FromArgb(160, 0, 0, 0));
            DrawText(ctx, item.Port.Name,
                new Rect(labelRect.X + 4, labelRect.Y, Math.Max(0, labelRect.Width - 8), labelRect.Height),
                Color.FromRgb(255, 255, 240), TextAlignment.Left, TextAlignment.Center, TextWrapping.NoWrap);
        }
    }

    private void DrawSidebarIcon(IGraphicsContext ctx, int index, double cx, double cy, Color color)
    {
        switch (index)
        {
            case 0:
                DrawLine(ctx, new Point(cx - 8, cy), new Point(cx + 8, cy), color, 2);
                DrawLine(ctx, new Point(cx, cy - 8), new Point(cx, cy + 8), color, 2);
                break;
            case 1:
                DrawLine(ctx, new Point(cx - 7, cy - 8), new Point(cx - 7, cy + 8), color, 2);
                DrawLine(ctx, new Point(cx - 3, cy - 7), new Point(cx + 9, cy), color, 2);
                DrawLine(ctx, new Point(cx + 9, cy), new Point(cx - 3, cy + 7), color, 2);
                DrawLine(ctx, new Point(cx - 3, cy + 7), new Point(cx - 3, cy - 7), color, 2);
                break;
            case 2:
                DrawLine(ctx, new Point(cx - 9, cy - 8), new Point(cx - 9, cy + 8), color, 2);
                DrawLine(ctx, new Point(cx - 5, cy - 8), new Point(cx - 5, cy + 8), color, 2);
                DrawLine(ctx, new Point(cx - 1, cy - 7), new Point(cx + 9, cy), color, 2);
                DrawLine(ctx, new Point(cx + 9, cy), new Point(cx - 1, cy + 7), color, 2);
                DrawLine(ctx, new Point(cx - 1, cy + 7), new Point(cx - 1, cy - 7), color, 2);
                break;
            case 3:
                FillRect(ctx, new Rect(cx - 6, cy - 6, 12, 12), color);
                break;
            case 4:
                DrawLine(ctx, new Point(cx, cy - 9), new Point(cx, cy + 1), color, 2);
                DrawLine(ctx, new Point(cx - 5, cy - 4), new Point(cx, cy - 9), color, 2);
                DrawLine(ctx, new Point(cx + 5, cy - 4), new Point(cx, cy - 9), color, 2);
                DrawLine(ctx, new Point(cx - 7, cy + 4), new Point(cx + 7, cy + 4), color, 2);
                break;
            case 5:
                DrawLine(ctx, new Point(cx - 7, cy - 4), new Point(cx + 7, cy - 4), color, 2);
                DrawLine(ctx, new Point(cx, cy - 1), new Point(cx, cy + 9), color, 2);
                DrawLine(ctx, new Point(cx - 5, cy + 4), new Point(cx, cy + 9), color, 2);
                DrawLine(ctx, new Point(cx + 5, cy + 4), new Point(cx, cy + 9), color, 2);
                break;
            case 6:
                double box = 5, gap = 3;
                FillRect(ctx, new Rect(cx - box - gap / 2, cy - box - gap / 2, box, box), color);
                FillRect(ctx, new Rect(cx + gap / 2, cy - box - gap / 2, box, box), color);
                FillRect(ctx, new Rect(cx - box - gap / 2, cy + gap / 2, box, box), color);
                FillRect(ctx, new Rect(cx + gap / 2, cy + gap / 2, box, box), color);
                break;
            case 7:
                DrawEllipse(ctx, cx, cy, 12, color, 1.5);
                DrawText(ctx, "?", new Rect(cx - 8, cy - 8, 16, 16), color,
                    TextAlignment.Center, TextAlignment.Center);
                break;
        }
    }

    private void DrawAddPanel(IGraphicsContext ctx)
    {
        double panelX = Bounds.X + FlowGraphConstants.SidebarWidth;
        double panelY = Bounds.Y + FlowGraphConstants.SidebarBtnY0;
        double panelW = FlowGraphConstants.AddPanelWidth;
        int count = _cardRegistry.Count;
        double panelH = FlowGraphConstants.AddPanelHeader + (count > 0 ? count * FlowGraphConstants.AddPanelItemH + 4 : 36);

        FillRect(ctx, new Rect(panelX, panelY, panelW, panelH), Color.FromRgb(28, 28, 36));
        DrawRectOutline(ctx, new Rect(panelX, panelY, panelW, panelH), Color.FromRgb(65, 65, 80), 1);
        DrawText(ctx, "添加节点", new Rect(panelX + 10, panelY + 2, panelW - 10, FlowGraphConstants.AddPanelHeader),
            Color.FromRgb(120, 120, 135), TextAlignment.Left, TextAlignment.Center);

        if (count == 0)
        {
            DrawText(ctx, "暂无卡片类型", new Rect(panelX + 4, panelY + FlowGraphConstants.AddPanelHeader, panelW - 4, panelH - FlowGraphConstants.AddPanelHeader - 4),
                Color.FromRgb(90, 90, 105), TextAlignment.Center, TextAlignment.Center);
            return;
        }

        int idx = 0;
        foreach (var desc in _cardRegistry.Values)
        {
            double iy = panelY + FlowGraphConstants.AddPanelHeader + idx * FlowGraphConstants.AddPanelItemH;
            bool isHover = _addPanelHover == idx;
            if (isHover)
                FillRect(ctx, new Rect(panelX + 1, iy + 1, panelW - 2, FlowGraphConstants.AddPanelItemH - 2),
                    Color.FromRgb(50, 50, 65));

            FillRect(ctx, new Rect(panelX + 8, iy + (FlowGraphConstants.AddPanelItemH - 16) / 2, 4, 16), desc.HeaderColor);
            var textColor = isHover ? Color.FromRgb(240, 240, 245) : Color.FromRgb(195, 195, 210);
            DrawText(ctx, desc.Name, new Rect(panelX + 20, iy, panelW - 32, FlowGraphConstants.AddPanelItemH),
                textColor, TextAlignment.Left, TextAlignment.Center);
            idx++;
        }
    }

    private void DrawChainPanel(IGraphicsContext ctx)
    {
        if (_nodes.Count == 0) return;
        int visibleCount = Math.Min(_nodes.Count, FlowGraphConstants.ChainPanelMaxVisible);
        double panelX = Bounds.X + FlowGraphConstants.SidebarWidth;
        double panelY = Bounds.Y + (_chainPanelMode == 0 ? FlowGraphConstants.SidebarBtnY1 : FlowGraphConstants.SidebarBtnY2);
        double panelW = FlowGraphConstants.ChainPanelWidth;
        double panelH = FlowGraphConstants.ChainPanelHeader + visibleCount * FlowGraphConstants.ChainPanelItemH + 4;

        FillRect(ctx, new Rect(panelX, panelY, panelW, panelH), Color.FromRgb(28, 28, 36));
        DrawRectOutline(ctx, new Rect(panelX, panelY, panelW, panelH), Color.FromRgb(65, 65, 80), 1);

        string title = _chainPanelMode == 0 ? "链路从头 - 选择节点" : "链路继续 - 选择节点";
        DrawText(ctx, title, new Rect(panelX + 8, panelY + 2, panelW - 16, FlowGraphConstants.ChainPanelHeader),
            Color.FromRgb(120, 120, 135), TextAlignment.Left, TextAlignment.Center);

        for (int i = 0; i < visibleCount; i++)
        {
            var node = _nodes[i];
            double iy = panelY + FlowGraphConstants.ChainPanelHeader + i * FlowGraphConstants.ChainPanelItemH;
            bool isHover = _chainPanelHover == i;
            if (isHover)
                FillRect(ctx, new Rect(panelX + 1, iy + 1, panelW - 2, FlowGraphConstants.ChainPanelItemH - 2),
                    Color.FromRgb(50, 50, 65));

            FillRect(ctx, new Rect(panelX + 8, iy + (FlowGraphConstants.ChainPanelItemH - 14) / 2, 4, 14), node.HeaderColor);
            FillEllipse(ctx, panelX + 20, iy + FlowGraphConstants.ChainPanelItemH / 2, 3.5, GetStatusColor(node.ExecutionStatus));

            var textColor = isHover ? Color.FromRgb(240, 240, 245) : Color.FromRgb(185, 185, 200);
            DrawText(ctx, $"{node.Title}(Id:{node.Id})",
                new Rect(panelX + 28, iy, panelW - 38, FlowGraphConstants.ChainPanelItemH),
                textColor, TextAlignment.Left, TextAlignment.Center, TextWrapping.NoWrap);
        }
    }

    private void DrawCancelPanel(IGraphicsContext ctx)
    {
        var busyNodes = _nodes.Where(n =>
            n.ExecutionStatus == FlowGraphExecutionStatus.Running ||
            n.ExecutionStatus == FlowGraphExecutionStatus.Pending).ToList();

        if (busyNodes.Count == 0) { _showCancelPanel = false; return; }

        int visibleCount = Math.Min(busyNodes.Count, FlowGraphConstants.CancelPanelMaxVisible);
        double panelX = Bounds.X + FlowGraphConstants.SidebarWidth;
        double panelY = Bounds.Y + FlowGraphConstants.SidebarBtnY3;
        double panelW = FlowGraphConstants.CancelPanelWidth;
        double panelH = FlowGraphConstants.CancelPanelHeader + visibleCount * FlowGraphConstants.CancelPanelItemH + 4;

        FillRect(ctx, new Rect(panelX, panelY, panelW, panelH), Color.FromRgb(36, 28, 28));
        DrawRectOutline(ctx, new Rect(panelX, panelY, panelW, panelH), Color.FromRgb(80, 65, 65), 1);
        DrawText(ctx, "强制取消执行", new Rect(panelX + 8, panelY + 2, panelW - 16, FlowGraphConstants.CancelPanelHeader),
            Color.FromRgb(255, 120, 120), TextAlignment.Left, TextAlignment.Center);

        for (int i = 0; i < visibleCount; i++)
        {
            var node = busyNodes[i];
            double iy = panelY + FlowGraphConstants.CancelPanelHeader + i * FlowGraphConstants.CancelPanelItemH;
            bool isHover = _cancelPanelHover == i;
            if (isHover)
                FillRect(ctx, new Rect(panelX + 1, iy + 1, panelW - 2, FlowGraphConstants.CancelPanelItemH - 2),
                    Color.FromRgb(65, 50, 50));

            FillEllipse(ctx, panelX + 20, iy + FlowGraphConstants.CancelPanelItemH / 2, 4, GetStatusColor(node.ExecutionStatus));
            var textColor = isHover ? Color.FromRgb(245, 240, 240) : Color.FromRgb(200, 185, 185);
            DrawText(ctx, $"{node.Title} (Id:{node.Id})",
                new Rect(panelX + 32, iy, panelW - 42, FlowGraphConstants.CancelPanelItemH),
                textColor, TextAlignment.Left, TextAlignment.Center, TextWrapping.NoWrap);
        }
    }

    private void DrawCustomPanel(IGraphicsContext ctx)
    {
        double panelX = Bounds.X + FlowGraphConstants.SidebarWidth;
        double panelY = Bounds.Y + FlowGraphConstants.SidebarBtnY6;
        double panelW = FlowGraphConstants.CustomPanelWidth;
        int count = _customItems.Length;
        double panelH = FlowGraphConstants.CustomPanelHeader + (count > 0 ? Math.Min(count, FlowGraphConstants.CustomPanelMaxVisible) * FlowGraphConstants.CustomPanelItemH + 4 : 36);

        FillRect(ctx, new Rect(panelX, panelY, panelW, panelH), Color.FromRgb(28, 28, 36));
        DrawRectOutline(ctx, new Rect(panelX, panelY, panelW, panelH), Color.FromRgb(65, 65, 80), 1);
        DrawText(ctx, "自定义", new Rect(panelX + 10, panelY + 2, panelW - 10, FlowGraphConstants.CustomPanelHeader),
            Color.FromRgb(120, 120, 135), TextAlignment.Left, TextAlignment.Center);

        if (count == 0)
        {
            DrawText(ctx, "暂无条目", new Rect(panelX + 4, panelY + FlowGraphConstants.CustomPanelHeader, panelW - 4, panelH - FlowGraphConstants.CustomPanelHeader - 4),
                Color.FromRgb(90, 90, 105), TextAlignment.Center, TextAlignment.Center);
            return;
        }

        int visibleCount = Math.Min(count, FlowGraphConstants.CustomPanelMaxVisible);
        for (int i = 0; i < visibleCount; i++)
        {
            double iy = panelY + FlowGraphConstants.CustomPanelHeader + i * FlowGraphConstants.CustomPanelItemH;
            bool isHover = _customPanelHover == i;
            if (isHover)
                FillRect(ctx, new Rect(panelX + 1, iy + 1, panelW - 2, FlowGraphConstants.CustomPanelItemH - 2),
                    Color.FromRgb(50, 50, 65));

            FillRect(ctx, new Rect(panelX + 8, iy + (FlowGraphConstants.CustomPanelItemH - 14) / 2, 4, 14),
                Color.FromRgb(220, 160, 60));
            var textColor = isHover ? Color.FromRgb(240, 240, 245) : Color.FromRgb(185, 185, 200);
            DrawText(ctx, _customItems[i], new Rect(panelX + 20, iy, panelW - 32, FlowGraphConstants.CustomPanelItemH),
                textColor, TextAlignment.Left, TextAlignment.Center, TextWrapping.NoWrap);
        }
    }

    private void DrawContextMenu(IGraphicsContext ctx)
    {
        if (!_contextMenuOpen) return;

        double menuW = FlowGraphConstants.ContextMenuWidth;
        double menuH = FlowGraphConstants.ContextMenuHeader * 2 + FlowGraphConstants.ContextMenuItemH * 4;
        double mx = Bounds.X + Math.Clamp(_contextMenuPosition.X, 0, Bounds.Width - menuW);
        double my = Bounds.Y + Math.Clamp(_contextMenuPosition.Y, 0, Bounds.Height - menuH);

        FillRect(ctx, new Rect(mx, my, menuW, menuH), Color.FromRgb(35, 35, 45));
        DrawRectOutline(ctx, new Rect(mx, my, menuW, menuH), Color.FromRgb(70, 70, 90), 1);

        string[] items = { "从头执行", "继续执行", "取消执行", "清空数据" };
        var node = FindNode(_contextMenuNodeId);

        for (int i = 0; i < 4; i++)
        {
            double iy = my + FlowGraphConstants.ContextMenuHeader + i * FlowGraphConstants.ContextMenuItemH;
            bool isHover = _contextMenuHover == i;

            if (isHover)
                FillRect(ctx, new Rect(mx + 4, iy + 2, menuW - 8, FlowGraphConstants.ContextMenuItemH - 4),
                    Color.FromArgb(200, 60, 120, 200));

            bool disabled = false;
            if (node != null)
            {
                if (i == 2 && node.ExecutionStatus != FlowGraphExecutionStatus.Running &&
                    node.ExecutionStatus != FlowGraphExecutionStatus.Pending)
                    disabled = true;
            }

            var textColor = disabled ? Color.FromRgb(100, 100, 110) : Color.FromRgb(220, 220, 230);
            DrawText(ctx, items[i], new Rect(mx, iy, menuW, FlowGraphConstants.ContextMenuItemH),
                textColor, TextAlignment.Center, TextAlignment.Center);
        }
    }

    private void DrawEditPanel(IGraphicsContext ctx)
    {
        if (!_showEditPanel) return;

        double pw = FlowGraphConstants.EditPanelWidth;
        double ph = FlowGraphConstants.EditPanelHeight;
        double px = Bounds.Right - pw - 20;
        double py = Bounds.Bottom - ph - 20;

        FillRect(ctx, new Rect(px, py, pw, ph), Color.FromRgb(32, 32, 40));
        DrawRectOutline(ctx, new Rect(px, py, pw, ph), Color.FromRgb(75, 75, 95), 1);

        var node = FindNode(_editPanelTargetNode);
        string title = node != null ? $"编辑内容 - {node.Title}(Id:{node.Id})" : "编辑内容";
        FillRect(ctx, new Rect(px + 1, py + 1, pw - 2, 28), Color.FromRgb(40, 40, 50));
        DrawText(ctx, title, new Rect(px + 12, py + 2, pw - 24, 28),
            Color.FromRgb(210, 210, 220), TextAlignment.Left, TextAlignment.Center);

        var btnRect = new Rect(px + pw - 120, py + ph - 45, 110, 35);
        FillRect(ctx, btnRect, Color.FromRgb(55, 100, 190));
        DrawRectOutline(ctx, btnRect, Color.FromRgb(90, 150, 240), 1);
        DrawText(ctx, "提交内容", btnRect, Color.FromRgb(220, 230, 245),
            TextAlignment.Center, TextAlignment.Center);
    }

    private void DrawProjectName(IGraphicsContext ctx)
    {
        if (string.IsNullOrEmpty(_projectName)) return;

        double textW = _projectName.Length * 14 + 40;
        double panelW = Math.Max(textW, 100);
        double panelH = 36;
        double panelX = Bounds.X + (Bounds.Width - panelW) / 2;
        double panelY = Bounds.Y + 15;

        if (panelX < Bounds.X + FlowGraphConstants.SidebarWidth + 10)
            panelX = Bounds.X + FlowGraphConstants.SidebarWidth + 10;

        FillRect(ctx, new Rect(panelX, panelY, panelW, panelH), Color.FromArgb(180, 20, 20, 25));
        DrawRectOutline(ctx, new Rect(panelX, panelY, panelW, panelH), Color.FromArgb(150, 80, 80, 100), 1);
        DrawText(ctx, _projectName, new Rect(panelX, panelY, panelW, panelH),
            Color.FromRgb(235, 235, 245), TextAlignment.Center, TextAlignment.Center);
    }

    private void DrawConnectingLine(IGraphicsContext ctx)
    {
        if (_connectingNodeId == -1 || _connectingPortIndex < 0) return;
        var node = FindNode(_connectingNodeId);
        if (node is null || !IsValidPortIndex(node, _connectingPortIndex)) return;

        var anchor = WorldToScreen(GetPortAnchor(node, _connectingPortIndex));
        var color = GetPortColor(node.Ports[_connectingPortIndex].DataType);
        DrawLine(ctx, ToWindow(anchor), ToWindow(_connectingPointer), color, 2);
    }

    private void DrawSelectionRect(IGraphicsContext ctx)
    {
        if (!_isSelecting) return;

        var a = WorldToScreen(_selectionStartWorld);
        var b = WorldToScreen(_selectionEndWorld);
        var rect = NormalizeRect(a, b);
        var screenRect = new Rect(Bounds.X + rect.X, Bounds.Y + rect.Y, rect.Width, rect.Height);

        FillRect(ctx, screenRect, Color.FromArgb(40, 100, 150, 255));
        DrawRectOutline(ctx, screenRect, Color.FromArgb(255, 100, 150, 255), 1.5);
    }

    private void DrawComboPopup(IGraphicsContext ctx)
    {
        if (_expandedComboNodeId == -1) return;
        var node = FindNode(_expandedComboNodeId);
        if (node is null || !IsValidPortIndex(node, _expandedComboPortIndex)) return;

        var port = node.Ports[_expandedComboPortIndex];
        if (port.ComboData is null || port.ComboData.Options.Count == 0) return;

        var widgetRect = WorldToScreen(GetWidgetRect(node, _expandedComboPortIndex));
        double itemH = FlowGraphConstants.ComboItemHeight * _zoom;
        int count = port.ComboData.Options.Count;
        double panelH = count * itemH;
        double panelX = Bounds.X + widgetRect.X;
        double panelY = Bounds.Y + widgetRect.Y + widgetRect.Height;
        double panelW = widgetRect.Width;

        FillRect(ctx, new Rect(panelX, panelY, panelW, panelH), Color.FromRgb(38, 38, 42));
        DrawRectOutline(ctx, new Rect(panelX, panelY, panelW, panelH), Color.FromRgb(70, 70, 80), 1);

        for (int k = 0; k < count; k++)
        {
            double iy = panelY + k * itemH;
            bool isHover = _comboHoverIndex == k;
            bool isSelected = port.ComboData.Current == k;

            if (isHover)
                FillRect(ctx, new Rect(panelX + 1, iy + 1, panelW - 2, itemH - 2), Color.FromRgb(60, 60, 75));

            var textColor = isSelected ? Color.FromRgb(100, 180, 255) : Color.FromRgb(217, 217, 217);
            DrawText(ctx, port.ComboData.Options[k],
                new Rect(panelX + 24 * _zoom, iy, panelW - 32 * _zoom, itemH),
                textColor, TextAlignment.Left, TextAlignment.Center, TextWrapping.NoWrap);

            if (isSelected)
                FillRect(ctx, new Rect(panelX + 4, iy + 6, 3, itemH - 6), Color.FromRgb(100, 180, 255));
        }
    }

    private void DrawWidgetResizeHandles(IGraphicsContext ctx)
    {
        foreach (var node in _nodes)
        {
            for (int i = 0; i < node.Ports.Count; i++)
            {
                var port = node.Ports[i];
                if (!HasVisibleWidget(port) || !IsResizableWidget(port.Widget))
                    continue;

                var rect = ToWindow(WorldToScreen(GetWidgetRect(node, i)));
                if (rect.Width < 8 || rect.Height < 8)
                    continue;

                var color = Color.FromRgb(150, 150, 150);
                var p1 = new Point(rect.Right, rect.Bottom - 9 * _zoom);
                var p2 = new Point(rect.Right, rect.Bottom);
                var p3 = new Point(rect.Right - 9 * _zoom, rect.Bottom);

                DrawLine(ctx, p1, p2, color, Math.Max(1, _zoom));
                DrawLine(ctx, p2, p3, color, Math.Max(1, _zoom));
                DrawLine(ctx,
                    new Point(rect.Right - 3 * _zoom, rect.Bottom - 8 * _zoom),
                    new Point(rect.Right - 8 * _zoom, rect.Bottom - 3 * _zoom),
                    color, Math.Max(1, _zoom));
            }
        }
    }

    private static bool IsResizableWidget(FlowGraphWidgetKind widget)
        => widget is FlowGraphWidgetKind.Edit
            or FlowGraphWidgetKind.Text
            or FlowGraphWidgetKind.MultilineText
            or FlowGraphWidgetKind.ImagePreview;

    private void DrawHandle(IGraphicsContext ctx, Point screenPoint)
    {
        var p = ToWindow(screenPoint);
        FillEllipse(ctx, p.X, p.Y, 4, Color.FromRgb(255, 255, 255));
    }

    private void DrawBezier(IGraphicsContext ctx, Point p0, Point p1, Point p2, Point p3, Color color, double thickness)
    {
        var previous = p0;
        for (int i = 1; i <= 28; i++)
        {
            var next = CubicBezier(p0, p1, p2, p3, i / 28.0);
            DrawLine(ctx, ToWindow(previous), ToWindow(next), color, thickness);
            previous = next;
        }
    }

    // ==================== 几何辅助 ====================
    private Rect NodeScreenRect(FlowGraphNode node)
    {
        var topLeft = WorldToScreen(new Point(node.X, node.Y));
        return new Rect(Bounds.X + topLeft.X, Bounds.Y + topLeft.Y, node.Width * _zoom, node.Height * _zoom);
    }

    private Point WorldToScreen(Point world) => new(world.X * _zoom - _panX, world.Y * _zoom - _panY);

    private Rect WorldToScreen(Rect world) => new(
        world.X * _zoom - _panX, world.Y * _zoom - _panY,
        world.Width * _zoom, world.Height * _zoom);

    private Point ToWorld(Point local) => new((local.X + _panX) / _zoom, (local.Y + _panY) / _zoom);
    private Point ToWindow(Point local) => new(Bounds.X + local.X, Bounds.Y + local.Y);
    private Rect ToWindow(Rect local) => new(Bounds.X + local.X, Bounds.Y + local.Y, local.Width, local.Height);

    private Point GetPortAnchor(FlowGraphNode node, int portIndex)
    {
        if (!IsValidPortIndex(node, portIndex)) return new Point(node.X, node.Y);
        var port = node.Ports[portIndex];
        if (port.Direction == FlowGraphPortDirection.Inline)
            return new Point(node.X + node.Width / 2, node.Y + node.Height / 2);

        int sameCount = 0;
        int index = 0;
        for (int i = 0; i < node.Ports.Count; i++)
        {
            if (node.Ports[i].Direction == port.Direction)
            {
                if (i == portIndex) index = sameCount;
                sameCount++;
            }
        }

        double startY = (node.Height - sameCount * FlowGraphConstants.PortSpacing) / 2;
        if (startY < FlowGraphConstants.TitleHeight + 5) startY = FlowGraphConstants.TitleHeight + 5;
        double cy = node.Y + startY + index * FlowGraphConstants.PortSpacing + FlowGraphConstants.PortSpacing / 2;

        if (port.Direction == FlowGraphPortDirection.Input)
            return new Point(node.X - FlowGraphConstants.PortOffsetX, cy);
        else
            return new Point(node.X + node.Width + FlowGraphConstants.PortOffsetX, cy);
    }

    private Rect GetWidgetRect(FlowGraphNode node, int portIndex)
    {
        var port = node.Ports[portIndex];
        double y = FlowGraphConstants.TitleHeight;

        for (int i = 0; i < portIndex; i++)
        {
            if (HasVisibleWidget(node.Ports[i]))
                y += Math.Max(24, node.Ports[i].WidgetHeight) + FlowGraphConstants.WidgetGap;
        }

        double availableWidgetW = Math.Max(0, node.Width - 20);
        double widgetW = port.WidgetWidth > 0
            ? Math.Min(port.WidgetWidth, availableWidgetW)
            : availableWidgetW;

        return new Rect(node.X + 10, node.Y + y, widgetW, Math.Max(24, port.WidgetHeight));
    }

    private bool HasVisibleWidget(FlowGraphPort port)
    {
        if (port.Widget == FlowGraphWidgetKind.None) return false;
        if (port.Direction == FlowGraphPortDirection.Output) return false;
        if (port.Direction == FlowGraphPortDirection.Inline) return true;
        return !port.IsConnected ||
               port.Widget is FlowGraphWidgetKind.Display or FlowGraphWidgetKind.ImagePreview;
    }

    private void FitNodeWidthToWidgets(FlowGraphNode node)
    {
        double maxWidgetWidth = 0;
        foreach (var port in node.Ports)
        {
            if (HasVisibleWidget(port) && port.WidgetWidth > 0)
                maxWidgetWidth = Math.Max(maxWidgetWidth, port.WidgetWidth);
        }

        node.Width = Math.Max(FlowGraphConstants.NodeMinWidth,
            maxWidgetWidth > 0 ? maxWidgetWidth + 20 : FlowGraphConstants.NodeMinWidth);
    }

    private void RecalculateNodeSize(FlowGraphNode node)
    {
        int inputCount = node.Ports.Count(p => p.Direction == FlowGraphPortDirection.Input);
        int outputCount = node.Ports.Count(p => p.Direction == FlowGraphPortDirection.Output);

        double widgetsHeight = 0;
        double requiredWidgetWidth = 0;

        foreach (var port in node.Ports)
        {
            if (!HasVisibleWidget(port))
                continue;

            widgetsHeight += Math.Max(24, port.WidgetHeight) + FlowGraphConstants.WidgetGap;
            if (port.WidgetWidth > 0)
                requiredWidgetWidth = Math.Max(requiredWidgetWidth, port.WidgetWidth);
        }

        double portHeight = Math.Max(inputCount, outputCount) * FlowGraphConstants.PortSpacing +
                            FlowGraphConstants.TitleHeight + 10;
        double contentHeight = FlowGraphConstants.TitleHeight + widgetsHeight + FlowGraphConstants.NodePadding;

        node.Height = Math.Max(portHeight, contentHeight);

        double requiredNodeWidth = Math.Max(
            FlowGraphConstants.NodeMinWidth,
            requiredWidgetWidth > 0 ? requiredWidgetWidth + 20 : FlowGraphConstants.NodeMinWidth);

        node.Width = Math.Max(node.Width, requiredNodeWidth);
    }

    private void SetCurveHandleOnCurve(FlowGraphConnection connection, int handle, Point draggedWorld)
    {
        var fromNode = FindNode(connection.FromNodeId);
        var toNode = FindNode(connection.ToNodeId);
        if (fromNode is null || toNode is null ||
            !IsValidPortIndex(fromNode, connection.FromPortIndex) ||
            !IsValidPortIndex(toNode, connection.ToPortIndex))
            return;

        if (handle is not (1 or 2)) return;

        var p0 = GetPortAnchor(fromNode, connection.FromPortIndex);
        var p3 = GetPortAnchor(toNode, connection.ToPortIndex);
        var oldP1 = connection.ControlPoint1;
        var oldP2 = connection.ControlPoint2;

        var q1 = CubicBezier(p0, oldP1, oldP2, p3, 1.0 / 3.0);
        var q2 = CubicBezier(p0, oldP1, oldP2, p3, 2.0 / 3.0);

        if (handle == 1) q1 = draggedWorld;
        else q2 = draggedWorld;

        var r1 = new Point(
            27 * q1.X - 8 * p0.X - p3.X,
            27 * q1.Y - 8 * p0.Y - p3.Y);
        var r2 = new Point(
            27 * q2.X - p0.X - 8 * p3.X,
            27 * q2.Y - p0.Y - 8 * p3.Y);

        var p1 = new Point((2 * r1.X - r2.X) / 18.0, (2 * r1.Y - r2.Y) / 18.0);
        var p2 = new Point((2 * r2.X - r1.X) / 18.0, (2 * r2.Y - r1.Y) / 18.0);

        connection.ControlPoint1 = p1;
        connection.ControlPoint2 = p2;
        connection.HasCustomControlPoints = true;
    }

    private static Point CubicBezier(Point p0, Point p1, Point p2, Point p3, double t)
    {
        double u = 1 - t;
        double uu = u * u, tt = t * t;
        return new Point(
            uu * u * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + tt * t * p3.X,
            uu * u * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + tt * t * p3.Y);
    }

    private static Rect NormalizeRect(Point a, Point b)
    {
        double x = Math.Min(a.X, b.X), y = Math.Min(a.Y, b.Y);
        return new Rect(x, y, Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }

    private static double Distance(Point a, Point b)
    {
        double dx = a.X - b.X, dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double DistanceToSegment(Point p, Point a, Point b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        if (dx == 0 && dy == 0) return Distance(p, a);
        double t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy), 0, 1);
        return Distance(p, new Point(a.X + t * dx, a.Y + t * dy));
    }

    private static double PositiveModulo(double value, double divisor)
    {
        double result = value % divisor;
        return result < 0 ? result + divisor : value;
    }

    private static double Clamp(double v, double min, double max) => Math.Max(min, Math.Min(max, v));

    private FlowGraphNode? FindNode(int id) => _nodes.FirstOrDefault(n => n.Id == id);
    private FlowGraphConnection? FindConnection(int id) => _connections.FirstOrDefault(c => c.Id == id);
    private static bool IsValidPortIndex(FlowGraphNode node, int index) => index >= 0 && index < node.Ports.Count;

    private static bool IsCompatible(FlowGraphDataType a, FlowGraphDataType b)
        => a == FlowGraphDataType.Any || b == FlowGraphDataType.Any || a == b;

    private static Color GetPortColor(FlowGraphDataType type) => type switch
    {
        FlowGraphDataType.Image => Color.FromRgb(180, 80, 80),
        FlowGraphDataType.Text => Color.FromRgb(80, 80, 180),
        FlowGraphDataType.Combo => Color.FromRgb(150, 50, 150),
        FlowGraphDataType.Video => Color.FromRgb(180, 130, 50),
        FlowGraphDataType.Audio => Color.FromRgb(255, 215, 0),
        FlowGraphDataType.Number => Color.FromRgb(88, 164, 226),
        FlowGraphDataType.Boolean => Color.FromRgb(208, 190, 92),
        FlowGraphDataType.Object => Color.FromRgb(145, 155, 170),
        _ => Color.FromRgb(200, 200, 200)
    };

    private static Color GetStatusColor(FlowGraphExecutionStatus status) => status switch
    {
        FlowGraphExecutionStatus.Pending => Color.FromRgb(255, 200, 0),
        FlowGraphExecutionStatus.Running => Color.FromRgb(0, 150, 255),
        FlowGraphExecutionStatus.Completed => Color.FromRgb(0, 200, 80),
        FlowGraphExecutionStatus.Failed => Color.FromRgb(255, 60, 60),
        _ => Color.FromRgb(128, 128, 128)
    };

    internal static string ToDisplayText(object? value) => value switch
    {
        null => string.Empty,
        string text => text,
        FlowGraphImageData img => string.IsNullOrWhiteSpace(img.SourcePath) ? "图片" : img.SourcePath,
        IFormattable f => f.ToString(null, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty
    };

    // ==================== 绘图辅助 ====================
    private static void DrawLine(IGraphicsContext ctx, Point from, Point to, Color color, double thickness)
        => ctx.DrawLine(from, to, color, thickness);

    private static void FillRect(IGraphicsContext ctx, Rect rect, Color color)
        => ctx.FillRectangle(rect, color);

    private static void DrawRectOutline(IGraphicsContext ctx, Rect rect, Color color, double thickness)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;
        DrawLine(ctx, new Point(rect.X, rect.Y), new Point(rect.Right, rect.Y), color, thickness);
        DrawLine(ctx, new Point(rect.Right, rect.Y), new Point(rect.Right, rect.Bottom), color, thickness);
        DrawLine(ctx, new Point(rect.Right, rect.Bottom), new Point(rect.X, rect.Bottom), color, thickness);
        DrawLine(ctx, new Point(rect.X, rect.Bottom), new Point(rect.X, rect.Y), color, thickness);
    }

    private static void FillEllipse(IGraphicsContext ctx, double cx, double cy, double radius, Color color)
    {
        if (radius <= 0) return;
        const double targetRowHeight = 0.65;
        int rows = Math.Max(2, (int)Math.Ceiling(radius * 2.0 / targetRowHeight));
        double rowHeight = radius * 2.0 / rows;
        double radiusSquared = radius * radius;

        for (int i = 0; i < rows; i++)
        {
            double y = -radius + (i + 0.5) * rowHeight;
            double halfWidth = Math.Sqrt(Math.Max(0, radiusSquared - y * y));
            if (halfWidth <= 0) continue;
            ctx.FillRectangle(
                new Rect(cx - halfWidth, cy - radius + i * rowHeight,
                    halfWidth * 2.0, rowHeight + 0.12),
                color);
        }
    }

    private static void DrawEllipse(IGraphicsContext ctx, double cx, double cy, double radius, Color color, double thickness)
    {
        int segments = Math.Max(32, (int)Math.Ceiling(radius * 8));
        for (int i = 0; i < segments; i++)
        {
            double a1 = 2 * Math.PI * i / segments;
            double a2 = 2 * Math.PI * (i + 1) / segments;
            ctx.DrawLine(
                new Point(cx + radius * Math.Cos(a1), cy + radius * Math.Sin(a1)),
                new Point(cx + radius * Math.Cos(a2), cy + radius * Math.Sin(a2)),
                color, thickness);
        }
    }

    private void DrawText(IGraphicsContext ctx, string text, Rect rect, Color color,
        TextAlignment horizontal, TextAlignment vertical, TextWrapping wrapping = TextWrapping.NoWrap)
    {
        DrawEngineText(ctx, text ?? string.Empty, rect, color, horizontal, vertical, wrapping);
    }

    private void InvalidateGraphVisual()
    {
        InvalidateVisual();
        GraphVisualInvalidated?.Invoke(this, EventArgs.Empty);
    }
}
