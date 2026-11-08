using System;

using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

using MewUI.Extended;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateExConsoleBoxWindow()
    {
        var window = new Window();

        window.Title("ConsoleBox 日志框示例");

        window.Resizable(650, 460);

        window.Padding(0);

        // =====================================================
        // 1. 创建日志框
        // =====================================================

        var consoleBox = new ExConsoleBox()
            .SetBackgroundColor(
                Color.FromRgb(30, 30, 30))
            .SetDefaultTextColor(
                Color.FromRgb(220, 220, 220))
            .SetFont("Consolas", 13)
            .SetItemPadding(
                new Thickness(5, 3, 5, 3))
            // 滚动条颜色。
            .SetScrollBarColor(
                Color.FromRgb(110, 120, 135),  // 默认
                Color.FromRgb(155, 170, 190),  // 悬停
                Color.FromRgb(190, 205, 225)); // 按下

        // =====================================================
        // 2. 添加测试日志
        // =====================================================

        consoleBox.AddLine(
            "[Info] 系统初始化完成，所有核心服务已正常启动。",
            Color.FromRgb(100, 220, 130));

        consoleBox.AddLine(
            "[Warn] 检测到配置文件版本过旧，已自动迁移至新格式，请检查设置项是否丢失。",
            Color.FromRgb(255, 200, 80));

        consoleBox.AddLine(
            "[Error] 数据库连接超时：无法连接到 192.168.1.100:3306，请检查网络或防火墙设置。重试次数：3/3。",
            Color.FromRgb(255, 90, 90));

        consoleBox.AddLine(
            "[Debug] 内存池分配详情 -> 总大小: 256MB | 已用: 142MB | 碎片率: 2.3% | 峰值: 198MB",
            Color.FromRgb(150, 180, 255));

        consoleBox.AddLine(
            "[Net] 收到客户端请求 [ID:8842] POST /api/v2/data/sync?token=xxx&ts=1716720000 耗时: 45ms",
            Color.FromRgb(100, 210, 230));

        consoleBox.AddLine(
            "[Render] 帧率稳定在 60FPS，GPU占用率 34%，显存使用 1.2GB / 8GB，渲染管线无瓶颈。",
            Color.FromRgb(180, 180, 255));

        consoleBox.AddLine(
            "[Auth] 用户 admin@system.local 登录成功，会话令牌已签发，有效期 7200 秒，IP: 10.0.0.55",
            Color.FromRgb(120, 220, 160));

        consoleBox.AddLine(
            "[IO] 日志文件滚动触发：当前文件大小达到 50MB 阈值，已归档为 app.log.20260526.bak",
            Color.FromRgb(180, 200, 220));

        consoleBox.AddLine(
            "[Perf] 主线程阻塞警告：消息处理耗时 320ms，超过 16ms 流畅阈值，建议将耗时操作移至工作线程执行以避免界面卡顿。",
            Color.FromRgb(255, 150, 80));

        // =====================================================
        // 3. 按钮
        // =====================================================

        int index = 0;

        var addButton = new Button()
            .Content("模拟添加")
            .Size(110, 30)
            .OnClick(() =>
            {
                index++;

                var colors = new[]
                {
                    Color.FromRgb(100, 220, 130),
                    Color.FromRgb(255, 200, 80),
                    Color.FromRgb(255, 90, 90),
                    Color.FromRgb(150, 180, 255),
                    Color.FromRgb(100, 210, 230)
                };

                var random = new Random();

                var color = colors[random.Next(colors.Length)];

                consoleBox.AddLine(
                    $"[Info] 新日志 #{index}：系统运行正常，当前时间：{DateTime.Now:HH:mm:ss}",
                    color);
            });

        var addLongButton = new Button()
            .Content("添加长文本")
            .Size(110, 30)
            .OnClick(() =>
            {
                index++;

                consoleBox.AddLine(
                    $"[Debug] 长文本测试 #{index}：这是用于测试日志自动换行的内容，" +
                    "当日志文本超过当前日志框的可用宽度时，应该自动换行显示。" +
                    "支持中文、English、IP地址192.168.1.100以及各种混合内容，" +
                    "并且每条日志都可以拥有独立的文字颜色。",
                    Color.FromRgb(150, 180, 255));
            });

        var clearButton = new Button()
            .Content("清空日志")
            .Size(110, 30)
            .OnClick(() =>
            {
                consoleBox.Clear();

                index = 0;
            });

        // =====================================================
        // 4. 按钮布局
        // =====================================================

        var buttons = new StackPanel()
            .Horizontal()
            .Spacing(8)
            .Children(
                addButton,
                addLongButton,
                clearButton);

        // =====================================================
        // 5. 窗口布局
        // =====================================================

        var root = new StackPanel()
            .Vertical()
            .Spacing(8)
            .Padding(10)
            .Children(
                consoleBox.Height(350),
                buttons);

        window.Content(root);

        return window;
    }
}