using HostComputer.Common;
using HostComputer.Common.Base;
using HostComputer.Common.Services;
using HostComputer.Models.RicipeEditor;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;

namespace HostComputer.ViewModels.Recipe_Editor
{
    /// <summary>
    /// 配方编辑 ViewModel
    /// 说明：
    ///  - UI 绑定 DataGrid 的行列数据，通过 ParamRows 和 Columns 控制
    ///  - 数据流转涉及 RecipeModel、UnitStepModel，操作增删改都在这里处理
    /// </summary>
    public class UnitRecipeViewModel : ViewModelBase
    {
        #region 属性

        /// <summary>当前显示的配方名称（UI 绑定）</summary>
        public string RecipeName { get; private set; }

        /// <summary>是否处于编辑状态（UI 控制可编辑按钮启用/禁用）</summary>
        private bool _isEditing = false;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (Set(ref _isEditing, value))
                {
                    // 编辑状态变化时刷新命令可用性
                    ((CommandBase)InsertStepBeforeCommand).RaiseCanExecuteChanged();
                    ((CommandBase)InsertStepAfterCommand).RaiseCanExecuteChanged();
                    ((CommandBase)DeleteStepCommand).RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 当前选中步骤索引（纯整数，不绑定 DataGridCellInfo）
        /// UI 点击 DataGrid 单元格会更新此值
        /// </summary>
        private int _currentStepIndex = -1;
        public int CurrentStepIndex
        {
            get => _currentStepIndex;
            set
            {
                if (Set(ref _currentStepIndex, value))
                {
                    // 步骤操作命令状态更新
                    ((CommandBase)InsertStepBeforeCommand).RaiseCanExecuteChanged();
                    ((CommandBase)InsertStepAfterCommand).RaiseCanExecuteChanged();
                    ((CommandBase)DeleteStepCommand).RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>DataGrid 行数据集合（UI 绑定）</summary>
        public ObservableCollection<RecipeParamRow> ParamRows { get; } = new();

        /// <summary>DataGrid 列集合（UI 绑定）</summary>
        public ObservableCollection<DataGridColumn> Columns { get; } = new();

        /// <summary>所有单元（UI 单元选择下拉框绑定）</summary>
        public ObservableCollection<UnitRecipeViewModelBase> Units { get; } = new();

        /// <summary>当前单元下的所有配方数据（UI 配方选择列表绑定）</summary>
        public ObservableCollection<RecipeModel> UnitRecipes { get; } = new();

        #endregion

        #region Selected 单元/配方

        private UnitRecipeViewModelBase _selectedUnit;
        public UnitRecipeViewModelBase SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                if (Set(ref _selectedUnit, value))
                {
                    // 当切换单元时，重新加载该单元下配方
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
                        ShowRecipe(value); // 显示选中配方到 DataGrid（UI 更新）
                }
            }
        }

        #endregion

        #region Commands 命令（UI 绑定按钮/点击事件）

        public ICommand SelectUnitCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }

        // DataGrid 单元格点击命令
        public ICommand CellClickCommand { get; }

        // 步骤操作命令
        public ICommand InsertStepBeforeCommand { get; }
        public ICommand InsertStepAfterCommand { get; }
        public ICommand DeleteStepCommand { get; }
        public ICommand EditCommand { get; }

        #endregion

        #region 构造函数

        public UnitRecipeViewModel()
        {
            // 单元选择命令
            SelectUnitCommand = new CommandBase
            {
                DoExecute = obj => SelectedUnit = obj as UnitRecipeViewModelBase,
            };

            // 加载所有单元
            LoadUnits();
            SelectedUnit = Units.FirstOrDefault();

            // 配方操作命令
            NewCommand = new CommandBase { DoExecute = _ => NewRecipe() };
            SaveCommand = new CommandBase { DoExecute = _ => SaveRecipe() };
            SaveAsCommand = new CommandBase { DoExecute = _ => SaveAsRecipe() };
            EditCommand = new CommandBase
            {
                DoExecute = _ =>
                {
                    IsEditing = true;
                    if (CurrentStepIndex == -1 && SelectedUnitRecipe?.Steps.Count > 0)
                        CurrentStepIndex = 0;
                }
            };
            DeleteCommand = new CommandBase { DoExecute = _ => DeleteRecipe() };
            ImportCommand = new CommandBase { DoExecute = _ => ImportRecipe() };
            ExportCommand = new CommandBase { DoExecute = _ => ExportRecipe() };

            // 步骤操作命令，绑定权限（UI 可用性由 PermissionService 控制）
            InsertStepBeforeCommand = new CommandBase
            {
                Permission = new PermissionAttribute(4),
                Name = "Insert Step Before",
                DoExecute = _ => InsertStep(true),
                CanExecuteFunc = _ => CanEditStep()
            };
            InsertStepAfterCommand = new CommandBase
            {
                Name = "Insert Step After",
                DoExecute = _ => InsertStep(false),
                CanExecuteFunc = _ => CanEditStep()
            };
            DeleteStepCommand = new CommandBase
            {
                Name = "Delete Step",
                DoExecute = _ => DeleteStep(),
                CanExecuteFunc = _ => CanEditStep()
            };

            // DataGrid 单元格点击命令
            CellClickCommand = new CommandBase
            {
                DoExecute = para =>
                {
                    if (para is MouseButtonEventArgs e)
                        OnCellClick(e);
                }
            };
        }

        #endregion

        #region DataGrid 单元格点击处理（UI 事件 → 数据流转）

        private void OnCellClick(MouseButtonEventArgs e)
        {
            if (e == null) return;

            DependencyObject dep = (DependencyObject)e.OriginalSource;

            // 找到点击的 DataGridCell
            while (dep != null && dep is not DataGridCell)
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is not DataGridCell cell) return;

            // 当前列索引
            int columnIndex = cell.Column.DisplayIndex;

            // 当前行数据
            var rowData = cell.DataContext as RecipeParamRow;

            // 如果第0列是 ITEM 列，跳过
            int stepIndex = columnIndex - 1;
            if (stepIndex < 0) return;

            // 更新 VM 属性（数据流转 → 当前步骤索引）
            CurrentStepIndex = stepIndex;

            // 调试输出
            Console.WriteLine($"点击单元格：行={rowData?.Definition.DisplayName}, 列={columnIndex}");
        }

        #endregion

        #region Unit / Recipe Load（数据流转）

        private void LoadUnits()
        {
            Units.Clear();
            string root = Path.Combine(PathManager.RecipeDir, "unit");
            if (!Directory.Exists(root)) return;

            foreach (var dir in Directory.GetDirectories(root))
                Units.Add(new GenericUnitRecipeViewModel(dir));
        }

        private void LoadUnitRecipes(UnitRecipeViewModelBase unit)
        {
            UnitRecipes.Clear();
            ParamRows.Clear();
            Columns.Clear();
            if (unit == null) return;

            // 构建 UI 列和空行
            BuildColumns();
            BuildEmptyRows();

            // 数据流转：读取配方文件到 UnitRecipes
            string unitPath = Path.Combine(PathManager.RecipeDir, "unit", unit.UnitName);
            foreach (var file in Directory.GetFiles(unitPath, "*.rcp"))
                UnitRecipes.Add(RecipeModel.LoadFromXml(file));

            // 默认选中第一个配方
            SelectedUnitRecipe = UnitRecipes.FirstOrDefault();
        }

        #endregion

        #region 显示配方（UI 绑定 DataGrid）

        private void ShowRecipe(RecipeModel recipe)
        {
            if (recipe == null || SelectedUnit == null)
                return;

            RecipeName = recipe.UnitRecipeName;
            Raise(nameof(RecipeName));

            // 数据流转：把 RecipeModel 组装成 ParamRows（UI DataGrid 行数据）
            RecipeAssembler.BuildRows(
                recipe,
                SelectedUnit.Items,
                ParamRows,
                SelectedUnit.StepCount);
        }

        private void BuildEmptyRows()
        {
            ParamRows.Clear();
            foreach (var item in SelectedUnit.Items)
            {
                var row = new RecipeParamRow
                {
                    Item = item.DisplayName,
                    Definition = item
                };
                for (int i = 0; i < SelectedUnit.StepCount; i++)
                    row.StepValues.Add(new RecipeStepValue { Value = string.Empty });
                ParamRows.Add(row);
            }
        }

        private void BuildColumns()
        {
            Columns.Clear();

            // UI 第一列：ITEM 列，显示行名
            Columns.Add(new DataGridTextColumn
            {
                Header = "ITEM",
                Binding = new Binding("Item"),
                IsReadOnly = true,
                MinWidth = 150,
                Width = DataGridLength.Auto
            });

            // 后续列：步骤列，绑定 StepValues（数据流转）
            for (int i = 0; i < SelectedUnit.StepCount; i++)
            {
                int index = i;
                Columns.Add(new DataGridTextColumn
                {
                    Header = $"STEP {i + 1}",
                    Binding = new Binding($"StepValues[{index}].Value") { Mode = BindingMode.TwoWay },
                    Width = 100
                });
            }
        }

        #endregion

        #region 新建/保存/删除/导入/导出（数据流转 + UI 更新）

        private void NewRecipe()
        {
            if (SelectedUnit == null) return;

            var newRecipe = new RecipeModel
            {
                UnitRecipeName = $"NewRecipe_{DateTime.Now:yyyyMMdd_HHmmss}"
            };
            UnitRecipes.Add(newRecipe);

            SelectedUnitRecipe = newRecipe; // UI 更新
        }

        private void SaveRecipe()
        {
            if (SelectedUnitRecipe == null) return;

            // 数据流转：把 ParamRows 同步到 RecipeModel
            SyncParamsToRecipe();

            string folder = Path.Combine(PathManager.RecipeDir, "unit", SelectedUnit.UnitName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string path = SelectedUnitRecipe.SourceFile ?? Path.Combine(folder, SelectedUnitRecipe.UnitRecipeName + ".rcp");
            WriteRecipeXml(path, SelectedUnitRecipe);

            SelectedUnitRecipe.SourceFile = path;

            // UI 记录已保存状态
            AcceptAllChanges();
        }

        private void SaveAsRecipe()
        {
            if (SelectedUnitRecipe == null || SelectedUnit == null) return;

            SyncParamsToRecipe();

            string folder = Path.Combine(PathManager.RecipeDir, "unit", SelectedUnit.UnitName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, SelectedUnitRecipe.UnitRecipeName + "_Copy_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".rcp");
            WriteRecipeXml(path, SelectedUnitRecipe);
        }

        /// <summary>数据流转：把 ParamRows 同步到 RecipeModel 的 Steps</summary>
        private void SyncParamsToRecipe()
        {
            foreach (var step in SelectedUnitRecipe.Steps)
                step.Parameters.Clear();

            foreach (var row in ParamRows)
            {
                for (int i = 0; i < row.StepValues.Count; i++)
                {
                    var step = SelectedUnitRecipe.Steps[i];
                    step.Parameters[row.Item] = row.StepValues[i].Value;
                }
            }
        }

        private void DeleteRecipe()
        {
            if (SelectedUnitRecipe == null) return;
            if (File.Exists(SelectedUnitRecipe.SourceFile)) File.Delete(SelectedUnitRecipe.SourceFile);

            UnitRecipes.Remove(SelectedUnitRecipe);

            SelectedUnitRecipe = UnitRecipes.FirstOrDefault(); // UI 更新
        }

        private void ImportRecipe()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Recipe Files (*.rcp)|*.rcp";

            if (dlg.ShowDialog() == true)
            {
                var recipe = RecipeModel.LoadFromXml(dlg.FileName);
                UnitRecipes.Add(recipe);
                SelectedUnitRecipe = recipe; // UI 更新
            }
        }

        private void ExportRecipe()
        {
            if (SelectedUnitRecipe == null) return;

            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "Recipe Files (*.rcp)|*.rcp";
            dlg.FileName = SelectedUnitRecipe.UnitRecipeName;

            if (dlg.ShowDialog() == true)
                WriteRecipeXml(dlg.FileName, SelectedUnitRecipe);
        }

        private void WriteRecipeXml(string path, RecipeModel recipe)
        {
            var root = new XElement("Recipe");

            foreach (var step in recipe.Steps.OrderBy(s => s.StepIndex))
            {
                var stepNode = new XElement($"step{step.StepIndex}");
                stepNode.SetAttributeValue("sid", step.StepIndex);

                foreach (var kv in step.Parameters)
                {
                    stepNode.Add(new XElement(kv.Key, kv.Value));
                }

                root.Add(stepNode);
            }

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
            doc.Save(path);
        }

        #endregion

        #region 插入/删除步骤（数据流转 + UI 更新）

        private bool CanEditStep()
        {
            // 这里可以加权限或编辑状态判断
            if(!IsEditing) return false;
            return true;
        }

        private void InsertStep(bool before)
        {
            if (!CanEditStep()) return;

            int insertIndex = before ? CurrentStepIndex : CurrentStepIndex + 1;

            // 数据流转：插入空 Step
            var newStep = new UnitStepModel();
            SelectedUnitRecipe.Steps.Insert(insertIndex, newStep);

            // UI 更新：每行插入空 StepValue
            foreach (var row in ParamRows)
                row.StepValues.Insert(insertIndex, new RecipeStepValue { Value = "" });

            SelectedUnit.StepCount++;
            BuildColumns(); // UI 刷新列

            ReIndexSteps();
            CurrentStepIndex = insertIndex;
        }

        private void DeleteStep()
        {
            if (!CanEditStep()) return;

            SelectedUnitRecipe.Steps.RemoveAt(CurrentStepIndex);

            foreach (var row in ParamRows)
                row.StepValues.RemoveAt(CurrentStepIndex);

            SelectedUnit.StepCount--;
            BuildColumns();

            ReIndexSteps();
            CurrentStepIndex = -1;
        }

        private void ReIndexSteps()
        {
            for (int i = 0; i < SelectedUnitRecipe.Steps.Count; i++)
                SelectedUnitRecipe.Steps[i].StepIndex = i + 1;
        }

        #endregion

        #region Dirty 状态

        /// <summary>UI 状态：标记所有单元格已保存</summary>
        public void AcceptAllChanges()
        {
            foreach (var row in ParamRows)
                foreach (var cell in row.StepValues)
                    cell.AcceptChanges();
        }

        #endregion
    }
}
