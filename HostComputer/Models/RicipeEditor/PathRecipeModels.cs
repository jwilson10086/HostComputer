using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Models.RicipeEditor
{
    public class PathRecipeModels
    {


        /// <summary>
        /// 路径序号
        /// </summary>
        public int PathNO
        {
            get; set;
        }



        /// <summary>
        /// 配方站点
        /// </summary>
        public string RecipeStation
        {
            get; set;
        }


        /// <summary>
        /// 子配方名称
        /// </summary>
        public string RecipeName
        {
            get; set;
        }



        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remark
        {
            get; set;
        }

    }
}
