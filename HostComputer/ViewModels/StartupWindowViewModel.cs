using HostComputer.Common.Base;
using HostComputer.Common.Services;
using MyLogger;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace HostComputer.ViewModels
{
    /// <summary>
    /// 启动窗口的 ViewModel，继承自 NotifyBase
    /// </summary>
    public class StartupWindowViewModel : ObservableObject
    {
        #region 事件
        public event EventHandler RequestCloseWindow; // 请求关闭窗口事件
        #endregion

        #region 字段
        private readonly StartupManager _startupManager;
        private readonly Logger _logger;
        private DateTime _startTime;
        private System.Windows.Threading.DispatcherTimer _timer;
        private bool _isInitializing = false;
        #endregion

        #region 绑定属性
        private string _statusText = "正在准备系统...";
        public string StatusText
        {
            get => _statusText;
            set => Set(ref _statusText, value);
        }

        private double _overallProgress;
        public double OverallProgress
        {
            get => _overallProgress;
            set
            {
                if (Set(ref _overallProgress, value))
                {
                    Raise(nameof(ProgressText));
                }
            }
        }

        public string ProgressText => $"{_overallProgress:F0}%";

        private string _elapsedTime = "0s";
        public string ElapsedTime
        {
            get => _elapsedTime;
            set => Set(ref _elapsedTime, value);
        }

        public ObservableCollection<ModuleStatusViewModel> Modules { get; } = new();
        #endregion

        #region 构造函数
        public StartupWindowViewModel(Logger logger)
        {
            _logger = logger;
            _startupManager = new StartupManager(logger);
            _startupManager.ProgressChanged += OnProgressChanged;
        }
        #endregion

        #region 公共方法
        public async Task InitializeAsync()
        {
            if (_isInitializing) return;
            _isInitializing = true;

            _startTime = DateTime.Now;
            StartElapsedTimer();

            try
            {
                StatusText = "开始系统初始化...";

                // 执行初始化
                var result = await _startupManager.InitializeAsync(progress =>
                {
                    // 更新总体进度
                    OverallProgress = progress;
                });

                await Task.Delay(500); // 给用户一点时间看完成状态

                if (result.IsSuccess || result.SuccessRate >= 0.7)
                {
                    StatusText = "系统初始化完成";
                    await Task.Delay(300);

                    // 启动完成，关闭启动窗口并显示登录窗口
                    await CloseStartupAndShowLoginAsync();
                }
                else
                {
                    StatusText = "系统初始化失败";
                    await Task.Delay(300);

                    // 启动失败，直接退出系统
                    Application.Current.Shutdown(1);
                }
            }
            catch (Exception ex)
            {
                StatusText = $"初始化异常: {ex.Message}";
                _logger.Fatal($"启动过程异常: {ex}");

                MessageBox.Show(
                    $"系统初始化失败: {ex.Message}\n应用程序将退出。",
                    "启动错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Application.Current.Shutdown(1);
            }
            finally
            {
                _timer?.Stop();
                _isInitializing = false;
            }
        }

        /// <summary>
        /// 关闭启动窗口并显示登录窗口
        /// </summary>
        private async Task CloseStartupAndShowLoginAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    _logger.Info("启动完成，准备关闭启动窗口并显示登录窗口...");

                    // 1. 首先关闭启动窗口
                    RequestCloseWindow?.Invoke(this, EventArgs.Empty);

                    // 2. 等待一小段时间确保窗口关闭动画完成
                    await Task.Delay(300);

                    // 3. 显示登录窗口
                    await ShowLoginWindowAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error($"显示登录窗口时发生错误: {ex.Message}");
                    MessageBox.Show(
                        $"登录窗口启动失败: {ex.Message}\n应用程序将退出。",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Application.Current.Shutdown(1);
                }
            });
        }

        /// <summary>
        /// 显示登录窗口并处理结果
        /// </summary>
        private async Task ShowLoginWindowAsync()
        {
            try
            {
                // 创建登录窗口
                var loginWindow = new Views.LoginWindow();

                // 使用 ShowDialog 显示模态窗口
                bool? dialogResult = loginWindow.ShowDialog();

                // 处理登录结果
                if (dialogResult == true)
                {
                    // 登录成功，显示主窗口
                    await ShowMainWindowAsync();
                }
                else
                {
                    // 登录失败或取消，关闭系统
                    _logger.Info("用户登录失败或取消，退出系统");
                    Application.Current.Shutdown(0);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"登录窗口异常: {ex.Message}");
                MessageBox.Show(
                    $"登录过程发生错误: {ex.Message}\n应用程序将退出。",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current.Shutdown(1);
            }
        }

        /// <summary>
        /// 显示主窗口
        /// </summary>
        private async Task ShowMainWindowAsync()
        {
            try
            {
                _logger.Info("用户登录成功，显示主窗口...");

                // 在主UI线程上创建并显示主窗口
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var mainWindow = new Views.MainWindow();
                    mainWindow.Show();
                });

                _logger.Info("主窗口已显示");
            }
            catch (Exception ex)
            {
                _logger.Error($"显示主窗口失败: {ex.Message}");
                MessageBox.Show(
                    $"主窗口启动失败: {ex.Message}\n应用程序将退出。",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current.Shutdown(1);
            }
        }
        #endregion

        #region 私有方法
        private void OnProgressChanged(object sender, StartupProgressEventArgs e)
        {
            // 在UI线程上更新
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 更新状态文本
                if (!string.IsNullOrEmpty(e.ModuleName) && e.ModuleName != "完成")
                {
                    StatusText = $"正在初始化: {e.ModuleName}";
                }

                // 添加到模块列表
                if (!string.IsNullOrEmpty(e.ModuleName) && e.ModuleName != "环境检查")
                {
                    var existing = Modules.FirstOrDefault(m => m.Name == e.ModuleName);
                    if (existing != null)
                    {
                        existing.Status = e.Status;
                        existing.Message = e.Message;
                        existing.Duration = $"{e.Duration.TotalMilliseconds:F0}ms";
                    }
                    else
                    {
                        Modules.Add(new ModuleStatusViewModel
                        {
                            Name = e.ModuleName,
                            Status = e.Status,
                            Message = e.Message,
                            Duration = $"{e.Duration.TotalMilliseconds:F0}ms"
                        });
                    }
                }

                // 更新进度
                if (e.Progress >= 0)
                {
                    OverallProgress = e.Progress;
                }
            });
        }

        private void StartElapsedTimer()
        {
            _timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.1)
            };

            _timer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - _startTime;
                ElapsedTime = $"{elapsed.TotalSeconds:F1}s";
            };

            _timer.Start();
        }
        #endregion
    }

    /// <summary>
    /// 模块状态视图模型，继承自 NotifyBase
    /// </summary>
    public class ModuleStatusViewModel : ObservableObject
    {
        private string _name;
        private string _status;
        private string _message;
        private string _duration;

        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        public string Status
        {
            get => _status;
            set
            {
                if (Set(ref _status, value))
                {
                    Raise(nameof(StatusColor));
                }
            }
        }

        public string Message
        {
            get => _message;
            set => Set(ref _message, value);
        }

        public string Duration
        {
            get => _duration;
            set => Set(ref _duration, value);
        }

        public string StatusColor => Status switch
        {
            "Success" => "#2ecc71",
            "Failed" => "#e74c3c",
            "Started" => "#3498db",
            "Skipped" => "#f39c12",
            _ => "#95a5a6"
        };
    }

    /// <summary>
    /// 简单的中继命令实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}