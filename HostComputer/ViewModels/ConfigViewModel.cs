using HostComputer.Common.Base;
using Mysqlx.Prepare;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HostComputer.ViewModels
{
    public class ConfigViewModel : NotifyBase
    {
        private bool? _windowState = false;

        // 当前来源页面
        public UserControl SourcePage { get; init; } = null!;

        // 页面标识（A / B / C）
        public string SourceViewName { get; init; } = "";

        // 原始组态数据（控件、位置、参数）
        public object OriginalLayout { get; init; } = null!;

        // 组态后的结果
        public object? ResultLayout { get; set; }

        // 是否保存
        public bool IsSaved { get; set; }

        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public ConfigViewModel()
        {
            
                SaveCommand = new CommandBase() { DoExecute = _ => { } };
                CancelCommand = new CommandBase()
                {
                    DoExecute = obj =>
                    {
                        //取消操作关闭窗口
                        var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                        if (window is not null)
                        (obj as Window).DialogResult = _windowState;
                    }
                };
            
            
           
        }
    }
}
