using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CustomControls.Controls;
using HostComputer.Common.Base;

namespace HostComputer.ViewModels
{
   public class RobotViewModel : ViewModelBase
{
    public string RobotId { get; }
    public WaferRobot RobotCtrl { get; }

    private FloatingPanel? _floatingPanel;

    public RobotViewModel(WaferRobot robot, string robotId)
    {
        RobotId = robotId;
        RobotCtrl = robot;

        // ⭐ 只在这里绑定一次
        RobotCtrl.RequestShowFloatingPanel += OnRequestShowFloatingPanel;
    }

    private void OnRequestShowFloatingPanel()
    {
        // 已存在
        if (_floatingPanel != null)
        {
            if (_floatingPanel.IsVisible)
            {
                _floatingPanel.Activate();
                return;
            }
            _floatingPanel = null;
        }

        // 新建
        _floatingPanel = new FloatingPanel(RobotCtrl);
        _floatingPanel.Closed += (_, __) => _floatingPanel = null;

        _floatingPanel.Show();
        _floatingPanel.Activate();
    }
}

}
