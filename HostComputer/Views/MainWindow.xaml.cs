using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HostComputer.Common.Actions;
using HostComputer.Common.Services;
using HostComputer.Models;
using HostComputer.ViewModels;

namespace HostComputer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private List<MenuItemModel> _menuData;

        public MainWindow()
        {
            InitializeComponent();
            var nav = new NavigationService(PageHost);
            DataContext = new MainViewModel(nav);
            // 注册 ActionManager 示例
            ActionManager.Register<object, bool>(
                "AAA",
                arg =>
                {
                    // 获取当前活跃窗口
                    var owner = Application
                        .Current.Windows.OfType<Window>()
                        .FirstOrDefault(w => w.IsActive);
                    var vm = new ConfigViewModel
                    {
                        SourceViewName = nav._currentState?.Page.GetType().Name??"Unknown", // 这里指定当前页面标识
                    };
                    nav.NavigatePopup(
                        "ComponentConfigView",
                        true,
                        w =>
                        {
                            w.Width = 1200;
                            w.Height = 800;
                            w.Owner = owner; 
                            w.DataContext = vm;
                        }
                    );
                    return new ComponentConfigView().DialogResult == true;
                }
            );
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
