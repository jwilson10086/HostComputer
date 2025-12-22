using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HostComputer.Common.Behaviors
{
    public abstract class TriggerBase
    {
        protected DependencyObject? AssociatedObject;

        public void Attach(DependencyObject obj)
        {
            AssociatedObject = obj;
            OnAttached();
        }

        protected abstract void OnAttached();
    }
}

