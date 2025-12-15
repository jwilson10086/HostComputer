using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using HostComputer.Common.Base;
using HostComputer.Common.Services;
using HostComputer.Common.Services.StartupModules;
using HostComputer.Models;
using Mysqlx.Prepare;
using static HostComputer.App;

namespace HostComputer.ViewModels
{
    public class MainViewModel : NotifyBase
    {
        #region === UserInfo ===
        private string _userName;
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                NotifyChanged();
            }
        }

        private string _userLevel;
        public string UserLevel
        {
            get => _userLevel;
            set
            {
                _userLevel = value;
                NotifyChanged();
            }
        }

        private void LoadUserInfo()
        {
            UserName = UserSession.UserName ?? "Unknown";
            UserLevel = UserSession.UserLevel ?? "N/A";
        }
        #endregion
        #region === Language Service ===
        public LanguageService LanguageService => App.Lang;
        public CultureInfo CurrentCulture => new CultureInfo("en-US");

        private string _selectedLanguage;
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value))
                {
                    LanguageService.CurrentLang = value;
                    RefreshMenuTitles();
                }
            }
        }
        #endregion


        #region === Menu Collections ===
        public ObservableCollection<MenuItemModel> Menus { get; } = new();
        public ObservableCollection<MenuItemModel> SecondLevel { get; } = new();
        public ObservableCollection<MenuItemModel> ThirdLevel { get; } = new();
        #endregion


        #region === Navigation & Breadcrumb ===
        private readonly NavigationService _navigation;

        private string _breadcrumb = "Home";
        public string Breadcrumb
        {
            get => _breadcrumb;
            set => SetProperty(ref _breadcrumb, value);
        }
        #endregion


        #region === Selected Menu (3 Levels) ===
        private MenuItemModel _selectedMenu1;
        public MenuItemModel SelectedMenu1
        {
            get => _selectedMenu1;
            set => SetProperty(ref _selectedMenu1, value);
        }

        private MenuItemModel _selectedMenu2;
        public MenuItemModel SelectedMenu2
        {
            get => _selectedMenu2;
            set => SetProperty(ref _selectedMenu2, value);
        }

        private MenuItemModel _selectedMenu3;
        public MenuItemModel SelectedMenu3
        {
            get => _selectedMenu3;
            set => SetProperty(ref _selectedMenu3, value);
        }
        #endregion


        #region === Commands ===
        public ICommand Menu1Command { get; }
        public ICommand Menu2Command { get; }
        public ICommand Menu3Command { get; }
        public ICommand BackCommand { get; }
        public ICommand ChangeLanguageCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand LoginCommand { get; }
        public ICommand MainViewCommand { get; }
        public ICommand SettingCommand { get; }
        #endregion


        #region === Constructor ===

        public UserModel UserViewModel { get; set; } = new UserModel();

        public MainViewModel(NavigationService navigation)
        {
            LoadUserInfo();
            // 实时时钟
            //var timer = new DispatcherTimer();
            //timer.Interval = TimeSpan.FromSeconds(1);
            //timer.Tick += (_, __) =>
            //{
            //    MainModel.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            //};
            //timer.Start();
            _navigation = navigation;

            // 语言默认选择
            SelectedLanguage = AppConfiguration.Current.UI.Language;

            LanguageService.LanguageChanged += RefreshMenuTitles;

            // 加载菜单
            foreach (var m in MenuService.LoadMenu())
                Menus.Add(m);

            RefreshMenuTitles();
            if (LanguageService.CurrentLang == "zh-CN")
            {
                OnMenu1Clicked(Menus.FirstOrDefault(m => m.Title == "主页"));
            }
            else
            {
                OnMenu1Clicked(Menus.FirstOrDefault(m => m.Title == "Overview"));
            }
            // 初始化命令
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
                DoExecute = _ => _navigation.NavigatePopup("LoginWindow", modal: true)
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
            SettingCommand = new CommandBase()
            {
                DoExecute = _ =>
                    _navigation.NavigatePopup(
                        "ComponentConfigView",
                        true,
                        window =>
                        {
                            window.Width = 800;
                            window.Height = 600;
                            window.Owner = Application.Current.MainWindow;
                        }
                    )
            };

            ChangeLanguageCommand = new CommandBase()
            {
                DoExecute = _ =>
                {
                    _navigation.ClearHistory();
                }
            };

            // 导航同步
            _navigation.OnNavigated += state =>
            {
                Breadcrumb = state.Breadcrumb;
                RestoreMenu(state);
            };
        }
        #endregion


        #region === Menu Click Logic ===

        #region === Menu Click Logic ===

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
        #endregion


        private void OnMenu2Clicked(MenuItemModel menu2)
        {
            var parent = Menus.FirstOrDefault(m => m.Children.Contains(menu2));
            OnMenu2Clicked(menu2, parent);
        }

        private void OnMenu3Clicked(MenuItemModel menu3)
        {
            var parent2 = SecondLevel.FirstOrDefault(m => m.Children.Contains(menu3));
            var parent1 = Menus.FirstOrDefault(m => m.Children.Contains(parent2));
            OnMenu3Clicked(menu3, parent1, parent2);
        }

        #endregion


        #region === Navigation Helpers ===
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


        #region === Language Update ===
        private void RefreshMenuTitles() => UpdateTitles(Menus);

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
}
