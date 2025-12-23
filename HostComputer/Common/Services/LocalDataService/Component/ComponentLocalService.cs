using HostComputer.Common.Base;
using HostComputer.Common.Services.Components;
using System.IO;

namespace HostComputer.Common.Services.LocalDataService.Component
{
    public class ComponentLocalService
    {
        private readonly string _folder;
        private readonly string _fileName;

        public ComponentLocalService(string folder = "DeviceConfig")
        {
            //_folder = Path.Combine(Environment.GetFolderPath(), folder);
            _folder = folder;
        }

        private string FilePath => Path.Combine(_folder, _fileName);

        /// <summary>
        /// 保存设备组态组件数据
        /// </summary>
        public bool SaveLayout(LayoutConfig config, string fileName)
        {
            fileName = $"{fileName}.json";
            string filepath = Path.Combine(_folder, fileName);

            return JsonFileHelper.SaveToFile(filepath, config);
        }

        /// <summary>
        /// 加载设备组态组件数据
        /// </summary>
        public LayoutConfig? LoadLayout(string fileName)
        {
            fileName = $"{fileName}.json";
            string filepath = Path.Combine(_folder, fileName);
            return JsonFileHelper.LoadFromFile<LayoutConfig>(filepath);
        }

        /// <summary>
        /// 删除配置文件
        /// </summary>
        public bool DeleteLayout()
        {
            return JsonFileHelper.Delete(FilePath);
        }

        /// <summary>
        /// 配置文件是否存在
        /// </summary>
        public bool Exists()
        {
            return JsonFileHelper.Exists(FilePath);
        }
    }
}
