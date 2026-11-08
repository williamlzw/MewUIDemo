
//using Aprillz.MewUI;
//using Aprillz.MewUI.Controls;


//using System;
//using System.IO;
//using System.Text.Json;

//namespace MewUIDemo;

//static partial class DemoWindows
//{
//    public static Window CreateWebView2Window()
//    {
//        var window = new Window();
//        window.Title("WebView2 示例");
//        window.Resizable(900, 640);
//        window.Padding(0);

//        // ---- 1. 创建 WebView2 ----
//        var webView = new Aprillz.MewUI.Controls.WebView2();

//        webView.DefaultBackgroundColor = Color.Black;

//        // ---- 2. 设置本地资源虚拟主机 ----
//        string assetsFolder = Path.Combine(
//            AppContext.BaseDirectory,
//            "assets"
//        );

//        Directory.CreateDirectory(assetsFolder);

//        // ---- 3. WebView2 核心初始化完成 ----
//        webView.CoreWebView2InitializationCompleted += (e) =>
//        {
//            if (!e.IsSuccess)
//            {
//                Console.WriteLine("[WebView2] 核心初始化失败。");
//                return;
//            }

//            try
//            {
//                // 将本地文件夹映射为虚拟主机
//                webView.SetVirtualHostNameToFolderMapping(
//                    "local.folder",
//                    assetsFolder,
//                    CoreWebView2HostResourceAccessKind.Allow
//                );

//                Console.WriteLine("[WebView2] 核心初始化成功。");
//                Console.WriteLine($"[WebView2] 本地资源目录: {assetsFolder}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"[WebView2] 虚拟主机映射失败: {ex.Message}");
//            }
//        };

//        // ---- 4. 网页导航完成事件 ----
//        webView.NavigationCompleted += (e) =>
//        {
//            if (e.IsSuccess)
//            {
//                Console.WriteLine("[WebView2] 网页加载完成。");
//            }
//            else
//            {
//                Console.WriteLine("[WebView2] 网页加载失败。");
//            }
//        };

//        // ---- 5. 网页标题变化事件 ----
//        webView.DocumentTitleChanged += (title) =>
//        {
//            window.Title($"WebView2 示例 - {title}");
//        };

//        // ---- 6. 接收网页消息 ----
//        webView.WebMessageReceived += (e) =>
//        {
//            Console.WriteLine(
//                $"[WebView2] 收到网页消息: {e.WebMessageAsJson}"
//            );
//        };

//        // ---- 7. 导航开始事件 ----
//        webView.NavigationStarting += (e) =>
//        {
//            Console.WriteLine($"[WebView2] 正在导航: {e.Uri}");
//        };

//        // ---- 8. 地址输入框 ----
//        var urlBox = new TextBox()
//            .Text("https://www.baidu.com")
//            .Width(480)
//            .Height(32);

//        // ---- 9. 导航按钮 ----
//        var goButton = new Button()
//            .Content("导航")
//            .Width(80)
//            .Height(32)
//            .OnClick(() =>
//            {
//                string address = urlBox.Text.Trim();

//                if (string.IsNullOrWhiteSpace(address))
//                    return;

//                // 未输入协议时，自动补充 HTTPS
//                if (!address.Contains("://"))
//                    address = "https://" + address;

//                if (!Uri.TryCreate(
//                    address,
//                    UriKind.Absolute,
//                    out var uri))
//                {
//                    Console.WriteLine("[WebView2] 无效的 URL。");
//                    return;
//                }

//                // 只允许 HTTP / HTTPS
//                if (uri.Scheme != Uri.UriSchemeHttp &&
//                    uri.Scheme != Uri.UriSchemeHttps)
//                {
//                    Console.WriteLine("[WebView2] 只允许 HTTP 或 HTTPS 地址。");
//                    return;
//                }

//                webView.Source = uri;
//            });

//        // ---- 10. 执行 JavaScript 按钮 ----
//        var jsButton = new Button()
//            .Content("执行 JS")
//            .Width(100)
//            .Height(32)
//            .OnClick(async () =>
//            {
//                try
//                {
//                    // 获取网页标题
//                    string json = await webView.ExecuteScriptAsync(
//                        "document.title"
//                    );

//                    // ExecuteScriptAsync 返回 JSON 编码结果
//                    string result =
//                        JsonSerializer.Deserialize<string>(json) ?? "";

//                    Console.WriteLine(
//                        $"[WebView2] JavaScript 执行结果: {result}"
//                    );
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine(
//                        $"[WebView2] JavaScript 执行失败: {ex.Message}"
//                    );
//                }
//            });

//        // ---- 11. 本地页面按钮 ----
//        var localButton = new Button()
//            .Content("打开本地页面")
//            .Width(120)
//            .Height(32)
//            .OnClick(() =>
//            {
//                webView.Source = new Uri(
//                    "https://local.folder/index.html"
//                );
//            });

//        // ---- 12. 工具栏 ----
//        var toolbar = new StackPanel()
//            .Horizontal()
//            .Spacing(8)
//            .Padding(8)
//            .Children(
//                urlBox,
//                goButton,
//                jsButton,
//                localButton
//            );

//        // ---- 13. DockPanel 布局 ----
//        var dockPanel = new DockPanel()
//            .LastChildFill(true)
//            .Children(
//                toolbar.DockTo(Dock.Top),
//                webView
//            );

//        window.Content(dockPanel);

//        // ---- 14. 设置初始网址 ----
//        // 事件已经注册完成，再设置初始地址。
//        webView.Source = new Uri("https://www.baidu.com");

//        return window;
//    }
//}
