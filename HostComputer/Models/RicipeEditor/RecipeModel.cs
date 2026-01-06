using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using HostComputer.Common.Base;

namespace HostComputer.Models.RicipeEditor
{
    public class RecipeModel
    {
        public string SourceFile { get; set; }
        public string UnitRecipeName { get; set; }
        public int StepCount => Steps.Count;
        public ObservableCollection<UnitStepModel> Steps { get; set; } = new();
        public UnitStepModel Step0 { get; set; } // ?? step0 单独存放

        public static RecipeModel LoadFromXml(string filePath)
        {
            var xml = XDocument.Load(filePath);
            var recipe = new RecipeModel
            {
                UnitRecipeName = Path.GetFileNameWithoutExtension(filePath),
            };

            foreach (var stepNode in xml.Root.Elements())
            {
                if (stepNode.Name == "step0")
                    continue; //  跳过 step0 标记
                int stepIndex =
                    stepNode.Attribute("sid") != null
                        ? int.Parse(stepNode.Attribute("sid").Value)
                        : recipe.Steps.Count + 1;

                var step = new UnitStepModel { StepIndex = stepIndex };

                foreach (var paramNode in stepNode.Elements())
                {
                    step.Parameters[paramNode.Name.LocalName] = paramNode.Value;
                }

                recipe.Steps.Add(step);
            }

            return recipe;
        }

    }
    public class UnitConfig
    {
        public string UnitName { get; set; }
        public int StepCount { get; set; }
        public List<UnitItemDefinition> Items { get; set; }
    }

    public class UnitItemDefinition
    {
        public string Name { get; set; }
        public string Type { get; set; }   // int / double / bool / string
        public double? Min { get; set; }
        public double? Max { get; set; }
    }

    public class UnitStepModel
    {
        public int StepIndex { get; set; }
        public bool IsMetaStep { get; set; } = false;
        public Dictionary<string, string> Parameters { get; set; } = new();
    }

    public class RecipeParamRow : ViewModelBase
    {
        /// <summary>
        /// 参数名（DataGrid ITEM 列显示）
        /// </summary>
        public string Item { get; set; }

        /// <summary>
        /// 参数定义（来自 unit.json）
        /// </summary>
        public UnitItemDefinition Definition { get; set; }

        /// <summary>
        /// 每个 Step 的值
        /// </summary>
        public ObservableCollection<RecipeStepValue> StepValues { get; } = new();
    }


    public class RecipeStepValue : ViewModelBase
    {
        private string _value;
        public string Value
        {
            get => _value;
            set => Set(ref _value, value);
        }

        public bool IsDirty { get; private set; }

        public void AcceptChanges() => IsDirty = false;

        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == nameof(Value))
                IsDirty = true;
        }
    }

}
