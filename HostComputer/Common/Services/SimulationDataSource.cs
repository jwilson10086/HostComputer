using IDataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Common.Services
{
    public class SimulationDataSource : IDataSource
    {
        public bool Connected => true;

        private readonly Dictionary<string, double> _analogValues = new();
        private readonly Dictionary<string, bool> _boolValues = new();

        private readonly Random _rand = new();

        public Task<double> ReadAnalogAsync(string tag)
        {
            if (!_analogValues.ContainsKey(tag))
                _analogValues[tag] = _rand.NextDouble() * 100;

            // 随机波动模拟真实设备
            _analogValues[tag] += _rand.NextDouble() - 0.5;
            return Task.FromResult(_analogValues[tag]);
        }

        public Task<bool> ReadBoolAsync(string tag)
        {
            if (!_boolValues.ContainsKey(tag))
                _boolValues[tag] = _rand.Next(0, 2) == 1;

            // 10% 概率切换状态
            if (_rand.NextDouble() < 0.1)
                _boolValues[tag] = !_boolValues[tag];

            return Task.FromResult(_boolValues[tag]);
        }

        public Task WriteAnalogAsync(string tag, double value)
        {
            _analogValues[tag] = value;
            return Task.CompletedTask;
        }

        public Task WriteBoolAsync(string tag, bool value)
        {
            _boolValues[tag] = value;
            return Task.CompletedTask;
        }
    }
}
