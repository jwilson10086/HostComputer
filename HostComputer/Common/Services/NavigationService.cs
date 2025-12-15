using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HostComputer.Common.Base;

namespace HostComputer.Common.Services
{
    public class NavigationState
    {

        public UserControl Page { get; set; } = null!;
        public string Breadcrumb { get; set; } = "";
        public string? Menu1 { get; set; }
        public string? Menu2 { get; set; }
        public string? Menu3 { get; set; }
    }

    public class NavigationService
    {
        private readonly ContentControl _contentHost;
        private readonly int _maxHistory = 20;
        private readonly LinkedList<NavigationState> _history = new();
        private NavigationState? _currentState;

        public event Action<NavigationState>? OnNavigated;
        public event Action<string>? OnPopupClosed; // 弹窗关闭事件，参数是窗口名

        public NavigationService(ContentControl contentHost)
        {
            _contentHost = contentHost;
        }

        public void Navigate(
            string viewName,
            string breadcrumb,
            string? menu1 = null,
            string? menu2 = null,
            string? menu3 = null
        )
        {
            var instance = CreatePageInstance(viewName);
            if (instance == null)
                return;

            if (_currentState != null)
            {
                _history.AddLast(_currentState);
                while (_history.Count > _maxHistory)
                    _history.RemoveFirst();
            }

            _contentHost.Content = instance;

            _currentState = new NavigationState
            {
                Page = instance,
                Breadcrumb = breadcrumb,
                Menu1 = menu1,
                Menu2 = menu2,
                Menu3 = menu3
            };

            OnNavigated?.Invoke(_currentState);
            App.Logger.Info($"导航服务: 导航到页面 {breadcrumb}");
        }

        /// <summary>
        /// 弹出窗口导航（不记录历史，弹窗关闭后触发 OnPopupClosed 回调）
        /// </summary>
        public void NavigatePopup(
            string windowName,
            bool modal = true,
            Action<Window>? configureWindow = null
        )
        {
            var assembly = typeof(App).Assembly;
            var type = assembly
                .GetTypes()
                .FirstOrDefault(t => t.Name == windowName && t.IsSubclassOf(typeof(Window)));

            if (type == null)
            {
                App.Logger.Warning($"导航服务: 未找到窗口类型 {windowName}");
                return;
            }

            var window = (Window)Activator.CreateInstance(type)!;
            configureWindow?.Invoke(window);

            // 注册关闭事件
            window.Closed += (s, e) =>
            {
                OnPopupClosed?.Invoke(windowName);
                App.Logger.Info($"导航服务: 弹窗 {windowName} 已关闭");
            };

            if (modal)
            {
                App.Logger.Info($"导航服务: 弹出窗口 {windowName}");
                window.ShowDialog();
            }
            else
            {
                App.Logger.Info($"导航服务: 弹出窗口 {windowName} (非模态)");
                window.Show();
            }
        }

        public void GoBack()
        {
            if (_history.Count == 0)
                return;

            var previous = _history.Last.Value;
            _history.RemoveLast();

            previous.Page.HorizontalAlignment = HorizontalAlignment.Stretch;
            previous.Page.VerticalAlignment = VerticalAlignment.Stretch;

            _contentHost.Content = previous.Page;
            _currentState = previous;

            OnNavigated?.Invoke(previous);
            App.Logger.Info($"导航服务: 回退到页面 {previous.Breadcrumb}");
        }

        public void ClearHistory() => _history.Clear();

        private UserControl? CreatePageInstance(string viewName)
        {
            var assembly = typeof(App).Assembly;
            var type = assembly
                .GetTypes()
                .FirstOrDefault(t => t.Name == viewName && t.IsSubclassOf(typeof(UserControl)));

            if (type == null)
            {
                App.Logger.Warning($"导航服务: 未找到页面类型 {viewName}");
                return null;
            }

            var instance = (UserControl)Activator.CreateInstance(type)!;
            instance.HorizontalAlignment = HorizontalAlignment.Stretch;
            instance.VerticalAlignment = VerticalAlignment.Stretch;
            return instance;
        }
    }
}
