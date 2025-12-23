using System.IO;
using System.Text.Json;
using HostComputer.Models;

namespace HostComputer.Common.Base
{
    public static class JsonFileHelper
    {
        /// <summary>
        /// 保存对象到 JSON 文件
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="filePath">完整文件路径</param>
        /// <param name="data">要保存的对象</param>
        /// <param name="overwrite">是否覆盖已有文件，默认 true</param>
        /// <returns>保存成功返回 true，否则 false</returns>
        public static bool SaveToFile<T>(string filePath, T data, bool overwrite = true)
        {
            try
            {
                string? dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!overwrite && File.Exists(filePath))
                    return false;

                var options = new JsonSerializerOptions { WriteIndented = true };

                string json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从 JSON 文件读取对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="filePath">完整文件路径</param>
        /// <returns>读取成功返回对象，失败返回默认(T)</returns>
        public static T? LoadFromFile<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return default;

                string json = File.ReadAllText(filePath);
                T? obj = JsonSerializer.Deserialize<T>(json);
                return obj;
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// 判断 JSON 文件是否存在
        /// </summary>
        public static bool Exists(string filePath)
        {
            return File.Exists(filePath);
        }

        /// <summary>
        /// 删除 JSON 文件
        /// </summary>
        public static bool Delete(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
