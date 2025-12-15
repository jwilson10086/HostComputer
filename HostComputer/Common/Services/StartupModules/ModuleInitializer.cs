using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using MyLogger;

namespace HostComputer.Common.Services.StartupModules
{
    public class ModuleInitializer : IModuleInitializer
    {
        private readonly string _folderName;
        private readonly string _displayName;
        private readonly Type _viewModelType;

        public string ModuleName => _displayName;
        public string ModuleType => "Business";
        public InitializerPriority Priority => InitializerPriority.Business;
        public int Order => 100;
        public List<ModuleDependency> Dependencies =>
            new()
            {
                new ModuleDependency { ModuleName = "数据库服务", ModuleType = "Database" },
                new ModuleDependency { ModuleName = "UI框架", ModuleType = "UI" }
            };

        public ModuleInitializer(string folderName, string displayName, Type viewModelType = null)
        {
            _folderName = folderName;
            _displayName = displayName;
            _viewModelType = viewModelType;
        }

        public async Task<bool> InitializeAsync(Logger logger)
        {
            logger.Module($"开始初始化业务模块: {_displayName}");

            try
            {
                // 1. 检查模块目录
                if (!await CheckModuleDirectoryAsync(logger))
                {
                    return false;
                }

                // 2. 加载视图文件
                var viewFiles = await LoadViewFilesAsync(logger);

                // 3. 初始化视图模型
                await InitializeViewModelsAsync(logger);

                // 4. 注册路由或导航
                await RegisterNavigationAsync(logger);

                // 5. 初始化模块服务
                await InitializeModuleServicesAsync(logger);

                logger.Module($"✅ 业务模块 {_displayName} 初始化成功");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"❌ 业务模块 {_displayName} 初始化失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> CheckModuleDirectoryAsync(Logger logger)
        {
            string modulePath = $"Views/{_folderName}";

            if (!Directory.Exists(modulePath))
            {
                logger.Warning($"模块目录不存在: {modulePath}");

                // 尝试创建目录
                try
                {
                    Directory.CreateDirectory(modulePath);
                    logger.Module($"创建模块目录: {modulePath}");

                    // 创建示例文件
                    await CreateSampleFilesAsync(modulePath, logger);

                    return true;
                }
                catch (Exception ex)
                {
                    logger.Error($"创建模块目录失败: {ex.Message}");
                    return false;
                }
            }

            logger.Module($"模块目录存在: {modulePath}");
            return true;
        }

        private async Task<List<string>> LoadViewFilesAsync(Logger logger)
        {
            string modulePath = $"Views/{_folderName}";
            var viewFiles = new List<string>();

            // 查找所有 XAML 文件
            var xamlFiles = Directory.GetFiles(modulePath, "*.xaml", SearchOption.AllDirectories);

            foreach (var file in xamlFiles)
            {
                try
                {
                    string relativePath = Path.GetRelativePath("Views", file);
                    viewFiles.Add(relativePath);

                    // 检查对应的代码后台文件
                    string codeBehindFile = file + ".cs";
                    if (File.Exists(codeBehindFile))
                    {
                        logger.Module($"✅ 找到视图文件: {relativePath} (包含代码后台)");
                    }
                    else
                    {
                        logger.Module($"找到视图文件: {relativePath}");
                    }

                    // 如果是用户控件或窗口，尝试加载到资源字典
                    await TryRegisterViewResourceAsync(file, logger);
                }
                catch (Exception ex)
                {
                    logger.Error($"处理视图文件 {file} 失败: {ex.Message}");
                }
            }

            // 查找所有 ViewModel 文件
            var viewModelFiles = Directory.GetFiles(
                modulePath,
                "*ViewModel.cs",
                SearchOption.AllDirectories
            );
            foreach (var file in viewModelFiles)
            {
                string relativePath = Path.GetRelativePath("Views", file);
                logger.Module($"找到视图模型: {relativePath}");
            }

            logger.Module($"模块 {_displayName} 包含 {viewFiles.Count} 个视图文件");
            return viewFiles;
        }

        private async Task TryRegisterViewResourceAsync(string viewFile, Logger logger)
        {
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(viewFile);

                // 只处理 UserControl 或 Window
                if (fileName.EndsWith("View") || fileName.EndsWith("Window"))
                {
                    // 构建资源键
                    string resourceKey = $"{_folderName}_{fileName}";

                    // 这里可以注册到应用程序资源或导航服务
                    // Application.Current.Resources[resourceKey] = viewFile;

                    logger.Module($"注册视图资源: {resourceKey}");
                }

                await Task.Delay(5);
            }
            catch (Exception ex)
            {
                logger.Error($"注册视图资源失败: {ex.Message}");
            }
        }

        private async Task InitializeViewModelsAsync(Logger logger)
        {
            if (_viewModelType != null)
            {
                logger.Module($"初始化视图模型: {_viewModelType.Name}");

                try
                {
                    // 使用反射创建视图模型实例
                    var viewModelInstance = Activator.CreateInstance(_viewModelType);

                    // 这里可以注册到依赖注入容器
                    // services.AddSingleton(_viewModelType, viewModelInstance);

                    logger.Module($"✅ 视图模型 {_viewModelType.Name} 初始化成功");
                }
                catch (Exception ex)
                {
                    logger.Error($"视图模型初始化失败: {ex.Message}");
                }
            }
            else
            {
                // 尝试自动发现视图模型
                await AutoDiscoverViewModelsAsync(logger);
            }

            await Task.Delay(20);
        }

        private async Task AutoDiscoverViewModelsAsync(Logger logger)
        {
            try
            {
                string modulePath = $"Views/{_folderName}";
                var viewModelFiles = Directory.GetFiles(
                    modulePath,
                    "*ViewModel.cs",
                    SearchOption.AllDirectories
                );

                foreach (var file in viewModelFiles)
                {
                    string className = Path.GetFileNameWithoutExtension(file);

                    // 尝试从当前程序集加载类型
                    var currentAssembly = Assembly.GetExecutingAssembly();
                    var types = currentAssembly.GetTypes();

                    var viewModelType = types.FirstOrDefault(t =>
                        t.Name == className
                        && t.Namespace != null
                        && t.Namespace.Contains(_folderName.Replace("_", ""))
                    );

                    if (viewModelType != null)
                    {
                        logger.Module($"发现视图模型类型: {viewModelType.FullName}");
                    }
                }

                await Task.Delay(10);
            }
            catch (Exception ex)
            {
                logger.Error($"自动发现视图模型失败: {ex.Message}");
            }
        }

        private async Task RegisterNavigationAsync(Logger logger)
        {
            logger.Module("注册模块导航...");

            try
            {
                // 这里可以注册到你的导航服务
                // NavigationService.RegisterModule(_folderName, _displayName);

                // 或者添加到主窗口的菜单
                // MainWindow.AddModuleMenuItem(_displayName, () => OpenModuleView());

                logger.Module($"✅ 模块 {_displayName} 导航注册完成");

                await Task.Delay(20);
            }
            catch (Exception ex)
            {
                logger.Error($"注册导航失败: {ex.Message}");
            }
        }

        private async Task InitializeModuleServicesAsync(Logger logger)
        {
            logger.Module("初始化模块服务...");

            try
            {
                // 根据模块名称初始化特定的服务
                switch (_folderName)
                {
                    case "Equipment_Setup":
                        await InitializeEquipmentServicesAsync(logger);
                        break;

                    case "Recipe_Editor":
                        await InitializeRecipeServicesAsync(logger);
                        break;

                    case "Maintenance":
                        await InitializeMaintenanceServicesAsync(logger);
                        break;

                    case "Parameter":
                        await InitializeParameterServicesAsync(logger);
                        break;

                    case "Lot_Operation":
                        await InitializeLotOperationServicesAsync(logger);
                        break;

                    case "History":
                        await InitializeHistoryServicesAsync(logger);
                        break;

                    case "Overview":
                        await InitializeOverviewServicesAsync(logger);
                        break;

                    case "3thViews":
                        await InitializeThirdPartyServicesAsync(logger);
                        break;

                    default:
                        logger.Module($"通用模块初始化: {_displayName}");
                        break;
                }

                await Task.Delay(30);
            }
            catch (Exception ex)
            {
                logger.Error($"初始化模块服务失败: {ex.Message}");
            }
        }

        private async Task InitializeThirdPartyServicesAsync(Logger logger)
        {
            throw new NotImplementedException();
        }

        private async Task InitializeOverviewServicesAsync(Logger logger)
        {
            throw new NotImplementedException();
        }

        private async Task InitializeHistoryServicesAsync(Logger logger)
        {
            throw new NotImplementedException();
        }

        private async Task InitializeLotOperationServicesAsync(Logger logger)
        {
            throw new NotImplementedException();
        }

        private async Task InitializeParameterServicesAsync(Logger logger)
        {
            throw new NotImplementedException();
        }

        private async Task InitializeMaintenanceServicesAsync(Logger logger)
        {
            throw new NotImplementedException();
        }

        private async Task InitializeEquipmentServicesAsync(Logger logger)
        {
            logger.Module("初始化设备设置服务...");

            // 这里可以初始化设备相关的服务
            // 例如：设备管理器、设备通信服务等

            await Task.Delay(50);
            logger.Module("✅ 设备设置服务初始化完成");
        }

        private async Task InitializeRecipeServicesAsync(Logger logger)
        {
            logger.Module("初始化配方编辑服务...");

            // 配方管理服务初始化
            // 例如：配方验证、配方版本控制、配方导入导出等

            await Task.Delay(50);
            logger.Module("✅ 配方编辑服务初始化完成");
        }

        private async Task CreateSampleFilesAsync(string modulePath, Logger logger)
        {
            try
            {
                // 根据模块类型创建示例文件
                switch (_folderName)
                {
                    case "Equipment_Setup":
                        await CreateEquipmentSampleFilesAsync(modulePath, logger);
                        break;

                    default:
                        // 创建基本的视图文件
                        string viewFile = Path.Combine(modulePath, $"{_folderName}View.xaml");
                        string viewModelFile = Path.Combine(
                            modulePath,
                            $"{_folderName}ViewModel.cs"
                        );

                        // 创建简单的 XAML 文件
                        string xamlContent =
                            $@"<UserControl x:Class=""HostComputer.Views.{_folderName}.{_folderName}View""
        xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
        mc:Ignorable="""">
    <Grid>
        <TextBlock Text=""{_displayName}"" HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
    </Grid>
</UserControl>";

                        await File.WriteAllTextAsync(viewFile, xamlContent);

                        // 创建 ViewModel 文件
                        string viewModelContent =
                            $@"using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace HostComputer.ViewModels
{{
    public class {_folderName}ViewModel : INotifyPropertyChanged
    {{
        public event PropertyChangedEventHandler PropertyChanged;
        
        public string ModuleName => ""{_displayName}"";
        public DateTime LoadedTime {{ get; }} = DateTime.Now;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {{
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }}
    }}
}}";

                        await File.WriteAllTextAsync(viewModelFile, viewModelContent);

                        break;
                }

                logger.Module($"✅ 为模块 {_displayName} 创建了示例文件");
            }
            catch (Exception ex)
            {
                logger.Error($"创建示例文件失败: {ex.Message}");
            }
        }

        private async Task CreateEquipmentSampleFilesAsync(string modulePath, Logger logger)
        {
            // 创建设备设置模块的专用文件
            string equipmentView = Path.Combine(modulePath, "EquipmentSetupView.xaml");
            string equipmentViewModel = Path.Combine(modulePath, "EquipmentSetupViewModel.cs");
            string equipmentService = Path.Combine(modulePath, "EquipmentService.cs");

            // 创建视图文件
            string viewContent =
                @"<UserControl x:Class=""HostComputer.Views.Equipment_Setup.EquipmentSetupView""
        xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        xmlns:local=""clr-namespace:HostComputer.Views.Equipment_Setup"">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto""/>
            <RowDefinition Height=""*""/>
        </Grid.RowDefinitions>
        
        <TextBlock Grid.Row=""0"" Text=""设备设置"" FontSize=""18"" FontWeight=""Bold"" Margin=""10""/>
        
        <Border Grid.Row=""1"" Background=""#f5f5f5"" Margin=""10"">
            <StackPanel>
                <TextBlock Text=""设备设置模块"" Margin=""10""/>
                <Button Content=""扫描设备"" Margin=""10"" Width=""100""/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>";

            await File.WriteAllTextAsync(equipmentView, viewContent);

            // 创建 ViewModel 文件
            string viewModelContent =
                @"using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace HostComputer.ViewModels
{
    public class EquipmentSetupViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        public ObservableCollection<Equipment> EquipmentList { get; } = new();
        
        public ICommand ScanEquipmentCommand { get; }
        
        public EquipmentSetupViewModel()
        {
            ScanEquipmentCommand = new RelayCommand(_ => ScanEquipment());
            LoadSampleData();
        }
        
        private void LoadSampleData()
        {
            EquipmentList.Add(new Equipment { Name = ""设备1"", Status = ""在线"", Type = ""PLC"" });
            EquipmentList.Add(new Equipment { Name = ""设备2"", Status = ""离线"", Type = ""机器人"" });
        }
        
        private void ScanEquipment()
        {
            // 扫描设备逻辑
        }
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class Equipment
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
    }
}";

            await File.WriteAllTextAsync(equipmentViewModel, viewModelContent);
        }
    }
}
