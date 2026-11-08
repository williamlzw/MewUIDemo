
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Markdown;
using System;
using System.Buffers.Binary;
using System.Diagnostics;

namespace MewUIDemo;

static partial class DemoWindows
{
    public static Window CreateMarkdownWindow()
    {
        var window = new Window();
        window.Title("Markdown 示例");
        window.Resizable(700, 560);
        window.Padding(0);

        string FullStreamMarkdown = """
            # 🛑 Markdown 全元素测试文档

            ## 1. 标题层级 (Headers)
            这是一级到六级标题的演示：
            # 一级标题 (H1)
            ## 二级标题 (H2)
            ### 三级标题 (H3)
            #### 四级标题 (H4)
            ##### 五级标题 (H5)
            ###### 六级标题 (H6)

            ---

            ## 2. 文本样式 (Inline Styles)
            这是**粗体文本 (Bold)**，这是*斜体文本 (Italic)*。这是***粗斜体文本 (Bold_Italic)***。
            这是`行内代码var a = 2; (Inline Code)`，这是~~删除线 (Strikethrough)~~。
            这是==高亮文本 (Highlight)==。

            ---

            ## 3. 列表 (Lists)

            ### 3.1 无序列表 (Unordered)
            - 列表项 A
            - 列表项 B
              - 嵌套子项 B-1
              - 嵌套子项 B-2
            - 列表项 C
            - ***分割线 (Horizontal Rule)***
            - **分割线 (Horizontal Rule)**
            - *分割线 (Horizontal Rule)*

            ### 3.2 有序列表 (Ordered)
            1. 第一项
            2. 第二项
               1. 嵌套有序项
               2. 嵌套有序项
            3. 第三项
            4. ***粗斜体文本 (Bold_Italic)***
            5. **粗体文本 (Bold)**
            6. *斜体文本 (Italic)*

            ### 3.3 任务列表 (Task Lists)
            - [x] 已完成的任务 (Completed Task)
            - [ ] 未完成的任务 (Incomplete Task)
              - [x] 嵌套已完成子任务
              - [ ] 嵌套未完成子任务

            ---

            ## 4. 引用块 (Blockquote)
            > **注意**:这是一段引用文本。
            > 可以包含多行。
            > > 这是嵌套引用 (Nested Quote)。

            ---

            ## 5. 代码块 (Code Blocks)

            ### 5.1 普通文本块
            ```plaintext
            这是一个普通的文本代码块，
            ⚠️没有语法高亮。 
            ```

            ### 5.2 C++ 代码块 
            ```cpp
            #include <iostream>
            int main() {
                std::cout << "Hello, ExDUIR!" << std::endl;
                return 0;
            }
            ```

            ### 5.3 Python 代码块  
            ```python
            def calculate_days(year, month):
                import calendar
                return calendar.monthrange(year, month)[1]
            print(calculate_days(2026, 4))
            ```

            ### 5.4 Diff 代码块
            ```diff
            - 删除的旧代码 (Removed)
            + 新增的代码 (Added)
              未更改的代码 (Unchanged)
            ```

            ### 5.5 Mermaid 图表 (Mermaid Diagram)
            ```mermaid
            graph TD
                A[开始] --> B{判断}
                B -- 是 --> C[执行]
                B -- 否 --> D[结束]
            ```

            ---

            ## 6. 图片、链接与脚注 (Images, Links)

            ### 6.1 图片 (Image)
            ![测试图片](https://www.w3.org/2008/site/images/logo-w3c-mobile-lg.png) 

            ### 6.2 链接 (Link)
            - 行内链接：[访问百度](https://www.baidu.com)

            ---

            ## 7. 表格 (Tables)

            ### 7.1 基础表格
            | 表头1 | 表头2 | 表头3 |
            |-------|-------|-------|
            | *** 单元格 *** | `单元格` | [访问百度](https://www.baidu.com) |
            | ** 数据1 ** | ~~数据2~~  | 数据3  |
            | * 改动点 * | 说明 | 数据4  |

            ### 7.2 对齐方式表格
            | 左对齐 (Left) | 居中对齐 (Center) | 右对齐 (Right) |
            | :--- | :---: | ---: |
            | 文本 | 文本 | 文本 |
            | 123 | 456 | 789 |

            ---

            ## 8. 其他 Markdown 拓展元素 (Other Markdown Extensions)
            ### 8.1 键盘按键 (Keyboard Keys)
            按下 <kbd>Ctrl</kbd> + <kbd>C</kbd> 复制，<kbd>Ctrl</kbd> + <kbd>V</kbd> 粘贴。

            ---

            ## 9. LaTeX 数学公式拓展示例 (LaTeX Math Extensions)

            ### 9.1 行内公式
            1. 万有引力公式 $F = G\dfrac{m_1 m_2}{r^2}$
            2. 勾股定理 $a^2 + b^2 = c^2$
            3. 对数换底公式 $\log_a b = \dfrac{\ln b}{\ln a}$
            4. 三角恒等式 $\sin^2\alpha + \cos^2\alpha = 1$
            5. 等差数列通项 $a_n = a_1 + (n-1)d$
            6. 等比数列求和 $S_n = \dfrac{a_1(1-q^n)}{1-q}\ (q\neq1)$
            7. 两点间距离 $d = \sqrt{(x_2-x_1)^2+(y_2-y_1)^2}$
            8. 一元三次方程简化形式 $x^3 + px + q = 0$
            9. 复数模长 $|z| = \sqrt{x^2+y^2},\ z=x+yi$
            10. 期望定义 $E(X) = \sum x_i P(X=x_i)$
            11. 波长波速关系 $c = \lambda \nu$
            12. 理想气体状态方程 $PV = nRT$
            13. 导数记号 $y' = \dfrac{dy}{dx}$
            14. 排列公式 $\mathrm{A}_n^k = \dfrac{n!}{(n-k)!}$
            15. 组合公式 $\mathrm{C}_n^k = \dfrac{n!}{k!(n-k)!}$

            ### 9.2 基础块级公式

            #### 9.2.1 三角函数、三角求和
            $$\sin(\alpha\pm\beta)=\sin\alpha\cos\beta\pm\cos\alpha\sin\beta$$
            $$\cos(\alpha\pm\beta)=\cos\alpha\cos\beta\mp\sin\alpha\sin\beta$$
            $$\tan2\theta=\frac{2\tan\theta}{1-\tan^2\theta}$$
            $$\sin^2\frac{\theta}{2}=\frac{1-\cos\theta}{2},\quad \cos^2\frac{\theta}{2}=\frac{1+\cos\theta}{2}$$

            #### 9.2.2 极限、连续、重要极限
            $$\lim_{x\to0}\frac{\sin x}{x}=1$$
            $$\lim_{x\to\infty}\left(1+\frac1x\right)^x=e$$
            $$\lim_{n\to\infty}\left(1+\frac1n\right)^n=e$$
            $$\lim_{x\to+\infty}\frac{\ln x}{x}=0$$

            #### 9.2.3 不定积分、定积分
            $$\int x^n dx=\frac{x^{n+1}}{n+1}+C\quad(n\neq-1)$$
            $$\int\frac1x dx=\ln|x|+C$$
            $$\int\sin x dx=-\cos x+C,\quad \int\cos x dx=\sin x+C$$
            $$\int_{a}^{b}f(x)dx=F(b)-F(a),\quad F'(x)=f(x)$$
            $$\int_{-\infty}^{+\infty}e^{-ax^2}dx=\sqrt{\frac{\pi}{a}}\quad(a>0)$$

            #### 9.2.4 级数、无穷乘积
            $$\sum_{k=0}^{\infty}x^k=\frac{1}{1-x}\quad(|x|<1)$$
            $$\sum_{k=1}^{\infty}\frac1{k^2}=\frac{\pi^2}{6}$$
            $$\sum_{k=1}^{\infty}\frac1{k^4}=\frac{\pi^4}{90}$$
            $$e^x=\sum_{n=0}^{\infty}\frac{x^n}{n!}$$
            $$\sin x=\sum_{n=0}^{\infty}\frac{(-1)^n x^{2n+1}}{(2n+1)!}$$

            #### 9.2.5 指数、对数、双曲函数
            $$a^x=e^{x\ln a}$$
            $$\ln(ab)=\ln a+\ln b,\quad \ln\frac{a}{b}=\ln a-\ln b$$
            $$\sinh x=\frac{e^x-e^{-x}}{2},\quad \cosh x=\frac{e^x+e^{-x}}{2}$$

            ### 9.3 线性代数：矩阵、行列式、向量、逆矩阵

            #### 9.3.1 二阶、三阶方阵
            $$M = \begin{pmatrix} 1 & -2 \\ 3 & 4 \end{pmatrix}$$
            $$B = \begin{pmatrix} 2 & 0 & 1 \\ -1 & 5 & 3 \\ 4 & -2 & 0 \end{pmatrix}$$

            #### 9.3.2 行列式
            $$\det\begin{pmatrix}a&b\\c&d\end{pmatrix}=ad-bc$$
            $$\begin{vmatrix}
            a_{11} & a_{12} & a_{13}\\
            a_{21} & a_{22} & a_{23}\\
            a_{31} & a_{32} & a_{33}
            \end{vmatrix}
            =a_{11}a_{22}a_{33}+a_{12}a_{23}a_{31}+a_{13}a_{21}a_{32}
            -a_{13}a_{22}a_{31}-a_{12}a_{21}a_{33}-a_{11}a_{23}a_{32}$$

            #### 9.3.3 列向量、行向量
            $$\boldsymbol{v}=\begin{pmatrix}x\\y\\z\end{pmatrix},\quad \boldsymbol{u}=(a,b,c)$$

            #### 9.3.4 矩阵乘法、单位矩阵
            $$\begin{pmatrix}a&b\\c&d\end{pmatrix}\begin{pmatrix}e&f\\g&h\end{pmatrix}=
            \begin{pmatrix}ae+bg&af+bh\\ce+dg&cf+dh\end{pmatrix}$$
            $$I_3=\begin{pmatrix}1&0&0\\0&1&0\\0&0&1\end{pmatrix}$$

            #### 9.3.5 逆矩阵公式（二阶）
            $$\begin{pmatrix}a&b\\c&d\end{pmatrix}^{-1}=\frac1{ad-bc}\begin{pmatrix}d&-b\\-c&a\end{pmatrix}$$

            #### 9.3.6 内积、范数
            $$\boldsymbol{a}\cdot\boldsymbol{b}=a_1b_1+a_2b_2+a_3b_3$$
            $$\|\boldsymbol{x}\|=\sqrt{x_1^2+x_2^2+\dots+x_n^2}$$

            ### 9.4 微积分：偏导、全微分、多重积分、梯度

            #### 9.4.1 高阶导数、混合偏导
            $$\frac{d^2y}{dx^2}=\frac{d}{dx}\left(\frac{dy}{dx}\right)$$
            $$\frac{\partial^2 u}{\partial x\partial y}=\frac{\partial}{\partial x}\left(\frac{\partial u}{\partial y}\right)$$

            #### 9.4.2 全微分
            $$du=\frac{\partial u}{\partial x}dx+\frac{\partial u}{\partial y}dy+\frac{\partial u}{\partial z}dz$$

            #### 9.4.3 梯度算子
            $$\nabla f=\left(\frac{\partial f}{\partial x},\frac{\partial f}{\partial y},\frac{\partial f}{\partial z}\right)$$

            #### 9.4.4 二重积分、极坐标变换
            $$\iint_D f(x,y)dxdy=\int_{\alpha}^{\beta}\int_{r_1(\theta)}^{r_2(\theta)}f(r\cos\theta,r\sin\theta)\cdot r\,dr d\theta$$

            #### 9.4.5 高斯积分拓展
            $$\iint_{-\infty}^{+\infty}e^{-(x^2+y^2)}dxdy=\pi$$

            ### 9.5 分段函数、符号函数、最值函数
            $$\mathrm{sgn}(x)=
            \begin{cases}
            1 & x>0\\
            0 & x=0\\
            -1 & x<0
            \end{cases}$$

            $$g(x)=
            \begin{cases}
            \ln x & x>1\\
            x-1 & 0<x\le1\\
            x^2 & x\le0
            \end{cases}$$

            $$|x|=
            \begin{cases}
            x & x\ge0\\
            -x & x<0
            \end{cases}$$

            $$h(t)=
            \begin{cases}
            e^{-kt}\sin\omega t & t\ge0\\
            0 & t<0
            \end{cases}$$

            ### 9.6 微分方程（常微分、波动方程）

            #### 9.6.1 一阶线性微分方程通解
            $$y'+P(x)y=Q(x),\quad y=e^{-\int Pdx}\left(\int Q e^{\int Pdx}dx+C\right)$$

            #### 9.6.2 二阶齐次常系数ODE
            $$y''+py'+qy=0$$

            #### 9.6.3 一维波动方程
            $$\frac{\partial^2 u}{\partial t^2}=c^2\frac{\partial^2 u}{\partial x^2}$$

            #### 9.6.4 热传导方程
            $$\frac{\partial u}{\partial t}=a^2\frac{\partial^2 u}{\partial x^2}$$

            ### 9.7 概率论与数理统计公式

            #### 9.7.1 正态分布概率密度
            $$f(x)=\frac{1}{\sqrt{2\pi}\sigma}\exp\left\{-\frac{(x-\mu)^2}{2\sigma^2}\right\}$$

            #### 9.7.2 方差定义
            $$D(X)=E(X^2)-[E(X)]^2$$

            #### 9.7.3 二项分布概率
            $$P(X=k)=\mathrm{C}_n^k p^k (1-p)^{n-k}$$

            #### 9.7.4 协方差
            $$\mathrm{Cov}(X,Y)=E[(X-EX)(Y-EY)]$$

            ### 9.8 几何、矢量分析、物理公式块级

            #### 9.8.1 球面面积、球体体积
            $$S=4\pi R^2,\quad V=\frac43\pi R^3$$

            #### 9.8.2 电场库仑定律矢量形式
            $$\boldsymbol{F}=k\frac{q_1 q_2}{r^2}\boldsymbol{e}_r$$

            #### 9.8.3 动能、机械能
            $$E_k=\frac12 mv^2,\quad E_p=mgh$$

            #### 9.8.4 角动量
            $$\boldsymbol{L}=\boldsymbol{r}\times m\boldsymbol{v}$$

            ### 9.9 补充 LaTeX 元素样式 (Additional LaTeX Styles)

            #### 9.9.1 多行公式对齐 (Aligned Equations)
            $$
            \begin{aligned}
            f(x) &= (x+1)^2 \\
                 &= x^2 + 2x + 1
            \end{aligned}
            $$

            #### 9.9.2 矩阵环境拓展 (Advanced Matrix Environments)
            $$
            A = \begin{bmatrix} 1 & 2 \\ 3 & 4 \end{bmatrix}, \quad
            B = \begin{vmatrix} 1 & 2 \\ 3 & 4 \end{vmatrix}, \quad
            C = \begin{Vmatrix} 1 & 2 \\ 3 & 4 \end{Vmatrix}
            $$

            #### 9.9.3 数学字体与文本 (Math Fonts and Text)
            $$
            \mathbb{R}, \mathbb{Z}, \mathbb{N}, \mathbb{Q}, \mathbb{C}
            $$
            $$
            \mathcal{L}, \mathcal{F}, \mathcal{H}
            $$
            $$
            \text{Let } x \in \mathbb{R} \text{ and } y > 0.
            $$

            #### 9.9.4 顶部/底部括号与箭头 (Over/Under Braces and Arrows)
            $$\underbrace{a + b + c}_{\text{3 terms}} + \overbrace{x + y}^{\text{2 terms}}$$
            $$A \xrightarrow{\text{反应条件}} B \quad A \xleftarrow{y} B$$
            """;

        // ---- Markdown 查看器 ----
        string markdownDirectory = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "Markdown")
        );

        Directory.CreateDirectory(markdownDirectory);
        var viewer = new MarkdownViewer()
            .BaseUri(new Uri(
                markdownDirectory + Path.DirectorySeparatorChar
            ))
            .Markdown(FullStreamMarkdown)
            .Options(new MarkdownOptions
            {
                // 启用有限 HTML 格式
                UseHtmlFormatting = true,

                // 单换行保留为换行
                SoftBreakAsNewLine = true,

                // 启用任务列表
                UseTaskLists = true,

                // 启用脚注
                UseFootnotes = true,

                // 启用管道表格
                UsePipeTables = true

                
            })
            .ImageResolver(new MyImageResolver(markdownDirectory))
            // 处理超链接点击事件
            .OnLinkRequested(args =>
            {
                Console.WriteLine($"用户点击了链接: {args.Url}");
            });

        // MarkdownViewer 自带滚动功能，不需要额外套 ScrollViewer
        window.Content(viewer);

        return window;
    }
}

/// <summary>
/// MewUI Markdown 图片解析器。
///
/// 支持：
/// 1. 本地图片文件
/// 2. HTTP / HTTPS 网络图片
/// 3. PNG / JPG / JPEG / GIF / BMP / WebP
/// 4. 图片文件大小限制
/// 5. 图片像素数量限制
/// 6. 本地目录访问限制
/// 7. 异步加载与取消
///
/// 网络图片使用临时文件，Lease 释放时自动删除。
/// </summary>
public sealed class MyImageResolver : IMarkdownImageResolver
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    // 网络图片临时存储目录
    private readonly string _tempDirectory;

    // 允许访问的本地图片根目录
    private readonly string _localRoot;

    // 单张图片最大字节数：16 MB
    private readonly long _maxBytes;

    // 最大像素数量：4000 万
    private readonly long _maxPixels;

    public MyImageResolver(
        string localRoot,
        long maxBytes = 16 * 1024 * 1024,
        long maxPixels = 40_000_000)
    {
        if (string.IsNullOrWhiteSpace(localRoot))
            throw new ArgumentException("必须指定本地图片根目录。", nameof(localRoot));

        if (maxBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxBytes));

        if (maxPixels <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxPixels));

        _localRoot = Path.GetFullPath(localRoot);

        _maxBytes = maxBytes;
        _maxPixels = maxPixels;

        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "MewUI",
            "MarkdownImages"
        );

        Directory.CreateDirectory(_localRoot);
    }

    /// <summary>
    /// 解析 Markdown 图片。
    /// </summary>
    public async ValueTask<MarkdownImageLease?> ResolveAsync(
        MarkdownImageRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Uri uri = request.ResolvedUri;

            if (!uri.IsAbsoluteUri)
                return null;

            // 本地文件
            if (uri.IsFile)
            {
                return await ResolveLocalAsync(
                    uri,
                    cancellationToken
                );
            }

            // 网络图片
            if (uri.Scheme == Uri.UriSchemeHttp ||
                uri.Scheme == Uri.UriSchemeHttps)
            {
                return await ResolveRemoteAsync(
                    uri,
                    cancellationToken
                );
            }

            // 不支持其他 URI 协议
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (
            ex is IOException ||
            ex is HttpRequestException ||
            ex is UnauthorizedAccessException ||
            ex is ArgumentException ||
            ex is NotSupportedException)
        {
            Debug.WriteLine(
                $"[MarkdownImage] 图片加载失败: {request.ResolvedUri}\n{ex}"
            );

            return null;
        }
    }

    // ============================================================
    // 本地图片
    // ============================================================

    private async ValueTask<MarkdownImageLease?> ResolveLocalAsync(
        Uri uri,
        CancellationToken cancellationToken)
    {
        string path = Path.GetFullPath(uri.LocalPath);

        // 限制本地访问目录
        if (!IsInsideLocalRoot(path))
        {
            Debug.WriteLine($"[MarkdownImage] 拒绝访问目录外的文件: {path}");
            return null;
        }

        if (!IsSupportedExtension(Path.GetExtension(path)))
            return null;

        var info = new FileInfo(path);

        if (!info.Exists || info.Length <= 0 || info.Length > _maxBytes)
            return null;

        // 检查图片格式与尺寸
        byte[] data = await File.ReadAllBytesAsync(
            path,
            cancellationToken
        );

        if (!IsValidImage(data))
            return null;

        // MewUI 文件图片源
        var source = ImageSource.FromFile(path);

        // 本地文件由宿主应用管理，不需要释放回调
        return new MarkdownImageLease(source, null);
    }

    /// <summary>
    /// 检查文件是否位于允许访问的目录内。
    /// </summary>
    private bool IsInsideLocalRoot(string path)
    {
        string relative = Path.GetRelativePath(_localRoot, path);

        if (Path.IsPathRooted(relative))
            return false;

        if (relative == "..")
            return false;

        if (relative.StartsWith(
            ".." + Path.DirectorySeparatorChar,
            StringComparison.Ordinal))
            return false;

        if (relative.StartsWith(
            ".." + Path.AltDirectorySeparatorChar,
            StringComparison.Ordinal))
            return false;

        return true;
    }

    // ============================================================
    // 网络图片
    // ============================================================

    private async ValueTask<MarkdownImageLease?> ResolveRemoteAsync(
        Uri uri,
        CancellationToken cancellationToken)
    {
        using var response = await Http.GetAsync(
        uri,
        HttpCompletionOption.ResponseHeadersRead,
        cancellationToken
    );

        if (!response.IsSuccessStatusCode)
            return null;

        Uri finalUri = response.RequestMessage?.RequestUri ?? uri;

        if (finalUri.Scheme != Uri.UriSchemeHttp &&
            finalUri.Scheme != Uri.UriSchemeHttps)
            return null;

        if (uri.Scheme == Uri.UriSchemeHttps &&
            finalUri.Scheme != Uri.UriSchemeHttps)
            return null;

        if (response.Content.Headers.ContentLength is long length &&
            (length <= 0 || length > _maxBytes))
            return null;

        string? extension = GetImageExtension(
            uri,
            response.Content.Headers.ContentType?.MediaType
        );

        if (extension == null)
            return null;

        Directory.CreateDirectory(_tempDirectory);

        string tempPath = Path.Combine(
            _tempDirectory,
            Guid.NewGuid().ToString("N") + extension
        );

        bool transferred = false;

        try
        {
            await using (var input = await response.Content.ReadAsStreamAsync(
                cancellationToken))
            await using (var output = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await CopyLimitedAsync(
                    input,
                    output,
                    _maxBytes,
                    cancellationToken
                );
            }

            cancellationToken.ThrowIfCancellationRequested();

            byte[] data = await File.ReadAllBytesAsync(
                tempPath,
                cancellationToken
            );

            if (!IsValidImage(data))
                return null;

            var source = ImageSource.FromFile(tempPath);

            var lease = new MarkdownImageLease(
                source,
                () => DeleteTempFile(tempPath)
            );

            // 所有权交给 Lease
            transferred = true;

            return lease;
        }
        finally
        {
            // 创建 Lease 失败时清理文件。
            // 创建成功后，由 Lease 的释放回调删除。
            if (!transferred)
            {
                DeleteTempFile(tempPath);
            }
        }
    }

    /// <summary>
    /// 限制网络图片下载大小。
    /// </summary>
    private static async Task CopyLimitedAsync(
        Stream input,
        Stream output,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[81920];

        long total = 0;

        while (true)
        {
            int read = await input.ReadAsync(
                buffer.AsMemory(0, buffer.Length),
                cancellationToken
            );

            if (read == 0)
                break;

            total += read;

            if (total > maxBytes)
                throw new IOException("图片超过最大允许下载大小。");

            await output.WriteAsync(
                buffer.AsMemory(0, read),
                cancellationToken
            );
        }
    }

    /// <summary>
    /// 删除网络图片临时文件。
    /// </summary>
    private static void DeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MarkdownImage] 删除临时文件失败: {ex.Message}");
        }
    }

    // ============================================================
    // 图片格式和扩展名
    // ============================================================

    private static bool IsSupportedExtension(string? extension)
    {
        return extension?.ToLowerInvariant() switch
        {
            ".png" => true,
            ".jpg" => true,
            ".jpeg" => true,
            ".gif" => true,
            ".bmp" => true,
            ".webp" => true,
            _ => false
        };
    }

    private static string? GetImageExtension(
        Uri uri,
        string? mediaType)
    {
        if (!string.IsNullOrWhiteSpace(mediaType) &&
            mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return mediaType.ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/pjpeg" => ".jpg",
                "image/gif" => ".gif",
                "image/bmp" => ".bmp",
                "image/x-ms-bmp" => ".bmp",
                "image/webp" => ".webp",
                _ => null
            };
        }

        // 服务器没有提供图片 MIME 类型时，根据 URL 后缀判断
        string extension = Path.GetExtension(uri.AbsolutePath);

        extension = extension.ToLowerInvariant();

        return IsSupportedExtension(extension)
            ? extension
            : null;
    }

    // ============================================================
    // 图片尺寸验证
    // ============================================================

    private bool IsValidImage(byte[] data)
    {
        if (data.Length == 0 || data.Length > _maxBytes)
            return false;

        if (!TryGetImageDimensions(data, out int width, out int height))
            return false;

        if (width <= 0 || height <= 0)
            return false;

        long pixels = (long)width * height;

        if (pixels > _maxPixels)
            return false;

        return true;
    }

    /// <summary>
    /// 从图片文件头读取宽度和高度。
    ///
    /// 支持 PNG、JPEG、GIF、BMP、WebP。
    /// 不需要完整解码图片。
    /// </summary>
    private static bool TryGetImageDimensions(
        ReadOnlySpan<byte> data,
        out int width,
        out int height)
    {
        width = 0;
        height = 0;

        // PNG
        if (data.Length >= 24 &&
            data[0] == 0x89 &&
            data[1] == 0x50 &&
            data[2] == 0x4E &&
            data[3] == 0x47 &&
            data[4] == 0x0D &&
            data[5] == 0x0A &&
            data[6] == 0x1A &&
            data[7] == 0x0A)
        {
            uint w = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(16, 4));
            uint h = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(20, 4));

            if (w > int.MaxValue || h > int.MaxValue)
                return false;

            width = (int)w;
            height = (int)h;

            return width > 0 && height > 0;
        }

        // GIF
        if (data.Length >= 10 &&
            data[0] == 'G' &&
            data[1] == 'I' &&
            data[2] == 'F' &&
            data[3] == '8' &&
            (data[4] == '7' || data[4] == '9') &&
            data[5] == 'a')
        {
            width = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6, 2));
            height = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8, 2));

            return width > 0 && height > 0;
        }

        // BMP
        if (data.Length >= 26 &&
            data[0] == 'B' &&
            data[1] == 'M')
        {
            int w = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(18, 4));
            int h = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(22, 4));

            long absoluteHeight = Math.Abs((long)h);

            if (w <= 0 || absoluteHeight <= 0 ||
                w > int.MaxValue || absoluteHeight > int.MaxValue)
                return false;

            width = w;
            height = (int)absoluteHeight;

            return true;
        }

        // JPEG
        if (data.Length >= 4 &&
            data[0] == 0xFF &&
            data[1] == 0xD8)
        {
            return TryGetJpegDimensions(data, out width, out height);
        }

        // WebP
        if (data.Length >= 30 &&
            data[0] == 'R' &&
            data[1] == 'I' &&
            data[2] == 'F' &&
            data[3] == 'F' &&
            data[8] == 'W' &&
            data[9] == 'E' &&
            data[10] == 'B' &&
            data[11] == 'P')
        {
            // VP8X
            if (HasFourCC(data, 12, "VP8X"))
            {
                width = 1 + ReadUInt24LittleEndian(data.Slice(24, 3));
                height = 1 + ReadUInt24LittleEndian(data.Slice(27, 3));

                return true;
            }

            // VP8L
            if (HasFourCC(data, 12, "VP8L") &&
                data.Length >= 25 &&
                data[20] == 0x2F)
            {
                int b1 = data[21];
                int b2 = data[22];
                int b3 = data[23];
                int b4 = data[24];

                width = 1 + (((b2 & 0x3F) << 8) | b1);

                height = 1 +
                    (((b4 & 0x0F) << 10) |
                     (b3 << 2) |
                     ((b2 & 0xC0) >> 6));

                return true;
            }

            // VP8
            if (HasFourCC(data, 12, "VP8 ") &&
                data.Length >= 30 &&
                data[23] == 0x9D &&
                data[24] == 0x01 &&
                data[25] == 0x2A)
            {
                width = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(26, 2)) & 0x3FFF;
                height = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(28, 2)) & 0x3FFF;

                return width > 0 && height > 0;
            }
        }

        return false;
    }

    private static bool HasFourCC(
        ReadOnlySpan<byte> data,
        int offset,
        string value)
    {
        if (data.Length < offset + 4)
            return false;

        return data[offset] == value[0] &&
               data[offset + 1] == value[1] &&
               data[offset + 2] == value[2] &&
               data[offset + 3] == value[3];
    }

    private static int ReadUInt24LittleEndian(ReadOnlySpan<byte> data)
    {
        return data[0] |
               (data[1] << 8) |
               (data[2] << 16);
    }

    /// <summary>
    /// 读取 JPEG SOF 段中的图像尺寸。
    /// </summary>
    private static bool TryGetJpegDimensions(
        ReadOnlySpan<byte> data,
        out int width,
        out int height)
    {
        width = 0;
        height = 0;

        int position = 2;

        while (position + 3 < data.Length)
        {
            if (data[position] != 0xFF)
            {
                position++;
                continue;
            }

            // 跳过 FF 填充字节
            while (position < data.Length && data[position] == 0xFF)
                position++;

            if (position >= data.Length)
                break;

            byte marker = data[position++];

            // JPEG 结束或开始扫描
            if (marker == 0xD9 || marker == 0xDA)
                break;

            // 无长度的独立标记
            if (marker == 0xD8 || marker == 0x01 ||
                (marker >= 0xD0 && marker <= 0xD7))
                continue;

            if (position + 2 > data.Length)
                break;

            int segmentLength =
                BinaryPrimitives.ReadUInt16BigEndian(
                    data.Slice(position, 2)
                );

            if (segmentLength < 2 ||
                position + segmentLength > data.Length)
                break;

            // SOF0 / SOF1 / SOF2 等帧头标记
            bool isSof =
                marker == 0xC0 ||
                marker == 0xC1 ||
                marker == 0xC2 ||
                marker == 0xC3 ||
                marker == 0xC5 ||
                marker == 0xC6 ||
                marker == 0xC7 ||
                marker == 0xC9 ||
                marker == 0xCA ||
                marker == 0xCB ||
                marker == 0xCD ||
                marker == 0xCE ||
                marker == 0xCF;

            if (isSof && segmentLength >= 7)
            {
                height = BinaryPrimitives.ReadUInt16BigEndian(
                    data.Slice(position + 3, 2)
                );

                width = BinaryPrimitives.ReadUInt16BigEndian(
                    data.Slice(position + 5, 2)
                );

                return width > 0 && height > 0;
            }

            position += segmentLength;
        }

        return false;
    }
}