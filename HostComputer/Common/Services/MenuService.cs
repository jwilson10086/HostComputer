using System;                     // 引入系统基础命名空间，包含常用类型（如异常、时间等）
using System.Collections.Generic; // 引入泛型集合类（如 List<T>、Dictionary<TKey,TValue>）
using System.IO;                  // 引入文件和数据流操作相关的类（如 File、StreamReader 等）
using System.Linq;                // 引入 LINQ（语言集成查询），提供集合操作的扩展方法
using System.Text;                // 引入文本处理相关类（如 Encoding）
using System.Text.Json;           // 引入 JSON 序列化和反序列化相关的类（System.Text.Json）
using System.Threading.Tasks;     // 引入异步编程相关类（Task 等）
using HostComputer.Models;

namespace HostComputer.Common.Services   // 定义命名空间，用于组织代码，避免类名冲突
{
    /// <summary>
    /// 提供菜单加载服务，从 JSON 文件读取并转换为 MenuItemModel 列表
    /// </summary>
    public class MenuService
    {
        /// <summary>
        /// 从指定路径加载菜单数据，默认文件名为 menu.json
        /// </summary>
        /// <param name="path">菜单配置文件路径，默认为 "menu.json"</param>
        /// <returns>返回反序列化后的菜单项列表，如果文件不存在则返回空列表</returns>
        public static List<MenuItemModel> LoadMenu(string path = "menu.json")
        {
            if (!File.Exists(path))
                return new List<MenuItemModel>();

            var json = File.ReadAllText(path);

            var list = JsonSerializer.Deserialize<List<MenuItemModel>>(json)
                ?? new List<MenuItemModel>();

            // 初始化 Key：如果 Key 为空，就把 Title 赋给 Key
            void InitKey(IEnumerable<MenuItemModel> items)
            {
                foreach (var item in items)
                {
                    if (string.IsNullOrEmpty(item.Key) && !string.IsNullOrEmpty(item.Title))
                        item.Key = item.Title;

                    if (item.Children != null && item.Children.Count > 0)
                        InitKey(item.Children);
                }
            }

            InitKey(list);

            return list;
        }

    }
}
