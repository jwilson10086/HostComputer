using HostComputer.ViewModels;
using MyLogger;
using System.Windows;
using System.Windows.Controls;

namespace HostComputer.Views
{
    public partial class StartupWindow : Window
    {
        private StartupWindowViewModel _viewModel;
        private readonly Logger _logger;

        public StartupWindow(Logger logger)
        {
            _logger = logger;
            InitializeComponent();
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            // 创建 ViewModel，传入 Logger
            _viewModel = new StartupWindowViewModel(_logger);
            // 订阅关闭窗口请求事件
            _viewModel.RequestCloseWindow += (s, e) =>
            {
                // 关闭启动窗口
                this.Close();
            };
            // 设置 DataContext
            DataContext = _viewModel;
        }

        private async void StartupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口加载完成后开始初始化
            await _viewModel.InitializeAsync();
        }

        private void ModuleList_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                var viewModel = DataContext as StartupWindowViewModel;
                if (viewModel == null) return;

                viewModel.Modules.CollectionChanged += (s, e2) =>
                {
                    if (listBox.Items.Count > 0)
                    {
                        listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
                    }
                };
            }
        }
    }
}