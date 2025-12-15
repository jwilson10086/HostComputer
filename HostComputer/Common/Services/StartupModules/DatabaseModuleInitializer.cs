using MyLogger;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace HostComputer.Common.Services.StartupModules
{
    public class DatabaseModuleInitializer : IModuleInitializer
    {
        public string ModuleName => "数据库服务";
        public string ModuleType => "Database";
        private string connectionString = AppConfiguration.Current.Database.ConnectionString;
        public InitializerPriority Priority => InitializerPriority.Core;
        public int Order => 2;
        public List<ModuleDependency> Dependencies => new()
        {
            new ModuleDependency { ModuleName = "配置服务", ModuleType = "Config" }
        };

        public async Task<bool> InitializeAsync(Logger logger)
        {
            logger.Database("开始初始化数据库服务...");

            try
            {
                // 1. 检查数据库文件
                string dbPath = AppConfiguration.Current.Database.ConnectionString.Replace("Data Source=", ""); ;
                await CheckDatabaseFileAsync(dbPath, logger);

                // 2. 测试数据库连接
                bool connectionOk = await TestDatabaseConnectionAsync(logger);
                if (!connectionOk)
                {
                    logger.Error("数据库连接测试失败");
                    return false;
                }

                // 3. 检查/创建数据表
                await CheckAndCreateTablesAsync(logger);

                // 4. 检查数据完整性
                await CheckDataIntegrityAsync(logger);

                logger.Database("✅ 数据库服务初始化成功");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"❌ 数据库初始化失败: {ex.Message}");
                logger.Debug($"详细错误: {ex}");
                return false;
            }
        }

        private async Task CheckDatabaseFileAsync(string dbPath, Logger logger)
        {
            string directory = Path.GetDirectoryName(dbPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                logger.Database($"创建数据库目录: {directory}");
            }

            if (!File.Exists(dbPath))
            {
                logger.Database($"数据库文件不存在，将创建新数据库: {dbPath}");
                // SQLite会在首次连接时自动创建文件
            }
            else
            {
                var fileInfo = new FileInfo(dbPath);
                logger.Database($"数据库文件已存在，大小: {fileInfo.Length / 1024}KB");
            }
        }

        private async Task<bool> TestDatabaseConnectionAsync(Logger logger)
        {
            try
            {
                logger.Database("测试数据库连接...");

               
                using (var connection = new SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // 测试简单查询
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT 1;";
                        var result = await command.ExecuteScalarAsync();

                        if (result?.ToString() == "1")
                        {
                            logger.Database("✅ 数据库连接测试通过");
                            return true;
                        }
                    }

                    await connection.CloseAsync();
                }

                return false;
            }
            catch (SqliteException sqlEx)
            {
                logger.Error($"SQLite连接错误: {sqlEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                logger.Error($"数据库连接异常: {ex.Message}");
                return false;
            }
        }

        private async Task CheckAndCreateTablesAsync(Logger logger)
        {
            try
            {
                logger.Database("检查数据库表结构...");

               

                using (var connection = new SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // 检查 Users 表
                    if (!await TableExistsAsync(connection, "Users"))
                    {
                        logger.Database("创建 Users 表...");
                        await CreateUsersTableAsync(connection);
                    }

                    // 检查 UserRemeber 表
                    if (!await TableExistsAsync(connection, "UserRemeber"))
                    {
                        logger.Database("创建 UserRemeber 表...");
                        await CreateUserRememberTableAsync(connection);
                    }

                    // 检查其他必要的业务表
                    var requiredTables = new[] { "Equipment", "Recipes", "Parameters", "History" };
                    foreach (var table in requiredTables)
                    {
                        if (!await TableExistsAsync(connection, table))
                        {
                            logger.Database($"表 {table} 不存在，将在需要时创建");
                        }
                        else
                        {
                            logger.Database($"✅ 表 {table} 存在");
                        }
                    }

                    await connection.CloseAsync();
                }

                logger.Database("数据库表结构检查完成");
            }
            catch (Exception ex)
            {
                logger.Error($"检查表结构时出错: {ex.Message}");
                throw;
            }
        }

        private async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT COUNT(*) FROM sqlite_master 
                    WHERE type='table' AND name=@tableName";
                command.Parameters.AddWithValue("@tableName", tableName);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
        }

        private async Task CreateUsersTableAsync(SqliteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserName TEXT NOT NULL UNIQUE,
                        Password TEXT NOT NULL,
                        DisplayName TEXT,
                        Role TEXT DEFAULT 'User',
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        LastLogin DATETIME,
                        IsActive BOOLEAN DEFAULT 1
                    );
                    
                    CREATE INDEX idx_users_username ON Users(UserName);
                    CREATE INDEX idx_users_role ON Users(Role);";

                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task CreateUserRememberTableAsync(SqliteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE UserRemeber (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserName TEXT NOT NULL,
                        Password TEXT NOT NULL,
                        RememberTime DATETIME DEFAULT CURRENT_TIMESTAMP
                    );
                    
                    CREATE INDEX idx_userremember_username ON UserRemeber(UserName);";

                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task CheckDataIntegrityAsync(Logger logger)
        {
            try
            {
                logger.Database("检查数据完整性...");

                

                using (var connection = new SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // 检查是否有默认管理员账户
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM Users WHERE Role = 'Admin'";
                        var adminCount = Convert.ToInt32(await command.ExecuteScalarAsync());

                        if (adminCount == 0)
                        {
                            logger.Warning("没有找到管理员账户，将创建默认管理员");
                            await CreateDefaultAdminAsync(connection);
                        }
                        else
                        {
                            logger.Database($"找到 {adminCount} 个管理员账户");
                        }
                    }

                    // 检查 UserRemeber 表中的数据
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM UserRemeber";
                        var rememberCount = Convert.ToInt32(await command.ExecuteScalarAsync());
                        logger.Database($"UserRemeber 表中有 {rememberCount} 条记录");
                    }

                    // 执行 PRAGMA 命令检查数据库完整性
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA integrity_check;";
                        var integrityResult = await command.ExecuteScalarAsync() as string;

                        if (integrityResult == "ok")
                        {
                            logger.Database("✅ 数据库完整性检查通过");
                        }
                        else
                        {
                            logger.Error($"数据库完整性检查失败: {integrityResult}");
                        }
                    }

                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"检查数据完整性时出错: {ex.Message}");
            }
        }

        private async Task CreateDefaultAdminAsync(SqliteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO Users (UserName, Password, DisplayName, Role, IsActive)
                    VALUES (@userName, @password, @displayName, @role, 1)";

                command.Parameters.AddWithValue("@userName", "admin");
                command.Parameters.AddWithValue("@password", "admin123"); // 注意：实际应用中应该加密
                command.Parameters.AddWithValue("@displayName", "系统管理员");
                command.Parameters.AddWithValue("@role", "Admin");

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}