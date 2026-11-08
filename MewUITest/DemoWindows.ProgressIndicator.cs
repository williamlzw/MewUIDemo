
//using Aprillz.MewUI;
//using Aprillz.MewUI.Controls;

//namespace MewUIDemo;

//static partial class DemoWindows
//{
//    public static Window CreateProgressIndicatorWindow()
//    {
//        var window = new Window();
//        window.Title("ProgressIndicator 示例");
//        window.Resizable(460, 300);
//        window.Padding(24);

//        // 1. 绑定活动状态
//        var isActive = new ObservableValue<bool>(true);

//        // 2. 创建 ProgressIndicator
//        var indicator = new ProgressIndicator()
//            .Width(32)
//            .Height(32)
//            .BindIsActive(isActive);

//        // 3. 状态文字
//        var statusLabel = new TextBlock()
//            .BindText(isActive, active =>
//                active ? "正在处理中..." : "处理已停止");

//        // 4. 开始按钮
//        var startButton = new Button()
//            .Content("开始")
//            .OnClick(() =>
//            {
//                isActive.Value = true;
//            });

//        // 5. 停止按钮
//        var stopButton = new Button()
//            .Content("停止")
//            .OnClick(() =>
//            {
//                isActive.Value = false;
//            });

//        // 6. 布局
//        var panel = new StackPanel()
//            .Vertical()
//            .Spacing(20)
//            .Children(
//                new TextBlock()
//                    .Text("ProgressIndicator 加载指示器")
//                    .FontSize(18)
//                    .Bold(),

//                new StackPanel()
//                    .Horizontal()
//                    .Spacing(12)
//                    .Children(
//                        indicator,
//                        statusLabel
//                    ),

//                new StackPanel()
//                    .Horizontal()
//                    .Spacing(10)
//                    .Children(
//                        startButton,
//                        stopButton
//                    )
//            );

//        window.Content(panel);

//        return window;
//    }
//}
