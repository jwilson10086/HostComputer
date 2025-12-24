using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CustomControls.Controls;
using HostComputer.Common.Actions;
using HostComputer.Common.Base;
using HostComputer.Common.Services;
using HostComputer.Common.Services.LocalDataService.Component;
using HostComputer.Common.Services.StartupModules;
using HostComputer.Models;
using Mysqlx.Prepare;
using static HostComputer.App;

namespace HostComputer.ViewModels
{
    #region MainViewModel 主视图模型
    /// <summary>
    /// 主视图模型，负责管理主界面的所有功能
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        #region === 构造函数 ===
        /// <summary>
        /// 初始化主视图模型
        /// </summary>
        /// <param name="navigation">导航服务实例</param>
        public MainViewModel(NavigationService navigation)
        {
            // 加载用户信息
            LoadUserInfo();

            _navigation = navigation;

            // 设置默认语言
            SelectedLanguage = AppConfiguration.Current.UI.Language;

            // 订阅语言变更事件
            LanguageService.LanguageChanged += RefreshMenuTitles;

            // 加载菜单
            LoadMenus();

            // 设置默认页面
            SetDefaultPage();

            // 初始化命令
            InitializeCommands();

            // 订阅导航事件
            SubscribeToNavigation();

            // 初始化本地组件服务
            _localService = new ComponentLocalService();

            // 实例化设备列表
            DeviceList = new List<DeviceItemModel>();

            // 初始化设备列表
            //LoadLayoutFromFile(_navigation._currentState.Page.GetType().Name);
            //挂面板
            //AttachFloatingPanels();
            PreloadAllLayouts();
        }
        #endregion

        #region === 私有字段 ===
        /// <summary>导航服务实例</summary>
        private readonly NavigationService _navigation;

        /// <summary>l
        /// 页面名 → 设备布局缓存
        /// </summary>
        private readonly Dictionary<string, List<DeviceItemModel>> _pageDeviceCache = new();

        /// <summary>本地组件服务</summary>
        private ComponentLocalService _localService;
        #endregion

        #region === 用户信息属性 ===
        private string _userName;

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName
        {
            get => _userName;
            set { _userName = value; }
        }

        private string _userLevel;

        /// <summary>
        /// 用户等级
        /// </summary>
        public string UserLevel
        {
            get => _userLevel;
            set { _userLevel = value; }
        }

        private string _group;

        /// <summary>
        /// 用户组
        /// </summary>
        public string Group
        {
            get => _group;
            set { _group = value; }
        }

        /// <summary>
        /// 用户视图模型
        /// </summary>
        public UserModel UserViewModel { get; set; } = new UserModel();

        /// <summary>
        /// 加载用户信息
        /// </summary>
        private void LoadUserInfo()
        {
            UserName = UserSession.UserName ?? "Unknown";
            UserLevel = UserSession.Level.ToString() ?? "N/A";
            Group = UserSession.Group ?? "Unknown";
        }
        #endregion

        #region === 语言服务相关 ===
        /// <summary>
        /// 语言服务实例
        /// </summary>
        public LanguageService LanguageService => App.Lang;

        /// <summary>
        /// 当前文化设置
        /// </summary>
        public CultureInfo CurrentCulture => new CultureInfo("en-US");

        private string _selectedLanguage;

        /// <summary>
        /// 选择的语言
        /// </summary>
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (Set(ref _selectedLanguage, value))
                {
                    LanguageService.CurrentLang = value;
                    RefreshMenuTitles();
                }
            }
        }
        #endregion

        #region === 菜单集合 ===
        /// <summary>
        /// 一级菜单集合
        /// </summary>
        public ObservableCollection<MenuItemModel> Menus { get; } = new();

        /// <summary>
        /// 二级菜单集合
        /// </summary>
        public ObservableCollection<MenuItemModel> SecondLevel { get; } = new();

        /// <summary>
        /// 三级菜单集合
        /// </summary>
        public ObservableCollection<MenuItemModel> ThirdLevel { get; } = new();
        #endregion

        #region === 设备布局相关 ===
        /// <summary>
        /// 设备列表
        /// </summary>
        public List<DeviceItemModel> DeviceList { get; set; }

        /// <summary>
        /// 从文件加载布局
        /// </summary>
        private void LoadLayoutFromFile(string? sourceViewName = null)
        {
            sourceViewName ??= _navigation._currentState.Page.GetType().Name;
            if (string.IsNullOrEmpty(sourceViewName))
                return;

            var config = _localService.LoadLayout(sourceViewName);
            if (config == null)
                return;

            var newDeviceList = new List<DeviceItemModel>();

            foreach (var device in config.Devices)
            {
                if (device.DeviceType == "WaferRobot")
                {
                    var robotCtrl = new WaferRobot
                    {
                        Width = device.Width,
                        Height = device.Height,
                        Permission = UserSession.Level
                    };

                    Canvas.SetLeft(robotCtrl, device.X);
                    Canvas.SetTop(robotCtrl, device.Y);

                    device.DeviceControl = robotCtrl;
                }

                newDeviceList.Add(device);
            }

            // ✅ 只更新缓存
            _pageDeviceCache[sourceViewName] = newDeviceList;

            // ✅ 如果是当前页面，再刷新 UI
            if (sourceViewName == _navigation._currentState.Page.GetType().Name)
            {
                DeviceList = newDeviceList;
                Raise(nameof(DeviceList));
            }
        }


        #endregion

        #region === 面包屑导航 ===
        private string _breadcrumb = "Home";

        /// <summary>
        /// 面包屑导航文本
        /// </summary>
        public string Breadcrumb
        {
            get => _breadcrumb;
            set => Set(ref _breadcrumb, value);
        }
        #endregion

        #region === 选中菜单项 ===
        private MenuItemModel _selectedMenu1;

        /// <summary>
        /// 选中的一级菜单项
        /// </summary>
        public MenuItemModel SelectedMenu1
        {
            get => _selectedMenu1;
            set => Set(ref _selectedMenu1, value);
        }

        private MenuItemModel _selectedMenu2;

        /// <summary>
        /// 选中的二级菜单项
        /// </summary>
        public MenuItemModel SelectedMenu2
        {
            get => _selectedMenu2;
            set => Set(ref _selectedMenu2, value);
        }

        private MenuItemModel _selectedMenu3;

        /// <summary>
        /// 选中的三级菜单项
        /// </summary>
        public MenuItemModel SelectedMenu3
        {
            get => _selectedMenu3;
            set => Set(ref _selectedMenu3, value);
        }
        #endregion

        #region === 命令 ===
        /// <summary>一级菜单点击命令</summary>
        public ICommand Menu1Command { get; private set; }

        /// <summary>二级菜单点击命令</summary>
        public ICommand Menu2Command { get; private set; }

        /// <summary>三级菜单点击命令</summary>
        public ICommand Menu3Command { get; private set; }

        /// <summary>返回命令</summary>
        public ICommand BackCommand { get; private set; }

        /// <summary>切换语言命令</summary>
        public ICommand ChangeLanguageCommand { get; private set; }

        /// <summary>关闭应用命令</summary>
        public ICommand CloseCommand { get; private set; }

        /// <summary>登录命令</summary>
        public ICommand LoginCommand { get; private set; }

        /// <summary>主视图命令</summary>
        public ICommand MainViewCommand { get; private set; }

        /// <summary>设置命令</summary>
        [Permission(4)]
        public ICommand SettingCommand { get; set; }
        #endregion

        #region === 私有方法 - 初始化 ===
        /// <summary>
        /// 加载菜单
        /// </summary>
        private void LoadMenus()
        {
            foreach (var menu in MenuService.LoadMenu())
                Menus.Add(menu);
        }

        /// <summary>
        /// 设置默认页面
        /// </summary>
        private void SetDefaultPage()
        {
            RefreshMenuTitles();
            if (LanguageService.CurrentLang == "zh-CN")
            {
                OnMenu1Clicked(Menus.FirstOrDefault(m => m.Title == "主页"));
            }
            else
            {
                OnMenu1Clicked(Menus.FirstOrDefault(m => m.Title == "Overview"));
            }
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            // ===== 登录完成后刷新按钮 =====
            CommandManager.InvalidateRequerySuggested();
            Menu1Command = new CommandBase() { DoExecute = m => OnMenu1Clicked((MenuItemModel)m) };
            Menu2Command = new CommandBase() { DoExecute = m => OnMenu2Clicked((MenuItemModel)m) };
            Menu3Command = new CommandBase() { DoExecute = m => OnMenu3Clicked((MenuItemModel)m) };
            BackCommand = new CommandBase() { DoExecute = _ => _navigation.GoBack() };

            CloseCommand = new CommandBase()
            {
                DoExecute = _ =>
                {
                    // 通过单例 App 调用 ExitApplication
                    if (Application.Current is App app)
                    {
                        app.ExitApplication();
                    }
                }
            };

            LoginCommand = new CommandBase()
            {
                DoExecute = _ =>
                {
                    _navigation.NavigatePopup("LoginWindow", modal: true);
                    // 登录完成后刷新权限
                    MenuService.RefreshMenuPermissions(Menus);
                }
            };

            MainViewCommand = new CommandBase()
            {
                DoExecute = _ =>
                {
                    if (LanguageService.CurrentLang != "zh-CN")
                    {
                        _navigation.Navigate(
                            "Overview_EquipmentView",
                            "Equipment Overview",
                            "Overview",
                            "Equipment Overview"
                        );
                    }
                    else
                    {
                        _navigation.Navigate("Overview_EquipmentView", "设备主页", "主页", "设备主页");
                    }
                }
            };

            SettingCommand = CreateCommand(
                nameof(SettingCommand),
                _ =>
                {
                    string targetPage = _navigation._currentState?.Page.GetType().Name ?? "Unknown";
                    if (ActionManager.Execute<object, bool>("AAA", targetPage))
                    {
                        LoadLayoutFromFile(targetPage);
                    }
                }
            );

            ChangeLanguageCommand = new CommandBase()
            {
                DoExecute = _ =>
                {
                    SelectedLanguage = LanguageService.CurrentLang == "zh-CN" ? "en-US" : "zh-CN";
                    _navigation.ClearHistory();
                }
            };
        }

        /// <summary>
        /// 订阅导航事件
        /// </summary>
        private void SubscribeToNavigation()
        {
            _navigation.OnNavigated += state =>
            {
                Breadcrumb = state.Breadcrumb;
                RestoreMenu(state);

                var pageName = state.Page.GetType().Name;

                if (_pageDeviceCache.TryGetValue(pageName, out var devices))
                {
                    DeviceList = devices;
                    Raise(nameof(DeviceList));
                }
                else
                {
                    // 第一次进这个页面，才加载
                    LoadLayoutFromFile(pageName);
                    int i = 9;
                }
            };
        }

        /// <summary>
        /// 收集所有的页面名称
        /// </summary>
        /// <returns></returns>
        private HashSet<string> CollectAllPageNames()
        {
            var pages = new HashSet<string>();

            void Traverse(IEnumerable<MenuItemModel> menus)
            {
                foreach (var menu in menus)
                {
                    if (!string.IsNullOrEmpty(menu.ViewName))
                        pages.Add(menu.ViewName);

                    if (menu.Children != null && menu.Children.Any())
                        Traverse(menu.Children);
                }
            }

            Traverse(Menus);
            return pages;
        }
        private void PreloadAllLayouts()
        {
            var pageNames = CollectAllPageNames();

            foreach (var page in pageNames)
            {
                // 避免重复加载
                if (_pageDeviceCache.ContainsKey(page))
                    continue;

                LoadLayoutFromFile(page);
            }
        }


        #endregion

        #region === 菜单点击逻辑 ===
        /// <summary>
        /// 处理一级菜单点击
        /// </summary>
        private void OnMenu1Clicked(MenuItemModel menu1)
        {
            if (menu1 == null || menu1 == SelectedMenu1)
                return; // ✅ 已选中则不做重复处理

            // 设置一级菜单选中
            SelectedMenu1 = menu1;

            // 清理二三级
            SelectedMenu2 = null;
            SelectedMenu3 = null;
            SecondLevel.Clear();
            ThirdLevel.Clear();

            // 填充二级菜单
            if (menu1.Children != null && menu1.Children.Any())
            {
                foreach (var child in menu1.Children)
                    SecondLevel.Add(child);

                // 默认选中第一个二级菜单
                OnMenu2Clicked(SecondLevel.First(), menu1);
                return;
            }

            // 一级菜单本身有 ViewName，直接导航
            if (!string.IsNullOrEmpty(menu1.ViewName))
                _navigation.Navigate(menu1.ViewName, menu1.Title);
        }

        /// <summary>
        /// 处理二级菜单点击（有父级菜单）
        /// </summary>
        private void OnMenu2Clicked(MenuItemModel menu2, MenuItemModel parentMenu1)
        {
            if (menu2 == null || menu2 == SelectedMenu2)
                return; // ✅ 已选中则不做重复处理

            SelectedMenu1 = parentMenu1;
            SelectedMenu2 = menu2;

            // 清理三级菜单
            SelectedMenu3 = null;
            ThirdLevel.Clear();

            // 填充三级菜单
            if (menu2.Children != null && menu2.Children.Any())
            {
                foreach (var child in menu2.Children)
                    ThirdLevel.Add(child);

                // 默认选中第一个三级菜单
                OnMenu3Clicked(ThirdLevel.First(), parentMenu1, menu2);
                return;
            }

            // 二级菜单本身有 ViewName，导航
            if (!string.IsNullOrEmpty(menu2.ViewName))
                _navigation.Navigate(
                    menu2.ViewName,
                    menu2.Title,
                    menu1: parentMenu1?.Title,
                    menu2: menu2.Title
                );
        }

        /// <summary>
        /// 处理二级菜单点击（无父级菜单）
        /// </summary>
        private void OnMenu2Clicked(MenuItemModel menu2)
        {
            var parent = Menus.FirstOrDefault(m => m.Children.Contains(menu2));
            OnMenu2Clicked(menu2, parent);
        }

        /// <summary>
        /// 处理三级菜单点击（有父级菜单）
        /// </summary>
        private void OnMenu3Clicked(
            MenuItemModel menu3,
            MenuItemModel parent1,
            MenuItemModel parent2
        )
        {
            if (menu3 == null || menu3 == SelectedMenu3)
                return; // ✅ 已选中则不做重复处理

            SelectedMenu1 = parent1;
            SelectedMenu2 = parent2;
            SelectedMenu3 = menu3;

            if (!string.IsNullOrEmpty(menu3.ViewName))
                _navigation.Navigate(
                    menu3.ViewName,
                    menu3.Title,
                    menu1: parent1?.Title,
                    menu2: parent2?.Title,
                    menu3: menu3.Title
                );
        }

        /// <summary>
        /// 处理三级菜单点击（无父级菜单）
        /// </summary>
        private void OnMenu3Clicked(MenuItemModel menu3)
        {
            var parent2 = SecondLevel.FirstOrDefault(m => m.Children.Contains(menu3));
            var parent1 = Menus.FirstOrDefault(m => m.Children.Contains(parent2));
            OnMenu3Clicked(menu3, parent1, parent2);
        }
        #endregion

        #region === 导航辅助方法 ===
        /// <summary>
        /// 查找第一个可导航的菜单项
        /// </summary>
        private (MenuItemModel Item, int Level, MenuItemModel Parent)? FindFirstViewable(
            MenuItemModel root,
            int level = 1,
            MenuItemModel parent = null
        )
        {
            if (root.Children == null)
                return null;

            foreach (var child in root.Children)
            {
                if (!string.IsNullOrEmpty(child.ViewName))
                    return (child, level + 1, parent);

                var deeper = FindFirstViewable(child, level + 1, child);
                if (deeper != null)
                    return deeper;
            }

            return null;
        }

        /// <summary>
        /// 导航到指定菜单项
        /// </summary>
        private void NavigateTo((MenuItemModel Item, int Level, MenuItemModel Parent) node)
        {
            if (node.Level == 2)
                OnMenu2Clicked(node.Item, node.Parent);
            else if (node.Level == 3)
                OnMenu3Clicked(
                    node.Item,
                    Menus.FirstOrDefault(m => m.Children.Contains(node.Parent)),
                    node.Parent
                );
        }

        /// <summary>
        /// 根据导航状态恢复菜单选择
        /// </summary>
        private void RestoreMenu(NavigationState state)
        {
            if (string.IsNullOrEmpty(state.Menu1))
                return;

            var menu1 = Menus.FirstOrDefault(m => m.Title == state.Menu1);
            if (menu1 != null)
                SelectedMenu1 = menu1;

            if (menu1?.Children != null)
            {
                SecondLevel.Clear();
                foreach (var child in menu1.Children)
                    SecondLevel.Add(child);

                if (!string.IsNullOrEmpty(state.Menu2))
                {
                    var menu2 = SecondLevel.FirstOrDefault(m => m.Title == state.Menu2);
                    if (menu2 != null)
                        SelectedMenu2 = menu2;

                    if (menu2?.Children != null)
                    {
                        ThirdLevel.Clear();
                        foreach (var child in menu2.Children)
                            ThirdLevel.Add(child);

                        if (!string.IsNullOrEmpty(state.Menu3))
                        {
                            var menu3 = ThirdLevel.FirstOrDefault(m => m.Title == state.Menu3);
                            if (menu3 != null)
                                SelectedMenu3 = menu3;
                        }
                    }
                }
            }
        }
        #endregion

        #region === 语言更新 ===
        /// <summary>
        /// 刷新菜单标题
        /// </summary>
        private void RefreshMenuTitles() => UpdateTitles(Menus);

        /// <summary>
        /// 更新菜单项标题
        /// </summary>
        private void UpdateTitles(IEnumerable<MenuItemModel> items)
        {
            if (items == null)
                return;

            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.Key))
                {
                    var text = LanguageService[item.Key];
                    if (!string.IsNullOrEmpty(text))
                        item.Title = text;
                }

                if (item.Children != null)
                    UpdateTitles(item.Children);
            }
        }
        #endregion
    }
    #endregion
}
