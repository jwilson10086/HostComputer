using MyLogger;
using System;

namespace DbFramework
{
    public enum DbType
    {
        MySQL,
        SQLServer,
        SQLite
    }
    public static class DbHelperFactory
    {
        public static Logger Logger { get; private set; }
       

        public static IDbHelper Create(DbType dbType, string connectionString)
    {
            #region 初始化日志
            Logger = new Logger(new LoggerConfig
            {
                LogDirectory = "Logs",
                LogFileName = "app.log",
                EnableConsole = true,
                MinLogLevel = LogLevel.Debug
            });
            #endregion
            switch (dbType)
        {
            case DbType.MySQL:
                return new MySqlHelper(connectionString);
            case DbType.SQLServer:
                return new SqlServerHelper(connectionString);
            case DbType.SQLite:
                return new SQLiteHelper(connectionString);
            default:
                throw new NotSupportedException("Unsupported database type");
        }
    }
}
}
