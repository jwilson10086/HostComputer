using IDataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Common.Services
{
    public class PlcDataSource : IDataSource
    {
        public bool Connected { get; private set; } = true; // 模拟在线状态

        public Task<double> ReadAnalogAsync(string tag)
        {
            // TODO: 实际 PLC 读取逻辑
            return Task.FromResult(0.0);
        }

        public Task<bool> ReadBoolAsync(string tag)
        {
            // TODO: 实际 PLC 读取逻辑
            return Task.FromResult(true);
        }

        public Task WriteAnalogAsync(string tag, double value)
        {
            // TODO: 实际 PLC 写入逻辑
            return Task.CompletedTask;
        }

        public Task WriteBoolAsync(string tag, bool value)
        {
            // TODO: 实际 PLC 写入逻辑
            return Task.CompletedTask;
        }
    }
}
