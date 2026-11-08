
using System.Runtime.CompilerServices;
using System.Text;

using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Input;
using Aprillz.MewUI.Platform;

namespace MewUI.Extended;

/// <summary>
/// TextBox 输入限制扩展。
///
/// 1. EnableLettersOnly：只允许英文字母
/// 2. EnableLettersAndNumbers：允许英文字母和数字
/// 3. EnableNumbersOnly：只允许数字
///
/// 三种模式均允许空格和换行。
/// 支持键盘输入、粘贴、剪贴板和模式切换。
/// </summary>
public static class TextBoxInputExtensions
{
    private enum InputFilterMode
    {
        Letters,
        LettersAndNumbers,
        Numbers
    }

    private sealed class FilterState
    {
        public InputFilterMode Mode;

        public bool IsInstalled;
    }

    // 每个 TextBox 保存独立的过滤状态。
    private static readonly ConditionalWeakTable<
        TextBox, FilterState> States = new();

    /// <summary>
    /// 模式一：只允许 A-Z、a-z。
    /// 允许空格和换行。
    /// </summary>
    public static TextBox EnableLettersOnly(
        this TextBox textBox)
    {
        return EnableFilter(textBox, InputFilterMode.Letters);
    }

    /// <summary>
    /// 模式二：允许 A-Z、a-z、0-9。
    /// 允许空格和换行。
    /// </summary>
    public static TextBox EnableLettersAndNumbers(
        this TextBox textBox)
    {
        return EnableFilter(textBox, InputFilterMode.LettersAndNumbers);
    }

    /// <summary>
    /// 模式三：只允许 0-9。
    /// 允许空格和换行。
    /// </summary>
    public static TextBox EnableNumbersOnly(
        this TextBox textBox)
    {
        return EnableFilter(textBox, InputFilterMode.Numbers);
    }

    /// <summary>
    /// 统一安装过滤器。
    /// 同一个控件只注册一次事件，允许重复调用三个模式方法切换模式。
    /// </summary>
    private static TextBox EnableFilter(
        TextBox textBox,
        InputFilterMode mode)
    {
        ArgumentNullException.ThrowIfNull(textBox);

        var state = States.GetValue(
            textBox,
            static _ => new FilterState());

        // 更新当前输入模式。
        state.Mode = mode;

        // 避免重复注册事件。
        if (state.IsInstalled)
            return textBox;

        state.IsInstalled = true;

        // =====================================================
        // 1. 键盘输入过滤
        // =====================================================

        // MewUI 0.22.1 的 TextInput 是 Action<TextInputEventArgs>。
        // 事件发生在文本真正插入文档之前。
        textBox.TextInput += e =>
        {
            if (textBox.IsReadOnly)
                return;

            string original = e.Text ?? string.Empty;

            string filtered = FilterText(
                original,
                state.Mode);

            // 完全合法时交给 MewUI 原生编辑流程。
            if (filtered == original)
                return;

            // 阻止原始文本继续插入。
            e.Handled = true;

            // 将合法字符通过正常编辑入口插入。
            // 不直接修改 Text。
            if (filtered.Length > 0)
            {
                textBox.ReplaceSelection(filtered);
            }
        };

        // =====================================================
        // 2. 粘贴命令过滤
        // =====================================================

        // 移除 TextBox 原来的 Paste 命令。
        textBox.Commands.Unregister(StandardCommands.Paste);

        // 注册自定义 Paste。
        // Ctrl+V 和使用 StandardCommands.Paste 的菜单都会走这里。
        textBox.Commands.Register(
            StandardCommands.Paste,
            () => PasteFiltered(textBox, state),
            () => CanPaste(textBox));

        return textBox;
    }

    /// <summary>
    /// 字符过滤。
    /// </summary>
    private static string FilterText(
        string? text,
        InputFilterMode mode)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var result = new StringBuilder(text.Length);

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            // CRLF 转换为 LF。
            if (c == '\r')
            {
                if (i + 1 < text.Length &&
                    text[i + 1] == '\n')
                {
                    i++;
                }

                result.Append('\n');
                continue;
            }

            // 统一不同平台的换行符。
            if (c == '\n' ||
                c == '\u0085' ||
                c == '\u2028' ||
                c == '\u2029')
            {
                result.Append('\n');
                continue;
            }

            // 所有模式均允许空格。
            if (c == ' ')
            {
                result.Append(c);
                continue;
            }

            bool isLetter =
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z');

            bool isNumber =
                c >= '0' && c <= '9';

            bool allowed = mode switch
            {
                InputFilterMode.Letters =>
                    isLetter,

                InputFilterMode.LettersAndNumbers =>
                    isLetter || isNumber,

                InputFilterMode.Numbers =>
                    isNumber,

                _ => false
            };

            if (allowed)
                result.Append(c);
        }

        return result.ToString();
    }

    // =========================================================
    // 剪贴板处理
    // =========================================================

    /// <summary>
    /// 获取当前剪贴板服务。
    /// 优先使用控件自身指定的 ClipboardService。
    /// </summary>
    private static IClipboardService? GetClipboard(
        TextBox textBox)
    {
        if (textBox.ClipboardService != null)
            return textBox.ClipboardService;

        if (Application.IsRunning)
        {
            return Application.Current
                .PlatformServices.Clipboard;
        }

        return null;
    }

    /// <summary>
    /// 判断是否可以粘贴。
    /// </summary>
    private static bool CanPaste(
        TextBox textBox)
    {
        if (textBox.IsReadOnly)
            return false;

        return GetClipboard(textBox)?.HasText() == true;
    }

    /// <summary>
    /// 执行经过过滤的粘贴。
    /// </summary>
    private static void PasteFiltered(
        TextBox textBox,
        FilterState state)
    {
        if (textBox.IsReadOnly)
            return;

        var clipboard = GetClipboard(textBox);

        if (clipboard == null)
            return;

        if (!clipboard.TryGetText(out string clipboardText))
            return;

        // 过滤剪贴板文本。
        string filtered = FilterText(
            clipboardText,
            state.Mode);

        // 没有合法字符时，不替换当前选区。
        if (filtered.Length == 0)
            return;

        // 使用原生编辑入口，保留编辑器的选区及撤销机制。
        textBox.ReplaceSelection(filtered);
    }
}
