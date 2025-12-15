using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using CustomControls.Controls;

namespace HostComputer.Views.Overview
{
    /// <summary>
    /// Interaction logic for Overview_EquipmentView.xaml
    /// </summary>
    public partial class Overview_EquipmentView : UserControl
    {
        private DispatcherTimer autoRotationTimer;
        private bool isAutoRotating = false;
        private FloatingPanel _floatingPanel;

        private void InitRobotPanel()
        {
            WaferRobotCtrl.RequestShowFloatingPanel += () =>
            {
                // 如果已经存在并且还没被关闭
                if (_floatingPanel != null)
                {
                    // 已显示 → Bring to front
                    if (_floatingPanel.IsVisible)
                    {
                        _floatingPanel.Activate();
                        return;
                    }
                    else
                    {
                        // 被关闭但引用没清
                        _floatingPanel = null;
                    }
                }

                // 创建新窗口
                _floatingPanel = new FloatingPanel(WaferRobotCtrl);

                // 监听关闭事件 → 自动清 NULL
                _floatingPanel.Closed += (s, e) => _floatingPanel = null;

                _floatingPanel.Show();
                _floatingPanel.Activate();
            };
        }
        public Overview_EquipmentView()
        {
            InitializeComponent();
            InitRobotPanel();
        }
    }
}
