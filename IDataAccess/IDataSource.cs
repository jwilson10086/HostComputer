using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDataAccess
{
    /// <summary>
    /// 数据源接口，统一在线/仿真读取方式
    /// </summary>
    public interface IDataSource
    {
        bool Connected { get; }

        Task<double> ReadAnalogAsync(string tag);
        Task<bool> ReadBoolAsync(string tag);

        Task WriteAnalogAsync(string tag, double value);
        Task WriteBoolAsync(string tag, bool value);
    }
}
