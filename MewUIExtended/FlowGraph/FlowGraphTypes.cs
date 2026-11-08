//FlowGraphTypes.cs
using Aprillz.MewUI;

namespace MewUIExtended.FlowGraph;

// ==================== 常量 ====================
public static class FlowGraphConstants
{
    // 侧边栏
    public const double SidebarWidth = 60;
    public const double SidebarBtnHeight = 70;
    public const double SidebarBtnY0 = 10;    // 添加
    public const double SidebarBtnY1 = 110;   // 链路从头
    public const double SidebarBtnY2 = 202;   // 链路继续
    public const double SidebarBtnY3 = 302;   // 取消
    public const double SidebarBtnY4 = 402;   // 导出
    public const double SidebarBtnY5 = 494;   // 导入
    public const double SidebarBtnY6 = 594;   // 自定义
    public const double SidebarBtnY7 = 694;   // 帮助
    public const int SidebarBtnCount = 8;

    // 面板
    public const double AddPanelWidth = 230;
    public const double AddPanelItemH = 38;
    public const double AddPanelHeader = 24;
    public const double ChainPanelWidth = 220;
    public const double ChainPanelItemH = 34;
    public const int ChainPanelMaxVisible = 10;
    public const double ChainPanelHeader = 24;
    public const double CancelPanelWidth = 220;
    public const double CancelPanelItemH = 34;
    public const int CancelPanelMaxVisible = 10;
    public const double CancelPanelHeader = 24;
    public const double CustomPanelWidth = 200;
    public const double CustomPanelItemH = 34;
    public const int CustomPanelMaxVisible = 10;
    public const double CustomPanelHeader = 24;

    // 节点
    public const double TitleHeight = 30;
    public const double PortSpacing = 36;
    public const double PortRadius = 7;
    public const double PortHoverRadius = 10;
    public const double PortOffsetX = 15; // 端口圆心相对节点边缘的外扩距离
    public const double NodeMinWidth = 200;
    public const double NodeCornerRadius = 5;
    public const double NodePadding = 10;
    public const double WidgetGap = 5;

    // 右键菜单
    public const double ContextMenuWidth = 130;
    public const double ContextMenuItemH = 32;
    public const double ContextMenuHeader = 8;

    // 编辑面板
    public const double EditPanelWidth = 580;
    public const double EditPanelHeight = 300;

    // 缩放
    public const double MinZoom = 0.5;
    public const double MaxZoom = 2.0;
    public const double GridSpacing = 30;

    // 组合框下拉
    public const double ComboItemHeight = 28;
}

// ==================== 枚举 ====================
public enum FlowGraphPortDirection
{
    Input,
    Output,
    Inline
}

public enum FlowGraphDataType
{
    Any,
    Image,
    Text,
    Combo,
    Video,
    Audio,
    Number,
    Boolean,
    Object
}

public enum FlowGraphWidgetKind
{
    None,
    Edit,
    Text,
    MultilineText,
    Combo,
    Button,
    DualButton,
    ImagePreview,
    VideoPreview,
    Display
}

public enum FlowGraphExecutionStatus
{
    Idle,
    Pending,
    Running,
    Completed,
    Failed
}

public enum FlowGraphExecutionMode
{
    Fresh,
    Continue
}

public enum FlowGraphExecutionResult
{
    Success,
    Failed
}

public enum FlowGraphSidebarButton
{
    Add = 0,
    ChainFresh = 1,
    ChainContinue = 2,
    Cancel = 3,
    Export = 4,
    Import = 5,
    Custom = 6,
    Help = 7
}

public enum FlowGraphSlotType
{
    Input = 1,
    Output = 2
}

// ==================== 事件参数 ====================
public sealed class FlowGraphNodeEventArgs : EventArgs
{
    public FlowGraphNodeEventArgs(FlowGraphNode node) => Node = node;
    public FlowGraphNode Node { get; }
}

public sealed class FlowGraphConnectionEventArgs : EventArgs
{
    public FlowGraphConnectionEventArgs(FlowGraphConnection connection) => Connection = connection;
    public FlowGraphConnection Connection { get; }
}

public sealed class FlowGraphPortEventArgs : EventArgs
{
    public FlowGraphPortEventArgs(FlowGraphNode node, FlowGraphPort port, int portIndex)
    {
        Node = node;
        Port = port;
        PortIndex = portIndex;
    }
    public FlowGraphNode Node { get; }
    public FlowGraphPort Port { get; }
    public int PortIndex { get; }
}

public sealed class FlowGraphExecutionEventArgs : EventArgs
{
    public FlowGraphExecutionEventArgs(FlowGraphNode node, Exception? exception = null, string? message = null)
    {
        Node = node;
        Exception = exception;
        Message = message ?? exception?.Message ?? string.Empty;
    }
    public FlowGraphNode Node { get; }
    public Exception? Exception { get; }
    public string Message { get; }
}

public sealed class FlowGraphButtonClickedEventArgs : EventArgs
{
    public FlowGraphButtonClickedEventArgs(int nodeId, int cardType, int portIndex, int portId, int whichButton = 0)
    {
        NodeId = nodeId;
        CardType = cardType;
        PortIndex = portIndex;
        PortId = portId;
        WhichButton = whichButton;
    }
    public int NodeId { get; }
    public int CardType { get; }
    public int PortIndex { get; }
    public int PortId { get; }
    public int WhichButton { get; } // 0=单按钮, 1=双按钮左, 2=双按钮右
}

public sealed class FlowGraphCustomItemClickedEventArgs : EventArgs
{
    public FlowGraphCustomItemClickedEventArgs(int index, string name)
    {
        Index = index;
        Name = name;
    }
    public int Index { get; }
    public string Name { get; }
}

public sealed class FlowGraphAddCardEventArgs : EventArgs
{
    public FlowGraphAddCardEventArgs(int cardType) => CardType = cardType;
    public int CardType { get; }
}
