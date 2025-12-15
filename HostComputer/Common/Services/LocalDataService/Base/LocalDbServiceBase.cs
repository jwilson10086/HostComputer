using DbFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Common.Services.LocalDataService.Base
{
    public abstract class LocalDbServiceBase
    {
        protected readonly IDbHelper Db;
        protected LocalDbServiceBase()
        {
            string conn = StartupModules.AppConfiguration.Current.Database.ConnectionString;
            Db = DbHelperFactory.Create(DbType.SQLite,conn);
        }
    }
}
