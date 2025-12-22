using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using HostComputer.Common.Base;
using HostComputer.Common.Services; // 引入日志
using HostComputer.Common.Services.Local.Auth;
using HostComputer.Models;
using MyLogger;
using static HostComputer.App;

namespace HostComputer.ViewModels
{
    #region LoginViewModel 登录视图模型
    /// <summary>
    /// 登录页面的 ViewModel
    /// 支持属性变更通知和操作日志记录
    /// </summary>
    public class LoginViewModel : ViewModelBase
    {
        #region 登录结果类
        /// <summary>
        /// 登录结果封装类
        /// </summary>
        public class LoginResult
        {
            /// <summary>登录是否成功</summary>
            public bool Success { get; set; }

            /// <summary>登录结果消息</summary>
            public string Message { get; set; }

            /// <summary>登录成功的用户信息</summary>
            public UserModel User { get; set; }
        }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化登录视图模型
        /// </summary>
        public LoginViewModel()
        {
            LoginCommand = CreateCommand(nameof(LoginCommand), obj =>
            {
                // 使用 Task.Run 或 async void 内部异步执行
                _ = ExecuteLoginAsync(obj);
            });

            InitAsync();
        }

        private async Task ExecuteLoginAsync(object obj)
        {
            try
            {
                App.Logger.Security("用户尝试登录");
                IsBusy = true;

                var result = await LocalDataAccess.LoginAsync(
                    UserViewModel.UserName,
                    UserViewModel.Password
                );

                if (!result.Success)
                {
                    ErrorMessage = "用户名或密码错误，请重新输入！";
                    App.Logger.Warning($"登录失败：{UserViewModel.UserName}");
                    return;
                }

                var user = result.User;

                // ===== 更新 ViewModel =====
                UserViewModel.UserName = user.UserName;
                UserViewModel.Password = user.Password;
                UserViewModel.Level = user.Level;
                UserViewModel.Group = user.Group;

                if (IsEnable)
                    await LocalDataAccess.SaveRememberUserAsync(user);
                else
                    await LocalDataAccess.DeleteAllUsersAsync();

                // ===== 更新全局 Session =====
                UserSession.UserName = user.UserName;
                UserSession.Level = user.Level;
                UserSession.Group = user.Group;
                UserSession.IsLogin = true;

            

                App.Logger.Info($"登录成功：{user.UserName}");

                // ===== 关闭窗口 =====
                if (obj is Window window)
                    window.DialogResult = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region 私有字段
        private string _errMessage = string.Empty;
        private CommandBase _closeCommand;
        public ICommand LoginCommand { get; set; }

        private bool _isEnable;
        #endregion

        #region 视图模型属性
        /// <summary>
        /// 用户视图模型
        /// </summary>
        public UserModel UserViewModel { get; set; } = new UserModel();

        /// <summary>
        /// 本地认证服务
        /// </summary>
        public AuthLocalService LocalDataAccess { get; set; } = new();
        #endregion

        #region 用户界面属性
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errMessage;
            set { _errMessage = value; }
        }

        /// <summary>
        /// 是否启用记住密码功能
        /// </summary>
        public bool IsEnable
        {
            get => _isEnable;
            set
            {
                if (_isEnable != value)
                {
                    _isEnable = value;
                }
            }
        }
        #endregion

        #region 命令

        /// <summary>
        /// 关闭窗口命令
        /// </summary>
        public ICommand CloseCommand = new CommandBase()
        {
            DoExecute = obj =>
            {
                App.Logger.Info("关闭登录窗口"); // 额外日志
                (obj as Window).DialogResult = false;
            }
        };

        #endregion

        #region 私有方法
        /// <summary>
        /// 异步初始化方法
        /// </summary>
        private async void InitAsync()
        {
            var user = await LocalDataAccess.GetLastUserAsync();

            if (user != null)
            {
                UserViewModel.UserName = user.UserName;
                UserViewModel.Password = user.Password;
                IsEnable = true;
            }
        }
        #endregion
    }
    #endregion
}
