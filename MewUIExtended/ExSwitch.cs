
using System;

using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Rendering;

namespace MewUI.Extended
{
    /// <summary>
    /// ExDUIR 风格的自绘开关控件。
    /// </summary>
    public sealed class ExSwitch : Control
    {
        // ============================================================
        // MewProperty
        // ============================================================

        /// <summary>
        /// 是否选中。
        /// 支持双向数据绑定。
        /// </summary>
        public static readonly MewProperty<bool> IsCheckedProperty =
            MewProperty<bool>.Register<ExSwitch>(
                nameof(IsChecked),
                false,
                MewPropertyOptions.AffectsVisualState |
                MewPropertyOptions.BindsTwoWayByDefault,
                static (owner, oldValue, newValue) =>
                {
                    owner.IsCheckedChanged?.Invoke(
                        owner,
                        new SwitchChangedEventArgs(oldValue, newValue));
                });

        /// <summary>
        /// 动画进度：0 = 关闭，1 = 开启。
        /// 由 Style 的 StateTrigger 控制。
        /// </summary>
        public static readonly MewProperty<double> ProgressProperty =
            MewProperty<double>.Register<ExSwitch>(
                nameof(Progress),
                0.0,
                MewPropertyOptions.AffectsRender);

        /// <summary>
        /// 开启状态显示的文字。
        /// </summary>
        public static readonly MewProperty<string> CheckedTextProperty =
            MewProperty<string>.Register<ExSwitch>(
                nameof(CheckedText),
                "是",
                MewPropertyOptions.AffectsRender);

        /// <summary>
        /// 关闭状态显示的文字。
        /// </summary>
        public static readonly MewProperty<string> UncheckedTextProperty =
            MewProperty<string>.Register<ExSwitch>(
                nameof(UncheckedText),
                "否",
                MewPropertyOptions.AffectsRender);

        // ============================================================
        // Properties
        // ============================================================

        public bool IsChecked
        {
            get => GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        public double Progress => GetValue(ProgressProperty);

        public string CheckedText
        {
            get => GetValue(CheckedTextProperty);
            set => SetValue(CheckedTextProperty, value);
        }

        public string UncheckedText
        {
            get => GetValue(UncheckedTextProperty);
            set => SetValue(UncheckedTextProperty, value);
        }

        // ============================================================
        // Events
        // ============================================================

        public event EventHandler<SwitchChangedEventArgs>?
            IsCheckedChanged;

        // ============================================================
        // Internal State
        // ============================================================

        private bool _pointerPressed;
        private bool _keyPressed;

        private bool IsPressed =>
            _pointerPressed || _keyPressed;

        // ============================================================
        // Default Style
        // ============================================================

        private static readonly Style _defaultStyle =
            CreateDefaultStyle();

        static ExSwitch()
        {
            // 支持 Tab 键聚焦和键盘操作。
            FocusableProperty.OverrideDefaultValue<ExSwitch>(true);
        }

        public ExSwitch()
        {
            StyleSheet = new StyleSheet();
            StyleSheet.Define<ExSwitch>(_defaultStyle);
        }

        private static Style CreateDefaultStyle()
        {
            return new Style(typeof(ExSwitch))
            {
                // Progress 从 0 到 1 时自动过渡。
                Transitions =
                [
                    Transition.Create(ProgressProperty)
                ],

                Setters =
                [
                    Setter.Create(ProgressProperty, 0.0)
                ],

                Triggers =
                [
                    // 开启状态。
                    new StateTrigger
                    {
                        Match = VisualStateFlags.Checked,

                        Setters =
                        [
                            Setter.Create(ProgressProperty, 1.0)
                        ]
                    }
                ]
            };
        }

        // ============================================================
        // Visual State
        // ============================================================

        protected override VisualState ComputeVisualState()
        {
            var state = base.ComputeVisualState();

            var flags = state.Flags;

            if (IsChecked)
            {
                flags |= VisualStateFlags.Checked;
            }

            return state with
            {
                Flags = flags
            };
        }

        // ============================================================
        // Layout
        // ============================================================

        protected override Size MeasureContent(Size availableSize)
        {
            // 默认开关尺寸。
            // Arrange 后会根据实际 Bounds 自适应。
            return new Size(64, 30);
        }

        // ============================================================
        // Rendering
        // ============================================================

        protected override void OnRender(IGraphicsContext context)
        {
            base.OnRender(context);//方便绘制组件边框 调试使用
            var bounds = GetSnappedBorderBounds(Bounds);

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;
          
            double progress = Math.Clamp(Progress, 0.0, 1.0);

            // --------------------------------------------------------
            // 1. Switch Track
            // --------------------------------------------------------

            double inset = 1.0;

            var track = new Rect(
                bounds.X + inset,
                bounds.Y + inset,
                Math.Max(0, bounds.Width - inset * 2),
                Math.Max(0, bounds.Height - inset * 2));

            double radius = track.Height / 2.0;

            // 开关背景颜色。
            Color trackColor;

            if (!IsEffectivelyEnabled)
            {
                trackColor = Theme.Palette.DisabledControlBackground;
            }
            else
            {
                // 从普通按钮颜色渐变到主题强调色。
                trackColor = Theme.Palette.ButtonFace.Lerp(
                    Theme.Palette.Accent,
                    progress);
            }

            // 绘制圆角轨道和边框。
            DrawBackgroundAndBorder(
                context,
                track,
                trackColor,
                Theme.Palette.ControlBorder,
                1.0,
                radius);

            // --------------------------------------------------------
            // 2. Switch Thumb
            // --------------------------------------------------------

            double thumbInset = 3.0;

            double thumbSize = Math.Max(
                0,
                track.Height - thumbInset * 2);

            double travel = Math.Max(
                0,
                track.Width - thumbSize - thumbInset * 2);

            // 根据动画进度计算滑块位置。
            double thumbX =
                track.X + thumbInset + travel * progress;

            var thumbRect = new Rect(
                thumbX,
                track.Y + thumbInset,
                thumbSize,
                thumbSize);

            // 滑块也进行像素对齐。
            thumbRect = LayoutRounding.SnapBoundsRectToPixels(
                thumbRect,
                GetDpi() / 96.0);

            Color thumbColor;

            if (!IsEffectivelyEnabled)
            {
                thumbColor =
                    Theme.Palette.DisabledControlBackground;
            }
            else if (IsPressed)
            {
                thumbColor =
                    Theme.Palette.ButtonPressedBackground;
            }
            else
            {
                thumbColor =
                    Theme.Palette.ControlBackground;
            }

            DrawBackgroundAndBorder(
                context,
                thumbRect,
                thumbColor,
                Theme.Palette.ControlBorder,
                0,
                thumbSize / 2.0);

            // --------------------------------------------------------
            // 3. Draw Text
            // --------------------------------------------------------

            if (!IsEffectivelyEnabled)
            {
                return;
            }

            Color textColor = Foreground;

            double textMargin = radius / 2.0;

            if (progress > 0.001)
            {
                // 开启状态：文字绘制在滑块左侧。
                double textRight = thumbRect.X;

                var textRect = new Rect(
                    track.X + textMargin,
                    track.Y,
                    Math.Max(
                        0,
                        textRight - track.X - textMargin),
                    track.Height);

                DrawEngineText(
                    context,
                    CheckedText,
                    textRect,
                    textColor,
                    TextAlignment.Center,
                    TextAlignment.Center,
                    TextWrapping.NoWrap);
            }
            else
            {
                // 关闭状态：文字绘制在滑块右侧。
                double textLeft = thumbRect.Right;

                var textRect = new Rect(
                    textLeft,
                    track.Y,
                    Math.Max(
                        0,
                        track.Right - textLeft - textMargin),
                    track.Height);

                DrawEngineText(
                    context,
                    UncheckedText,
                    textRect,
                    textColor,
                    TextAlignment.Center,
                    TextAlignment.Center,
                    TextWrapping.NoWrap);
            }
        }

        // ============================================================
        // Mouse Input
        // ============================================================

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (!IsEffectivelyEnabled ||
                e.Button != MouseButton.Left)
            {
                return;
            }

            Focus();

            // 捕获鼠标，确保 MouseUp 能正常收到。
            if (FindVisualRoot() is Window window)
            {
                window.CaptureMouse(this);
            }

            if (!IsMouseCaptured)
            {
                return;
            }

            _pointerPressed = true;

            SetPressed(true);

            InvalidateVisual();

            e.Handled = true;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button != MouseButton.Left ||
                !_pointerPressed)
            {
                return;
            }

            bool wasPressed = _pointerPressed;

            // 检测鼠标释放位置。
            var local = e.GetPosition(this);

            var point = new Point(
                Bounds.X + local.X,
                Bounds.Y + local.Y);

            bool releasedInside = Bounds.Contains(point);

            // 释放鼠标捕获。
            if (FindVisualRoot() is Window window)
            {
                window.ReleaseMouseCapture();
            }

            _pointerPressed = false;

            SetPressed(_keyPressed);

            InvalidateVisual();

            // 只有在控件内部释放才切换状态。
            if (wasPressed &&
                releasedInside &&
                IsEffectivelyEnabled)
            {
                Toggle();
            }

            e.Handled = true;
        }

        // ============================================================
        // Keyboard Input
        // ============================================================

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!IsEffectivelyEnabled)
                return;

            if (e.Key == Key.Space ||
                e.Key == Key.Enter)
            {
                // 防止按住按键重复切换。
                if (!_keyPressed)
                {
                    _keyPressed = true;

                    SetPressed(true);

                    InvalidateVisual();
                }

                e.Handled = true;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.Key != Key.Space &&
                e.Key != Key.Enter)
            {
                return;
            }

            if (_keyPressed)
            {
                _keyPressed = false;

                SetPressed(_pointerPressed);

                InvalidateVisual();

                if (IsEffectivelyEnabled)
                {
                    Toggle();
                }
            }

            e.Handled = true;
        }

        // ============================================================
        // Mouse Capture Cleanup
        // ============================================================

        protected override void OnMewPropertyChanged(
            MewProperty property)
        {
            base.OnMewPropertyChanged(property);

            // 控件被禁用、隐藏或者移出视觉树时，
            // 鼠标捕获可能自动结束。
            if (property == IsMouseCapturedProperty &&
                !IsMouseCaptured)
            {
                if (_pointerPressed)
                {
                    _pointerPressed = false;

                    SetPressed(_keyPressed);

                    InvalidateVisual();
                }
            }
        }

        // ============================================================
        // Public API
        // ============================================================

        public void Toggle()
        {
            if (!IsEffectivelyEnabled)
                return;

            IsChecked = !IsChecked;
        }

        public void SetChecked(bool value)
        {
            IsChecked = value;
        }
    }

    // ================================================================
    // Event Arguments
    // ================================================================

    public sealed class SwitchChangedEventArgs : EventArgs
    {
        public bool OldValue { get; }

        public bool NewValue { get; }

        public SwitchChangedEventArgs(
            bool oldValue,
            bool newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
