using HostComputer.Common.Services.LocalDataService.Base;
using HostComputer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Common.Services.LocalDataService.Alarm
{
    public class AlarmLocalService : LocalDbServiceBase
    {
        /// <summary>
        /// 查询所有报警记录
        /// </summary>
        /// <returns></returns>
        public async Task<List<AlarmModel>> QueryAllAlarmRecords()
        {
            string sql = "SELECT * FROM AlarmLog ORDER BY PostTime DESC";
            var alarms = await Db.QueryListAsync<AlarmModel>(sql);
            return alarms;
        }
    }
}
