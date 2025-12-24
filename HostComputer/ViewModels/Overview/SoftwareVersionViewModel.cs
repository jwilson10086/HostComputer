using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.ViewModels.Overview
{
    public class SoftwareVersionViewModel
    {
        public string SoftwareName { get; }
        public string SoftwareVersion { get; }
        public string PlcFirmwareVersion { get; }
        public string GitRevision { get; }
        public string BuildTime { get; }
        public string LastModifiedDate { get; }

        public SoftwareVersionViewModel()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();

            SoftwareName = assemblyName.Name ?? "Unknown";
            SoftwareVersion = assemblyName.Version?.ToString() ?? "N/A";

            // PLC Firmware（一般来自配置/PLC通讯）
            PlcFirmwareVersion = ReadPlcFirmwareVersion();

            // Git Revision（来自 AssemblyInfo / 自动注入）
            GitRevision = ReadGitRevision();

            // Build Time
            BuildTime = GetBuildTime(assembly).ToString("yyyy-MM-dd HH:mm:ss");

            // Last Modified Date
            LastModifiedDate = File.GetLastWriteTime(assembly.Location)
                                   .ToString("yyyy-MM-dd HH:mm:ss");
        }

        private string ReadPlcFirmwareVersion()
        {
            // 实际项目中可以：
            // 1. 从 PLC 读取
            // 2. 从 Config.json
            // 3. 从 MES 返回
            return "PLC-FW-2.18.5";
        }

        private string ReadGitRevision()
        {
            // 推荐方式：AssemblyMetadata
            var attr = Assembly.GetExecutingAssembly()
                               .GetCustomAttribute<AssemblyMetadataAttribute>();

            return attr?.Value ?? "N/A";
        }

        private DateTime GetBuildTime(Assembly assembly)
        {
            return File.GetLastWriteTime(assembly.Location);
        }
    }
}

