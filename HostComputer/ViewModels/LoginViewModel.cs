using HostComputer.Common.Base;
using HostComputer.Common.Services; // 引入日志
using HostComputer.Common.Services.Local.Auth;
using HostComputer.Models;
using MyLogger;
using System;
using System.Windows;
using static HostComputer.App;

namespace HostComputer.ViewModels
{
    #region LoginViewModel 登录视图模型
    /// <summary>
    /// 登录页面的 ViewModel
    /// 支持属性变更通知和操作日志记录
    /// </summary>
    public class LoginViewModel : NotifyBase
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
            InitAsync();
        }
        #endregion

        #region 私有字段
        private string _errMessage = string.Empty;
        private CommandBase _closeCommand;
        private CommandBase _loginCommand;
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
            set
            {
                _errMessage = value;
                this.NotifyChanged();
            }
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
                    NotifyChanged();
                }
            }
        }
        #endregion

        #region 命令
        /// <summary>
        /// 关闭窗口命令
        /// </summary>
        public CommandBase CloseCommand
        {
            get
            {
                if (_closeCommand == null)
                {
                    // 传入 Logger，自动记录按钮点击
                    _closeCommand = new CommandBase();
                    _closeCommand.DoExecute = obj =>
                    {
                        App.Logger.Info("关闭登录窗口"); // 额外日志
                        (obj as Window).DialogResult = false;
                    };
                }
                return _closeCommand;
            }
        }

        /// <summary>
        /// 登录命令
        /// </summary>
        public CommandBase LoginCommand
        {
            get
            {
                if (_loginCommand == null)
                {
                    _loginCommand = new CommandBase();
                    _loginCommand.DoExecute = async obj =>
                    {
                        App.Logger.Security("用户尝试登录");

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

                        // ===== 登录成功，写入 ViewModel =====
                        var user = result.User;

                        UserViewModel.UserName = user.UserName;
                        UserViewModel.Password = user.Password;
                        UserViewModel.Level = user.Level;
                        UserViewModel.Group = user.Group;

                        // ===== 是否记住用户（只存一条）=====
                        if (IsEnable)
                        {
                            await LocalDataAccess.SaveRememberUserAsync(user);
                            App.Logger.Security("用户选择记住密码（唯一一条）");
                        }
                        else
                        {
                            await LocalDataAccess.DeleteAllUsersAsync();
                        }

                        // ===== 写入全局 Session =====
                        Session.UserName = user.UserName;
                        Session.Level = user.Level;
                        Session.Group = user.Group;
                        Session.IsLogin = true;

                        App.Logger.Info($"登录成功：{user.UserName}");

                        (obj as Window)!.DialogResult = true;
                    };
                }
                return _loginCommand;
            }
        }
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