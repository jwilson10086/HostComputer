using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;


    namespace HostComputer.Common.Behaviors
    {
        public static class Interaction
        {
            public static ObservableCollection<TriggerBase> GetTriggers(DependencyObject obj)
            {
                var triggers = (ObservableCollection<TriggerBase>)obj.GetValue(TriggersProperty);
                if (triggers == null)
                {
                    triggers = new ObservableCollection<TriggerBase>();
                    obj.SetValue(TriggersProperty, triggers);
                }
                return triggers;
            }

            public static void SetTriggers(DependencyObject obj, ObservableCollection<TriggerBase> value)
            {
                obj.SetValue(TriggersProperty, value);
            }

            public static readonly DependencyProperty TriggersProperty =
                DependencyProperty.RegisterAttached(
                    "Triggers",
                    typeof(ObservableCollection<TriggerBase>),
                    typeof(Interaction),
                    new PropertyMetadata(null, OnTriggersChanged));

            private static void OnTriggersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                if (e.NewValue is ObservableCollection<TriggerBase> triggers)
                {
                    foreach (var trigger in triggers)
                    {
                        trigger.Attach(d);
                    }
                }
            }
        }
    }


