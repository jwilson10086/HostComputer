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
        public string CommitMessage { get; }

        public SoftwareVersionViewModel()
        {
          
            var assembly =  Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();

            SoftwareName = assemblyName.Name ?? "Unknown";
            SoftwareVersion = assembly.GetName().Version?.ToString() ?? "N/A";
            PlcFirmwareVersion = "Melsoft FX5U Ver 1.10"; // 假设从某个配置或服务获取PLC固件版本
            GitRevision = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                                  .FirstOrDefault(a => a.Key == "GitRevision")?.Value ?? "N/A";
            App.Logger.Info("GitRevision: " + GitRevision);

            CommitMessage = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                                  .FirstOrDefault(a => a.Key == "GitCommitMessage")?.Value ?? "N/A";
            App.Logger.Info("CommitMessage: " + CommitMessage);
            BuildTime = File.GetLastWriteTime(assembly.Location)
                            .ToString("yyyy-MM-dd HH:mm:ss");
        }

    }
}

