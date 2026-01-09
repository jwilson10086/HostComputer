using System.IO;
using HostComputer.Common.Base;
using HostComputer.Common.Services.Components;

namespace HostComputer.Common.Services.LocalDataService.Component
{
    /// <summary>
    /// 设备组态组件本地存储服务
    /// </summary>
    public class ComponentLocalService
    {
        private readonly string _configDir;

        public ComponentLocalService()
        {
            // ConfigFile/DeviceConfig
            _configDir = PathManager.ConfigDevice;

            Directory.CreateDirectory(_configDir);
        }

        /// <summary>
        /// 获取完整文件路径
        /// </summary>
        private string GetFilePath(string fileName)
        {
            return Path.Combine(_configDir, $"{fileName}.json");
        }

        /// <summary>
        /// 保存设备组态组件数据
        /// </summary>
        public bool SaveLayout(LayoutConfig config, string fileName)
        {
            var path = GetFilePath(fileName);
            return JsonFileHelper.SaveToFile(path, config);
        }

        /// <summary>
        /// 加载设备组态组件数据
        /// </summary>
        public LayoutConfig? LoadLayout(string fileName)
        {
            var path = GetFilePath(fileName);
            return JsonFileHelper.LoadFromFile<LayoutConfig>(path);
        }

        /// <summary>
        /// 删除配置文件
        /// </summary>
        public bool DeleteLayout(string fileName)
        {
            var path = GetFilePath(fileName);
            return JsonFileHelper.Delete(path);
        }

        /// <summary>
        /// 配置文件是否存在
        /// </summary>
        public bool Exists(string fileName)
        {
            var path = GetFilePath(fileName);
            return JsonFileHelper.Exists(path);
        }
    }
}
