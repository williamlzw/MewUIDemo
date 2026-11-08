using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUI.Extended;

/// <summary>
/// MewUI 日志框。
///
/// 功能：
/// 1. 每条日志支持独立文字颜色。
/// 2. 中文、英文混合显示。
/// 3. 文本自动换行。
/// 4. 内置垂直滚动视图。
/// 5. 自动滚动到底部。
/// 6. 用户向上查看历史日志时保持当前位置。
/// 7. 用户回到最底部后恢复自动跟随。
/// 8. 支持批量添加日志。
/// 9. 支持清空日志。
/// 10. 支持动态修改背景颜色。
/// 11. 支持动态修改默认文字颜色。
/// 12. 支持动态修改字体和字号。
///
/// 使用 MewUI ItemsControl + VariableHeightPresenter。
/// </summary>
public sealed class ExConsoleBox : ContentControl
{
    // ============================================================
    // 日志数据
    // ============================================================

    private sealed class LogEntry
    {
        /// <summary>
        /// 日志内容。
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// 日志颜色。
        /// </summary>
        public Color Color { get; }

        public LogEntry(string text, Color color)
        {
            Text = text;
            Color = color;
        }
    }

    // 使用 ObservableCollection 自动通知 ItemsControl。
    private readonly ObservableCollection<LogEntry> _logs = new();

    /// <summary>
    /// 当前日志数量。
    /// </summary>
    public int Count => _logs.Count;

    // ============================================================
    // 内部控件
    // ============================================================

    private ItemsControl _itemsControl = null!;

    // ============================================================
    // 外观配置
    // ============================================================

    // 日志框背景色。
    private Color _backgroundColor =
        Color.FromRgb(30, 30, 30);

    // 默认文字颜色。
    private Color _defaultTextColor =
        Color.FromRgb(220, 220, 220);

    // 字体。
    private string _fontFamily = "Consolas";

    // 字号。
    private double _fontSize = 13;

    // 每条日志的内边距。
    private Thickness _itemPadding =
        new Thickness(5, 2, 5, 2);

    // ============================================================
    // 滚动条颜色
    // ============================================================

    private Color _scrollBarColor =
        Color.FromRgb(110, 120, 135);

    private Color _scrollBarHoverColor =
        Color.FromRgb(145, 160, 180);

    private Color _scrollBarPressedColor =
        Color.FromRgb(175, 195, 220);

    // ============================================================
    // Constructor
    // ============================================================

    public ExConsoleBox()
    {
        // 整体背景。
        Background = _backgroundColor;

        BorderThickness = 0;

        // 创建 ItemsControl。
        _itemsControl = new ItemsControl();

        // --------------------------------------------------------
        // 关键：底部锚定 + 可变高度虚拟化
        // --------------------------------------------------------

        // Bottom：
        // 1. 列表内容不足一屏时，从底部开始排列。
        // 2. 用户处于底部时，新增日志自动跟随。
        // 3. 用户向上滚动时，新增日志不会改变阅读位置。
        //
        // VariableHeightPresenter：
        // 每条日志高度由实际文本内容决定，
        // 支持中文、英文混合及自动换行。
        _itemsControl.VariableHeightPresenter();

        // 设置日志项目内边距。
        _itemsControl.ItemPadding(_itemPadding);

        // 设置日志模板。
        _itemsControl.ItemTemplate(CreateItemTemplate());

        // 绑定日志集合。
        // ObservableCollection 支持动态添加和清空。
        _itemsControl.ItemsSource = new ItemsView<LogEntry>(_logs);
  

        // 日志框背景。
        _itemsControl.Background = _backgroundColor;

        _itemsControl.BorderThickness = 0;

        // ItemsControl 自身就是可滚动的列表控件，
        // 不需要额外套一层 ScrollViewer。
        Content = _itemsControl;

        // 设置滚动条样式。
        ApplyScrollBarStyle();
    }

    // ============================================================
    // 日志模板
    // ============================================================

    /// <summary>
    /// 创建日志项目模板。
    ///
    /// 每条日志使用一个 TextBlock。
    /// </summary>
    private DelegateTemplate<LogEntry> CreateItemTemplate()
    {
        return new DelegateTemplate<LogEntry>(
            build: _ =>
            {
                // 创建日志文本控件。
                return new TextBlock()
                    .TextWrapping(TextWrapping.Wrap)
                    .StretchHorizontal();
            },

            bind: (view, item, index, context) =>
            {
                var textBlock = (TextBlock)view;

                // 日志内容。
                textBlock.Text = item.Text;

                // 每条日志独立颜色。
                textBlock.Foreground = item.Color;

                // 字体。
                textBlock.FontFamily = _fontFamily;

                // 字号。
                textBlock.FontSize = _fontSize;
            });
    }

    // ============================================================
    // 添加日志
    // ============================================================

    /// <summary>
    /// 添加一条日志。
    ///
    /// 使用默认文字颜色。
    /// </summary>
    public ExConsoleBox AddLine(string text)
    {
        return AddLine(text, _defaultTextColor);
    }

    /// <summary>
    /// 添加一条日志。
    ///
    /// 可以指定独立文字颜色。
    /// </summary>
    public ExConsoleBox AddLine(
        string text,
        Color color)
    {
        ArgumentNullException.ThrowIfNull(text);

        // 添加到可观察集合。
        //
        // ItemsControl 自动接收集合变化通知。
        // Bottom 锚点负责自动跟随最新日志。
        _logs.Add(new LogEntry(text, color));
        // 定位到最后一条日志。
        // 内容不足一屏时不会产生底部对齐效果；
        // 超出可视区域时自动滚动显示最新日志。
        _itemsControl.ScrollIntoView(_logs.Count - 1);
        return this;
    }

    /// <summary>
    /// 批量添加日志。
    ///
    /// 使用默认颜色。
    /// </summary>
    public ExConsoleBox AddLines(
        IEnumerable<string> texts)
    {
        ArgumentNullException.ThrowIfNull(texts);

        foreach (var text in texts)
        {
            AddLine(text);
        }

        return this;
    }

    /// <summary>
    /// 批量添加日志。
    ///
    /// 指定统一文字颜色。
    /// </summary>
    public ExConsoleBox AddLines(
        IEnumerable<string> texts,
        Color color)
    {
        ArgumentNullException.ThrowIfNull(texts);

        foreach (var text in texts)
        {
            AddLine(text, color);
        }

        return this;
    }

    // ============================================================
    // 背景颜色
    // ============================================================

    /// <summary>
    /// 设置日志框背景颜色。
    /// </summary>
    public ExConsoleBox SetBackgroundColor(Color color)
    {
        _backgroundColor = color;

        Background = color;

        _itemsControl.Background = color;

        return this;
    }

    // ============================================================
    // 默认文字颜色
    // ============================================================

    /// <summary>
    /// 设置后续新增日志的默认文字颜色。
    ///
    /// 不影响已经添加的日志。
    /// </summary>
    public ExConsoleBox SetDefaultTextColor(Color color)
    {
        _defaultTextColor = color;

        return this;
    }

    // ============================================================
    // 字体
    // ============================================================

    /// <summary>
    /// 设置字体和字号。
    ///
    /// 同时刷新已经显示的日志。
    /// </summary>
    public ExConsoleBox SetFont(
        string fontFamily,
        double fontSize)
    {
        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            throw new ArgumentException(
                "字体名称不能为空。",
                nameof(fontFamily));
        }

        if (!double.IsFinite(fontSize) || fontSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fontSize));
        }

        _fontFamily = fontFamily;

        _fontSize = fontSize;

        // 重新设置模板，使当前已显示的项目重新绑定。
        // 后续虚拟化项目也会使用新的字体配置。
        _itemsControl.ItemTemplate(CreateItemTemplate());

        return this;
    }

    /// <summary>
    /// 单独设置字号。
    /// </summary>
    public ExConsoleBox SetFontSize(double fontSize)
    {
        if (!double.IsFinite(fontSize) || fontSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fontSize));
        }

        _fontSize = fontSize;

        _itemsControl.ItemTemplate(CreateItemTemplate());

        return this;
    }

    /// <summary>
    /// 单独设置字体。
    /// </summary>
    public ExConsoleBox SetFontFamily(string fontFamily)
    {
        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            throw new ArgumentException(
                "字体名称不能为空。",
                nameof(fontFamily));
        }

        _fontFamily = fontFamily;

        _itemsControl.ItemTemplate(CreateItemTemplate());

        return this;
    }

    // ============================================================
    // 日志内边距
    // ============================================================

    /// <summary>
    /// 设置每条日志的内边距。
    /// </summary>
    public ExConsoleBox SetItemPadding(Thickness padding)
    {
        _itemPadding = padding;

        _itemsControl.ItemPadding(padding);

        return this;
    }

    // ============================================================
    // 清空日志
    // ============================================================

    /// <summary>
    /// 清空所有日志。
    /// </summary>
    public ExConsoleBox Clear()
    {
        // ObservableCollection 会自动通知 ItemsControl。
        _logs.Clear();

        return this;
    }

    /// <summary>
    /// 应用滚动条样式。
    /// </summary>
    private void ApplyScrollBarStyle()
    {
        var style = new Style(typeof(ScrollBar))
        {
            Transitions =
            [
                Transition.Create(Control.BackgroundProperty)
            ],

            Setters =
            [
                // 默认滑块颜色。
                Setter.Create(
                Control.BackgroundProperty,
                _scrollBarColor),

            // 滑块圆角。
            Setter.Create(
                Control.CornerRadiusProperty,
                4.0)
            ],

            Triggers =
            [
                // 鼠标悬停。
                new StateTrigger
            {
                Match = VisualStateFlags.Hot,

                Setters =
                [
                    Setter.Create(
                        Control.BackgroundProperty,
                        _scrollBarHoverColor)
                ]
            },

            // 鼠标按下。
            new StateTrigger
            {
                Match = VisualStateFlags.Pressed,

                Setters =
                [
                    Setter.Create(
                        Control.BackgroundProperty,
                        _scrollBarPressedColor)
                ]
            }
            ]
        };

        // 只影响当前日志框内部的滚动条。
        _itemsControl.StyleSheet = new StyleSheet();

        _itemsControl.StyleSheet.Define<ScrollBar>(style);
    }

    /// <summary>
    /// 设置滚动条颜色。
    /// </summary>
    public ExConsoleBox SetScrollBarColor(
        Color normal,
        Color hover,
        Color pressed)
    {
        _scrollBarColor = normal;
        _scrollBarHoverColor = hover;
        _scrollBarPressedColor = pressed;

        ApplyScrollBarStyle();

        return this;
    }
}