using System;
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using MewUI.Extended;

namespace MewUIDemo;

class Program
{
    [STAThread]
    static void Main()
    {
        //windows系统运行下面两行
        Win32Platform.Register();
        MewVGWin32Backend.Register();

        //linux系统运行下面两行
        //X11Platform.Register();
        //MewVGX11Backend.Register();

        //MacOSPlatform.Register();
        //MewVGMacOSBackend.Register();

        var mainWindow = CreateMainWindow();
        Application.Run(mainWindow);
    }


    static Window CreateMainWindow()
    {
        var bg = ImageSource.FromFile(
            @"assets/images/bkg.jpg"
        );
        var icon = ImageSource.FromFile(
            @"assets/images/icon.ico"
        );
        var taskIcon = IconSource.FromFile(
            @"assets/images/icon.ico"
        );
        var window = new CustomWindow(backgroundImage: bg,
                                            backgroundColor: new Color(35, 38, 50),
                                            titleforegroundColor:  new Color(78, 78, 230),
                                            showProgramIcon: true,
                                            programIcon: icon,
                                            taskbarIcon: taskIcon);
        window.Title("MewUI 示例");
        window.Resizable(1280, 800);
        window.Padding(0);
        
        var entries = new (string Title, Func<Window> Factory)[]
        {
           // 基础展示与文本
            ("Label 状态标签",        DemoWindows.CreateLabelWindow),
            ("TextBlock 文本段", DemoWindows.CreateTextBlockWindow),
            ("MarkupTextBlock 标记文本", DemoWindows.CreateMarkupTextBlockWindow),
            ("MultiLineTextBox 多行文本", DemoWindows.CreateAdvancedTextBoxWindow),
            ("SyntaxViewer 只读代码视图", DemoWindows.CreateSyntaxViewerWindow),
            ("Markdown 原生渲染", DemoWindows.CreateMarkdownWindow),
            ("Image 图片",            DemoWindows.CreateImageWindow),
            ("Separator 分隔线",      DemoWindows.CreateSeparatorWindow),

            // 按钮与切换
            ("Button 按钮",           DemoWindows.CreateButtonWindow),
            ("ExIconButton 图标按钮", DemoWindows.CreateExIconButtonWindow),
            ("ToggleButton 切换",    DemoWindows.CreateToggleButtonWindow),
            ("RepeatButton 重复按钮", DemoWindows.CreateRepeatButtonWindow),
            ("SplitButton 分体按钮",  DemoWindows.CreateSplitButtonWindow),
            ("DropDownButton 下拉按钮", DemoWindows.CreateDropDownButtonWindow),
            ("ButtonGroup 按钮组",    DemoWindows.CreateButtonGroupWindow),
            ("SegmentedControl 分段控制", DemoWindows.CreateSegmentedControlWindow),
            ("CheckBox 复选框",       DemoWindows.CreateCheckBoxWindow),
            ("RadioButton 单选",      DemoWindows.CreateRadioButtonWindow),
            ("ExSwitch 开关",         DemoWindows.CreateExSwitchWindow),
            ("ToggleSwitch 开关",     DemoWindows.CreateToggleSwitchWindow),

            // 输入与选择
            ("Slider 滑块",           DemoWindows.CreateSliderWindow),
            ("NumericUpDown 数值选择", DemoWindows.CreateNumericUpDownWindow),
            ("ComboBox 下拉选择",     DemoWindows.CreateComboBoxWindow),
            ("ColorPicker 颜色选择",  DemoWindows.CreateColorPickerWindow),
            ("DatePicker 日期选择",   DemoWindows.CreateDatePickerWindow),
            ("LunarDateBox 农历日期", DemoWindows.CreateExLunarDateBoxWindow),
            ("Calendar 日历",         DemoWindows.CreateCalendarWindow),

            // 列表与数据展示
            ("ListBox 列表",            DemoWindows.CreateListBoxWindow),
            ("ListBox 自定义模板",      DemoWindows.CreateListBoxTemplateWindow),
            ("ItemsControl 项集合",     DemoWindows.CreateItemsControlWindow),
            ("GridView 网格视图",       DemoWindows.CreateGridViewWindow),
            ("TreeView 树",             DemoWindows.CreateTreeViewWindow),
            ("ExPropertyGrid 属性框",   DemoWindows.CreateExPropertyGridWindow),
            ("ExConsoleBox 日志框",     DemoWindows.CreateExConsoleBoxWindow),

            // 布局与容器
            ("StackPanel 线性布局",     DemoWindows.CreateStackPanelWindow),
            ("WrapPanel 流式布局",      DemoWindows.CreateWrapPanelWindow),
            ("DockPanel 边缘停靠布局",  DemoWindows.CreateDockPanelWindow),
            ("UniformGrid 等分网格",    DemoWindows.CreateUniformGridWindow),
            ("SplitPanel 分割布局",     DemoWindows.CreateSplitPanelWindow),
            ("Border 边框容器",         DemoWindows.CreateBorderWindow),
            ("GroupBox 分组框",         DemoWindows.CreateGroupBoxWindow),
            ("Expander 折叠面板",       DemoWindows.CreateExpanderWindow),
            ("ScrollViewer 滚动容器",   DemoWindows.CreateScrollViewerWindow),
            ("ContentControl 内容容器", DemoWindows.CreateContentControlWindow),
            ("UserControl 自定义组件",  DemoWindows.CreateUserControlWindow),
            ("TabControl 选项卡",       DemoWindows.CreateTabControlWindow),

            // 导航与菜单
            ("MenuBar 菜单栏",          DemoWindows.CreateMenuBarWindow),
            ("ContextMenu 右键菜单",    DemoWindows.CreateContextMenuWindow),
            ("ToolBar 工具栏",          DemoWindows.CreateToolBarWindow),
            ("NavigationView 风格导航", DemoWindows.CreateNavigationViewWindow),

            // 反馈、浮层与对话框
            ("ProgressBar 进度条",      DemoWindows.CreateProgressBarWindow),
            ("ProgressRing 环形加载",   DemoWindows.CreateProgressRingWindow),
            ("ToolTip 提示",            DemoWindows.CreateToolTipWindow),
            ("Popup 弹出层",            DemoWindows.CreatePopupWindow),
            ("MessageBox 消息框",       DemoWindows.CreateMessageBoxWindow),
            ("FileDialog 文件对话框",   DemoWindows.CreateFileDialogWindow),
            ("DispatcherTimer 计时器",  DemoWindows.CreateDispatcherTimerWindow),

            // 图表
            ("CartesianChart 笛卡尔图表", DemoWindows.CreateCartesianChartWindow),
            ("PieChart 饼图",             DemoWindows.CreatePieChartWindow),
            ("PolarChart 极坐标图",       DemoWindows.CreatePolarChartWindow),

            // 高级与扩展
            ("TransitionContentControl \n过渡内容控制", DemoWindows.CreateTransitionContentControlWindow),
            ("ExDrawingBoard 鼠标绘制板", DemoWindows.CreateExDrawingBoardWindow),
            //("WebView2 浏览器",           DemoWindows.CreateWebView2Window),
            ("FlowGraphControl 流图控制", DemoWindows.CreateFlowGraphWindow),
        };

        // 动态构建按钮集合
        var buttons = new List<Control>();
        foreach (var (title, factory) in entries)
        {
            var button = new Button();
            button.Content(title);
            button.Size(180, 50);
            button.Click += () => factory().Show();
            buttons.Add(button);
        }

        // WrapPanel 流式布局：宽度不足自动换行
        var panel = new WrapPanel()
            .Orientation(Orientation.Horizontal)  // 若为 .Horizontal() 请替换
            .Spacing(10)                          // 若支持双参用 .Spacing(10, 10)
            .Padding(20);
        panel.Children(buttons.ToArray());
        // 2. 用 ScrollViewer 包裹，并设置垂直滚动
        var scrollViewer = new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)   // 内容超出时自动显示竖向滚动条
            .Content(panel);               // 把 WrapPanel 作为滚动内容
        window.Content(scrollViewer);
        
        return window;
    }
}