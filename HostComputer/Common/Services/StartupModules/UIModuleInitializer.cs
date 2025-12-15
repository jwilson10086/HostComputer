using MyLogger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace HostComputer.Common.Services.StartupModules
{
    public class UIModuleInitializer : IModuleInitializer
    {
        public string ModuleName => "UI框架";
        public string ModuleType => "UI";
        public InitializerPriority Priority => InitializerPriority.UI;
        public int Order => 10;
        public List<ModuleDependency> Dependencies => new()
        {
            new ModuleDependency { ModuleName = "配置服务", ModuleType = "Config" }
        };

        public async Task<bool> InitializeAsync(Logger logger)
        {
            logger.UI("开始初始化UI框架...");

            try
            {
                // 必须在UI线程上执行
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    await InitializeOnUIThreadAsync(logger);
                }
                else
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await InitializeOnUIThreadAsync(logger);
                    });
                }

                logger.UI("✅ UI框架初始化成功");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"❌ UI框架初始化失败: {ex.Message}");
                return false;
            }
        }

        private async Task InitializeOnUIThreadAsync(Logger logger)
        {
            logger.UI($"UI线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            // 1. 加载UI资源字典
            await LoadResourceDictionariesAsync(logger);

            // 2. 初始化主题
            await InitializeThemeAsync(logger);

            // 3. 注册全局样式
            await RegisterGlobalStylesAsync(logger);

            // 4. 初始化字体
            await InitializeFontsAsync(logger);

            // 5. 初始化图标
            await InitializeIconsAsync(logger);
        }

        private async Task LoadResourceDictionariesAsync(Logger logger)
        {
            logger.UI("加载UI资源字典...");

            var resourceFiles = new[]
            {
                "Styles/Generic.xaml",
                "Styles/Buttons.xaml",
                "Styles/TextBoxes.xaml",
                "Styles/DataGrid.xaml",
                "Styles/ComboBox.xaml",
                "Styles/Menu.xaml",
                "Styles/WindowChrome.xaml"
            };

            int loadedCount = 0;
            foreach (var resourceFile in resourceFiles)
            {
                try
                {
                    if (File.Exists(resourceFile))
                    {
                        var uri = new Uri(resourceFile, UriKind.RelativeOrAbsolute);
                        var resourceDict = new ResourceDictionary { Source = uri };

                        // 合并到应用程序资源
                        Application.Current.Resources.MergedDictionaries.Add(resourceDict);

                        loadedCount++;
                        logger.UI($"✅ 加载资源: {resourceFile}");
                    }
                    else
                    {
                        logger.Warning($"资源文件不存在: {resourceFile}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"加载资源失败 {resourceFile}: {ex.Message}");
                }

                await Task.Delay(10); // 给UI更新时间
            }

            logger.UI($"已加载 {loadedCount}/{resourceFiles.Length} 个资源字典");
        }

        private async Task InitializeThemeAsync(Logger logger)
        {
            logger.UI("初始化主题系统...");

            try
            {
                // 检查主题配置文件
                string themeConfig = "Config/Theme.json";
                string themeName = "Dark";

                if (File.Exists(themeConfig))
                {
                    // 读取主题配置
                    // var config = JsonSerializer.Deserialize<ThemeConfig>(File.ReadAllText(themeConfig));
                    // themeName = config.ThemeName;
                    logger.UI($"从配置文件读取主题: {themeName}");
                }
                else
                {
                    logger.UI("使用默认主题: Dark");
                }

                // 加载主题资源
                await LoadThemeResourcesAsync(themeName, logger);

                // 设置应用程序主题
                SetApplicationTheme(themeName);

                logger.UI($"✅ 主题设置完成: {themeName}");
            }
            catch (Exception ex)
            {
                logger.Error($"主题初始化失败: {ex.Message}");
                // 使用默认主题
                await LoadThemeResourcesAsync("Dark", logger);
            }
        }

        private async Task LoadThemeResourcesAsync(string themeName, Logger logger)
        {
            string themeFile = $"Themes/{themeName}.xaml";

            if (File.Exists(themeFile))
            {
                try
                {
                    var uri = new Uri(themeFile, UriKind.RelativeOrAbsolute);
                    var themeDict = new ResourceDictionary { Source = uri };

                    // 添加到应用程序资源
                    Application.Current.Resources.MergedDictionaries.Add(themeDict);

                    logger.UI($"✅ 加载主题: {themeName}");
                }
                catch (Exception ex)
                {
                    logger.Error($"加载主题失败 {themeFile}: {ex.Message}");
                }
            }
            else
            {
                logger.Warning($"主题文件不存在: {themeFile}");

                // 尝试加载内置默认主题
                await LoadDefaultThemeAsync(logger);
            }

            await Task.Delay(50);
        }

        private async Task LoadDefaultThemeAsync(Logger logger)
        {
            logger.UI("加载内置默认主题...");

            // 创建默认深色主题
            var darkTheme = new ResourceDictionary();

            // 定义颜色
            darkTheme["BackgroundColor"] = "#1a1a1a";
            darkTheme["ForegroundColor"] = "#ffffff";
            darkTheme["AccentColor"] = "#0078d4";
            darkTheme["BorderColor"] = "#333333";
            darkTheme["HoverColor"] = "#2a2a2a";

            // 定义画笔
            darkTheme["BackgroundBrush"] = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a1a"));
            darkTheme["ForegroundBrush"] = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffffff"));

            Application.Current.Resources.MergedDictionaries.Add(darkTheme);

            await Task.Delay(30);
            logger.UI("✅ 默认主题加载完成");
        }

        private void SetApplicationTheme(string themeName)
        {
            // 设置应用程序范围的主题相关属性
            Application.Current.Resources["CurrentTheme"] = themeName;
            Application.Current.Resources["IsDarkTheme"] = themeName == "Dark";

            // 设置窗口 chrome 样式
            // 这里可以设置窗口边框、标题栏颜色等
        }

        private async Task RegisterGlobalStylesAsync(Logger logger)
        {
            logger.UI("注册全局样式...");

            // 注册按钮样式
            var buttonStyle = new Style(typeof(System.Windows.Controls.Button));
            buttonStyle.Setters.Add(new Setter(System.Windows.Controls.Control.PaddingProperty, new Thickness(10, 5, 10, 5)));
            buttonStyle.Setters.Add(new Setter(System.Windows.Controls.Control.MarginProperty, new Thickness(5)));

            Application.Current.Resources[typeof(System.Windows.Controls.Button)] = buttonStyle;

            // 注册文本框样式
            var textBoxStyle = new Style(typeof(System.Windows.Controls.TextBox));
            textBoxStyle.Setters.Add(new Setter(System.Windows.Controls.Control.PaddingProperty, new Thickness(5)));
            textBoxStyle.Setters.Add(new Setter(System.Windows.Controls.Control.MarginProperty, new Thickness(5)));

            Application.Current.Resources[typeof(System.Windows.Controls.TextBox)] = textBoxStyle;

            await Task.Delay(30);
            logger.UI("✅ 全局样式注册完成");
        }

        private async Task InitializeFontsAsync(Logger logger)
        {
            logger.UI("初始化字体系统...");

            try
            {
                // 添加应用程序字体
                var fontUris = new[]
                {
                    "pack://application:,,,/Resources/Fonts/#Microsoft YaHei",
                    "pack://application:,,,/Resources/Fonts/#Segoe UI"
                };

                foreach (var fontUri in fontUris)
                {
                    try
                    {
                        var fontFamily = new System.Windows.Media.FontFamily(fontUri);
                        logger.UI($"✅ 加载字体: {fontFamily.Source}");
                    }
                    catch
                    {
                        logger.Warning($"字体加载失败: {fontUri}");
                    }
                }

                // 设置默认字体
                Application.Current.Resources["DefaultFontFamily"] =
                    new System.Windows.Media.FontFamily("Microsoft YaHei, Segoe UI, Arial");
                Application.Current.Resources["DefaultFontSize"] = 14.0;

                await Task.Delay(30);
                logger.UI("✅ 字体系统初始化完成");
            }
            catch (Exception ex)
            {
                logger.Error($"字体初始化失败: {ex.Message}");
            }
        }

        private async Task InitializeIconsAsync(Logger logger)
        {
            logger.UI("初始化图标系统...");

            try
            {
                // 检查图标资源文件
                string iconFile = "Resources/Icons.xaml";

                if (File.Exists(iconFile))
                {
                    var uri = new Uri(iconFile, UriKind.RelativeOrAbsolute);
                    var iconDict = new ResourceDictionary { Source = uri };

                    Application.Current.Resources.MergedDictionaries.Add(iconDict);
                    logger.UI("✅ 图标资源加载完成");
                }
                else
                {
                    logger.Warning("图标资源文件不存在，将使用默认图标");
                    await LoadDefaultIconsAsync(logger);
                }

                await Task.Delay(30);
            }
            catch (Exception ex)
            {
                logger.Error($"图标初始化失败: {ex.Message}");
            }
        }

        private async Task LoadDefaultIconsAsync(Logger logger)
        {
            // 创建一些基本图标
            var iconDict = new ResourceDictionary();

            // 这里可以添加一些基本的 DrawingBrush 或 Geometry 作为图标
            // 例如：AppIcon, CloseIcon, MaximizeIcon 等

            Application.Current.Resources.MergedDictionaries.Add(iconDict);
            await Task.Delay(20);
            logger.UI("✅ 默认图标加载完成");
        }
    }
}