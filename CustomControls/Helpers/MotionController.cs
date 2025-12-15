using System;
using System.Threading.Tasks;

namespace CustomControls.Controls
{
    public class MotionController
    {
        private readonly WaferRobot _robot;

        public MotionController(WaferRobot robot)
        {
            _robot = robot;
        }

        // ========== 任务封装 ==========
        private Task AnimateBaseAsync(double angle)
        {
            var tcs = new TaskCompletionSource<bool>();
            _robot.AnimateBaseTo(angle, () => tcs.TrySetResult(true));
            return tcs.Task;
        }

        private Task AnimateArm1_1Async(double val)
        {
            var tcs = new TaskCompletionSource<bool>();
            _robot.AnimateArm1_1To(val, () => tcs.TrySetResult(true));
            return tcs.Task;
        }
        private Task AnimateArm1_2Async(double val)
        {
            var tcs = new TaskCompletionSource<bool>();
            _robot.AnimateArm1_2To(val, () => tcs.TrySetResult(true));
            return tcs.Task;
        }
        private Task AnimateArm1_3Async(double val)
        {
            var tcs = new TaskCompletionSource<bool>();
            _robot.AnimateArm1_3To(val, () => tcs.TrySetResult(true));
            return tcs.Task;
        }

        private Task AnimateArm2_1Async(double val)
        {
            var tcs = new TaskCompletionSource<bool>();
            _robot.AnimateArm2_1To(val, () => tcs.TrySetResult(true));
            return tcs.Task;
        }
        private Task AnimateArm2_2Async(double val)
        {
            var tcs = new TaskCompletionSource<bool>();
            _robot.AnimateArm2_2To(val, () => tcs.TrySetResult(true));
            return tcs.Task;
        }
        private Task AnimateArm2_3Async(double val)
        {
            var tcs = new TaskCompletionSource<bool>();
            _robot.AnimateArm2_3To(val, () => tcs.TrySetResult(true));
            return tcs.Task;
        }

        // ========== 基础常用动作 ==========
        public Task RotateTo(double angle) => AnimateBaseAsync(angle);

        public async Task ExtendFingerA()
        {
            await Task.WhenAll(
                AnimateArm1_1Async(130),
                AnimateArm1_2Async(-260),
                AnimateArm1_3Async(130)
            );
        }
        public async Task ExtendFingerB()
        {
            await Task.WhenAll(
                AnimateArm2_1Async(130),
                AnimateArm2_2Async(-260),
                AnimateArm2_3Async(130)
            );
        }

        public async Task HomeArms()
        {
            await Task.WhenAll(
              
                AnimateArm1_1Async(75),
                AnimateArm1_2Async(-150),
                AnimateArm1_3Async(75),

                AnimateArm2_1Async(75),
                AnimateArm2_2Async(-150),
                AnimateArm2_3Async(75)
            );
        }
        public async Task HomeAll()
        {
            await Task.WhenAll(
                AnimateBaseAsync(0),
                AnimateArm1_1Async(75),
                AnimateArm1_2Async(-150),
                AnimateArm1_3Async(75),

                AnimateArm2_1Async(75),
                AnimateArm2_2Async(-150),
                AnimateArm2_3Async(75)
            );
        }

        // 获取手指对应的晶圆控件
        private WaferControl GetFingerWafer(string finger)
        {
            if (finger == "FingerA") return _robot.WaferCtrl_Lower;
            return _robot.WaferCtrl_Upper;
        }

        // 获取工位晶圆控件
        private WaferControl GetStationWafer(string finger)
        {
            if (finger == "FingerA") return _robot.WaferCtrl_Lower;
            return _robot.WaferCtrl_Upper;
        }

        // ========== 等待晶圆状态 ==========
        private async Task WaitUntil(Func<bool> condition, int timeoutMs = 5000)
        {
            int elapsed = 0;
            while (!condition())
            {
                await Task.Delay(50);
                elapsed += 50;
                if (elapsed >= timeoutMs)
                    throw new TimeoutException("等待晶圆状态超时");
            }
        }


        // =====================================================
        //                       取 片
        // =====================================================
        public async Task Pick(string finger, PoseData pose)
        {
            var fingerWafer = GetFingerWafer(finger);
            var stationWafer = GetStationWafer(finger);

            // 手指必须为空
            if (fingerWafer.WaferVisible)
                throw new Exception("手臂上已有晶圆，无法执行取片");

            // 旋转到位
            await RotateTo(finger == "FingerA" ? pose.J7 : pose.J8);

            // 伸出
            if (finger == "FingerA") await ExtendFingerA();
            else await ExtendFingerB();

            // 等待工位出现晶圆
            stationWafer.WaferVisible = true;
            await WaitUntil(() => stationWafer.WaferVisible);

            // 收回
            await HomeArms();

            // 手指上变成有片
            fingerWafer.WaferVisible = true;
        }


        // =====================================================
        //                       放 片
        // =====================================================
        public async Task Place(string finger, PoseData pose)
        {
            var fingerWafer = GetFingerWafer(finger);
            var stationWafer = GetStationWafer(finger);

            if (!fingerWafer.WaferVisible)
                throw new Exception("手臂为空，无法执行放片");

            // 旋转到位
            await RotateTo(finger == "FingerA" ? pose.J7 : pose.J8);

            // 伸出
            if (finger == "FingerA") await ExtendFingerA();
            else await ExtendFingerB();
            stationWafer.WaferVisible = false;
            // 等待工位变为空
            await WaitUntil(() => !stationWafer.WaferVisible);

            // 收回
            await HomeArms();

            // 手指变为空
            fingerWafer.WaferVisible = false;
        }

        // Move to pose: rotate then move hand (serial: rotate -> move hand)
        public async Task MoveToPose(PoseData p, string finger)
        {
            // Decide base angle depending on mapping (we stored J7/J8 for base)
            double baseAngle = (finger == "FingerB") ? p.J8 : p.J7;

            // 先旋转
            await RotateTo(baseAngle);

            // 再移动对应手指（并行移动三个关节）
            if (finger == "FingerB")
            {
                var t1 = AnimateArm2_1Async(p.J4);
                var t2 = AnimateArm2_2Async(-p.J5);
                var t3 = AnimateArm2_3Async(p.J6);
                await Task.WhenAll(t1, t2, t3);
            }
            else
            {
                var t1 = AnimateArm1_1Async(p.J1);
                var t2 = AnimateArm1_2Async(p.J2);
                var t3 = AnimateArm1_3Async(p.J3);
                await Task.WhenAll(t1, t2, t3);
            }
        }
    }
}
