using HostComputer.Models.RicipeEditor;using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Text;using System.Text.Json;using System.Threading.Tasks;
namespace HostComputer.ViewModels.Recipe_Editor{   
    public class GenericUnitRecipeViewModel : UnitRecipeViewModelBase
    {
        public string UnitFolder { get; }

        private readonly List<UnitItemDefinition> _items = new();
        public override IReadOnlyList<UnitItemDefinition> Items => _items;

        public GenericUnitRecipeViewModel(string unitDir)
        {
            UnitFolder = unitDir;

            var unitName = Path.GetFileName(unitDir);
            var configPath = Path.Combine(unitDir, "unit.json");

            // ① 如果不存在，先生成基础模版
            if (!File.Exists(configPath))
            {
                CreateDefaultUnitConfig(configPath, unitName);
            }

            // ② 再读取
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<UnitConfig>(json)
                         ?? CreateFallbackConfig(unitName);

            UnitName = string.IsNullOrWhiteSpace(config.UnitName)
                ? unitName
                : config.UnitName;

            StepCount = config.StepCount > 0 ? config.StepCount : 1;

            _items.AddRange(
                (config.Items != null && config.Items.Any())
                    ? config.Items
                    : CreateFallbackItems()
            );
        }

        #region Default / Fallback

        private static void CreateDefaultUnitConfig(string path, string unitName)
        {
            var defaultConfig = new UnitConfig
            {
                UnitName = unitName,
                StepCount = 1,
                Items = new List<UnitItemDefinition>
            {
                new UnitItemDefinition
                {
                    DisplayName = "Param1",
                    DataType =ParamDataType.String,
                    Key = "Param1",
                    Value = "",
                },
                new UnitItemDefinition
                {
                    DisplayName = "Param2",
                    DataType =ParamDataType.Int,
                    Key = "Param2",
                    Value = 0,
                    Unit = "units",
                    Min = 0,
                    Max = 100
                },
                new UnitItemDefinition
                {
                    DisplayName = "Param3",
                    DataType =ParamDataType.Double,
                    Key = "Param3",
                    Value = 0.0,
                    Unit = "units",
                    Min = 0.0,
                    Max = 100.0
                }
            }
            };

            var json = JsonSerializer.Serialize(
                defaultConfig,
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(path, json);
        }

        private static UnitConfig CreateFallbackConfig(string unitName)
        {
            return new UnitConfig
            {
                UnitName = unitName,
                StepCount = 1,
                Items = CreateFallbackItems()
            };
        }

        private static List<UnitItemDefinition> CreateFallbackItems()
        {
            return new List<UnitItemDefinition>
        {
             new UnitItemDefinition
                {
                    DisplayName = "Param1",
                    DataType =ParamDataType.String,
                    Key = "Param1",
                    Value = "",
                },
                new UnitItemDefinition
                {
                    DisplayName = "Param2",
                    DataType =ParamDataType.Int,
                    Key = "Param2",
                    Value = 0,
                    Unit = "units",
                    Min = 0,
                    Max = 100
                },
                new UnitItemDefinition
                {
                    DisplayName = "Param3",
                    DataType =ParamDataType.Double,
                    Key = "Param3",
                    Value = 0.0,
                    Unit = "units",
                    Min = 0.0,
                    Max = 100.0
                }
        };
        }

        #endregion
    }
}