using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Models.RicipeEditor
{
    public class ModuleRecipeModel
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
        /// APT配方数据集合
        /// </summary>
        public List<ModuleSubRecipeModel> SubRecipes
        {
            get; set;
        } = new List<ModuleSubRecipeModel>();

        public string ModuleId { get; set; } = string.Empty;   // CH1 / PM1
        public string ModuleName { get; set; } = string.Empty;

        public ObservableCollection<RecipeModel> Recipes { get; set; } = new();

    }
}
