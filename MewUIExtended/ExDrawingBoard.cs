
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Rendering;

namespace MewUI.Extended;

/// <summary>
/// 绘制工具。
/// </summary>
public enum ExDrawingBoardTool
{
    /// <summary>画笔。</summary>
    Pen,

    /// <summary>橡皮擦。</summary>
    Eraser
}

/// <summary>
/// 鼠标绘制板。
///
/// 功能：
/// 1. 鼠标自由绘制。
/// 2. 画笔和橡皮擦。
/// 3. 自定义画笔颜色。
/// 4. 自定义画笔和橡皮擦粗细。
/// 5. 清空画布。
/// 6. 导出 PNG 图片。
///
/// 绘制：MewUI IGraphicsContext（由 MewVG 后端执行）。
/// 导出：纯 C# PNG 编码，不依赖 Skia。
/// </summary>
public sealed class ExDrawingBoard : Control
{
    // ============================================================
    // Properties
    // ============================================================

    public static readonly MewProperty<ExDrawingBoardTool> ToolProperty =
        MewProperty<ExDrawingBoardTool>.Register<ExDrawingBoard>(
            nameof(Tool),
            ExDrawingBoardTool.Pen,
            MewPropertyOptions.AffectsRender);

    public static readonly MewProperty<Color> PenColorProperty =
        MewProperty<Color>.Register<ExDrawingBoard>(
            nameof(PenColor),
            Color.FromRgb(30, 30, 30),
            MewPropertyOptions.AffectsRender);

    public static readonly MewProperty<double> PenWidthProperty =
        MewProperty<double>.Register<ExDrawingBoard>(
            nameof(PenWidth),
            3.0,
            MewPropertyOptions.AffectsRender);

    public static readonly MewProperty<double> EraserWidthProperty =
        MewProperty<double>.Register<ExDrawingBoard>(
            nameof(EraserWidth),
            10.0,
            MewPropertyOptions.AffectsRender);

    public static readonly MewProperty<Color> CanvasBackgroundProperty =
        MewProperty<Color>.Register<ExDrawingBoard>(
            nameof(CanvasBackground),
            Color.FromRgb(255, 255, 255),
            MewPropertyOptions.AffectsRender);

    // ============================================================
    // Public Properties
    // ============================================================

    /// <summary>
    /// 当前绘制工具。
    /// </summary>
    public ExDrawingBoardTool Tool
    {
        get => GetValue(ToolProperty);
        set => SetValue(ToolProperty, value);
    }

    /// <summary>
    /// 画笔颜色。
    /// </summary>
    public Color PenColor
    {
        get => GetValue(PenColorProperty);
        set => SetValue(PenColorProperty, value);
    }

    /// <summary>
    /// 画笔粗细。
    /// 默认 3 DIP。
    /// </summary>
    public double PenWidth
    {
        get => GetValue(PenWidthProperty);
        set
        {
            ValidateWidth(value, nameof(PenWidth));
            SetValue(PenWidthProperty, value);
        }
    }

    /// <summary>
    /// 橡皮擦粗细。
    /// 默认 10 DIP。
    /// </summary>
    public double EraserWidth
    {
        get => GetValue(EraserWidthProperty);
        set
        {
            ValidateWidth(value, nameof(EraserWidth));
            SetValue(EraserWidthProperty, value);
        }
    }

    /// <summary>
    /// 画布背景颜色。
    /// 默认白色。
    /// </summary>
    public Color CanvasBackground
    {
        get => GetValue(CanvasBackgroundProperty);
        set => SetValue(CanvasBackgroundProperty, value);
    }

    /// <summary>
    /// 当前保存的笔迹数量。
    /// </summary>
    public int StrokeCount => _strokes.Count;

    // ============================================================
    // Internal State
    // ============================================================

    private readonly List<Stroke> _strokes = new();

    private Stroke? _currentStroke;

    private bool _isDrawing;

    private const double BorderWidth = 1.0;

    // ============================================================
    // Stroke Data
    // ============================================================

    /// <summary>
    /// 一次完整的鼠标绘制轨迹。
    /// 每次鼠标按下至释放形成一条 Stroke。
    /// </summary>
    private sealed class Stroke
    {
        public Color Color { get; }

        public double Width { get; }

        public List<Point> Points { get; } = new();

        public Stroke(Color color, double width)
        {
            Color = color;
            Width = width;
        }
    }

    // ============================================================
    // Constructor
    // ============================================================

    static ExDrawingBoard()
    {
        FocusableProperty.OverrideDefaultValue<ExDrawingBoard>(true);
    }

    public ExDrawingBoard()
    {
        CanvasBackground = Color.FromRgb(255, 255, 255);
        PenColor = Color.FromRgb(30, 30, 30);
    }

    // ============================================================
    // Layout
    // ============================================================

    protected override Size MeasureContent(Size availableSize)
    {
        return new Size(480, 320);
    }

    // ============================================================
    // Rendering - MewVG Backend
    // ============================================================

    protected override void OnRender(IGraphicsContext context)
    {
        // 1. 获取像素对齐后的边界。
        var bounds = GetSnappedBorderBounds(Bounds);

        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        // 2. 绘制画布背景和边框。
        DrawBackgroundAndBorder(
            context,
            bounds,
            CanvasBackground,
            Theme.Palette.ControlBorder,
            BorderWidth,
            0);

        if (_strokes.Count == 0)
            return;

        // 3. 限制笔迹绘制范围，避免覆盖画布边框。
        var clipBounds = new Rect(
            bounds.X + BorderWidth,
            bounds.Y + BorderWidth,
            Math.Max(0, bounds.Width - BorderWidth * 2),
            Math.Max(0, bounds.Height - BorderWidth * 2));

        context.Save();
        
        context.SetClipRoundedRect(
            LayoutRounding.MakeClipRect(
                clipBounds,
                context.DpiScale),
            0,
            0);

        // 4. 绘制全部笔迹。
        foreach (var stroke in _strokes)
        {
            var points = stroke.Points;

            if (points.Count == 0)
                continue;

            // 鼠标只点击一次，没有移动。
            // 画出一个很短的笔触，避免单击不显示。
            if (points.Count == 1)
            {
                var p = points[0];

                double half = Math.Max(
                    0.05,
                    stroke.Width / 4.0);
    
                context.DrawLine(
                    new Point(
                        bounds.X + p.X - half,
                        bounds.Y + p.Y),
                    new Point(
                        bounds.X + p.X + half,
                        bounds.Y + p.Y),
                    stroke.Color,
                    stroke.Width);

                continue;
            }

            // 连续绘制所有线段。
            for (int i = 1; i < points.Count; i++)
            {
                var p1 = points[i - 1];
                var p2 = points[i];

                context.DrawLine(
                    new Point(
                        bounds.X + p1.X,
                        bounds.Y + p1.Y),
                    new Point(
                        bounds.X + p2.X,
                        bounds.Y + p2.Y),
                    stroke.Color,
                    stroke.Width);
            }
        }
        
        context.Restore();
    }

    // ============================================================
    // Mouse Input
    // ============================================================

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (!IsEffectivelyEnabled ||
            e.Button != MouseButton.Left)
        {
            return;
        }

        Focus();

        // 捕获鼠标，保证移出控件后仍能接收 MouseUp。
        if (FindVisualRoot() is Window window)
        {
            window.CaptureMouse(this);
        }

        if (!IsMouseCaptured)
            return;

        // 根据当前工具确定笔迹颜色和粗细。
        Color color = Tool == ExDrawingBoardTool.Eraser
            ? CanvasBackground
            : PenColor;

        double width = Tool == ExDrawingBoardTool.Eraser
            ? EraserWidth
            : PenWidth;

        _currentStroke = new Stroke(color, width);

        _strokes.Add(_currentStroke);

        _isDrawing = true;

        // 记录起始位置。
        AddPoint(e);

        InvalidateVisual();

        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_isDrawing || _currentStroke == null)
            return;

        AddPoint(e);

        InvalidateVisual();

        e.Handled = true;
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button != MouseButton.Left || !_isDrawing)
            return;

        FinishStroke();

        // 释放鼠标捕获。
        if (FindVisualRoot() is Window window)
        {
            window.ReleaseMouseCapture();
        }

        e.Handled = true;
    }

    // ============================================================
    // Mouse Position
    // ============================================================

    private void AddPoint(MouseEventArgs e)
    {
        if (_currentStroke == null)
            return;

        var p = e.GetPosition(this);

        // 限制坐标在画布范围内。
        double x = Math.Clamp(p.X, 0, Bounds.Width);
        double y = Math.Clamp(p.Y, 0, Bounds.Height);

        var point = new Point(x, y);

        var points = _currentStroke.Points;

        if (points.Count > 0)
        {
            var previous = points[^1];

            double dx = point.X - previous.X;
            double dy = point.Y - previous.Y;

            // 过滤非常小的鼠标移动，减少无效线段。
            if (dx * dx + dy * dy < 0.16)
                return;
        }

        points.Add(point);
    }

    // ============================================================
    // Drawing State
    // ============================================================

    private void FinishStroke()
    {
        _isDrawing = false;
        _currentStroke = null;

        InvalidateVisual();
    }

    protected override void OnMewPropertyChanged(MewProperty property)
    {
        base.OnMewPropertyChanged(property);

        // 鼠标捕获意外丢失时，停止当前笔迹。
        if (property == IsMouseCapturedProperty &&
            !IsMouseCaptured)
        {
            FinishStroke();
        }
    }

    // ============================================================
    // Public API - Tool
    // ============================================================

    /// <summary>
    /// 切换为画笔模式。
    /// </summary>
    public ExDrawingBoard SetPen()
    {
        Tool = ExDrawingBoardTool.Pen;
        return this;
    }

    /// <summary>
    /// 切换为橡皮擦模式。
    /// </summary>
    public ExDrawingBoard SetEraser()
    {
        Tool = ExDrawingBoardTool.Eraser;
        return this;
    }

    /// <summary>
    /// 设置绘制工具。
    /// </summary>
    public ExDrawingBoard SetTool(ExDrawingBoardTool tool)
    {
        Tool = tool;
        return this;
    }

    // ============================================================
    // Public API - Pen
    // ============================================================

    public ExDrawingBoard SetPenColor(Color color)
    {
        PenColor = color;
        return this;
    }

    public ExDrawingBoard SetPenWidth(double width)
    {
        PenWidth = width;
        return this;
    }

    // ============================================================
    // Public API - Eraser
    // ============================================================

    public ExDrawingBoard SetEraserWidth(double width)
    {
        EraserWidth = width;
        return this;
    }

    // ============================================================
    // Public API - Background
    // ============================================================

    public ExDrawingBoard SetCanvasBackground(Color color)
    {
        CanvasBackground = color;
        return this;
    }

    // ============================================================
    // Clear
    // ============================================================

    /// <summary>
    /// 清空全部笔迹。
    /// </summary>
    public void Clear()
    {
        _strokes.Clear();

        _currentStroke = null;
        _isDrawing = false;

        if (IsMouseCaptured &&
            FindVisualRoot() is Window window)
        {
            window.ReleaseMouseCapture();
        }

        InvalidateVisual();
    }

    // ============================================================
    // Save PNG
    // ============================================================

    /// <summary>
    /// 将当前画布导出为 PNG。
    ///
    /// scale = 1：1 倍分辨率。
    /// scale = 2：2 倍分辨率。
    /// scale = 3：3 倍分辨率。
    ///
    /// 返回保存后的完整文件路径。
    /// </summary>
    public string SavePng(string filePath, double scale = 2.0)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "图片保存路径不能为空。",
                nameof(filePath));
        }

        if (!double.IsFinite(scale) ||
            scale <= 0 ||
            scale > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(scale));
        }

        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            throw new InvalidOperationException(
                "画布尚未完成布局，无法保存图片。");
        }

        int width = checked(
            (int)Math.Ceiling(Bounds.Width * scale));

        int height = checked(
            (int)Math.Ceiling(Bounds.Height * scale));

        string fullPath = Path.GetFullPath(filePath);

        string? directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        PngWriter.Save(
            fullPath,
            width,
            height,
            scale,
            CanvasBackground,
            _strokes);

        return fullPath;
    }

    // ============================================================
    // Validation
    // ============================================================

    private static void ValidateWidth(double value, string name)
    {
        if (!double.IsFinite(value) ||
            value <= 0 ||
            value > 512)
        {
            throw new ArgumentOutOfRangeException(
                name,
                "画刷粗细必须在 0 到 512 DIP 之间。");
        }
    }

    // ============================================================
    // PNG Encoder
    // Pure C# - No Skia / System.Drawing
    // ============================================================

    private static class PngWriter
    {
        private static readonly byte[] Signature =
        [
            137, 80, 78, 71, 13, 10, 26, 10
        ];

        public static void Save(
            string path,
            int width,
            int height,
            double scale,
            Color background,
            IReadOnlyList<Stroke> strokes)
        {
            if (width <= 0 || height <= 0 ||
                (long)width * height > 32_000_000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    "导出图片尺寸过大。");
            }

            // RGBA 像素。
            byte[] pixels = new byte[
                checked(width * height * 4)];

            FillBackground(
                pixels,
                background);

            // 重放所有笔迹。
            foreach (var stroke in strokes)
            {
                var points = stroke.Points;

                if (points.Count == 0)
                    continue;

                if (points.Count == 1)
                {
                    DrawSegment(
                        pixels,
                        width,
                        height,
                        points[0],
                        points[0],
                        stroke.Width,
                        stroke.Color,
                        scale);

                    continue;
                }

                for (int i = 1; i < points.Count; i++)
                {
                    DrawSegment(
                        pixels,
                        width,
                        height,
                        points[i - 1],
                        points[i],
                        stroke.Width,
                        stroke.Color,
                        scale);
                }
            }

            WritePng(path, width, height, pixels);
        }

        // --------------------------------------------------------
        // Background
        // --------------------------------------------------------

        private static void FillBackground(
            byte[] pixels,
            Color color)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = color.R;
                pixels[i + 1] = color.G;
                pixels[i + 2] = color.B;
                pixels[i + 3] = color.A;
            }
        }

        // --------------------------------------------------------
        // Rasterize Line
        // --------------------------------------------------------

        private static void DrawSegment(
            byte[] pixels,
            int width,
            int height,
            Point start,
            Point end,
            double lineWidth,
            Color color,
            double scale)
        {
            double ax = start.X * scale;
            double ay = start.Y * scale;

            double bx = end.X * scale;
            double by = end.Y * scale;

            double radius = Math.Max(
                0.5,
                lineWidth * scale / 2.0);

            double minX = Math.Min(ax, bx) - radius - 1;
            double maxX = Math.Max(ax, bx) + radius + 1;

            double minY = Math.Min(ay, by) - radius - 1;
            double maxY = Math.Max(ay, by) + radius + 1;

            int left = Math.Max(0, (int)Math.Floor(minX));
            int right = Math.Min(
                width - 1,
                (int)Math.Ceiling(maxX));

            int top = Math.Max(0, (int)Math.Floor(minY));
            int bottom = Math.Min(
                height - 1,
                (int)Math.Ceiling(maxY));

            double dx = bx - ax;
            double dy = by - ay;

            double lengthSquared = dx * dx + dy * dy;

            for (int y = top; y <= bottom; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    double px = x + 0.5;
                    double py = y + 0.5;

                    double t = 0;

                    if (lengthSquared > 0.000001)
                    {
                        t = ((px - ax) * dx +
                             (py - ay) * dy) / lengthSquared;

                        t = Math.Clamp(t, 0, 1);
                    }

                    double nearestX = ax + t * dx;
                    double nearestY = ay + t * dy;

                    double distance = Math.Sqrt(
                        (px - nearestX) * (px - nearestX) +
                        (py - nearestY) * (py - nearestY));

                    // 简单抗锯齿覆盖率。
                    double coverage = Math.Clamp(
                        radius + 0.5 - distance,
                        0,
                        1);

                    if (coverage <= 0)
                        continue;

                    int index = (y * width + x) * 4;

                    BlendPixel(
                        pixels,
                        index,
                        color,
                        coverage);
                }
            }
        }

        // --------------------------------------------------------
        // Alpha Blend
        // --------------------------------------------------------

        private static void BlendPixel(
            byte[] pixels,
            int index,
            Color color,
            double coverage)
        {
            double sourceAlpha =
                color.A / 255.0 * coverage;

            double destinationAlpha =
                pixels[index + 3] / 255.0;

            double outputAlpha =
                sourceAlpha +
                destinationAlpha * (1 - sourceAlpha);

            if (outputAlpha <= 0)
                return;

            double destinationFactor =
                destinationAlpha * (1 - sourceAlpha);

            pixels[index] = ClampByte(
                (color.R * sourceAlpha +
                 pixels[index] * destinationFactor) /
                outputAlpha);

            pixels[index + 1] = ClampByte(
                (color.G * sourceAlpha +
                 pixels[index + 1] * destinationFactor) /
                outputAlpha);

            pixels[index + 2] = ClampByte(
                (color.B * sourceAlpha +
                 pixels[index + 2] * destinationFactor) /
                outputAlpha);

            pixels[index + 3] = ClampByte(
                outputAlpha * 255);
        }

        private static byte ClampByte(double value)
        {
            return (byte)Math.Clamp(
                (int)Math.Round(value),
                0,
                255);
        }

        // --------------------------------------------------------
        // PNG File
        // --------------------------------------------------------

        private static void WritePng(
            string path,
            int width,
            int height,
            byte[] pixels)
        {
            using var stream = new FileStream(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            stream.Write(Signature);

            // IHDR
            byte[] header = new byte[13];

            BinaryPrimitives.WriteInt32BigEndian(
                header.AsSpan(0, 4),
                width);

            BinaryPrimitives.WriteInt32BigEndian(
                header.AsSpan(4, 4),
                height);

            header[8] = 8; // bit depth
            header[9] = 6; // RGBA
            header[10] = 0;
            header[11] = 0;
            header[12] = 0;

            WriteChunk(stream, "IHDR", header);

            // PNG Scanlines, filter type 0.
            using var raw = new MemoryStream();

            int rowBytes = width * 4;

            for (int y = 0; y < height; y++)
            {
                raw.WriteByte(0);

                raw.Write(
                    pixels,
                    y * rowBytes,
                    rowBytes);
            }

            // ZLIB compression.
            using var compressed = new MemoryStream();

            using (var zlib = new ZLibStream(
                compressed,
                CompressionLevel.Optimal,
                true))
            {
                raw.Position = 0;
                raw.CopyTo(zlib);
            }

            WriteChunk(
                stream,
                "IDAT",
                compressed.ToArray());

            WriteChunk(
                stream,
                "IEND",
                Array.Empty<byte>());
        }

        // --------------------------------------------------------
        // PNG Chunk
        // --------------------------------------------------------

        private static void WriteChunk(
            Stream stream,
            string name,
            byte[] data)
        {
            byte[] type = Encoding.ASCII.GetBytes(name);

            Span<byte> buffer = stackalloc byte[4];

            BinaryPrimitives.WriteInt32BigEndian(
                buffer,
                data.Length);

            stream.Write(buffer);

            stream.Write(type, 0, type.Length);

            stream.Write(data, 0, data.Length);

            uint crc = 0xFFFFFFFF;

            foreach (byte b in type)
            {
                crc = UpdateCrc(crc, b);
            }

            foreach (byte b in data)
            {
                crc = UpdateCrc(crc, b);
            }

            crc ^= 0xFFFFFFFF;

            BinaryPrimitives.WriteUInt32BigEndian(
                buffer,
                crc);

            stream.Write(buffer);
        }

        private static uint UpdateCrc(
            uint crc,
            byte value)
        {
            crc ^= value;

            for (int i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0
                    ? 0xEDB88320u ^ (crc >> 1)
                    : crc >> 1;
            }

            return crc;
        }
    }
}
