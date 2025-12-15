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
    /// <summary>
    /// 登录页面的 ViewModel
    /// 支持属性变更通知和操作日志记录
    /// </summary>
    public class LoginViewModel : NotifyBase
    {
        public class LoginResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }

            public UserModel User { get; set; }
        }

        public LoginViewModel()
        {
            InitAsync();
        }

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

        public UserModel UserViewModel { get; set; } = new UserModel();
        public AuthLocalService LocalDataAccess { get; set; } = new ();

        private string _errMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errMessage;
            set
            {
                _errMessage = value;
                this.NotifyChanged();
            }
        }

        // ==================== 关闭窗口命令 ====================
        private CommandBase _closeCommand;
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

        // ==================== 登录命令 ====================
        private CommandBase _loginCommand;
        private bool _isEnable;
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

    }
}
