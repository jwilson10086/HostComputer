using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;

namespace HostComputer.Common.Behaviors
{
    public class EventTrigger : TriggerBase
    {
        public string EventName { get; set; } = string.Empty;

        public ActionCollection Actions { get; } = new ActionCollection();

        protected override void OnAttached()
        {
            if (AssociatedObject == null) return;

            var eventInfo = AssociatedObject.GetType().GetEvent(EventName);
            if (eventInfo == null)
                throw new Exception($"Event '{EventName}' not found");

            var handler = new EventHandler((s, e) =>
            {
                foreach (var action in Actions)
                {
                    action.Invoke(s);
                }
            });

            eventInfo.AddEventHandler(AssociatedObject, handler);
        }
    }
    
}
