using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HostComputer.Common.Behaviors
{
    public static class ComboBoxBehaviors
    {
        // ICommand 属性
        public static readonly DependencyProperty SelectionChangedCommandProperty =
            DependencyProperty.RegisterAttached(
                "SelectionChangedCommand",
                typeof(ICommand),
                typeof(ComboBoxBehaviors),
                new PropertyMetadata(null, OnSelectionChangedCommandChanged)
            );

        public static void SetSelectionChangedCommand(DependencyObject obj, ICommand value) =>
            obj.SetValue(SelectionChangedCommandProperty, value);

        public static ICommand GetSelectionChangedCommand(DependencyObject obj) =>
            (ICommand)obj.GetValue(SelectionChangedCommandProperty);

        // 可选的 CommandParameter 属性（如果不设置，则会传 ComboBox 自身）
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached(
                "CommandParameter",
                typeof(object),
                typeof(ComboBoxBehaviors),
                new PropertyMetadata(null)
            );

        public static void SetCommandParameter(DependencyObject obj, object value) =>
            obj.SetValue(CommandParameterProperty, value);

        public static object GetCommandParameter(DependencyObject obj) =>
            obj.GetValue(CommandParameterProperty);

        // 当绑定变化时（attach / detach）
        private static void OnSelectionChangedCommandChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (d is ComboBox cb)
            {
                // 先移除已有处理器（避免重复）
                cb.SelectionChanged -= ComboBox_SelectionChanged;

                if (e.NewValue is ICommand)
                {
                    cb.SelectionChanged += ComboBox_SelectionChanged;
                }
            }
        }

        private static void ComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb)
            {
                var command = GetSelectionChangedCommand(cb);
                if (command == null)
                    return;

                // 优先使用用户传入的 CommandParameter；否则传入 ComboBox 本身
                var parameter = GetCommandParameter(cb) ?? cb;

                if (command.CanExecute(parameter))
                {
                    command.Execute(parameter);
                }
            }
        }
    }
}
