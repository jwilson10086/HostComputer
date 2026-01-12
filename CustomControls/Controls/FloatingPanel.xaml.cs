// 文件：FloatingPanel.xaml.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CustomControls.Helpers;

namespace CustomControls.Controls
{
    public partial class FloatingPanel : Window
    {
        private readonly string _poseFile = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "RobotPose.csv"
        );
        private readonly WaferRobot _robot;
        private readonly MotionController _motion;

        private readonly DispatcherTimer _leftTimer = new DispatcherTimer();
        private readonly DispatcherTimer _rightTimer = new DispatcherTimer();
        private readonly DispatcherTimer _baseTimer = new DispatcherTimer();

        private double _leftTarget;
        private double _rightTarget;
        private double _baseTarget;
        private const int ThrottleMs = 30;

        public FloatingPanel(WaferRobot robot)
        {
            InitializeComponent();

            _robot = robot ?? throw new ArgumentNullException(nameof(robot));
            _motion = _robot.Controller;
            DataContext = robot;

            _leftTimer.Interval = TimeSpan.FromMilliseconds(ThrottleMs);
            _leftTimer.Tick += LeftTimer_Tick;
            _rightTimer.Interval = TimeSpan.FromMilliseconds(ThrottleMs);
            _rightTimer.Tick += RightTimer_Tick;
            _baseTimer.Interval = TimeSpan.FromMilliseconds(ThrottleMs);
            _baseTimer.Tick += BaseTimer_Tick;

            CsvHelperEx.EnsureFile(_poseFile);

            InitUI();
            BindEvents();
        }

        private void InitUI()
        {
            FingerSelector.ItemsSource = new[] { "FingerA", "FingerB" };
            StationSelector.ItemsSource = Enumerable
                .Range(1, 12)
                .Select(i => $"Station-{i}")
                .ToList();

            SliderLeft.Value = _robot.Arm1_1Angle;
            SliderRight.Value = _robot.Arm2_1Angle;
            SliderBase.Value = _robot.BaseAngle;
        }

        private void BindEvents()
        {
            SliderLeft.ValueChanged += (s, e) =>
            {
                _leftTarget = e.NewValue;
                _leftTimer.Stop();
                _leftTimer.Start();
            };

            SliderRight.ValueChanged += (s, e) =>
            {
                _rightTarget = e.NewValue;
                _rightTimer.Stop();
                _rightTimer.Start();
            };

            SliderBase.ValueChanged += (s, e) =>
            {
                _baseTarget = e.NewValue;
                _baseTimer.Stop();
                _baseTimer.Start();
            };

            BtnExtendLeft.Click += (s, e) => _ = _motion.ExtendFingerA();
            BtnExtendRight.Click += (s, e) => _ = _motion.ExtendFingerB();
            BtnHome.Click += (s, e) => _ = _motion.HomeAll();
            BtnRotateBase.Click += (s, e) => _ = _motion.RotateTo((_robot.BaseAngle + 35) % 360);
            BtnClose.Click += (s, e) => Close();

            BtnTeach.Click += BtnTeach_Click;
            BtnLocate.Click += async (s, e) => await BtnLocate_ClickAsync();
            BtnOpenPositions.Click += BtnOpenPose_Click;
            BtnPick.Click += (s, e) => _ = BtnPick_Click();
            BtnPlace.Click += (s, e) => _ = BtnPlace_Click();
        }

        private async Task BtnPlace_Click()
        {
            if (FingerSelector.SelectedItem == null || StationSelector.SelectedItem == null)
            {
                ShowMessage("请先选择手指 和 站位！");
                return;
            }

            string finger = FingerSelector.SelectedItem.ToString();
            string station = StationSelector.SelectedItem.ToString();

            // read current joint mapping: (注意：保持与你的 WaferRobot 映射一致)
            var pose =
                CsvHelperEx.FindPose(_poseFile, station) ?? new PoseData { Station = station };

            await _motion.Place(finger, pose);

            ShowMessage("放置成功！");
            return;
        }

        private async Task BtnPick_Click()
        {
            if (FingerSelector.SelectedItem == null || StationSelector.SelectedItem == null)
            {
                ShowMessage("请先选择手指 和 站位！");
                return;
            }

            string finger = FingerSelector.SelectedItem.ToString();
            string station = StationSelector.SelectedItem.ToString();

            // read current joint mapping: (注意：保持与你的 WaferRobot 映射一致)
            var pose =
                CsvHelperEx.FindPose(_poseFile, station) ?? new PoseData { Station = station };

            await _motion.Pick(finger, pose);

            ShowMessage("拾取成功！");
            return;
        }

        #region Throttles
        private void LeftTimer_Tick(object sender, EventArgs e)
        {
            _leftTimer.Stop();
            _robot.AnimateArm1_1To(_leftTarget);
            _robot.AnimateArm1_2To(-_leftTarget * 2);
            _robot.AnimateArm1_3To(_leftTarget);
        }

        private void RightTimer_Tick(object sender, EventArgs e)
        {
            _rightTimer.Stop();
            _robot.AnimateArm2_1To(_rightTarget);
            _robot.AnimateArm2_2To(-_rightTarget * 2);
            _robot.AnimateArm2_3To(_rightTarget);
        }

        private void BaseTimer_Tick(object sender, EventArgs e)
        {
            _baseTimer.Stop();
            double newAngle = _baseTarget % 360;
            if (newAngle < 0)
                newAngle += 360;
            _robot.AnimateBaseTo(newAngle);
        }
        #endregion

        #region Teach / Locate / Open
        private void BtnTeach_Click(object sender, RoutedEventArgs e)
        {
            if (FingerSelector.SelectedItem == null || StationSelector.SelectedItem == null)
            {
                ShowMessage("请先选择手指 和 站位！");
                return;
            }

            string finger = FingerSelector.SelectedItem.ToString();
            string station = StationSelector.SelectedItem.ToString();

            // read current joint mapping: (注意：保持与你的 WaferRobot 映射一致)
            var pose =
                CsvHelperEx.FindPose(_poseFile, station) ?? new PoseData { Station = station };

            // mapping (你的约定)：上手指 -> Arm1 (J1..J3), 下手指 -> Arm2 (J4..J6)
            // 但在你之前代码里有用负号映射，我们保持写入原始角度（写什么后面读到时要按你的映射使用）
            // 我采用和你 FloatingPanel 里一致的写法：
            if (finger == "FingerB")
            {
                // FingerB mapping: J4..J6 stored from Arm2 (with sign handled earlier)
                // pose.Finger = "FingerB";
                pose.J4 = -_robot.Arm2_1Angle;
                pose.J5 = -_robot.Arm2_2Angle;
                pose.J6 = -_robot.Arm2_3Angle;
                pose.J8 = _robot.BaseAngle;
            }
            else
            {
                // pose.Finger = "FingerA";
                pose.J1 = _robot.Arm1_1Angle;
                pose.J2 = _robot.Arm1_2Angle;
                pose.J3 = _robot.Arm1_3Angle;
                pose.J7 = _robot.BaseAngle;
            }

            CsvHelperEx.UpsertPose(_poseFile, pose);
            ShowMessage("示教成功！");
        }

        private async Task BtnLocate_ClickAsync()
        {
            if (FingerSelector.SelectedItem == null || StationSelector.SelectedItem == null)
            {
                ShowMessage("请先选择手指 和 站位！");
                return;
            }

            string finger = FingerSelector.SelectedItem.ToString();
            string station = StationSelector.SelectedItem.ToString();

            var pose = CsvHelperEx.FindPose(_poseFile, station);
            if (pose == null)
            {
                ShowMessage("未示教该点位！");
                return;
            }

            // 使用 MotionController 串行动作：旋转 -> 到位 -> 并行移动关节
            await _motion.MoveToPose(pose, finger);

            // 同步滑条显示（把最新角度写到滑条，但不触发节流器动画）
            if (finger == "FingerB")
            {
                _rightTimer.Stop();
                SliderRight.Value = pose.J4;
                _baseTimer.Stop();
                SliderBase.Value = pose.J8;
            }
            else
            {
                _leftTimer.Stop();
                SliderLeft.Value = pose.J1;
                _baseTimer.Stop();
                SliderBase.Value = pose.J7;
            }

            ShowMessage("定位到点位！");
        }

        private void BtnOpenPose_Click(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists(_poseFile))
                System.Diagnostics.Process.Start("explorer.exe", _poseFile);
            else
                ShowMessage("点位文件不存在！");
        }
        #endregion

        #region Message & Drag
        private void ShowMessage(string msg)
        {
            MessageText.Text = msg;
            MessageText.Visibility = Visibility.Visible;
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            t.Tick += (s, e) =>
            {
                MessageText.Visibility = Visibility.Hidden;
                t.Stop();
            };
            t.Start();
        }

        private void Window_MouseLeftButtonDown(
            object sender,
            System.Windows.Input.MouseButtonEventArgs e
        )
        {
            try
            {
                DragMove();
            }
            catch { }
        }
        #endregion
    }
}
