using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CustomControls.Controls
{
    public partial class WaferRobot : UserControl
    {
        public WaferRobot()
        {
            InitializeComponent();
            Loaded += WaferRobot_Loaded;
        }

        private void WaferRobot_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyAllAngles();    // ★ 初始化时同步所有角度
            ApplyAllWaferProperties(); // ★ 初始化时同步晶圆属性
        }

        #region ======================= 依赖属性（角度） =======================

        public static readonly DependencyProperty BaseAngleProperty =
            DependencyProperty.Register(nameof(BaseAngle), typeof(double), typeof(WaferRobot),
                new PropertyMetadata(0.0, OnAngleChanged));

        public static readonly DependencyProperty Arm1_1AngleProperty =
            DependencyProperty.Register(nameof(Arm1_1Angle), typeof(double), typeof(WaferRobot),
                new PropertyMetadata(75.0, OnAngleChanged));

        public static readonly DependencyProperty Arm1_2AngleProperty =
            DependencyProperty.Register(nameof(Arm1_2Angle), typeof(double), typeof(WaferRobot),
                new PropertyMetadata(-150.0, OnAngleChanged));

        public static readonly DependencyProperty Arm1_3AngleProperty =
            DependencyProperty.Register(nameof(Arm1_3Angle), typeof(double), typeof(WaferRobot),
                new PropertyMetadata(75.0, OnAngleChanged));

        public static readonly DependencyProperty Arm2_1AngleProperty =
            DependencyProperty.Register(nameof(Arm2_1Angle), typeof(double), typeof(WaferRobot),
                new PropertyMetadata(-75.0, OnAngleChanged));

        public static readonly DependencyProperty Arm2_2AngleProperty =
            DependencyProperty.Register(nameof(Arm2_2Angle), typeof(double), typeof(WaferRobot),
                new PropertyMetadata(-150.0, OnAngleChanged));

        public static readonly DependencyProperty Arm2_3AngleProperty =
            DependencyProperty.Register(nameof(Arm2_3Angle), typeof(double), typeof(WaferRobot),
                new PropertyMetadata(-75.0, OnAngleChanged));


        public double BaseAngle { get => (double)GetValue(BaseAngleProperty); set => SetValue(BaseAngleProperty, value); }
        public double Arm1_1Angle { get => (double)GetValue(Arm1_1AngleProperty); set => SetValue(Arm1_1AngleProperty, value); }
        public double Arm1_2Angle { get => (double)GetValue(Arm1_2AngleProperty); set => SetValue(Arm1_2AngleProperty, value); }
        public double Arm1_3Angle { get => (double)GetValue(Arm1_3AngleProperty); set => SetValue(Arm1_3AngleProperty, value); }

        public double Arm2_1Angle { get => (double)GetValue(Arm2_1AngleProperty); set => SetValue(Arm2_1AngleProperty, value); }
        public double Arm2_2Angle { get => (double)GetValue(Arm2_2AngleProperty); set => SetValue(Arm2_2AngleProperty, value); }
        public double Arm2_3Angle { get => (double)GetValue(Arm2_3AngleProperty); set => SetValue(Arm2_3AngleProperty, value); }

        #endregion

        #region ======================= WaferControl 属性（Lower / Upper） =======================
        // 为了两个 WaferControl（WaferCtrl_Lower、WaferCtrl_Upper）分别暴露属性
        // Lower (下手指)
        public bool LowerWaferVisible
        {
            get => (bool)GetValue(LowerWaferVisibleProperty);
            set => SetValue(LowerWaferVisibleProperty, value);
        }
        public static readonly DependencyProperty LowerWaferVisibleProperty =
            DependencyProperty.Register(nameof(LowerWaferVisible), typeof(bool), typeof(WaferRobot),
                new PropertyMetadata(true, (d, e) =>
                {
                    var r = (WaferRobot)d;
                    if (r.WaferCtrl_Lower != null) r.WaferCtrl_Lower.WaferVisible = (bool)e.NewValue;
                }));

        public WaferControl.WaferStatus LowerWaferStatus
        {
            get => (WaferControl.WaferStatus)GetValue(LowerWaferStatusProperty);
            set => SetValue(LowerWaferStatusProperty, value);
        }
        public static readonly DependencyProperty LowerWaferStatusProperty =
            DependencyProperty.Register(nameof(LowerWaferStatus), typeof(WaferControl.WaferStatus), typeof(WaferRobot),
                new PropertyMetadata(WaferControl.WaferStatus.BeforeProcess, (d, e) =>
                {
                    var r = (WaferRobot)d;
                    if (r.WaferCtrl_Lower != null) r.WaferCtrl_Lower.Status = (WaferControl.WaferStatus)e.NewValue;
                }));

        public string LowerWaferLabel
        {
            get => (string)GetValue(LowerWaferLabelProperty);
            set => SetValue(LowerWaferLabelProperty, value);
        }
        public static readonly DependencyProperty LowerWaferLabelProperty =
            DependencyProperty.Register(nameof(LowerWaferLabel), typeof(string), typeof(WaferRobot),
                new PropertyMetadata(string.Empty, (d, e) =>
                {
                    var r = (WaferRobot)d;
                    if (r.WaferCtrl_Lower != null) r.WaferCtrl_Lower.WaferLabel = (string)e.NewValue;
                }));

        public Brush LowerWaferFontColor
        {
            get => (Brush)GetValue(LowerWaferFontColorProperty);
            set => SetValue(LowerWaferFontColorProperty, value);
        }
        public static readonly DependencyProperty LowerWaferFontColorProperty =
            DependencyProperty.Register(nameof(LowerWaferFontColor), typeof(Brush), typeof(WaferRobot),
                new PropertyMetadata(Brushes.White, (d, e) =>
                {
                    var r = (WaferRobot)d;
                    if (r.WaferCtrl_Lower != null) r.WaferCtrl_Lower.FontColor = (Brush)e.NewValue;
                }));

        public FontFamily LowerWaferFontFamily
        {
            get => (FontFamily)GetValue(LowerWaferFontFamilyProperty);
            set => SetValue(LowerWaferFontFamilyProperty, value);
        }
        public static readonly DependencyProperty LowerWaferFontFamilyProperty =
            DependencyProperty.Register(nameof(LowerWaferFontFamily), typeof(FontFamily), typeof(WaferRobot),
                new PropertyMetadata(new FontFamily("Segoe UI"), (d, e) =>
                {
                    var r = (WaferRobot)d;
                    if (r.WaferCtrl_Lower != null) r.WaferCtrl_Lower.WaferFontFamily = (FontFamily)e.NewValue;
                }));

        public double LowerWaferFontSize
        {
            get => (double)GetValue(LowerWaferFontSizeProperty);
            set => SetValue(LowerWaferFontSizeProperty, value);
        }
        public static readonly DependencyProperty LowerWaferFontSizeProperty =
            DependencyProperty.Register(nameof(LowerWaferFontSize), typeof(double), typeof(WaferRobot),
                new PropertyMetadata(16d, (d, e) =>
                {
                    var r = (WaferRobot)d;
                    if (r.WaferCtrl_Lower != null) r.WaferCtrl_Lower.WaferFontSize = (double)e.NewValue;
                }));

        public FontWeight LowerWaferFontWeight
        {
            get => (FontWeight)GetValue(LowerWaferFontWeightProperty);
            set => SetValue(LowerWaferFontWeightProperty, value);
        }
        public static readonly DependencyProperty LowerWaferFontWeightProperty =
            DependencyProperty.Register(nameof(LowerWaferFontWeight), typeof(FontWeight), typeof(WaferRobot),
                new PropertyMetadata(FontWeights.SemiBold, (d, e) =>
                {
                    var r = (WaferRobot)d;
                    if (r.WaferCtrl_Lower != null) r.WaferCtrl_Lower.WaferFontWeight = (FontWeight)e.NewValue;
                }));

        // Upper (上手指)
        public bool UpperWaferVisible
        {
            get => (bool)GetValue(UpperWaferVisibleProperty);
            set => SetValue(UpperWaferVisibleProperty, value);
        }
        public static readonly DependencyProperty UpperWaferVisibleProperty =
            DependencyProperty.Register(nameof(UpperWaferVisible), typeof(bool), typeof(WaferRobot),
                new PropertyMetadata(true, (d, e) =>
                {
                    var r = (WaferRobot)d;
                    if (r.WaferCtrl_Upper != null) r.WaferCtrl_Upper.WaferVisible = (bool)e.NewValue;
                }));

        public WaferControl.WaferStatus UpperWaferStatus
        {
            get => (WaferControl.WaferStatus)GetValue(UpperWaferStatusProperty);
            set => SetValue(UpperWaferStatusProperty, value);
        }
        public static readonly DependencyProperty UpperWaferStatusProperty =
            DependencyProperty.Register(nameof(UpperWaferStatus), typeof(WaferControl.WaferStatus), typeof(WaferRobot),
                new PropertyMetadata(WaferControl.WaferStatus.BeforeProcess, (d, e) =>
                {
                    var r = (WaferRobot)d;
                    if (r.WaferCtrl_Upper != null) r.WaferCtrl_Upper.Status = (WaferControl.WaferStatus)e.NewValue;
                }));

        public string UpperWaferLabel
        {
            get => (string)GetValue(UpperWaferLabelProperty);
            set => SetValue(UpperWaferLabelProperty, value);
        }
        public static readonly DependencyProperty UpperWaferLabelProperty =
            DependencyProperty.Register(nameof(UpperWaferLabel), typeof(string), typeof(WaferRobot),
                new PropertyMetadata(string.Empty, (d, e) =>
                {
                    var r = (WaferRobot)d;
                    if (r.WaferCtrl_Upper != null) r.WaferCtrl_Upper.WaferLabel = (string)e.NewValue;
                }));

        public Brush UpperWaferFontColor
        {
            get => (Brush)GetValue(UpperWaferFontColorProperty);
            set => SetValue(UpperWaferFontColorProperty, value);
        }
        public static readonly DependencyProperty UpperWaferFontColorProperty =
            DependencyProperty.Register(nameof(UpperWaferFontColor), typeof(Brush), typeof(WaferRobot),
                new PropertyMetadata(Brushes.White, (d, e) =>
                {
                    var r = (WaferRobot)d;
                    if (r.WaferCtrl_Upper != null) r.WaferCtrl_Upper.FontColor = (Brush)e.NewValue;
                }));

        public FontFamily UpperWaferFontFamily
        {
            get => (FontFamily)GetValue(UpperWaferFontFamilyProperty);
            set => SetValue(UpperWaferFontFamilyProperty, value);
        }
        public static readonly DependencyProperty UpperWaferFontFamilyProperty =
            DependencyProperty.Register(nameof(UpperWaferFontFamily), typeof(FontFamily), typeof(WaferRobot),
                new PropertyMetadata(new FontFamily("Segoe UI"), (d, e) =>
                {
                    var r = (WaferRobot)d;
                    if (r.WaferCtrl_Upper != null) r.WaferCtrl_Upper.WaferFontFamily = (FontFamily)e.NewValue;
                }));

        public double UpperWaferFontSize
        {
            get => (double)GetValue(UpperWaferFontSizeProperty);
            set => SetValue(UpperWaferFontSizeProperty, value);
        }
        public static readonly DependencyProperty UpperWaferFontSizeProperty =
            DependencyProperty.Register(nameof(UpperWaferFontSize), typeof(double), typeof(WaferRobot),
                new PropertyMetadata(16d, (d, e) =>
                {
                    var r = (WaferRobot)d;
                    if (r.WaferCtrl_Upper != null) r.WaferCtrl_Upper.WaferFontSize = (double)e.NewValue;
                }));

        public FontWeight UpperWaferFontWeight
        {
            get => (FontWeight)GetValue(UpperWaferFontWeightProperty);
            set => SetValue(UpperWaferFontWeightProperty, value);
        }
        public static readonly DependencyProperty UpperWaferFontWeightProperty =
            DependencyProperty.Register(nameof(UpperWaferFontWeight), typeof(FontWeight), typeof(WaferRobot),
                new PropertyMetadata(FontWeights.SemiBold, (d, e) =>
                {
                    var r = (WaferRobot)d;
                    if (r.WaferCtrl_Upper != null) r.WaferCtrl_Upper.WaferFontWeight = (FontWeight)e.NewValue;
                }));

        #endregion

        private void ApplyAllWaferProperties()
        {
            try
            {
                if (WaferCtrl_Lower != null)
                {
                    WaferCtrl_Lower.WaferVisible = LowerWaferVisible;
                    WaferCtrl_Lower.Status = LowerWaferStatus;
                    WaferCtrl_Lower.WaferLabel = LowerWaferLabel;
                    WaferCtrl_Lower.FontColor = LowerWaferFontColor ?? WaferCtrl_Lower.FontColor;
                    WaferCtrl_Lower.WaferFontFamily = LowerWaferFontFamily ?? WaferCtrl_Lower.WaferFontFamily;
                    WaferCtrl_Lower.WaferFontSize = LowerWaferFontSize;
                    WaferCtrl_Lower.WaferFontWeight = LowerWaferFontWeight;
                }

                if (WaferCtrl_Upper != null)
                {
                    WaferCtrl_Upper.WaferVisible = UpperWaferVisible;
                    WaferCtrl_Upper.Status = UpperWaferStatus;
                    WaferCtrl_Upper.WaferLabel = UpperWaferLabel;
                    WaferCtrl_Upper.FontColor = UpperWaferFontColor ?? WaferCtrl_Upper.FontColor;
                    WaferCtrl_Upper.WaferFontFamily = UpperWaferFontFamily ?? WaferCtrl_Upper.WaferFontFamily;
                    WaferCtrl_Upper.WaferFontSize = UpperWaferFontSize;
                    WaferCtrl_Upper.WaferFontWeight = UpperWaferFontWeight;
                }
            }
            catch
            {
                // 安全保护：在设计时和资源加载顺序不同的情况下避免崩溃
            }
        }

        #region ======================= 依赖属性变化回调（角度） =======================

        private static void OnAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as WaferRobot)?.ApplyAllAngles();
        }

        #endregion

        #region ======================= 初始化直接赋值 =======================

        private void ApplyAllAngles()
        {
            // 直接套用当前 DP 值到 RotateTransform（用于初始化或非动画场景）
            if (BaseRotate != null) BaseRotate.Angle = BaseAngle;
            if (Arm1_1_Rotate != null) Arm1_1_Rotate.Angle = Arm1_1Angle;
            if (Arm1_2_Rotate != null) Arm1_2_Rotate.Angle = Arm1_2Angle;
            if (Arm1_3_Rotate != null) Arm1_3_Rotate.Angle = Arm1_3Angle;

            if (Arm2_1_Rotate != null) Arm2_1_Rotate.Angle = Arm2_1Angle;
            if (Arm2_2_Rotate != null) Arm2_2_Rotate.Angle = Arm2_2Angle;
            if (Arm2_3_Rotate != null) Arm2_3_Rotate.Angle = Arm2_3Angle;
        }

        #endregion

        #region ======================= 丝滑依赖属性动画 =======================

        private void AnimateAngle(DependencyProperty dp, double fromValue, double toValue, int duration = 800, Action completed = null)
        {
            var ani = new DoubleAnimation
            {
                From = fromValue,
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
                FillBehavior = FillBehavior.Stop
            };

            ani.Completed += (s, e) =>
            {
                SetValue(dp, toValue);
                completed?.Invoke();
            };

            BeginAnimation(dp, ani);
        }

        #endregion

        #region ======================= 外部可调动画接口（带回调） =======================

        public void AnimateBaseTo(double target, Action done = null) =>
            AnimateAngle(BaseAngleProperty, BaseAngle, target, 800, done);

        public void AnimateArm1_1To(double target, Action done = null) =>
            AnimateAngle(Arm1_1AngleProperty, Arm1_1Angle, target, 800, done);

        public void AnimateArm1_2To(double target, Action done = null) =>
            AnimateAngle(Arm1_2AngleProperty, Arm1_2Angle, target, 800, done);

        public void AnimateArm1_3To(double target, Action done = null) =>
            AnimateAngle(Arm1_3AngleProperty, Arm1_3Angle, target, 800, done);

        // 注意：右臂可能镜像/方向相反，所以动画接口内部保持和你之前一致的符号约定（-target / -value）
        public void AnimateArm2_1To(double target, Action done = null) =>
            AnimateAngle(Arm2_1AngleProperty, Arm2_1Angle, -target, 800, done);

        public void AnimateArm2_2To(double target, Action done = null) =>
            AnimateAngle(Arm2_2AngleProperty, Arm2_2Angle, target, 800, done);

        public void AnimateArm2_3To(double target, Action done = null) =>
            AnimateAngle(Arm2_3AngleProperty, Arm2_3Angle, -target, 800, done);

        #endregion

        #region ======================= ★★ 动作队列 Action Queue ★★ =======================

        private readonly Queue<Action<Action>> _actionQueue = new();
        private bool _isRunning = false;

        /// <summary>
        /// Enqueue 一个动作。动作签名为 Action<Action>，当动作完成时必须调用传入的回调以继续队列。
        /// 例如： EnqueueAction(next => { AnimateBaseTo(30, () => { AnimateArm1_1To(100, next); }); });
        /// </summary>
        public void EnqueueAction(Action<Action> act)
        {
            _actionQueue.Enqueue(act);
        }

        public void RunQueue()
        {
            if (_isRunning || _actionQueue.Count == 0)
                return;

            _isRunning = true;
            RunNext();
        }

        private void RunNext()
        {
            if (_actionQueue.Count == 0)
            {
                _isRunning = false;
                return;
            }

            var act = _actionQueue.Dequeue();
            try
            {
                act(() => RunNext());
            }
            catch
            {
                // 避免单个动作抛异常中断队列
                RunNext();
            }
        }

        #endregion

        #region ======================= 右键显示控制面板 =======================

        public event Action RequestShowFloatingPanel;

        private void OnRobotRightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RequestShowFloatingPanel?.Invoke();
        }

        #endregion
    }
}
