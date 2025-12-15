using IDataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Common.Services
{
    public class DataRouter
    {
        public IDataSource Current { get; private set; }

        public DataRouter()
        {
            SystemStateManager.Instance.OnStateChanged += state =>
            {
                switch (state)
                {
                    case SystemState.Online:
                        Current = new PlcDataSource();
                        break;
                    case SystemState.Simulation:
                        Current = new SimulationDataSource();
                        break;
                    default:
                        Current = null;
                        break;
                }
            };
        }
    }
}
