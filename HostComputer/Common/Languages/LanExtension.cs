using System;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Controls;


namespace HostComputer.Common.Languages
{
    public class LangExtension : MarkupExtension
    {
        public string Key { get; set; }


        public LangExtension(string key)
        {
            Key = key;
        }


        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (App.Lang == null) return Key;


            var pvt = serviceProvider?.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            var targetObject = pvt?.TargetObject as DependencyObject;
            var targetProperty = pvt?.TargetProperty as DependencyProperty;


            if (targetObject == null || targetProperty == null)
                return App.Lang[Key];


            // 防止重复订阅
            App.Lang.LanguageChanged -= OnLangChanged;
            App.Lang.LanguageChanged += OnLangChanged;


            void OnLangChanged()
            {
                // UI 线程更新
                targetObject.Dispatcher.Invoke(() =>
                {
                    targetObject.SetValue(targetProperty, App.Lang[Key]);
                });
            }


            // 返回当前值
            return App.Lang[Key];
        }
    }
}