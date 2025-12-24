using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using HostComputer.Common.Services;
using HostComputer.Models;
using MyLogger;

namespace HostComputer.Common.Base
{
    public class CommandBase : ICommand
    {
        public string Name { get; set; } = "(未命名命令)";
        internal PermissionAttribute? Permission { get; set; }
        public Action<object?>? DoExecute { get; set; }

        private bool? _cachedCanExecute; // 缓存权限判断结果

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            // 如果缓存存在，则直接返回
            if (_cachedCanExecute.HasValue)
                return _cachedCanExecute.Value;

            // 否则进行权限判断
            _cachedCanExecute = PermissionService.CanExecute(Permission, Name);
            return _cachedCanExecute.Value;
        }

        public void Execute(object? parameter)
        {

            if(parameter is MouseEventArgs e )
            {
                DoExecute?.Invoke(parameter);
                return;
            }
            // 日志记录只在 Execute 中
            LogControlUI(parameter);

            try
            {
                DoExecute?.Invoke(parameter);
                App.Logger?.Info("命令执行成功");
            }
            catch (Exception ex)
            {
                App.Logger?.Error("命令执行报错：", ex);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            _cachedCanExecute = null; // 清空缓存，下一次 CanExecute 会重新计算
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        /*********************************************************
         *           核心：自动识别控件类型并记录日志
         *********************************************************/
        private void LogControlUI(object? parameter)
        {
            switch (parameter)
            {
                /*---------------- ① Window ----------------*/
                case Window w:
                    App.Logger?.UI($"窗口操作：{w.Title}");
                    return;

                /*---------------- ② 自定义菜单模型 ----------------*/
                case MenuItemModel m:
                    App.Logger?.UI($"窗口跳转：{m.Title}");
                    return;

                /*---------------- ③ Button / MenuItem ----------------*/
                case Button b:
                    App.Logger?.UI($"[{b.Name}] 按钮点击");
                    return;

                case MenuItem menu:
                    App.Logger?.UI($"[{menu.Header}] 菜单点击");
                    return;

                /*---------------- ④ ComboBox（选择变化） ----------------*/
                case ComboBox cb:
                    string? oldVal = cb.Tag?.ToString();
                    string? newVal = cb.SelectedItem?.ToString();

                    if (oldVal != newVal)
                        App.Logger?.UI($"[{cb.Name}] 选择变化：{oldVal} → {newVal}");

                    cb.Tag = newVal; // 保存新状态
                    return;

                /*---------------- ⑤ TextBox（输入变化） ----------------*/
                case TextBox tb:
                    string? oldText = tb.Tag?.ToString();
                    string? newText = tb.Text;

                    if (oldText != newText)
                        App.Logger?.UI($"[{tb.Name}] 文本输入：\"{oldText}\" → \"{newText}\"");

                    tb.Tag = newText;
                    return;

                /*---------------- ⑥ PasswordBox（仅记录操作，不记录内容） ----------------*/
                case PasswordBox pb:
                    App.Logger?.UI($"[{pb.Name}] 密码输入变化（内容已隐藏）");
                    return;

                /*---------------- ⑦ CheckBox ----------------*/
                case CheckBox chk:
                    App.Logger?.UI($"[{chk.Name}] 勾选状态：{chk.IsChecked}");
                    return;

                /*---------------- ⑧ RadioButton ----------------*/
                case RadioButton rb:
                    App.Logger?.UI($"[{rb.Name}] 单选选择：Selected = {rb.IsChecked}");
                    return;

                /*---------------- ⑨ ToggleButton / Switch ----------------*/
                case ToggleButton tg:
                    App.Logger?.UI($"[{tg.Name}] 切换状态：{tg.IsChecked}");
                    return;

                /*---------------- ⑩ Slider（滑动条） ----------------*/
                case Slider sld:
                    double oldSld = sld.Tag is double d ? d : -999;
                    double newSld = sld.Value;

                    if (Math.Abs(oldSld - newSld) > 0.0001)
                        App.Logger?.UI($"[{sld.Name}] 数值变化：{oldSld} → {newSld}");

                    sld.Tag = newSld;
                    return;

                /*---------------- 11. ListBox / ListView 选择 ----------------*/
                case ListBox list:
                    App.Logger?.UI($"[{list.Name}] 选中项：{list.SelectedItem}");
                    return;

                //case ListView lv:
                //    App.Logger?.UI($"[{lv.Name}] 选中项：{lv.SelectedItem}");
                //    return;

                /*---------------- 12. TreeView ----------------*/
                case TreeView tv:
                    App.Logger?.UI($"[{tv.Name}] 选中节点：{tv.SelectedItem}");
                    return;

                /*---------------- 13. TabControl ----------------*/
                case TabControl tab:
                    if (tab.SelectedItem is TabItem ti)
                        App.Logger?.UI($"[{tab.Name}] 标签切换：{ti.Header}");
                    return;

                /*---------------- 14. 其它 FrameworkElement 兜底 ----------------*/
                case FrameworkElement fe:
                    App.Logger?.UI($"[{fe.Name}] 控件操作");
                    return;

                /*---------------- 15. 什么也不是（无控件） ----------------*/
                default:
                    App.Logger?.UI("无控件参数的命令触发");
                    return;
            }
        }
    }
}
