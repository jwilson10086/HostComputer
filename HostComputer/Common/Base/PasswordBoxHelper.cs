using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace HostComputer.Common.Base
{
    public class PasswordBoxHelper
    {
        // 定义一个依赖属性，用于存储密码
        public static readonly DependencyProperty PasswordProperty = DependencyProperty.RegisterAttached(
            "Password", // 属性名称
            typeof(string), // 属性类型
            typeof(PasswordBoxHelper), // 所属类
            new PropertyMetadata(new PropertyChangedCallback(OnPropertyChanged)) // 属性变化时的回调
        );

        // 获取密码的附加属性值
        public static string GetPassword(DependencyObject d)
        {
            return (string)d.GetValue(PasswordProperty);
        }

        // 设置密码的附加属性值
        public static void SetPassword(DependencyObject d, string value)
        {
            d.SetValue(PasswordProperty, value);
        }

        // 定义一个依赖属性，用于附加属性的存储
        public static readonly DependencyProperty AttachProperty = DependencyProperty.RegisterAttached(
            "Attach", // 属性名称
            typeof(string), // 属性类型
            typeof(PasswordBoxHelper), // 所属类
            new PropertyMetadata(new PropertyChangedCallback(OnAttachChanged)) // 属性变化时的回调
        );

        // 获取附加属性的值
        public static string GetAttach(DependencyObject d)
        {
            return (string)d.GetValue(AttachProperty); // 修正了此处的属性引用
        }

        // 设置附加属性的值
        public static void SetAttach(DependencyObject d, string value)
        {
            d.SetValue(AttachProperty, value); // 修正了此处的属性引用
        }

        static bool _isUpdating = false; // 防止递归更新的标志

        // 当密码属性变化时的处理方法
        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox pb = d as PasswordBox;
            pb.PasswordChanged -= Pb_PasswordChanged; // 移除事件处理程序以避免重复注册

            if (!_isUpdating) // 只在非更新状态下设置密码
                pb.Password = e.NewValue.ToString(); // 更新密码框的密码

            pb.PasswordChanged += Pb_PasswordChanged; // 重新注册事件处理程序
        }

        // 当附加属性变化时的处理方法
        private static void OnAttachChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox pb = d as PasswordBox;
            pb.PasswordChanged += Pb_PasswordChanged; // 注册密码变化事件处理程序
        }

        // 密码变化时的事件处理程序
        private static void Pb_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox pb = sender as PasswordBox;
            _isUpdating = true; // 设置更新状态为真，防止递归调用
            SetPassword(pb, pb.Password); // 更新依赖属性的值
            _isUpdating = false; // 恢复更新状态
        }
    }
}
