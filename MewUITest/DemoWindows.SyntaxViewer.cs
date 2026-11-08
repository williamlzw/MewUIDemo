using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Text;

namespace MewUIDemo;

sealed class KeywordHighlighter : ITextClassifier
{
    private readonly HashSet<string> _keywords;
    private readonly Color _foreground;

    public KeywordHighlighter(IEnumerable<string> keywords, Color foreground)
    {
        _keywords = new HashSet<string>(keywords, StringComparer.Ordinal);
        _foreground = foreground;
    }

    public void Classify(in TextClassificationContext context, IList<TextPaintSpan> output)
    {
        ReadOnlySpan<char> text = context.Text.Span; // 当前行的显示文本

        for (int i = 0; i < text.Length; i++)
        {
            if (!char.IsLetter(text[i])) continue;

            int start = i;
            while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                i++;

            var word = text[start..i].ToString();
            if (_keywords.Contains(word))
            {
                output.Add(new TextPaintSpan(
                    new TextRange(start, i - start),
                    Foreground: _foreground
                ));
            }
        }
    }
}

static partial class DemoWindows
{
    public static Window CreateSyntaxViewerWindow()
    {
        var window = new Window();
        window.Title("SyntaxViewer 示例");
        window.Resizable(600, 400);
        window.Padding(0);

        // 1. 创建 SyntaxViewer 并设置只读文本
        var viewer = new SyntaxViewer();
        viewer.CanvasPosition(20, 20);
        viewer.Size(540, 340);

        viewer.Text =
            "public class Demo\n" +
            "{\n" +
            "    public void SayHello()\n" +
            "    {\n" +
            "        Console.WriteLine(\"Hello, MewUI!\");\n" +
            "    }\n" +
            "}";

        // 2. 注册关键字高亮分类器
        var highlighter = new KeywordHighlighter(
            new[] { "public", "class", "void" },
            new Color(86, 156, 214)   // 类似 Visual Studio 的关键字蓝
        );
        viewer.Extensions.Classifiers.Add(highlighter);

        // 3. 首次渲染前通知视图重建
        viewer.InvalidateTextView();

        var canvas = new Canvas();
        canvas.Add(viewer);

        window.Content(canvas);
        return window;
    }
}
