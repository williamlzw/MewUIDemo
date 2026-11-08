
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Rendering;
using System.Globalization;

namespace MewUI.Extended;

/// <summary>
/// 自绘农历日期框。
///
/// 视图模式：
/// 0 = 日期选择
/// 1 = 月份选择
/// 2 = 年份选择
///
/// 控件使用自身 Bounds 的下半部分显示展开日历。
/// 推荐尺寸：320 x 370 DIP。
/// </summary>
public sealed class ExLunarDateBox : Control
{
    // ============================================================
    // MewProperty
    // ============================================================

    public static readonly MewProperty<DateTime?> SelectedDateProperty =
        MewProperty<DateTime?>.Register<ExLunarDateBox>(
            nameof(SelectedDate),
            null,
            MewPropertyOptions.AffectsRender |
            MewPropertyOptions.BindsTwoWayByDefault,
            static (owner, oldValue, newValue) =>
            {
                if (newValue.HasValue)
                {
                    owner._displayDate = new DateTime(
                        newValue.Value.Year,
                        newValue.Value.Month,
                        1);
                }

                owner.SelectedDateChanged?.Invoke(newValue);
            });

    public static readonly MewProperty<string> DateFormatProperty =
        MewProperty<string>.Register<ExLunarDateBox>(
            nameof(DateFormat),
            "yyyy-MM-dd",
            MewPropertyOptions.AffectsRender);

    public static readonly MewProperty<string> PlaceholderProperty =
        MewProperty<string>.Register<ExLunarDateBox>(
            nameof(Placeholder),
            "请选择日期",
            MewPropertyOptions.AffectsRender);

    public static readonly MewProperty<bool> IsDropDownOpenProperty =
    MewProperty<bool>.Register<ExLunarDateBox>(
        nameof(IsDropDownOpen),
        false,
        MewPropertyOptions.AffectsRender,
        static (owner, oldValue, newValue) =>
        {
            if (newValue)
            {
                // 【修复核心】：每次打开日历面板时，强制将显示月份重置为当前已选中日期的月份。
                // 如果当前没有选中任何日期，则默认显示当前系统时间所在的月份。
                if (owner.SelectedDate.HasValue)
                {
                    owner._displayDate = new DateTime(
                        owner.SelectedDate.Value.Year,
                        owner.SelectedDate.Value.Month,
                        1);
                }
                else
                {
                    owner._displayDate = new DateTime(
                        DateTime.Today.Year,
                        DateTime.Today.Month,
                        1);
                }

                // 【体验优化】：每次打开时重置为“日期选择”视图（Month），
                // 避免用户上次在“年/月选择”视图下关闭弹窗，下次打开仍停留在该视图的问题。
                owner._mode = CalendarMode.Month;

                owner.AttachOutsideClickHandler();
            }
            else
            {
                owner.DetachOutsideClickHandler();
            }
        });

    // ============================================================
    // Properties
    // ============================================================

    public DateTime? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public string DateFormat
    {
        get => GetValue(DateFormatProperty);
        set => SetValue(DateFormatProperty, value ?? "yyyy-MM-dd");
    }

    public string Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value ?? "");
    }

    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }


    private DayOfWeek _firstDayOfWeek = DayOfWeek.Monday;
    /// <summary>
    /// 星期起始日，默认星期一。
    /// </summary>
    public DayOfWeek FirstDayOfWeek
    {
        get => _firstDayOfWeek;
        set
        {
            if (_firstDayOfWeek == value)
                return;

            _firstDayOfWeek = value;
            InvalidateVisual();
        }
    }

    /// <summary>
    /// 当前日历显示的月份。
    /// </summary>
    public DateTime DisplayDate
    {
        get => _displayDate;

        set
        {
            _displayDate = new DateTime(
                value.Year,
                value.Month,
                1);

            InvalidateVisual();
        }
    }

    // ============================================================
    // Events
    // ============================================================

    public event Action<DateTime?>? SelectedDateChanged;

    // ============================================================
    // Internal State
    // ============================================================

    // 当前注册了外部点击监听的窗口
    private Window? _outsideClickWindow;

    private enum CalendarMode
    {
        Month = 0,
        MonthSelector = 1,
        YearSelector = 2
    }

    private CalendarMode _mode = CalendarMode.Month;

    private DateTime _displayDate = DateTime.Today;

    private int _hoverIndex = -1;

    private readonly Rect[] _dayRects = new Rect[42];

    private readonly DateTime[] _dayDates = new DateTime[42];

    private readonly Rect[] _selectorRects = new Rect[12];

    private Rect _outerRect;
    private Rect _headerRect;
    private Rect _panelRect;

    private Rect _titleRect;
    private Rect _todayRect;
    private Rect _previousRect;
    private Rect _nextRect;

    private Rect _weekRect;

    // ============================================================
    // Layout Constants
    // ============================================================

    private const double HeaderHeight = 36;

    private const double PanelGap = 3;

    private const double PanelTitleHeight = 34;

    private const double WeekHeight = 22;

    private const double OuterPadding = 8;


    // ============================================================
    // Constructor
    // ============================================================

    static ExLunarDateBox()
    {
        FocusableProperty.OverrideDefaultValue<ExLunarDateBox>(true);
    }

    public ExLunarDateBox()
    {
        // 与 ExDUIR 一样，默认初始化为今天。
        SelectedDate = DateTime.Today;
        // 点击其他可聚焦控件时，失去焦点自动关闭。
        this.OnLostFocus(CloseCalendar);
    }

    // ============================================================
    // Public API
    // ============================================================

    public void OpenCalendar()
    {
        IsDropDownOpen = true;

        // 确保窗口监听已经注册。
        AttachOutsideClickHandler();

        _hoverIndex = -1;

        InvalidateVisual();
    }

    public void CloseCalendar()
    {
        IsDropDownOpen = false;

        // 移除窗口监听。
        DetachOutsideClickHandler();

        _hoverIndex = -1;

        InvalidateVisual();
    }

    public void ToggleCalendar()
    {
        if (IsDropDownOpen)
            CloseCalendar();
        else
            OpenCalendar();
    }

    public void SetDate(DateTime date)
    {
        SelectedDate = date.Date;
    }

    // ============================================================
    // Measure
    // ============================================================

    protected override Size MeasureContent(Size availableSize)
    {
        // 注意：
        // 展开面板需要在控件内部绘制，因此整个控件需要预留高度。
        return new Size(320, 370);
    }

    // ============================================================
    // Geometry
    // ============================================================

    private void UpdateGeometry()
    {
        _outerRect = GetSnappedBorderBounds(Bounds);

        _headerRect = new Rect(
            _outerRect.X,
            _outerRect.Y,
            _outerRect.Width,
            Math.Min(HeaderHeight, _outerRect.Height));

        _panelRect = new Rect(
            _outerRect.X,
            _outerRect.Y + HeaderHeight + PanelGap,
            _outerRect.Width,
            Math.Max(
                0,
                _outerRect.Height - HeaderHeight - PanelGap));

        if (_panelRect.Width <= 0 ||
            _panelRect.Height <= 0)
        {
            return;
        }

        double right = _panelRect.Right - OuterPadding;

        double titleY = _panelRect.Y + 4;

        double buttonHeight = 26;

        double todayWidth = 46;
        double arrowWidth = 26;

        // 今天按钮。
        _todayRect = new Rect(
            right - todayWidth,
            titleY,
            todayWidth,
            buttonHeight);

        // 下一页。
        _nextRect = new Rect(
            _todayRect.X - arrowWidth,
            titleY,
            arrowWidth,
            buttonHeight);

        // 上一页。
        _previousRect = new Rect(
            _nextRect.X - arrowWidth,
            titleY,
            arrowWidth,
            buttonHeight);

        // 年月标题。
        _titleRect = new Rect(
            _panelRect.X + OuterPadding,
            titleY,
            Math.Max(
                0,
                _previousRect.X -
                _panelRect.X -
                OuterPadding * 2),
            buttonHeight);

        if (_mode == CalendarMode.Month)
        {
            UpdateDayGeometry();
        }
        else
        {
            UpdateSelectorGeometry();
        }
    }

    private void UpdateDayGeometry()
    {
        double gridX = _panelRect.X + OuterPadding;

        double gridWidth =
            _panelRect.Width - OuterPadding * 2;

        double weekY =
            _panelRect.Y + PanelTitleHeight;

        _weekRect = new Rect(
            gridX,
            weekY,
            gridWidth,
            WeekHeight);

        double startY = weekY + WeekHeight;

        double gridHeight = Math.Max(
            0,
            _panelRect.Bottom - OuterPadding - startY);

        double cellWidth = gridWidth / 7.0;
        double cellHeight = gridHeight / 6.0;

        // 本月1号。
        DateTime firstDay = new DateTime(
            _displayDate.Year,
            _displayDate.Month,
            1);

        // 计算从星期一开始的偏移。
        int firstOffset =
            ((int)firstDay.DayOfWeek -
             (int)FirstDayOfWeek +
             7) % 7;

        DateTime startDate;

        try
        {
            startDate = firstDay.AddDays(-firstOffset);
        }
        catch (ArgumentOutOfRangeException)
        {
            startDate = DateTime.MinValue;
        }

        for (int i = 0; i < 42; i++)
        {
            int row = i / 7;
            int col = i % 7;

            _dayRects[i] = new Rect(
                gridX + col * cellWidth,
                startY + row * cellHeight,
                cellWidth,
                cellHeight);

            try
            {
                _dayDates[i] = startDate.AddDays(i);
            }
            catch (ArgumentOutOfRangeException)
            {
                _dayDates[i] = DateTime.MaxValue.Date;
            }
        }
    }

    private void UpdateSelectorGeometry()
    {
        double gridX = _panelRect.X + OuterPadding;

        double gridY =
            _panelRect.Y + PanelTitleHeight + 5;

        double gridWidth =
            _panelRect.Width - OuterPadding * 2;

        double gridHeight = Math.Max(
            0,
            _panelRect.Bottom - OuterPadding - gridY);

        double cellWidth = gridWidth / 4.0;
        double cellHeight = gridHeight / 3.0;

        for (int i = 0; i < 12; i++)
        {
            int row = i / 4;
            int col = i % 4;

            _selectorRects[i] = new Rect(
                gridX + col * cellWidth,
                gridY + row * cellHeight,
                cellWidth,
                cellHeight);
        }
    }

    // ============================================================
    // Rendering
    // ============================================================

    protected override void OnRender(IGraphicsContext context)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        // 必须先计算布局，再使用 _outerRect。
        UpdateGeometry();

        DrawHeader(context);

        if (!IsDropDownOpen)
            return;

        DrawCalendarPanel(context);
    }

    // ============================================================
    // Header
    // ============================================================

    private void DrawHeader(IGraphicsContext context)
    {
        var palette = Theme.Palette;

        DrawBackgroundAndBorder(
            context,
            _headerRect,
            palette.ControlBackground,
            BorderBrush,
            BorderThickness,
            CornerRadius);

        string text;

        if (SelectedDate.HasValue)
        {
            DateTime date = SelectedDate.Value;

            string solar = date.ToString(
                DateFormat,
                CultureInfo.CurrentCulture);

            string lunar = ChineseLunarText.GetFullText(date);

            text = string.IsNullOrEmpty(lunar)
                ? solar
                : $"{solar}  {lunar}";
        }
        else
        {
            text = Placeholder;
        }

        var textRect = new Rect(
            _headerRect.X + 10,
            _headerRect.Y,
            Math.Max(0, _headerRect.Width - 42),
            _headerRect.Height);

        DrawEngineText(
            context,
            text,
            textRect,
            IsEffectivelyEnabled
                ? Foreground
                : palette.DisabledText,
            TextAlignment.Left,
            TextAlignment.Center,
            TextWrapping.NoWrap);

        var arrowRect = new Rect(
            _headerRect.Right - 34,
            _headerRect.Y,
            26,
            _headerRect.Height);

        DrawEngineText(
            context,
            IsDropDownOpen ? "▲" : "▼",
            arrowRect,
            palette.DisabledText,
            TextAlignment.Center,
            TextAlignment.Center,
            TextWrapping.NoWrap);
    }

    // ============================================================
    // Calendar Panel
    // ============================================================

    private void DrawCalendarPanel(IGraphicsContext context)
    {
        var palette = Theme.Palette;

        DrawBackgroundAndBorder(
            context,
            _panelRect,
            palette.ControlBackground,
            palette.ControlBorder,
            1.0,
            4.0);

        DrawPanelTitle(context);

        switch (_mode)
        {
            case CalendarMode.Month:
                DrawMonthView(context);
                break;

            case CalendarMode.MonthSelector:
                DrawMonthSelector(context);
                break;

            case CalendarMode.YearSelector:
                DrawYearSelector(context);
                break;
        }
    }

    // ============================================================
    // Panel Title
    // ============================================================

    private void DrawPanelTitle(IGraphicsContext context)
    {
        var palette = Theme.Palette;

        string title = _mode switch
        {
            CalendarMode.Month =>
                $"{_displayDate.Year}年{_displayDate.Month}月",

            CalendarMode.MonthSelector =>
                $"{_displayDate.Year}年",

            CalendarMode.YearSelector =>
                $"{_displayDate.Year - 1}-{_displayDate.Year + 10}年",

            _ => ""
        };

        DrawEngineText(
            context,
            title,
            _titleRect,
            Foreground,
            TextAlignment.Left,
            TextAlignment.Center,
            TextWrapping.NoWrap);

        DrawEngineText(
            context,
            "‹",
            _previousRect,
            palette.WindowText,
            TextAlignment.Center,
            TextAlignment.Center,
            TextWrapping.NoWrap);

        DrawEngineText(
            context,
            "›",
            _nextRect,
            palette.WindowText,
            TextAlignment.Center,
            TextAlignment.Center,
            TextWrapping.NoWrap);

        DrawEngineText(
            context,
            "今天",
            _todayRect,
            palette.Accent,
            TextAlignment.Center,
            TextAlignment.Center,
            TextWrapping.NoWrap);
    }

    // ============================================================
    // Month View
    // ============================================================

    private void DrawMonthView(IGraphicsContext context)
    {
        var palette = Theme.Palette;

        string[] weekdays =
        {
            "日", "一", "二", "三", "四", "五", "六"
        };

        // 星期标题。
        double weekWidth = _weekRect.Width / 7.0;

        for (int i = 0; i < 7; i++)
        {
            int weekdayIndex =
       ((int)FirstDayOfWeek + i) % 7;

            var rect = new Rect(
                _weekRect.X + i * weekWidth,
                _weekRect.Y,
                weekWidth,
                _weekRect.Height);

            DrawEngineText(
                context,
                weekdays[weekdayIndex],
                rect,
                Theme.Palette.DisabledText,
                TextAlignment.Center,
                TextAlignment.Center,
                TextWrapping.NoWrap);
        }

        // 42个日期单元格。
        for (int i = 0; i < 42; i++)
        {
            DrawDayCell(
                context,
                i,
                _dayRects[i],
                _dayDates[i]);
        }
    }

    // ============================================================
    // Day Cell
    // ============================================================

    private void DrawDayCell(
        IGraphicsContext context,
        int index,
        Rect cell,
        DateTime date)
    {
        var palette = Theme.Palette;

        bool isOtherMonth =
            date.Year != _displayDate.Year ||
            date.Month != _displayDate.Month;

        bool isToday =
            date.Date == DateTime.Today;

        bool isSelected =
            !isOtherMonth &&
            SelectedDate.HasValue &&
            SelectedDate.Value.Date == date.Date;

        bool isHover = _hoverIndex == index;

        Rect tile = new Rect(
            cell.X + 2,
            cell.Y + 2,
            Math.Max(0, cell.Width - 4),
            Math.Max(0, cell.Height - 4));

        // 鼠标悬停背景。
        if (isHover)
        {
            DrawBackgroundAndBorder(
                context,
                tile,
                palette.ButtonHoverBackground,
                Color.Transparent,
                0,
                4);
        }
        // 今天背景。
        else if (isToday)
        {
            Color todayColor =
                palette.ControlBackground.Lerp(
                    palette.Accent,
                    0.16);

            DrawBackgroundAndBorder(
                context,
                tile,
                todayColor,
                Color.Transparent,
                0,
                4);
        }

        // 选中日期蓝色边框。
        if (isSelected)
        {
            DrawBackgroundAndBorder(
                context,
                tile,
                Color.Transparent,
                palette.Accent,
                1.8,
                4);
        }

        Color solarColor = isOtherMonth
            ? palette.DisabledText
            : Foreground;

        Color lunarColor = isOtherMonth
            ? palette.DisabledText
            : palette.PlaceholderText;

        if (isToday && !isSelected)
        {
            solarColor = palette.Accent;
        }

        string solarText =
            date.Day.ToString(CultureInfo.InvariantCulture);

        string lunarText =
            ChineseLunarText.GetCellText(date);

        // 公历日期。
        Rect solarRect = new Rect(
            cell.X,
            cell.Y + 1,
            cell.Width,
            Math.Min(19, cell.Height));

        DrawEngineText(
            context,
            solarText,
            solarRect,
            solarColor,
            TextAlignment.Center,
            TextAlignment.Center,
            TextWrapping.NoWrap);

        // 农历日期。
        Rect lunarRect = new Rect(
            cell.X + 1,
            cell.Y + 19,
            Math.Max(0, cell.Width - 2),
            Math.Max(0, cell.Height - 19));

        DrawEngineText(
            context,
            lunarText,
            lunarRect,
            lunarColor,
            TextAlignment.Center,
            TextAlignment.Center,
            TextWrapping.NoWrap);
    }

    // ============================================================
    // Month Selector
    // ============================================================

    private void DrawMonthSelector(IGraphicsContext context)
    {
        var palette = Theme.Palette;

        for (int i = 0; i < 12; i++)
        {
            Rect cell = _selectorRects[i];

            bool selected = i + 1 == _displayDate.Month;

            DrawSelectorCell(
                context,
                i,
                cell,
                $"{i + 1}月",
                selected
                );
        }
    }

    // ============================================================
    // Year Selector
    // ============================================================

    private void DrawYearSelector(IGraphicsContext context)
    {
        var palette = Theme.Palette;

        for (int i = 0; i < 12; i++)
        {
            int year = _displayDate.Year - 1 + i;

            Rect cell = _selectorRects[i];

            bool valid = year >= 1 && year <= 9999;

            bool selected =
                valid && year == _displayDate.Year;

            DrawSelectorCell(
                context,
                i,
                cell,
                valid ? $"{year}年" : "",
                selected
                );
        }
    }

    // ============================================================
    // Selector Cell
    // ============================================================

    private void DrawSelectorCell(
    IGraphicsContext context,
    int index,
    Rect cell,
    string text,
    bool selected)
    {
        var palette = Theme.Palette;

        Rect tile = new Rect(
            cell.X + 3,
            cell.Y + 3,
            Math.Max(0, cell.Width - 6),
            Math.Max(0, cell.Height - 6));

        if (_hoverIndex == index)
        {
            DrawBackgroundAndBorder(
                context,
                tile,
                palette.ButtonHoverBackground,
                Color.Transparent,
                0,
                5);
        }

        if (selected)
        {
            DrawBackgroundAndBorder(
                context,
                tile,
                Color.Transparent,
                palette.Accent,
                1.8,
                5);
        }

        DrawEngineText(
            context,
            text,
            cell,
            selected ? palette.Accent : Foreground,
            TextAlignment.Center,
            TextAlignment.Center,
            TextWrapping.NoWrap);
    }

    // ============================================================
    // Navigation
    // ============================================================

    private void ChangePage(int direction)
    {
        try
        {
            switch (_mode)
            {
                // 日期视图：上一月、下一月。
                case CalendarMode.Month:

                    _displayDate = _displayDate.AddMonths(direction);

                    break;

                // 月份视图：上一年、下一年。
                case CalendarMode.MonthSelector:

                    _displayDate = _displayDate.AddYears(direction);

                    break;

                // 年份视图：上一组12年、下一组12年。
                case CalendarMode.YearSelector:

                    _displayDate = _displayDate.AddYears(direction * 12);

                    break;
            }

            _hoverIndex = -1;

            InvalidateVisual();
        }
        catch (ArgumentOutOfRangeException)
        {
            // 到达 DateTime 支持范围的边界，不再继续翻页。
        }
    }

    private void CycleView()
    {
        _mode = _mode switch
        {
            CalendarMode.Month =>
                CalendarMode.MonthSelector,

            CalendarMode.MonthSelector =>
                CalendarMode.YearSelector,

            _ =>
                CalendarMode.Month
        };

        _hoverIndex = -1;

        InvalidateVisual();
    }

    // ============================================================
    // Hit Test
    // ============================================================

    private Point GetAbsolutePosition(MouseEventArgs e)
    {
        Point local = e.GetPosition(this);

        return new Point(
            Bounds.X + local.X,
            Bounds.Y + local.Y);
    }

    private int HitTestDay(Point point)
    {
        for (int i = 0; i < 42; i++)
        {
            if (_dayRects[i].Contains(point))
                return i;
        }

        return -1;
    }

    private int HitTestSelector(Point point)
    {
        for (int i = 0; i < 12; i++)
        {
            if (_selectorRects[i].Contains(point))
                return i;
        }

        return -1;
    }

    // ============================================================
    // Select Date
    // ============================================================

    private void SelectDate(DateTime date)
    {
        _displayDate = new DateTime(
            date.Year,
            date.Month,
            1);

        _mode = CalendarMode.Month;

        _hoverIndex = -1;

        SelectedDate = date.Date;

        CloseCalendar();
    }

    // ============================================================
    // Today
    // ============================================================

    private void NavigateToday()
    {
        DateTime today = DateTime.Today;

        _displayDate = new DateTime(
            today.Year,
            today.Month,
            1);

        // 与 C++ 一样：
        // 今天按钮主要负责跳转到今天所在月份，
        // 不强制改变已经保存的选择值。
        _mode = CalendarMode.Month;

        _hoverIndex = -1;

        InvalidateVisual();
    }

    // ============================================================
    // Mouse Down
    // ============================================================

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Handled ||
            !IsEffectivelyEnabled ||
            e.Button != MouseButton.Left)
        {
            return;
        }

        Focus();

        UpdateGeometry();

        Point point = GetAbsolutePosition(e);

        // 点击日期框本身，打开或关闭日历。
        if (_headerRect.Contains(point))
        {
            ToggleCalendar();

            e.Handled = true;

            return;
        }

        if (!IsDropDownOpen)
            return;

        // 当前控件内部，弹层以外的区域。
        if (!_panelRect.Contains(point))
        {
            CloseCalendar();

            e.Handled = true;

            return;
        }

        // 今天。
        if (_todayRect.Contains(point))
        {
            NavigateToday();

            e.Handled = true;

            return;
        }

        // 上一页。
        if (_previousRect.Contains(point))
        {
            ChangePage(-1);

            e.Handled = true;

            return;
        }

        // 下一页。
        if (_nextRect.Contains(point))
        {
            ChangePage(1);

            e.Handled = true;

            return;
        }

        // 点击年月标题，切换视图。
        if (_titleRect.Contains(point))
        {
            CycleView();

            e.Handled = true;

            return;
        }

        // --------------------------------------------------------
        // 日期选择
        // --------------------------------------------------------

        if (_mode == CalendarMode.Month)
        {
            int hit = HitTestDay(point);

            if (hit >= 0)
            {
                DateTime date = _dayDates[hit];

                // 只允许选择当前月份。
                // 上月、下月补充出来的日期仅用于显示。
                if (date.Year == _displayDate.Year &&
                    date.Month == _displayDate.Month)
                {
                    SelectDate(date);
                }

                e.Handled = true;
            }

            return;
        }

        // --------------------------------------------------------
        // 月份选择
        // --------------------------------------------------------

        if (_mode == CalendarMode.MonthSelector)
        {
            int hit = HitTestSelector(point);

            if (hit >= 0)
            {
                _displayDate = new DateTime(
                    _displayDate.Year,
                    hit + 1,
                    1);

                _mode = CalendarMode.Month;

                _hoverIndex = -1;

                InvalidateVisual();

                e.Handled = true;
            }

            return;
        }

        // --------------------------------------------------------
        // 年份选择
        // --------------------------------------------------------

        if (_mode == CalendarMode.YearSelector)
        {
            int hit = HitTestSelector(point);

            if (hit >= 0)
            {
                int year = _displayDate.Year - 1 + hit;

                if (year >= 1 && year <= 9999)
                {
                    _displayDate = new DateTime(
                        year,
                        _displayDate.Month,
                        1);

                    _mode = CalendarMode.MonthSelector;

                    _hoverIndex = -1;

                    InvalidateVisual();
                }

                e.Handled = true;
            }
        }
    }

    // ============================================================
    // Mouse Move
    // ============================================================

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!IsDropDownOpen)
            return;

        UpdateGeometry();

        Point point = GetAbsolutePosition(e);

        int newHover = -1;

        if (_panelRect.Contains(point))
        {
            newHover = _mode switch
            {
                CalendarMode.Month =>
                    HitTestDay(point),

                CalendarMode.MonthSelector =>
                    HitTestSelector(point),

                CalendarMode.YearSelector =>
                    HitTestSelector(point),

                _ => -1
            };
        }

        if (newHover != _hoverIndex)
        {
            _hoverIndex = newHover;

            InvalidateVisual();
        }
    }

    // ============================================================
    // Mouse Leave
    // ============================================================

    protected override void OnMouseLeave()
    {
        base.OnMouseLeave();

        if (_hoverIndex != -1)
        {
            _hoverIndex = -1;

            InvalidateVisual();
        }
    }

    // ============================================================
    // Keyboard
    // ============================================================

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled ||
            !IsEffectivelyEnabled)
        {
            return;
        }

        if (!IsDropDownOpen)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Space)
            {
                OpenCalendar();

                e.Handled = true;
            }

            return;
        }

        if (e.Key == Key.Escape)
        {
            CloseCalendar();

            e.Handled = true;

            return;
        }

        DateTime current =
            SelectedDate ?? _displayDate;

        DateTime next;

        try
        {
            switch (e.Key)
            {
                case Key.Left:
                    next = current.AddDays(-1);
                    break;

                case Key.Right:
                    next = current.AddDays(1);
                    break;

                case Key.Up:
                    next = current.AddDays(-7);
                    break;

                case Key.Down:
                    next = current.AddDays(7);
                    break;

                case Key.Enter:
                    CloseCalendar();
                    e.Handled = true;
                    return;

                default:
                    return;
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }

        _mode = CalendarMode.Month;

        _displayDate = new DateTime(
            next.Year,
            next.Month,
            1);

        SelectedDate = next.Date;

        e.Handled = true;
    }

    // ============================================================
    // Outside Mouse Click
    // ============================================================

    /// <summary>
    /// 注册所属窗口的鼠标按下事件。
    /// </summary>
    private void AttachOutsideClickHandler()
    {
        if (FindVisualRoot() is not Window window)
            return;

        if (ReferenceEquals(_outsideClickWindow, window))
            return;

        DetachOutsideClickHandler();

        _outsideClickWindow = window;

        // 监听整个窗口冒泡上来的 MouseDown。
        window.MouseDown += OnWindowMouseDown;
    }

    /// <summary>
    /// 移除窗口鼠标事件监听。
    /// </summary>
    private void DetachOutsideClickHandler()
    {
        if (_outsideClickWindow == null)
            return;

        _outsideClickWindow.MouseDown -= OnWindowMouseDown;

        _outsideClickWindow = null;
    }

    /// <summary>
    /// 窗口级别鼠标事件。
    /// </summary>
    private void OnWindowMouseDown(MouseEventArgs e)
    {
        if (!IsDropDownOpen)
            return;

        if (!IsEffectivelyEnabled)
            return;

        // 更新面板几何位置。
        UpdateGeometry();

        // 获取鼠标相对于日期框的位置，再转换成窗口坐标。
        Point point = GetAbsolutePosition(e);

        // 点击日期框或者日历面板内部，不关闭。
        if (_headerRect.Contains(point) ||
            _panelRect.Contains(point))
        {
            return;
        }

        // 点击其他位置，关闭面板。
        CloseCalendar();
    }
}


// ================================================================
// 中国农历工具
// ================================================================

internal static class ChineseLunarText
{
    private static readonly ChineseLunisolarCalendar Calendar = new();

    // ============================================================
    // 农历月份
    // ============================================================

    private static readonly string[] MonthNames =
    {
        "正", "二", "三", "四",
        "五", "六", "七", "八",
        "九", "十", "冬", "腊"
    };

    // ============================================================
    // 农历日期
    // ============================================================

    private static readonly string[] DayNames =
    {
        "初一", "初二", "初三", "初四", "初五",
        "初六", "初七", "初八", "初九", "初十",

        "十一", "十二", "十三", "十四", "十五",
        "十六", "十七", "十八", "十九", "二十",

        "廿一", "廿二", "廿三", "廿四", "廿五",
        "廿六", "廿七", "廿八", "廿九", "三十"
    };

    // ============================================================
    // 公历节日
    //
    // 参考 C++ Chjrmc 数组。
    // ============================================================

    private static string GetSolarFestival(DateTime date)
    {
        return (date.Month, date.Day) switch
        {
            (1, 1) => "元旦",
            (2, 14) => "情人",
            (3, 8) => "妇女",
            (3, 12) => "植树",
            (4, 1) => "愚人",
            (5, 1) => "劳动",
            (5, 4) => "青年",
            (5, 12) => "护士",
            (6, 1) => "儿童",
            (7, 1) => "建党",
            (8, 1) => "建军",
            (9, 10) => "教师",
            (10, 1) => "国庆",
            (12, 24) => "平安",
            (12, 25) => "圣诞",

            _ => ""
        };
    }

    // ============================================================
    // 二十四节气
    //
    // 对应 C++ Chjqmc 数组。
    //
    // 标准计算顺序：
    // 0  小寒
    // 1  大寒
    // 2  立春
    // 3  雨水
    // 4  惊蛰
    // 5  春分
    // 6  清明
    // 7  谷雨
    // 8  立夏
    // 9  小满
    // 10 芒种
    // 11 夏至
    // 12 小暑
    // 13 大暑
    // 14 立秋
    // 15 处暑
    // 16 白露
    // 17 秋分
    // 18 寒露
    // 19 霜降
    // 20 立冬
    // 21 小雪
    // 22 大雪
    // 23 冬至
    // ============================================================

    private static readonly string[] SolarTermNames =
    {
        "小寒", "大寒",
        "立春", "雨水",
        "惊蛰", "春分",
        "清明", "谷雨",
        "立夏", "小满",
        "芒种", "夏至",
        "小暑", "大暑",
        "立秋", "处暑",
        "白露", "秋分",
        "寒露", "霜降",
        "立冬", "小雪",
        "大雪", "冬至"
    };

    /// <summary>
    /// 获取指定年份某个节气对应的北京时间日期。
    ///
    /// 根据太阳视黄经计算，不使用固定分钟偏移表。
    ///
    /// termIndex:
    /// 0  小寒  285°
    /// 1  大寒  300°
    /// 2  立春  315°
    /// ...
    /// 20 立冬  225°
    /// 21 小雪  240°
    /// 22 大雪  255°
    /// 23 冬至  270°
    /// </summary>
    private static DateTime GetSolarTermDate(
        int year,
        int termIndex)
    {
        if (termIndex < 0 || termIndex >= 24)
            throw new ArgumentOutOfRangeException(nameof(termIndex));

        // 太阳黄经目标值。
        double targetLongitude =
            (285.0 + termIndex * 15.0) % 360.0;

        // 以1月5日为起点，估算节气时间。
        DateTime estimate = new DateTime(
            year, 1, 5,
            0, 0, 0,
            DateTimeKind.Utc);

        estimate = estimate.AddDays(
            termIndex * 365.2422 / 24.0);

        // 前后5天搜索交节点。
        DateTime low = estimate.AddDays(-5);
        DateTime high = estimate.AddDays(5);

        double lowDiff = NormalizeSignedAngle(
            GetApparentSolarLongitude(low) - targetLongitude);

        double highDiff = NormalizeSignedAngle(
            GetApparentSolarLongitude(high) - targetLongitude);

        if (lowDiff > 0 || highDiff < 0)
        {
            throw new InvalidOperationException(
                $"无法计算 {year} 年 {SolarTermNames[termIndex]} 的交节时间。");
        }

        // 二分法求解太阳黄经到达目标角度的时刻。
        for (int i = 0; i < 50; i++)
        {
            long ticks = low.Ticks +
                (high.Ticks - low.Ticks) / 2;

            DateTime middle = new DateTime(
                ticks,
                DateTimeKind.Utc);

            double currentDiff = NormalizeSignedAngle(
                GetApparentSolarLongitude(middle) -
                targetLongitude);

            if (currentDiff < 0)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        // 转换为中国标准时间 UTC+8。
        DateTime utc = low.AddTicks(
            (high.Ticks - low.Ticks) / 2);

        return utc.AddHours(8).Date;
    }

    /// <summary>
    /// 计算太阳视黄经。
    ///
    /// 使用太阳地心视黄经近似计算方法。
    /// </summary>
    private static double GetApparentSolarLongitude(
        DateTime utc)
    {
        // 儒略日。
        double jd =
            1721425.5 +
            utc.Ticks / (double)TimeSpan.TicksPerDay;

        // 儒略世纪。
        double T = (jd - 2451545.0) / 36525.0;

        // 太阳平黄经。
        double L0 =
            280.46646 +
            T * (36000.76983 + 0.0003032 * T);

        L0 = NormalizeAngle(L0);

        // 太阳平近点角。
        double M =
            357.52911 +
            T * (35999.05029 - 0.0001537 * T);

        double Mr = DegreesToRadians(M);

        // 地球轨道偏心率修正项。
        double C =
            Math.Sin(Mr) *
            (1.914602 - T * (0.004817 + 0.000014 * T))

            + Math.Sin(2 * Mr) *
            (0.019993 - 0.000101 * T)

            + Math.Sin(3 * Mr) *
            0.000289;

        // 太阳真黄经。
        double trueLongitude = L0 + C;

        // 章动及光行差修正。
        double omega =
            DegreesToRadians(125.04 - 1934.136 * T);

        // 太阳视黄经。
        double apparentLongitude =
            trueLongitude
            - 0.00569
            - 0.00478 * Math.Sin(omega);

        return NormalizeAngle(apparentLongitude);
    }

    /// <summary>
    /// 将角度归一化到 0~360 度。
    /// </summary>
    private static double NormalizeAngle(double angle)
    {
        angle %= 360.0;

        if (angle < 0)
            angle += 360.0;

        return angle;
    }

    /// <summary>
    /// 将角度归一化到 -180~180 度。
    /// </summary>
    private static double NormalizeSignedAngle(double angle)
    {
        angle %= 360.0;

        if (angle > 180.0)
            angle -= 360.0;
        else if (angle < -180.0)
            angle += 360.0;

        return angle;
    }

    /// <summary>
    /// 角度转换为弧度。
    /// </summary>
    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    /// <summary>
    /// 获取指定日期的二十四节气名称。
    /// </summary>
    private static string GetSolarTerm(DateTime date)
    {
        date = date.Date;

        for (int i = 0; i < 24; i++)
        {
            if (GetSolarTermDate(date.Year, i) == date)
            {
                return SolarTermNames[i];
            }
        }

        return "";
    }

    // ============================================================
    // 农历日期解析
    // ============================================================

    private static bool TryGetParts(
        DateTime date,
        out int lunarYear,
        out int lunarMonth,
        out int lunarDay,
        out bool isLeapMonth,
        out int rawMonth)
    {
        lunarYear = 0;
        lunarMonth = 0;
        lunarDay = 0;
        isLeapMonth = false;
        rawMonth = 0;

        date = date.Date;

        if (date < Calendar.MinSupportedDateTime.Date ||
            date > Calendar.MaxSupportedDateTime.Date)
        {
            return false;
        }

        try
        {
            lunarYear = Calendar.GetYear(date);

            rawMonth = Calendar.GetMonth(date);

            int leapMonth =
                Calendar.GetLeapMonth(lunarYear);

            isLeapMonth =
                leapMonth > 0 &&
                rawMonth == leapMonth;

            // 闰月及后续月份修正。
            lunarMonth = rawMonth;

            if (leapMonth > 0 &&
                rawMonth >= leapMonth)
            {
                lunarMonth--;
            }

            lunarDay = Calendar.GetDayOfMonth(date);

            return lunarMonth >= 1 &&
                   lunarMonth <= 12 &&
                   lunarDay >= 1 &&
                   lunarDay <= 30;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    // ============================================================
    // 农历传统节日
    //
    // 参考 C++ Chjrmc 数组。
    // ============================================================

    private static string GetLunarFestival(
        int month,
        int day,
        int daysInMonth)
    {
        return (month, day) switch
        {
            (1, 1) => "春节",
            (1, 15) => "元宵",

            (2, 2) => "龙抬头",

            (5, 5) => "端午",

            (7, 7) => "七夕",
            (7, 15) => "祭祖",

            (8, 15) => "中秋",

            (9, 9) => "重阳",

            (12, 8) => "腊八",
            (12, 23) => "小年",

            _ => month == 12 && day == daysInMonth
                ? "除夕"
                : ""
        };
    }

    // ============================================================
    // 获取指定日期的节日或节气名称
    //
    // 优先级：
    // 公历节日 > 二十四节气 > 农历节日。
    // ============================================================

    private static string GetSpecialDayText(DateTime date)
    {
        date = date.Date;

        // 1. 公历节日。
        string solarFestival =
            GetSolarFestival(date);

        if (!string.IsNullOrEmpty(solarFestival))
            return solarFestival;

        // 2. 二十四节气。
        string solarTerm =
            GetSolarTerm(date);

        if (!string.IsNullOrEmpty(solarTerm))
            return solarTerm;

        // 3. 农历节日。
        if (!TryGetParts(
            date,
            out int year,
            out int month,
            out int day,
            out bool isLeap,
            out int rawMonth))
        {
            return "";
        }

        // 闰月不使用普通月份节日。
        if (isLeap)
            return "";

        int daysInMonth =
            Calendar.GetDaysInMonth(year, rawMonth);

        return GetLunarFestival(
            month,
            day,
            daysInMonth);
    }

    // ============================================================
    // 日期格显示
    // ============================================================

    /// <summary>
    /// 获取日期格中的农历文字。
    ///
    /// 例如：
    /// 元旦、春节、清明、立春、十五、正月等。
    /// </summary>
    public static string GetCellText(DateTime date)
    {
        date = date.Date;

        // 优先显示节日或节气。
        string specialText =
            GetSpecialDayText(date);

        if (!string.IsNullOrEmpty(specialText))
            return specialText;

        if (!TryGetParts(
            date,
            out _,
            out int month,
            out int day,
            out bool isLeap,
            out _))
        {
            return "";
        }

        // 农历初一显示月份。
        if (day == 1)
        {
            return (isLeap ? "闰" : "") +
                   MonthNames[month - 1] +
                   "月";
        }

        return DayNames[day - 1];
    }

    // ============================================================
    // 日期框完整农历信息
    // ============================================================

    /// <summary>
    /// 获取日期框中显示的完整农历信息。
    ///
    /// 例如：
    /// 农历正月初一（春节）
    /// 农历二月初十（情人）
    /// 农历八月十五（中秋）
    /// </summary>
    public static string GetFullText(DateTime date)
    {
        if (!TryGetParts(
            date,
            out _,
            out int month,
            out int day,
            out bool isLeap,
            out _))
        {
            return "";
        }

        string monthText =
            (isLeap ? "闰" : "") +
            MonthNames[month - 1] +
            "月";

        string lunarText =
            $"农历{monthText}{DayNames[day - 1]}";

        string specialText =
            GetSpecialDayText(date);

        if (!string.IsNullOrEmpty(specialText))
        {
            lunarText += $"（{specialText}）";
        }

        return lunarText;
    }
}
