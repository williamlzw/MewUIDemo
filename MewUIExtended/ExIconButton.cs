
using System;

using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUI.Extended;

/// <summary>
/// ExDUIR 风格的图标按钮。
///
/// 布局：
/// 上方约 2/3：居中显示正方形图标。
/// 下方约 1/3：居中显示按钮文字。
/// </summary>
public sealed class ExIconButton : Button
{
    private readonly Image _icon;
    private readonly Label _caption;
    private readonly Grid _iconArea;

    private double _iconSize = 48;
    private double _iconTopMargin = 10;

    // ============================================================
    // Constructor
    // ============================================================

    public ExIconButton()
    {
        // 去除按钮默认内边距。
        this.Padding(0);

        // --------------------------------------------------------
        // 1. 图标
        // --------------------------------------------------------

        _icon = new Image()
            .Size(_iconSize, _iconSize)
            .StretchMode(Stretch.Uniform)
            .Center();

        // --------------------------------------------------------
        // 2. 图标区域
        //
        // 顶部增加留白。
        // Image 固定正方形，水平垂直居中。
        // --------------------------------------------------------

        _iconArea = new Grid()
            .Padding(0, _iconTopMargin, 0, 0)
            .Children(_icon);

        // --------------------------------------------------------
        // 3. 按钮文字
        // --------------------------------------------------------

        _caption = new Label()
            .TextAlignment(TextAlignment.Center)
            .VerticalTextAlignment(TextAlignment.Center)
            .FontSize(13);

        // --------------------------------------------------------
        // 4. 整体布局
        //
        // 第一行：2*
        // 第二行：*
        //
        // 图标区域占 2/3。
        // 文字区域占 1/3。
        // --------------------------------------------------------

        var layout = new Grid()
            .Rows("2*,*")
            .Columns("*")
            .Children(
                _iconArea.Row(0),
                _caption.Row(1)
            );

        Content = layout;
    }

    public ExIconButton(string iconPath, string caption)
        : this()
    {
        SetIcon(iconPath);
        Caption = caption;
    }

    // ============================================================
    // Properties
    // ============================================================

    /// <summary>
    /// 按钮显示的文字。
    /// </summary>
    public string Caption
    {
        get => _caption.Text;
        set => _caption.Text = value ?? string.Empty;
    }

    /// <summary>
    /// 图标正方形边长。
    /// 默认 48 DIP。
    /// </summary>
    public double IconSize
    {
        get => _iconSize;
        set
        {
            if (!double.IsFinite(value) || value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _iconSize = value;
            _icon.Size(value, value);
        }
    }

    /// <summary>
    /// 图标区域顶部留白。
    /// 默认 10 DIP。
    /// </summary>
    public double IconTopMargin
    {
        get => _iconTopMargin;
        set
        {
            if (!double.IsFinite(value) || value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _iconTopMargin = value;

            _iconArea.Padding(
                0,
                value,
                0,
                0);
        }
    }

    // ============================================================
    // Public API
    // ============================================================

    /// <summary>
    /// 设置按钮图标。
    /// </summary>
    public ExIconButton SetIcon(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "图标路径不能为空。",
                nameof(filePath));
        }

        _icon.SourceFile(filePath);

        return this;
    }

    /// <summary>
    /// 设置按钮文字。
    /// </summary>
    public ExIconButton SetCaption(string caption)
    {
        Caption = caption;
        return this;
    }

    /// <summary>
    /// 设置图标尺寸。
    /// </summary>
    public ExIconButton SetIconSize(double size)
    {
        IconSize = size;
        return this;
    }

    /// <summary>
    /// 设置图标顶部留白。
    /// </summary>
    public ExIconButton SetIconTopMargin(double margin)
    {
        IconTopMargin = margin;
        return this;
    }
}
