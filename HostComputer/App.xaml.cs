using Hardcodet.Wpf.TaskbarNotification;
using HostComputer.Common.Languages;
using HostComputer.Common.Services;
using HostComputer.Common.Services.StartupModules;
using HostComputer.Common.Session;
using HostComputer.Views;
using MyLogger;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;

namespace HostComputer
{
    public partial class App : Application
    {
        private MainWindow _mainWindow;
        private TaskbarIcon _trayIcon;
        private ResourceDictionary? _currentTheme;
        public static Session Session { get; } = new Session();
        public static LanguageService Lang { get; private set; }
        public static Logger Logger { get; private set; }

        /******************************************************
         *                      Win32 API
         ******************************************************/
        #region Win32 API

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        private delegate bool ConsoleEventDelegate(int eventType);
        private static ConsoleEventDelegate _consoleHandler;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool DeleteMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        // 更稳定的方式：修改窗口样式移除关闭按钮
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const uint SC_CLOSE = 0xF060;
        private const uint MF_BYCOMMAND = 0x00000000;

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x00080000;

        private IntPtr _consoleHandle;

        #endregion



        /******************************************************
         *                   应用程序初始化
         ******************************************************/
        static App()
        {
            Lang = new LanguageService();
        }
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 初始化日志（先于其他所有操作）
            Logger = new Logger(AppConfiguration.Current.LoggingConfig);
           
            try
            {
                #region 初始化托盘图标
                try
                {
                    _trayIcon = this.Resources["TrayIcon"] as TaskbarIcon;
                }
                catch
                {
                    _trayIcon = null;
                }

                if (_trayIcon == null)
                    Logger?.Info("托盘图标初始化失败：资源 TrayIcon 未找到。");
                #endregion


                #region 初始化控制台
                AllocConsole();
                _consoleHandle = GetConsoleWindow();

                DisableConsoleCloseButton();

                _consoleHandler = new ConsoleEventDelegate(ConsoleEventCallback);
                SetConsoleCtrlHandler(_consoleHandler, true);

                Logger.Debug("程序启动，控制台已显示。");
                #endregion
                Logger.Startup("🚀 应用程序启动开始");
                Logger.Startup($"启动参数: {string.Join(" ", e.Args)}");
                Logger.Startup($"工作目录: {Environment.CurrentDirectory}");

                // 显示启动窗口（传入Logger实例）
                var startupWindow = new StartupWindow(Logger);
                startupWindow.Show();

                Logger.Startup("启动窗口已显示，开始后台初始化");

                //ApplyUIConfig();


                HostComputer.Common.Services.StartupModules.AppConfiguration.OnConfigChanged += () =>
                {
                    // 已在配置模块内部用 Dispatcher 调度到 UI 线程
                    ApplyUIConfig();
                };
            }
            catch (Exception ex)
            {
                // 如果日志系统都初始化失败，使用MessageBox
                MessageBox.Show($"应用程序启动失败: {ex.Message}\n{ex.StackTrace}",
                    "致命错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger?.Info($"应用程序退出，退出码: {e.ApplicationExitCode}");
            Logger?.Dispose();
            base.OnExit(e);
        }

        private void CleanupAndExit()
        {
            Logger.Startup("开始应用程序清理...");

            try
            {
                _trayIcon?.Dispose();
                HideConsole();
                SetConsoleCtrlHandler(_consoleHandler, false);
            }
            catch (Exception ex)
            {
                Logger.Warning($"清理过程中发生错误: {ex.Message}");
            }

            Logger.Startup("应用程序退出");
            Logger.Dispose();
            Shutdown();
        }
        /********************************************************
          *                 界面配置应用
          ******************************************************/
        #region 界面配置应用
        public void ApplyTheme(string themeName)
        {
            try
            {
                // 移除旧主题
                if (_currentTheme != null)
                {
                    Resources.MergedDictionaries.Remove(_currentTheme);
                    _currentTheme = null;
                }

                // 找到主题文件
                var uri = new Uri($"/Assets/Styles/Themes/{themeName}.xaml", UriKind.Relative);
                var dict = new ResourceDictionary { Source = uri };

                Resources.MergedDictionaries.Add(dict);
                _currentTheme = dict;
            }
            catch (Exception ex)
            {
                // 如果加载失败，写日志并忽略
                Logger?.Error($"加载主题失败: {ex.Message}");
            }
        }

        public void ApplyUIConfig()
        {
            var ui = AppConfiguration.Current?.UI;
            if (ui == null) return;

            // 更新基础资源（DynamicResource）
            Resources["Global.FontSize"] = ui.FontSize;
            Resources["Global.CornerRadius"] = ui.CornerRadius;
            Resources["DefaultWindowWidth"] = ui.WindowWidth;
            Resources["DefaultWindowHeight"] = ui.WindowHeight;

            // 应用主题（Dark/Light）
            ApplyTheme(ui.Theme ?? "Dark");

            // 如果需要，强制刷新现有窗口（大多数控件会响应 DynamicResource）
            foreach (Window w in Current.Windows)
            {
                // 对于字体大小变更，重新设置 FontSize 可以强制应用（可选）
                w.Dispatcher.Invoke(() =>
                {
                    if (Resources.Contains("Global.FontSize"))
                        w.FontSize = (double)Resources["Global.FontSize"];
                });
            }

            Logger?.Config($"应用 UI 配置: Theme={ui.Theme}, FontSize={ui.FontSize}, Window={ui.WindowWidth}x{ui.WindowHeight}");
        }
        #endregion

        /******************************************************
         *                  主窗口相关逻辑
         ******************************************************/
        #region 主窗口显示与隐藏

        public void ShowMainWindow()
        {
            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow();
                _mainWindow.Closing += MainWindow_Closing;
                _mainWindow.Closed += (s, e) => _mainWindow = null;
            }

            _mainWindow.Show();
            _mainWindow.Activate();
        }

        private void HideMainWindow() => _mainWindow?.Hide();

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!_isExiting)
            {
                e.Cancel = true;
                HideMainWindow();
            }
        }

        #endregion



        /******************************************************
         *                    程序退出
         ******************************************************/
        #region 退出程序

        private bool _isExiting = false;

        public void ExitApplication()
        {
            _isExiting = true;

            var result = MessageBox.Show(
                "确定要退出程序吗？",
                "退出确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No,
                MessageBoxOptions.DefaultDesktopOnly);

            if (result == MessageBoxResult.Yes)
            {
                
                try { _trayIcon?.Dispose(); } catch { }
                try { _mainWindow?.Close(); } catch { }
                
                HideConsole();
                try { SetConsoleCtrlHandler(_consoleHandler, false); } catch { }
                CleanupAndExit();
                Shutdown();
            }
            else
            {
                _isExiting = false;
            }
        }

        #endregion



        /******************************************************
         *                托盘菜单事件
         ******************************************************/
        #region 托盘菜单事件

        private void ShowMainWindow_Click(object sender, RoutedEventArgs e) => ShowMainWindow();
        private void HideMainWindow_Click(object sender, RoutedEventArgs e) => HideMainWindow();
        private void ShowConsole_Click(object sender, RoutedEventArgs e) => ShowConsole();
        private void CloseConsole_Click(object sender, RoutedEventArgs e) => HideConsole();
        private void ExitApp_Click(object sender, RoutedEventArgs e) => ExitApplication();

        #endregion



        /******************************************************
         *                  控制台管理
         ******************************************************/
        #region 控制台显示控制

        private void ShowConsole()
        {
            if (_consoleHandle == IntPtr.Zero)
                _consoleHandle = GetConsoleWindow();
            if (_consoleHandle != IntPtr.Zero)
                ShowWindow(_consoleHandle, SW_SHOW);
        }

        private void HideConsole()
        {
            if (_consoleHandle == IntPtr.Zero)
                _consoleHandle = GetConsoleWindow();
            if (_consoleHandle != IntPtr.Zero)
                ShowWindow(_consoleHandle, SW_HIDE);
        }

        private void DisableConsoleCloseButton()
        {
            if (_consoleHandle == IntPtr.Zero)
                return;

            try
            {
                int style = GetWindowLong(_consoleHandle, GWL_STYLE);
                SetWindowLong(_consoleHandle, GWL_STYLE, style & ~WS_SYSMENU);
            }
            catch (Exception ex)
            {
                Logger?.Warning($"DisableConsoleCloseButton 失败: {ex.Message}");
            }
        }

        #endregion



        /******************************************************
         *                 控制台事件拦截
         ******************************************************/
        #region 控制台事件回调

        private bool ConsoleEventCallback(int eventType)
        {
            const int CTRL_C_EVENT = 0;
            const int CTRL_BREAK_EVENT = 1;
            const int CTRL_CLOSE_EVENT = 2;
            const int CTRL_LOGOFF_EVENT = 5;
            const int CTRL_SHUTDOWN_EVENT = 6;

            switch (eventType)
            {
                case CTRL_CLOSE_EVENT:
                    HideConsole();
                    return true;
                default:
                    return false;
            }
        }

        #endregion
    }
}
