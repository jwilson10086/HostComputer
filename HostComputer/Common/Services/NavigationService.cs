using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HostComputer.Common.Base;

namespace HostComputer.Common.Services
{
    #region NavigationState 导航状态类
    /// <summary>
    /// 导航状态类，用于记录页面导航的相关信息
    /// </summary>
    public class NavigationState
    {
        /// <summary>当前显示的用户控件页面</summary>
        public UserControl Page { get; set; } = null!;

        /// <summary>面包屑导航文本，用于显示当前页面位置</summary>
        public string Breadcrumb { get; set; } = "";

        /// <summary>一级菜单标识（可选）</summary>
        public string? Menu1 { get; set; }

        /// <summary>二级菜单标识（可选）</summary>
        public string? Menu2 { get; set; }

        /// <summary>三级菜单标识（可选）</summary>
        public string? Menu3 { get; set; }
    }
    #endregion

    #region NavigationService 导航服务类
    /// <summary>
    /// 导航服务类，负责管理页面导航、历史记录和弹窗显示
    /// </summary>
    public class NavigationService
    {
        #region 私有字段
        /// <summary>内容宿主控件，用于显示导航的页面</summary>
        private readonly ContentControl _contentHost;

        /// <summary>历史记录最大容量，防止内存泄漏</summary>
        private readonly int _maxHistory = 20;

        /// <summary>导航历史记录，使用链表实现先进先出</summary>
        private readonly LinkedList<NavigationState> _history = new();

        /// <summary>当前导航状态</summary>
        public NavigationState? _currentState;
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化导航服务
        /// </summary>
        /// <param name="contentHost">内容宿主控件，用于显示页面</param>
        public NavigationService(ContentControl contentHost)
        {
            _contentHost = contentHost ?? throw new ArgumentNullException(nameof(contentHost));
        }
        #endregion

        #region 公共事件
        /// <summary>
        /// 导航完成时触发的事件
        /// </summary>
        public event Action<NavigationState>? OnNavigated;

        /// <summary>
        /// 弹窗关闭时触发的事件，参数为窗口名称
        /// </summary>
        public event Action<string>? OnPopupClosed;
        #endregion

        #region 私有方法
        /// <summary>
        /// 创建页面实例
        /// </summary>
        /// <param name="viewName">页面类型名称</param>
        /// <returns>创建的用户控件实例，如果找不到类型则返回null</returns>
        private UserControl? CreatePageInstance(string viewName)
        {
            // 通过反射查找页面类型
            var assembly = typeof(App).Assembly;
            var type = assembly
                .GetTypes()
                .FirstOrDefault(t => t.Name == viewName && t.IsSubclassOf(typeof(UserControl)));

            if (type == null)
            {
                App.Logger.Warning($"导航服务: 未找到页面类型 {viewName}");
                return null;
            }

            // 创建实例并设置布局属性
            var instance = (UserControl)Activator.CreateInstance(type)!;
            instance.HorizontalAlignment = HorizontalAlignment.Stretch;
            instance.VerticalAlignment = VerticalAlignment.Stretch;
            return instance;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 导航到指定页面
        /// </summary>
        /// <param name="viewName">页面类型名称（UserControl的类名）</param>
        /// <param name="breadcrumb">面包屑导航文本</param>
        /// <param name="menu1">一级菜单标识（可选）</param>
        /// <param name="menu2">二级菜单标识（可选）</param>
        /// <param name="menu3">三级菜单标识（可选）</param>
        public void Navigate(
            string viewName,
            string breadcrumb,
            string? menu1 = null,
            string? menu2 = null,
            string? menu3 = null
        )
        {
            // 创建页面实例
            var instance = CreatePageInstance(viewName);
            if (instance == null)
                return;

            // 保存当前状态到历史记录
            if (_currentState != null)
            {
                _history.AddLast(_currentState);
                // 保持历史记录不超过最大限制
                while (_history.Count > _maxHistory)
                    _history.RemoveFirst();
            }

            // 更新内容宿主显示
            _contentHost.Content = instance;

            // 创建新的导航状态
            _currentState = new NavigationState
            {
                Page = instance,
                Breadcrumb = breadcrumb,
                Menu1 = menu1,
                Menu2 = menu2,
                Menu3 = menu3
            };

            // 触发导航完成事件
            OnNavigated?.Invoke(_currentState);
            App.Logger.Info($"导航服务: 导航到页面 {breadcrumb}");
        }

        /// <summary>
        /// 弹出窗口导航（不记录历史，弹窗关闭后触发 OnPopupClosed 回调）
        /// </summary>
        /// <param name="windowName">窗口类型名称（Window的类名）</param>
        /// <param name="modal">是否模态窗口，默认为true</param>
        /// <param name="configureWindow">窗口配置回调（可选）</param>
        public void NavigatePopup(
            string windowName,
            bool modal = true,
            Action<Window>? configureWindow = null
        )
        {
            // 通过反射查找窗口类型
            var assembly = typeof(App).Assembly;
            var type = assembly
                .GetTypes()
                .FirstOrDefault(t => t.Name == windowName && t.IsSubclassOf(typeof(Window)));

            if (type == null)
            {
                App.Logger.Warning($"导航服务: 未找到窗口类型 {windowName}");
                return;
            }

            // 创建窗口实例
            var window = (Window)Activator.CreateInstance(type)!;

            // 应用自定义配置（如果有）
            configureWindow?.Invoke(window);

            // 注册窗口关闭事件
            window.Closed += (s, e) =>
            {
                OnPopupClosed?.Invoke(windowName);
                App.Logger.Info($"导航服务: 弹窗 {windowName} 已关闭");
            };

            // 显示窗口
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

        /// <summary>
        /// 回退到上一个页面
        /// </summary>
        public void GoBack()
        {
            // 检查是否有历史记录
            if (_history.Count == 0)
                return;

            // 获取上一个导航状态
            var previous = _history.Last.Value;
            _history.RemoveLast();

            // 确保页面布局属性
            previous.Page.HorizontalAlignment = HorizontalAlignment.Stretch;
            previous.Page.VerticalAlignment = VerticalAlignment.Stretch;

            // 显示上一个页面
            _contentHost.Content = previous.Page;
            _currentState = previous;

            // 触发导航事件
            OnNavigated?.Invoke(previous);
            App.Logger.Info($"导航服务: 回退到页面 {previous.Breadcrumb}");
        }

        /// <summary>
        /// 清空导航历史记录
        /// </summary>
        public void ClearHistory() => _history.Clear();
        #endregion
    }
    #endregion
}