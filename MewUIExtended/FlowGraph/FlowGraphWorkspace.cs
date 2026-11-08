using System.Diagnostics;
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Platform;
using MewUI.Extended;

namespace MewUIExtended.FlowGraph;

/// <summary>
/// 工作区控件：包含节点图 + 右下角浮动多行编辑器。。
/// </summary>
public sealed class FlowGraphWorkspace : UserControl
{
    private readonly Grid _editorPanel;
    private readonly Border _editorBackground;
    private readonly TextBlock _editorHeading;
    private readonly MultiLineTextBox _editor;
    private readonly Control _tooltipBackgroundOverlay;
    private readonly Canvas _tooltipImageOverlay;
    private readonly Control _floatingOverlay;
    private readonly Control _topmostOverlay;
    private readonly Dictionary<(int NodeId, int PortId, bool IsTooltip), Image> _tooltipImageViews = new();
    private readonly Dictionary<(int NodeId, int PortId, bool IsTooltip), IImageSource> _tooltipImageSources = new();
    private int _editingNodeId = -1;
    private int _editingPortIndex = -1;
    private string _draftText = string.Empty;

    public FlowGraphWorkspace(FlowGraphControl? graph = null)
    {
        Graph = graph ?? new FlowGraphControl();
        Graph.AllowDrop = true;
        Graph.DragOver += OnGraphDragOver;
        Graph.Drop += OnGraphDrop;

  
        _tooltipBackgroundOverlay = Graph.CreateTooltipBackgroundOverlay();
        _tooltipImageOverlay = new Canvas
        {
            IsHitTestVisible = false
        };
        _floatingOverlay = Graph.CreateFloatingOverlay();
        _topmostOverlay = Graph.CreateTopmostOverlay();

        _editorHeading = new TextBlock
        {
            Text = "节点参数编辑",
            Foreground = Color.FromRgb(238, 240, 244),
            FontSize = 15
        };

        _editor = new MultiLineTextBox
        {
            Text = string.Empty,
            AcceptTab = true,
            Wrap = true,
            Placeholder = "请输入节点参数...",
            Height = 180,
            MinHeight = 180,
            MaxHeight = 180,
            Foreground = Color.FromRgb(238, 240, 245),
            Background = Color.FromRgb(25, 27, 31),
            BorderBrush = Color.FromRgb(90, 101, 118),
            BorderThickness = 1,
            Padding = new Thickness(7)
        };

        _editor.OnTextChanged(text =>
        {
            _draftText = text;

        });

        var buttons = new StackPanel()
            .Horizontal()
            .Spacing(8)
            .Children(
                new Button().Content("应用").OnClick(ApplyEditor),
                new Button().Content("取消").OnClick(CancelEditor));

        var editorBody = new StackPanel()
            .Vertical()
            .Spacing(8)
            .Padding(12)
            .Children(_editorHeading, _editor, buttons);

        // 独立背景，不再包含任何子控件
        _editorBackground = new Border
        {
            Background = Color.FromRgb(34, 37, 43),
            BorderBrush = Color.FromRgb(82, 88, 100),
            BorderThickness = 1,
            CornerRadius = 6,
            ClipToBounds = false
        };

        
        _editorPanel = new Grid
        {
            Width = 390,
            MinWidth = 390,
            MaxWidth = 390,
            Height = 280,
            MinHeight = 280,
            MaxHeight = 280,
            Margin = new Thickness(0, 0, 24, 24),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            ClipToBounds = false,
            IsVisible = false,
        };
        _editorPanel.Rows("*")
            .Columns("*")
            .Children(
                _editorBackground.GridPosition(0, 0),
                editorBody.GridPosition(0, 0)
            );
        Content = new Grid()
            .Rows("*")
            .Columns("*")
            .Children(
                Graph.GridPosition(0, 0),
                _tooltipBackgroundOverlay.GridPosition(0, 0),
                _tooltipImageOverlay.GridPosition(0, 0),
                _floatingOverlay.GridPosition(0, 0),
                _topmostOverlay.GridPosition(0, 0),
                _editorPanel.GridPosition(0, 0));
      
        Graph.GraphVisualInvalidated += OnGraphVisualInvalidated;
        Graph.GraphPointerPressed += OnGraphPointerPressed;
        Graph.PortWidgetClicked += OnPortWidgetClicked;
        Graph.YamlExportRequested += OnYamlExportRequested;
        Graph.YamlImportRequested += OnYamlImportRequested;

        RefreshTooltipImageOverlay();
    }

    public FlowGraphControl Graph { get; }
    public bool IsEditorOpen => _editorPanel.IsVisible;

    public void CloseEditor() => CancelEditor();

    private void OnGraphVisualInvalidated(object? sender, EventArgs e)
    {
        RefreshTooltipImageOverlay();
        Graph.InvalidateFloatingOverlay();
    }

    private void OnGraphPointerPressed(object? sender, EventArgs e)
    {
        if (_editorPanel.IsVisible)
            CancelEditor();
    }

    private void RefreshTooltipImageOverlay()
    {
        var placements = Graph.GetImagePlacements();
        var visibleKeys = new HashSet<(int NodeId, int PortId, bool IsTooltip)>();

        foreach (var placement in placements)
        {
            // 只处理标题预览图片
            if (!placement.IsTooltip) continue;

            var key = (placement.NodeId, placement.PortId, placement.IsTooltip);
            visibleKeys.Add(key);

            if (!_tooltipImageViews.TryGetValue(key, out var image))
            {
                image = new Image
                {
                    IsHitTestVisible = false
                };
                image.StretchMode(Stretch.Uniform);
                _tooltipImageViews.Add(key, image);
                _tooltipImageOverlay.Add(image);
            }

            if (!_tooltipImageSources.TryGetValue(key, out var currentSource) ||
                !ReferenceEquals(currentSource, placement.Source))
            {
                image.Source(placement.Source);
                _tooltipImageSources[key] = placement.Source;
            }

            image.Size(placement.Bounds.Width, placement.Bounds.Height);
            image.CanvasPosition(placement.Bounds.X, placement.Bounds.Y);
            image.IsVisible = true;
        }

        foreach (var pair in _tooltipImageViews)
        {
            if (!visibleKeys.Contains(pair.Key))
                pair.Value.IsVisible = false;
        }
    }

    private void OnGraphDragOver(DragEventArgs e)
    {
        bool hasFiles = e.Data.TryGetData(DataFormats.StorageItems, out IReadOnlyList<string>? paths) &&
                        paths is not null && paths.Any(IsSupportedImagePath);
        bool targetAccepts = hasFiles && Graph.CanAcceptImageDrop(e.Position);

        e.Effect = targetAccepts && (e.AllowedEffects & DragDropEffects.Copy) != 0
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Accepted = e.Effect != DragDropEffects.None;
    }

    private void OnGraphDrop(DragEventArgs e)
    {
        if (!Graph.CanAcceptImageDrop(e.Position) ||
            !e.Data.TryGetData(DataFormats.StorageItems, out IReadOnlyList<string>? paths) ||
            paths is null)
            return;

        foreach (var path in paths)
        {
            if (!IsSupportedImagePath(path))
                continue;

            if (Graph.TryDropImageFile(path, e.Position))
            {
                e.Handled = true;
                return;
            }
        }
    }

    private static bool IsSupportedImagePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        return Path.GetExtension(path).ToLowerInvariant() is
            ".png" or ".jpg" or ".jpeg" or ".bmp";
    }

    private void OnYamlExportRequested(object? sender, EventArgs e)
    {
        try
        {
            string? path = FileDialog.SaveFile(new SaveFileDialogOptions
            {
                Owner = Graph.OwnerWindow,
                Title = "导出流程图 YAML",
                FileName = "FlowGraph.yaml",
                Filters = FileFilter.Parse("YAML 文件 (*.yaml;*.yml)|*.yaml;*.yml|所有文件 (*.*)|*.*")
            });
            if (!string.IsNullOrWhiteSpace(path))
                Graph.ExportYaml(path);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"导出 FlowGraph YAML 失败：{ex}");
        }
    }

    private void OnYamlImportRequested(object? sender, EventArgs e)
    {
        try
        {
            string? path = FileDialog.OpenFile(new OpenFileDialogOptions
            {
                Owner = Graph.OwnerWindow,
                Title = "导入流程图 YAML",
                Filters = FileFilter.Parse("YAML 文件 (*.yaml;*.yml)|*.yaml;*.yml|所有文件 (*.*)|*.*")
            });

            if (!string.IsNullOrWhiteSpace(path))
                Graph.ImportYaml(path);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"导入 FlowGraph YAML 失败：{ex}");
        }
    }

    private void OnPortWidgetClicked(object? sender, FlowGraphPortEventArgs e)
    {
        if (e.Port.Widget is not (FlowGraphWidgetKind.Edit or FlowGraphWidgetKind.Text or FlowGraphWidgetKind.MultilineText))
            return;

        _editingNodeId = e.Node.Id;
        _editingPortIndex = e.PortIndex;
        _draftText = FlowGraphControl.ToDisplayText(e.Port.Data ?? e.Port.Value);

        _editorHeading.Text = $"编辑：{e.Node.Title}";
        _editor.Text = _draftText;
        SetEditorVisible(true);
        _editor.Focus();
    }

    private void SetEditorVisible(bool visible)
    {
        if (_editorPanel.IsVisible == visible)
            return;

        _editorPanel.IsVisible = visible;
    }

    private void ApplyEditor()
    {
        if (_editingNodeId < 0 || _editingPortIndex < 0)
        {
            SetEditorVisible(false);
            return;
        }

        Graph.SetPortData(_editingNodeId, _editingPortIndex, _draftText);
        SetEditorVisible(false);
        _editingNodeId = -1;
        _editingPortIndex = -1;
        Graph.Focus();
    }

    private void CancelEditor()
    {
        SetEditorVisible(false);
        _editingNodeId = -1;
        _editingPortIndex = -1;
        Graph.Focus();
    }
}
