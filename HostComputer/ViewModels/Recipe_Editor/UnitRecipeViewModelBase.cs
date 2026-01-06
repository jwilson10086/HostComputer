using HostComputer.Common.Base;
using HostComputer.Models.RicipeEditor;
using System.Collections.ObjectModel;

namespace HostComputer.ViewModels.Recipe_Editor
{
    public abstract class UnitRecipeViewModelBase
    {
        public string UnitName { get; protected set; }
        public int StepCount { get; protected set; }

        public abstract IReadOnlyList<UnitItemDefinition> Items { get; }
    }

}
