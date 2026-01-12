using CustomControls.Controls;
using HostComputer.Common.Base;
using HostComputer.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace HostComputer.ViewModels.Overview
{
    public class EquipmentViewModel : ViewModelBase
    {
        public ObservableCollection<DeviceItemModel> DeviceList { get; set; } = new();

        public EquipmentViewModel()
        {

            // 测试机械手实例化
            Task.Run(async () =>
            {
                //// 等待 DeviceList 初始化完毕（这里假设你先把设备加进 DeviceList）
                //while (DeviceList.Count == 0)
                //    await Task.Delay(100);

                //// 取第一个机械手测试
                //var robotDevice = DeviceList.FirstOrDefault(d => d.DeviceType == "WaferRobot");
                //if (robotDevice == null) return;

                //var robot = robotDevice.DeviceControl as WaferRobot;
                //if (robot == null) return;

                //// 测试 Pick / Place
                //var testPose = new PoseData { J1 = 130, J2 = -260, J3 = 130, J4 = 75, J5 = 150, J6 = 130 };

                //// 测试抓取
                //await robot.Controller.Pick("FingerA", testPose);

                //// 测试放置
                //await robot.Controller.Place("FingerA", testPose);
            });
        }
    }
}
