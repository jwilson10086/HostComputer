using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Models.RicipeEditor
{
    public class ModuleSubRecipeModel
    {
        /// <summary>
        /// 时间/S
        /// </summary>
        public int Delay_Time { get; set; }


        /// <summary>
        /// 主轴转速R/min
        /// </summary>
        public int SpinSpeed { get; set; }

        /// <summary>
        /// 摆臂选择
        /// </summary>
        public int ARM_Select { get; set; }


        /// <summary>
        /// 摆臂速度选择
        /// </summary>
        public int ARM_SpeedSelect { get; set; }


        /// <summary>
        /// 清洗方式选择
        /// </summary>
        public int ARM_Pattern { get; set; }


        /// <summary>
        /// 喷嘴选择
        /// </summary>
        public int NozzleSelect { get; set; }


        /// <summary>
        /// 摆臂1中心位置°
        /// </summary>
        public int ARM1_Center { get; set; }


        /// <summary>
        /// 摆臂1边缘位置°
        /// </summary>
        public int ARM1_Edge { get; set; }


        /// <summary>
        /// 摆臂2中心位置°
        /// </summary>
        public int ARM2_Center { get; set; }

        /// <summary>
        /// 摆臂2边缘位置°
        /// </summary>
        public int ARM2_Edge { get; set; }

        /// <summary>
        /// 主轴加速度
        /// </summary>
        public int SpinAcc { get; set; }

        /// <summary>
        /// 主轴减速度
        /// </summary>
        public int SpinDec { get; set; }

        /// <summary>
        /// RinseData1
        /// </summary>
        public int RinseData1 { get; set; }

        /// <summary>
        /// RinseData2
        /// </summary>
        public int RinseData2 { get; set; }

        /// <summary>
        /// RinseData3
        /// </summary>
        public int RinseData3 { get; set; }

        /// <summary>
        /// RinseData4
        /// </summary>
        public int RinseData4 { get; set; }



        /// <summary>STEP 序号（1-based）</summary>
        public int StepIndex { get; set; }

        /// <summary>步骤名称，可选</summary>
        public string StepName { get; set; } = string.Empty;

        /// <summary>参数集合</summary>
        public ObservableCollection<RecipeParamRow> Parameters { get; set; } = new();
    }
}
