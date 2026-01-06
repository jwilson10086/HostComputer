using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using HostComputer.Common.Base;
using HostComputer.Models.RicipeEditor;

namespace HostComputer.ViewModels.Recipe_Editor
{
    public class UnitRecipeViewModel : ViewModelBase
    {
        public string RecipeName { get; private set; }

        // DataGrid 行
        public ObservableCollection<RecipeParamRow> ParamRows { get; } = new();
        public ObservableCollection<RecipeParamRow> ISDirty { get; set; } = new();

        // DataGrid 列
        public ObservableCollection<DataGridColumn> Columns { get; } = new();

        // 单元
        public ObservableCollection<UnitRecipeViewModelBase> Units { get; } = new();

        // 当前单元下所有 RecipeModel
        public ObservableCollection<RecipeModel> UnitRecipes { get; } = new();

        #region Selected

        private UnitRecipeViewModelBase _selectedUnit;
        public UnitRecipeViewModelBase SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                if (Set(ref _selectedUnit, value))
                {
                    LoadUnitRecipes(value);
                }
            }
        }

        private RecipeModel _selectedUnitRecipe;
        public RecipeModel SelectedUnitRecipe
        {
            get => _selectedUnitRecipe;
            set
            {
                if (Set(ref _selectedUnitRecipe, value))
                {
                    if (value != null)
                        ShowRecipe(value);
                }
            }
        }

        #endregion

        public ICommand SelectUnitCommand { get; }
        // 命令
        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }

        public UnitRecipeViewModel()
        {
            SelectUnitCommand = new CommandBase
            {
                DoExecute = obj => SelectedUnit = obj as UnitRecipeViewModelBase,
            };

            LoadUnits();
            SelectedUnit = Units.FirstOrDefault();
            // 初始化命令
            NewCommand = new CommandBase { DoExecute = _ => NewRecipe() };
            SaveCommand = new CommandBase { DoExecute = _ => SaveRecipe() };
            SaveAsCommand = new CommandBase { DoExecute = _ => SaveAsRecipe() };
            DeleteCommand = new CommandBase { DoExecute = _ => DeleteRecipe() };
            ImportCommand = new CommandBase { DoExecute = _ => ImportRecipe() };
            ExportCommand = new CommandBase { DoExecute = _ => ExportRecipe() };
        }

        #region Unit / Recipe Load

        private void LoadUnits()
        {
            Units.Clear();

            string root = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Recipe",
                "unit");

            if (!Directory.Exists(root))
                return;

            foreach (var dir in Directory.GetDirectories(root))
            {
                Units.Add(new GenericUnitRecipeViewModel(dir));
            }
        }
        #region Load Recipes

        private void LoadUnitRecipes(UnitRecipeViewModelBase unit)
        {
            UnitRecipes.Clear();
            ParamRows.Clear();
            Columns.Clear();

            if (unit == null) return;

            // ① 先建列
            BuildColumns();

            // ② 先按 unit.json 建“空行”
            BuildEmptyRows();

            // ③ 再加载 recipe 列表
            string unitPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Recipe", "unit", unit.UnitName);

            foreach (var file in Directory.GetFiles(unitPath, "*.rcp"))
            {
                UnitRecipes.Add(RecipeModel.LoadFromXml(file));
            }

            // 可选：自动选第一个
            SelectedUnitRecipe = UnitRecipes.FirstOrDefault();
        }


        #endregion


        #endregion

        #region 核心：显示配方

        private void ShowRecipe(RecipeModel recipe)
        {
            if (recipe == null || SelectedUnit == null)
                return;

            RecipeName = recipe.UnitRecipeName;
            Raise(nameof(RecipeName));

            // 不再 BuildColumns / BuildRows
            BuildRows(recipe);
        }

        private void BuildEmptyRows()
        {
            ParamRows.Clear();

            foreach (var item in SelectedUnit.Items)
            {
                var row = new RecipeParamRow
                {
                    Item = item.Name,
                    Definition = item
                };

                for (int i = 0; i < SelectedUnit.StepCount; i++)
                {
                    row.StepValues.Add(new RecipeStepValue
                    {
                        Value = string.Empty
                    });
                }

                ParamRows.Add(row);
            }
        }

        private void BuildRows(RecipeModel recipe)
        {
            if (recipe == null) return;

            foreach (var row in ParamRows)
            {
                for (int i = 0; i < SelectedUnit.StepCount; i++)
                {
                    string value = string.Empty;

                    if (i < recipe.Steps.Count &&
                        recipe.Steps[i].Parameters.TryGetValue(row.Item, out var v))
                    {
                        value = v;
                    }

                    row.StepValues[i].Value = value;
                }
            }
        }


        private void BuildColumns()
        {
            Columns.Clear();

            Columns.Add(new DataGridTextColumn
            {
                Header = "ITEM",
                Binding = new Binding(nameof(RecipeParamRow.Item)),
                IsReadOnly = true,
                Width = 180
            });

            for (int i = 0; i < SelectedUnit.StepCount; i++)
            {
                int index = i;
                Columns.Add(new DataGridTextColumn
                {
                    Header = $"Step {i + 1}",
                    Width = 120,
                    Binding = new Binding($"StepValues[{index}].Value")
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    }
                });
            }
        }

        #endregion

        #region 新建 / 保存 / 另存 / 删除 / 导入 / 导出

        private void NewRecipe()
        {
            if (SelectedUnit == null) return;

            var newRecipe = new RecipeModel
            {
                UnitRecipeName = $"NewRecipe_{DateTime.Now:yyyyMMdd_HHmmss}"
            };
            UnitRecipes.Add(newRecipe);
            SelectedUnitRecipe = newRecipe;
        }

        private void SaveRecipe()
        {
            if (SelectedUnitRecipe == null) return;

            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipe", "unit", SelectedUnit.UnitName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string path = SelectedUnitRecipe.SourceFile ?? Path.Combine(folder, SelectedUnitRecipe.UnitRecipeName + ".rcp");

            WriteRecipeXml(path, SelectedUnitRecipe);
            SelectedUnitRecipe.SourceFile = path;

            // 重置脏标记
            AcceptAllChanges();
        }

        private void SaveAsRecipe()
        {
            if (SelectedUnitRecipe == null || SelectedUnit == null) return;

            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipe", "unit", SelectedUnit.UnitName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, SelectedUnitRecipe.UnitRecipeName + "_Copy_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".rcp");

            WriteRecipeXml(path, SelectedUnitRecipe);
        }

        private void DeleteRecipe()
        {
            if (SelectedUnitRecipe == null) return;

            if (File.Exists(SelectedUnitRecipe.SourceFile))
                File.Delete(SelectedUnitRecipe.SourceFile);

            UnitRecipes.Remove(SelectedUnitRecipe);
            SelectedUnitRecipe = UnitRecipes.FirstOrDefault();
        }

        private void ImportRecipe()
        {
            // 可以弹出 OpenFileDialog 选择文件
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Recipe Files (*.rcp)|*.rcp";
            if (dlg.ShowDialog() == true)
            {
                var recipe = RecipeModel.LoadFromXml(dlg.FileName);
                UnitRecipes.Add(recipe);
                SelectedUnitRecipe = recipe;
            }
        }

        private void ExportRecipe()
        {
            if (SelectedUnitRecipe == null) return;

            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "Recipe Files (*.rcp)|*.rcp";
            dlg.FileName = SelectedUnitRecipe.UnitRecipeName;
            if (dlg.ShowDialog() == true)
            {
                WriteRecipeXml(dlg.FileName, SelectedUnitRecipe);
            }
        }

        private void WriteRecipeXml(string path, RecipeModel recipe)
        {
            var doc = new System.Xml.Linq.XDocument();
            var root = new System.Xml.Linq.XElement("Recipe");

            // 可选: 先写 step0
            //if (recipe.Step0 != null)
            //{
            //    var step0 = new System.Xml.Linq.XElement("step0");
            //    foreach (var kv in recipe.Step0.Parameters)
            //        step0.Add(new System.Xml.Linq.XElement(kv.Key, kv.Value));
            //    root.Add(step0);
            //}

            foreach (var step in recipe.Steps)
            {
                var stepNode = new System.Xml.Linq.XElement("step");
                stepNode.SetAttributeValue("sid", step.StepIndex);
                foreach (var kv in step.Parameters)
                    stepNode.Add(new System.Xml.Linq.XElement(kv.Key, kv.Value));
                root.Add(stepNode);
            }

            doc.Add(root);
            doc.Save(path);
        }

        #endregion

        #region 保存配方后重置 Dirty

        public void AcceptAllChanges()
        {
            foreach (var row in ParamRows)
            {
                foreach (var cell in row.StepValues)
                    cell.AcceptChanges();
            }
        }

        #endregion
    }
}
