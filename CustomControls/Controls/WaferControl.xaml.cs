using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CustomControls.Controls
{
    public partial class WaferControl : UserControl
    {
        public WaferControl()
        {
            InitializeComponent();
        }

        // -------------------------
        //  Visibility 控制
        // -------------------------
        public bool WaferVisible
        {
            get => (bool)GetValue(WaferVisibleProperty);
            set => SetValue(WaferVisibleProperty, value);
        }

        public static readonly DependencyProperty WaferVisibleProperty =
            DependencyProperty.Register("WaferVisible", typeof(bool),
            typeof(WaferControl),
            new PropertyMetadata(true, OnVisibleChanged));

        private static void OnVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WaferControl)d).Visibility =
                (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }


        // -------------------------
        //  晶圆状态枚举
        // -------------------------
        public enum WaferStatus
        {
            BeforeProcess,
            Processing,
            Completed,
            Fail
        }

        public WaferStatus Status
        {
            get => (WaferStatus)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(WaferStatus),
            typeof(WaferControl),
            new PropertyMetadata(WaferStatus.BeforeProcess, OnStatusChanged));

        private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (WaferControl)d;
            ctrl.UpdateWaferColor((WaferStatus)e.NewValue);
        }

        private void UpdateWaferColor(WaferStatus status)
        {
            switch (status)
            {
                case WaferStatus.BeforeProcess:        // 未加工
                    WaferEllipse.Fill = new SolidColorBrush(Color.FromRgb(150, 160, 170));
                    break;

                case WaferStatus.Processing:          // 加工中
                    WaferEllipse.Fill = new SolidColorBrush(Color.FromRgb(80, 140, 255));
                    break;

                case WaferStatus.Completed:           // 加工完成
                    WaferEllipse.Fill = new SolidColorBrush(Color.FromRgb(0, 200, 0));
                    break;

                case WaferStatus.Fail:                // 加工失败
                    WaferEllipse.Fill = new SolidColorBrush(Color.FromRgb(220, 40, 40));
                    break;
            }
        }


        // -------------------------
        //  显示文字
        // -------------------------
        public string WaferLabel
        {
            get => (string)GetValue(WaferLabelProperty);
            set => SetValue(WaferLabelProperty, value);
        }

        public static readonly DependencyProperty WaferLabelProperty =
            DependencyProperty.Register("WaferLabel", typeof(string),
            typeof(WaferControl),
            new PropertyMetadata("", OnLabelChanged));

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WaferControl)d).WaferText.Text = (string)e.NewValue;
        }


        // -------------------------
        // 字体颜色
        // -------------------------
        public Brush FontColor
        {
            get => (Brush)GetValue(FontColorProperty);
            set => SetValue(FontColorProperty, value);
        }

        public static readonly DependencyProperty FontColorProperty =
            DependencyProperty.Register("FontColor", typeof(Brush),
            typeof(WaferControl),
            new PropertyMetadata(Brushes.White, OnFontColorChanged));

        private static void OnFontColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WaferControl)d).WaferText.Foreground = (Brush)e.NewValue;
        }


        // -------------------------
        // 字体类型
        // -------------------------
        public FontFamily WaferFontFamily
        {
            get => (FontFamily)GetValue(WaferFontFamilyProperty);
            set => SetValue(WaferFontFamilyProperty, value);
        }

        public static readonly DependencyProperty WaferFontFamilyProperty =
            DependencyProperty.Register("WaferFontFamily", typeof(FontFamily),
            typeof(WaferControl),
            new PropertyMetadata(new FontFamily("Segoe UI"), OnFontFamilyChanged));

        private static void OnFontFamilyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WaferControl)d).WaferText.FontFamily = (FontFamily)e.NewValue;
        }


        // -------------------------
        // 字体大小
        // -------------------------
        public double WaferFontSize
        {
            get => (double)GetValue(WaferFontSizeProperty);
            set => SetValue(WaferFontSizeProperty, value);
        }

        public static readonly DependencyProperty WaferFontSizeProperty =
            DependencyProperty.Register("WaferFontSize", typeof(double),
            typeof(WaferControl),
            new PropertyMetadata(16d, OnFontSizeChanged));

        private static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WaferControl)d).WaferText.FontSize = (double)e.NewValue;
        }


        // -------------------------
        // 字体粗细
        // -------------------------
        public FontWeight WaferFontWeight
        {
            get => (FontWeight)GetValue(WaferFontWeightProperty);
            set => SetValue(WaferFontWeightProperty, value);
        }

        public static readonly DependencyProperty WaferFontWeightProperty =
            DependencyProperty.Register("WaferFontWeight", typeof(FontWeight),
            typeof(WaferControl),
            new PropertyMetadata(FontWeights.SemiBold, OnFontWeightChanged));

        private static void OnFontWeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WaferControl)d).WaferText.FontWeight = (FontWeight)e.NewValue;
        }
    }
}
