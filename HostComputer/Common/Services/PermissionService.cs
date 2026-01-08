using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HostComputer.Common.Base;
using HostComputer.Common.Session;

namespace HostComputer.Common.Services
{
    public static class PermissionService
    {
        private static bool _loginChecked = false; // 登录按钮只判断一次

        public static bool CanExecute(PermissionAttribute? permission, string? commandName = null)
        {
            var session = App.UserSession;
            string cmdInfo = commandName ?? "(未知命令)";

            // 登录前直接允许，不打印日志
            if (session == null || session.IsLogin)
                return true;

            string sessionInfo = $"User={session.UserName}, Level={session.Level}, Group={session.Group}";

            bool result;

            // 无 Permission 特性 → 默认允许
            if (permission == null)
            {
                App.Logger.Info($"[Permission] {cmdInfo}: 没有 Permission 特性 → 允许 | {sessionInfo}");
                result = true;
            }
            // Level 不够
            else if (permission.Level > 0 && session.Level < permission.Level)
            {
                App.Logger.Info(
                    $"[Permission] {cmdInfo}: Level 不够 → 禁用 | Required={permission.Level}, Current={session.Level} | {sessionInfo}"
                );
                result = false;
            }
            // Group 不匹配
            else if (!string.IsNullOrEmpty(permission.PermissionKey) && session.Group != permission.PermissionKey)
            {
                App.Logger.Info(
                    $"[Permission] {cmdInfo}: Group 不匹配 → 禁用 | Required={permission.PermissionKey}, Current={session.Group} | {sessionInfo}"
                );
                result = false;
            }
            else
            {
                App.Logger.Info($"[Permission] {cmdInfo}: 允许 | {sessionInfo}");
                result = true;
            }

            return result;
        }


        // 可选：登录后刷新所有命令权限
        public static void RefreshAllCommands()
        {
            CommandRegistry.RefreshAll();
        }
    }
}
