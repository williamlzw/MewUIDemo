
using System;
using System.Collections.Generic;
using System.Globalization;

using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUI.Extended;

/// <summary>
/// 属性文本输入模式。
/// </summary>
public enum PropertyGridTextInputMode
{
    /// <summary>
    /// 不限制输入。
    /// </summary>
    Any,

    /// <summary>
    /// 只允许英文字母。
    /// </summary>
    LettersOnly,

    /// <summary>
    /// 允许英文字母和数字。
    /// </summary>
    LettersAndNumbers,

    /// <summary>
    /// 只允许数字。
    /// </summary>
    NumbersOnly
}

/// <summary>
/// MewUI 属性框。
///
/// 使用原生 MewUI 控件组合实现。
///
/// 支持：
/// 1. 属性分组。
/// 2. 普通文本编辑框。
/// 3. 英文字母限制。
/// 4. 字母数字限制。
/// 5. 数字限制。
/// 6. 数值编辑框。
/// 7. 只读编辑框。
/// 8. 开关。
/// 9. 下拉列表。
/// 10. 颜色选择器。
/// 11. 日期选择器。
/// 12. 普通按钮。
/// 13. 编辑框与按钮组合。
/// 14. 获取和设置属性值。
/// 15. 属性值改变事件。
/// </summary>
public sealed class ExPropertyGrid : ContentControl
{
    

    // ============================================================
    // Internal Controls
    // ============================================================

    private StackPanel _groups;

    private StackPanel? _currentRows;

    // 保存文本编辑器，供运行时修改输入模式。
    private readonly Dictionary<string, TextBox> _textEditors =
        new(StringComparer.Ordinal);

    // 保存文本编辑器当前输入模式。
    private readonly Dictionary<string, PropertyGridTextInputMode> _textModes =
        new(StringComparer.Ordinal);

    // 属性访问器。
    private sealed class PropertyEntry
    {
        public Func<object?> GetValue { get; }

        public Action<object?> SetValue { get; }

        public PropertyEntry(
            Func<object?> getValue,
            Action<object?> setValue)
        {
            GetValue = getValue;
            SetValue = setValue;
        }
    }

    private readonly Dictionary<string, PropertyEntry> _entries =
        new(StringComparer.Ordinal);

    // ============================================================
    // Events
    // ============================================================

    /// <summary>
    /// 属性值改变事件。
    /// </summary>
    public event EventHandler<PropertyGridValueChangedEventArgs>?
        ValueChanged;

    // ============================================================
    // Appearance
    // ============================================================

    // ScrollViewer。
    private ScrollViewer _scrollViewer = null!;

    // 所有属性项目的 Border。
    private readonly List<Border> _itemBorders = new();

    // 整体背景色。
    private Color _backgroundColor = Color.Transparent;

    // 属性项目背景色。
    private Color _itemBackgroundColor = Color.Transparent;

    // 属性项目边框颜色。
    private Color _itemBorderColor = Color.Transparent;

    // 属性项目边框粗细。
    private double _itemBorderThickness = 0;

    // 属性项目圆角。
    private double _itemCornerRadius = 0;

    // ============================================================
    // Constructor
    // ============================================================

    public ExPropertyGrid()
    {
        _groups = CreateGroups();

        // 默认不显示整体边框。
        BorderThickness = 0;

        // 默认背景透明。
        Background = Color.Transparent;

        _scrollViewer = CreateScrollViewer(_groups);

        Content = _scrollViewer;
    }

    private static StackPanel CreateGroups()
    {
        return new StackPanel()
            .Vertical()
            .Spacing(2);
    }

    private ScrollViewer CreateScrollViewer(
        StackPanel groups)
    {
        return new ScrollViewer()
        .VerticalScroll(ScrollMode.Auto)
        .Background(_backgroundColor)
        .BorderThickness(0)
        .Content(groups);
    }

    // ============================================================
    // Appearance - Background
    // ============================================================

    /// <summary>
    /// 设置属性框整体背景色。
    /// </summary>
    public ExPropertyGrid SetBackgroundColor(Color color)
    {
        _backgroundColor = color;

        // 设置 ExPropertyGrid 自身背景。
        Background = color;

        // 设置滚动区域背景。
        if (_scrollViewer != null)
        {
            _scrollViewer.Background = color;
        }

        return this;
    }

    /// <summary>
    /// 设置所有属性项目的背景色。
    /// </summary>
    public ExPropertyGrid SetItemBackground(Color color)
    {
        _itemBackgroundColor = color;

        // 同步更新已经创建的属性项目。
        foreach (var item in _itemBorders)
        {
            item.Background = color;
        }

        return this;
    }

    // ============================================================
    // Appearance - Grid Border
    // ============================================================

    /// <summary>
    /// 设置属性框外围边框。
    ///
    /// thickness = 0 表示不显示边框。
    /// </summary>
    public ExPropertyGrid SetGridBorder(
        Color color,
        double thickness = 1,
        double cornerRadius = 0)
    {
        ValidateBorder(thickness, cornerRadius);

        BorderBrush = color;

        BorderThickness = thickness;

        CornerRadius = cornerRadius;

        return this;
    }

    // ============================================================
    // Appearance - Item Border
    // ============================================================

    /// <summary>
    /// 设置所有属性项目的边框。
    ///
    /// thickness = 0 表示不显示项目边框。
    /// </summary>
    public ExPropertyGrid SetItemBorder(
        Color color,
        double thickness = 1,
        double cornerRadius = 0)
    {
        ValidateBorder(thickness, cornerRadius);

        _itemBorderColor = color;

        _itemBorderThickness = thickness;

        _itemCornerRadius = cornerRadius;

        // 同步更新已经创建的属性项目。
        foreach (var item in _itemBorders)
        {
            item.BorderBrush = color;

            item.BorderThickness = thickness;

            item.CornerRadius = cornerRadius;
        }

        return this;
    }

    /// <summary>
    /// 隐藏所有属性项目边框。
    /// </summary>
    public ExPropertyGrid HideItemBorder()
    {
        return SetItemBorder(
            Color.Transparent,
            0,
            0);
    }

    /// <summary>
    /// 设置属性项目边框显示。
    /// </summary>
    public ExPropertyGrid ShowItemBorder(
        Color color,
        double thickness = 1)
    {
        return SetItemBorder(
            color,
            thickness,
            _itemCornerRadius);
    }

    // ============================================================
    // Validation
    // ============================================================

    private static void ValidateBorder(
        double thickness,
        double cornerRadius)
    {
        if (!double.IsFinite(thickness) ||
            thickness < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(thickness));
        }

        if (!double.IsFinite(cornerRadius) ||
            cornerRadius < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cornerRadius));
        }
    }

    // ============================================================
    // Group
    // ============================================================

    /// <summary>
    /// 添加属性分组。
    ///
    /// 后续添加的属性自动属于该分组。
    /// </summary>
    public ExPropertyGrid AddGroup(
        string title,
        bool expanded = true)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "分组名称不能为空。",
                nameof(title));
        }

        var rows = new StackPanel()
            .Vertical()
            .Spacing(3)
            .Padding(6, 5, 6, 5);

        var expander = new ExPropertyGridExpander()
            .Header(title)
            .IsExpanded(expanded)
            .Content(rows);
        
        _groups.Children(expander);

        _currentRows = rows;

        return this;
    }

    // ============================================================
    // TextBox
    // ============================================================

    /// <summary>
    /// 添加普通文本编辑框。
    ///
    /// 可以指定输入过滤模式。
    /// 默认不限制输入。
    /// </summary>
    public ExPropertyGrid AddText(
        string key,
        string title,
        string? value = "",
        bool readOnly = false,
        PropertyGridTextInputMode inputMode =
            PropertyGridTextInputMode.Any)
    {
        EnsureKey(key);

        var editor = new TextBox()
            .Text(value ?? string.Empty)
            .IsReadOnly(readOnly);

        if (readOnly && inputMode != PropertyGridTextInputMode.Any)
        {
            throw new ArgumentException(
                "只读文本框不需要设置输入过滤模式。",
                nameof(inputMode));
        }

        if (!readOnly)
        {
            ApplyTextInputMode(editor, inputMode);
        }

        editor.OnTextChanged(text =>
        {
            RaiseValueChanged(key, text);
        });

        AddRow(title, editor);

        Register(
            key,
            () => editor.Text,
            v => editor.Text = Convert.ToString(
                v,
                CultureInfo.InvariantCulture) ?? string.Empty);

        _textEditors.Add(key, editor);
        _textModes.Add(key, inputMode);

        return this;
    }

    /// <summary>
    /// 添加文本框，直接指定输入模式。
    /// </summary>
    public ExPropertyGrid AddText(
        string key,
        string title,
        string value,
        PropertyGridTextInputMode inputMode)
    {
        return AddText(
            key,
            title,
            value,
            false,
            inputMode);
    }

    // ============================================================
    // TextBox Convenience Methods
    // ============================================================

    /// <summary>
    /// 添加只允许英文字母的编辑框。
    /// </summary>
    public ExPropertyGrid AddLettersOnly(
        string key,
        string title,
        string value = "")
    {
        return AddText(
            key,
            title,
            value,
            PropertyGridTextInputMode.LettersOnly);
    }

    /// <summary>
    /// 添加允许字母和数字的编辑框。
    /// </summary>
    public ExPropertyGrid AddLettersAndNumbers(
        string key,
        string title,
        string value = "")
    {
        return AddText(
            key,
            title,
            value,
            PropertyGridTextInputMode.LettersAndNumbers);
    }

    /// <summary>
    /// 添加只允许数字的编辑框。
    /// </summary>
    public ExPropertyGrid AddNumbersOnly(
        string key,
        string title,
        string value = "")
    {
        return AddText(
            key,
            title,
            value,
            PropertyGridTextInputMode.NumbersOnly);
    }

    /// <summary>
    /// 应用输入过滤模式。
    ///
    /// 使用已有的 TextBoxInputExtensions。
    /// 重复调用可以切换三种输入模式。
    /// </summary>
    private static void ApplyTextInputMode(
        TextBox editor,
        PropertyGridTextInputMode mode)
    {
        switch (mode)
        {
            case PropertyGridTextInputMode.Any:
                break;

            case PropertyGridTextInputMode.LettersOnly:
                editor.EnableLettersOnly();
                break;

            case PropertyGridTextInputMode.LettersAndNumbers:
                editor.EnableLettersAndNumbers();
                break;

            case PropertyGridTextInputMode.NumbersOnly:
                editor.EnableNumbersOnly();
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(mode));
        }
    }

    /// <summary>
    /// 运行时修改指定文本框的输入模式。
    ///
    /// 支持在三种过滤模式之间切换。
    /// 注意：当前过滤扩展没有取消过滤的方法，
    /// 因此不能从已过滤状态恢复为 Any。
    /// </summary>
    public ExPropertyGrid SetTextInputMode(
        string key,
        PropertyGridTextInputMode mode)
    {
        if (!_textEditors.TryGetValue(key, out var editor))
        {
            throw new KeyNotFoundException(
                $"文本属性不存在：{key}");
        }

        if (editor.IsReadOnly)
        {
            throw new InvalidOperationException(
                $"属性 {key} 是只读文本框，不能修改输入模式。");
        }

        var oldMode = _textModes[key];

        if (mode == PropertyGridTextInputMode.Any)
        {
            if (oldMode != PropertyGridTextInputMode.Any)
            {
                throw new NotSupportedException(
                    "当前输入过滤扩展不支持取消过滤。");
            }

            return this;
        }

        ApplyTextInputMode(editor, mode);

        _textModes[key] = mode;

        return this;
    }

    // ============================================================
    // NumericUpDown
    // ============================================================

    /// <summary>
    /// 添加数值编辑框。
    /// </summary>
    public ExPropertyGrid AddNumber(
        string key,
        string title,
        double value = 0,
        double minimum = 0,
        double maximum = 100000)
    {
        EnsureKey(key);

        if (minimum > maximum)
        {
            throw new ArgumentException(
                "最小值不能大于最大值。");
        }

        var editor = new NumericUpDown()
            .Range(minimum, maximum)
            .Value(value)
            .OnValueChanged(number =>
            {
                RaiseValueChanged(key, number);
            });

        AddRow(title, editor);

        Register(
            key,
            () => editor.Value,
            v => editor.Value = Convert.ToDouble(
                v,
                CultureInfo.InvariantCulture));

        return this;
    }

    // ============================================================
    // ToggleSwitch
    // ============================================================

    /// <summary>
    /// 添加开关。
    /// </summary>
    public ExPropertyGrid AddSwitch(
        string key,
        string title,
        bool value = false)
    {
        EnsureKey(key);

        var editor = new ToggleSwitch()
            .IsChecked(value)
            .OnCheckedChanged(checkedValue =>
            {
                RaiseValueChanged(key, checkedValue);
            });

        AddRow(title, editor);

        Register(
            key,
            () => editor.IsChecked,
            v => editor.IsChecked = Convert.ToBoolean(
                v,
                CultureInfo.InvariantCulture));

        return this;
    }

    // ============================================================
    // ComboBox
    // ============================================================

    /// <summary>
    /// 添加组合框。
    /// </summary>
    public ExPropertyGrid AddComboBox(
        string key,
        string title,
        string[] options,
        int selectedValue = 0)
    {
        EnsureKey(key);

        ArgumentNullException.ThrowIfNull(options);

        var editor = new ComboBox()
            .Items(options)
            .SelectedIndex(selectedValue)
            .OnSelectionChanged(selected =>
            {
                RaiseValueChanged(key, selected);
            });

        AddRow(title, editor);

        Register(
            key,
            () => editor.SelectedItem,
            v => editor.SelectedItem = v);

        return this;
    }

    // ============================================================
    // ColorPicker
    // ============================================================

    /// <summary>
    /// 添加颜色选择器。
    /// </summary>
    public ExPropertyGrid AddColor(
        string key,
        string title,
        Color value)
    {
        EnsureKey(key);

        var editor = new ColorPicker()
            .SelectedColor(value)
            .OnSelectedColorChanged(color =>
            {
                RaiseValueChanged(key, color);
            });

        AddRow(title, editor);

        Register(
            key,
            () => editor.SelectedColor,
            v => editor.SelectedColor = RequireValue<Color>(
                v,
                key));

        return this;
    }

    // ============================================================
    // DatePicker
    // ============================================================

    /// <summary>
    /// 添加日期选择器。
    /// </summary>
    public ExPropertyGrid AddDate(
        string key,
        string title,
        DateTime? value = null)
    {
        EnsureKey(key);

        var editor = new DatePicker()
            .SelectedDate(value)
            .OnSelectedDateChanged(date =>
            {
                RaiseValueChanged(key, date);
            });

        AddRow(title, editor);

        Register(
            key,
            () => editor.SelectedDate,
            v => editor.SelectedDate = v == null
                ? null
                : RequireValue<DateTime>(v, key));

        return this;
    }

    // ============================================================
    // Button
    // ============================================================

    /// <summary>
    /// 添加普通按钮。
    /// </summary>
    public ExPropertyGrid AddButton(
        string title,
        string buttonText,
        Action? onClick = null)
    {
        var editor = new Button()
            .Content(buttonText)
            .OnClick(() => onClick?.Invoke());

        AddRow(title, editor);

        return this;
    }

    // ============================================================
    // Edit + Button
    // ============================================================

    /// <summary>
    /// 添加编辑框和按钮组合。
    /// 也支持输入限制。
    /// </summary>
    public ExPropertyGrid AddEditButton(
        string key,
        string title,
        string value,
        string buttonText,
        Action? onClick = null,
        PropertyGridTextInputMode inputMode =
            PropertyGridTextInputMode.Any)
    {
        EnsureKey(key);

        var textBox = new TextBox()
            .Text(value);

        ApplyTextInputMode(textBox, inputMode);

        textBox.OnTextChanged(text =>
        {
            RaiseValueChanged(key, text);
        });

        var button = new Button()
            .Content(buttonText)
            .OnClick(() => onClick?.Invoke());

        var editor = new StackPanel()
            .Horizontal()
            .Spacing(5)
            .Children(
                textBox,
                button);

        AddRow(title, editor);

        Register(
            key,
            () => textBox.Text,
            v => textBox.Text = Convert.ToString(
                v,
                CultureInfo.InvariantCulture) ?? string.Empty);

        _textEditors.Add(key, textBox);
        _textModes.Add(key, inputMode);

        return this;
    }

    // ============================================================
    // Row Layout
    // ============================================================

    /// <summary>
    /// 添加属性行。
    /// </summary>
    private void AddRow(
        string title,
        Element editor)
    {
        if (_currentRows == null)
        {
            throw new InvalidOperationException(
                "请先使用 AddGroup() 创建属性分组。");
        }

        // --------------------------------------------------------
        // 1. 属性名称
        // --------------------------------------------------------

        var label = new Label()
            .Text(title)
            .VerticalTextAlignment(TextAlignment.Center)
            .Padding(4, 0, 6, 0);

        // --------------------------------------------------------
        // 2. 属性行布局
        // --------------------------------------------------------

        var row = new Grid()
            .Rows("Auto")
            .Columns("120,*")
            .Children(
                label.Column(0),
                editor.Column(1))
            .Padding(4, 3, 4, 3);

        // --------------------------------------------------------
        // 3. 属性项目边框
        // --------------------------------------------------------

        var itemBorder = new Border
        {
            Child = row,

            Background = _itemBackgroundColor,

            BorderBrush = _itemBorderColor,

            BorderThickness =
                _itemBorderThickness,

            CornerRadius =
                _itemCornerRadius
        };

        // 保存 Border，以便运行时统一修改外观。
        _itemBorders.Add(itemBorder);

        // 添加到当前分组。
        _currentRows.Children(itemBorder);
    }

    // ============================================================
    // Value Management
    // ============================================================

    /// <summary>
    /// 获取属性值。
    /// </summary>
    public object? GetValue(string key)
    {
        if (!_entries.TryGetValue(key, out var entry))
        {
            throw new KeyNotFoundException(
                $"属性不存在：{key}");
        }

        return entry.GetValue();
    }

    /// <summary>
    /// 设置属性值。
    /// </summary>
    public ExPropertyGrid SetValue(
        string key,
        object? value)
    {
        if (!_entries.TryGetValue(key, out var entry))
        {
            throw new KeyNotFoundException(
                $"属性不存在：{key}");
        }

        entry.SetValue(value);

        return this;
    }

    /// <summary>
    /// 检查属性是否存在。
    /// </summary>
    public bool Contains(string key)
    {
        return _entries.ContainsKey(key);
    }

    private void Register(
        string key,
        Func<object?> getter,
        Action<object?> setter)
    {
        _entries.Add(
            key,
            new PropertyEntry(getter, setter));
    }

    private void EnsureKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                "属性 Key 不能为空。",
                nameof(key));
        }

        if (_entries.ContainsKey(key))
        {
            throw new ArgumentException(
                $"属性 Key 已存在：{key}",
                nameof(key));
        }
    }

    private static T RequireValue<T>(
        object? value,
        string key)
    {
        if (value is T result)
            return result;

        throw new ArgumentException(
            $"属性 {key} 的值类型不正确，需要 {typeof(T).Name}。");
    }

    // ============================================================
    // Value Changed
    // ============================================================

    private void RaiseValueChanged(
        string key,
        object? value)
    {
        ValueChanged?.Invoke(
            this,
            new PropertyGridValueChangedEventArgs(
                key,
                value));
    }

    // ============================================================
    // Clear
    // ============================================================

    /// <summary>
    /// 清空所有分组和属性。
    /// </summary>
    public ExPropertyGrid Clear()
    {
        _entries.Clear();

        _textEditors.Clear();

        _textModes.Clear();

        _itemBorders.Clear();

        _currentRows = null;

        // 重建分组容器。
        _groups = CreateGroups();

        _scrollViewer = CreateScrollViewer(_groups);

        Content = _scrollViewer;

        return this;
    }

    // ============================================================
    // PropertyGrid Expander
    // ============================================================

    /// <summary>
    /// 属性分组专用 Expander。
    /// </summary>
    private sealed class ExPropertyGridExpander : Expander
    {
        protected override void OnKeyUp(KeyEventArgs e)
        {
            // 空格和 Enter 是 Expander 默认的折叠快捷键。
            if (e.Key == Key.Space || e.Key == Key.Enter)
            {
                // 只有 Expander 自身获得焦点时，
                // 才允许执行原生折叠操作。
                //
                // 如果焦点在内部 TextBox、ComboBox 等控件，
                // 直接跳过 Expander 的 OnKeyUp。
                if (!IsFocused)
                    return;
            }

            base.OnKeyDown(e);
        }
    }
}

// ================================================================
// Event Arguments
// ================================================================

/// <summary>
/// 属性值改变事件参数。
/// </summary>
public sealed class PropertyGridValueChangedEventArgs : EventArgs
{
    public string Key { get; }

    public object? Value { get; }

    public PropertyGridValueChangedEventArgs(
        string key,
        object? value)
    {
        Key = key;
        Value = value;
    }
}
