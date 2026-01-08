using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Models.RicipeEditor
{
    public class ToolRecipeModels
    {
        /// <summary>
        /// 配方名称
        /// </summary>
        public string RecipeName
        {
            get; set;
        }

        /// <summary>
        /// 配方编辑人员
        /// </summary>
        public string Editor
        {
            get;
            set;
        }

        /// <summary>
        /// 配方最新编辑日期
        /// </summary>
        public string Date
        {
            get;
            set;
        }


        /// <summary>
        /// 路径配方集合
        /// </summary>
        public List<PathRecipeModels> PathRecipes
        {
            get; set;
        } = new List<PathRecipeModels>();




        public string ToolId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;

        public ObservableCollection<ModuleRecipeModel> Modules { get; set; } = new();
    }
}
