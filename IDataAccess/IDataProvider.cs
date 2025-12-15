using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDataAccess
{
    public interface IDataProvider
    {
        bool IsConnected { get; }
        Task<bool> ConnectAsync();
        void Disconnect();
        // 基本读写抽象（键值）
        Task<double> ReadAnalogAsync(string tag);
        Task<bool> ReadBoolAsync(string tag);
        Task WriteAnalogAsync(string tag, double value);
        Task WriteBoolAsync(string tag, bool value);
    }

    public class TickEventArgs : EventArgs { public double Delta { get; set; } }
}
