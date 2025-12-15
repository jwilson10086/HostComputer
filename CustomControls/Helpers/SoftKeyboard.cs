using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using CustomControls.Controls;

namespace CustomControls.Helpers
{
    public static class SoftKeyboard
    {
        private static Popup _popup;
        private static KeyboardControl _keyboard;

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(SoftKeyboard),
                new PropertyMetadata(false, OnIsEnabledChanged)
            );

        public static void SetIsEnabled(UIElement element, bool value) =>
            element.SetValue(IsEnabledProperty, value);

        public static bool GetIsEnabled(UIElement element) =>
            (bool)element.GetValue(IsEnabledProperty);

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.RegisterAttached(
                "Mode",
                typeof(KeyboardMode),
                typeof(SoftKeyboard),
                new PropertyMetadata(KeyboardMode.Numeric)
            );

        public static void SetMode(UIElement element, KeyboardMode value) =>
            element.SetValue(ModeProperty, value);

        public static KeyboardMode GetMode(UIElement element) =>
            (KeyboardMode)element.GetValue(ModeProperty);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb)
            {
                if ((bool)e.NewValue)
                    tb.GotFocus += Element_GotFocus;
                else
                    tb.GotFocus -= Element_GotFocus;
            }
            else if (d is PasswordBox pb)
            {
                if ((bool)e.NewValue)
                    pb.GotFocus += Element_GotFocus;
                else
                    pb.GotFocus -= Element_GotFocus;
            }
        }

        private static void Element_GotFocus(object sender, RoutedEventArgs e)
        {
            KeyboardMode mode;
            if (sender is TextBox tb)
            {
                mode = GetMode(tb);
                ShowKeyboard(tb, mode);
            }
            else if (sender is PasswordBox pb)
            {
                mode = GetMode(pb);
                ShowKeyboard(pb, mode);
            }
        }

        private static void ShowKeyboard(TextBox tb, KeyboardMode mode)
        {
            EnsurePopupExists();

            // 如果 Popup 已经打开且目标相同，直接返回
            if (_popup.IsOpen && _keyboard.TargetTextBox == tb)
                return;

            _keyboard.Mode = mode;
            _keyboard.TargetTextBox = tb;
            _keyboard.TargetPasswordBox = null;

            _popup.PlacementTarget = tb;
            _popup.IsOpen = true;
        }

        private static void ShowKeyboard(PasswordBox pb, KeyboardMode mode)
        {
            EnsurePopupExists();

            if (_popup.IsOpen && _keyboard.TargetPasswordBox == pb)
                return;

            _keyboard.Mode = mode;
            _keyboard.TargetPasswordBox = pb;
            _keyboard.TargetTextBox = null;

            _popup.PlacementTarget = pb;
            _popup.IsOpen = true;
        }


        private static void EnsurePopupExists()
        {
            if (_popup == null)
            {
                _keyboard = new KeyboardControl
                {
                    Width = 600,
                    Height = 250
                };
                _keyboard.RequestClose += () => _popup.IsOpen = false;

                _popup = new Popup
                {
                    StaysOpen = true, // 关键：true
                    AllowsTransparency = true,
                    Child = _keyboard
                };

                // 点击外部关闭 Popup
                //Application.Current.MainWindow.PreviewMouseDown += (s, e) =>
                //{
                //    if (_popup.IsOpen && !IsClickInsidePopup(e))
                //    {
                //        _popup.IsOpen = false;
                //    }
                //};
            }
        }

        private static bool IsClickInsidePopup(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_popup == null || _popup.Child == null) return false;

            var pos = e.GetPosition(_popup.Child);
            return pos.X >= 0 && pos.X <= _popup.Child.RenderSize.Width
                && pos.Y >= 0 && pos.Y <= _popup.Child.RenderSize.Height;
        }

        public static void ClosePopup()
        {
            if (_popup != null)
                _popup.IsOpen = false;
        }
    }
}
