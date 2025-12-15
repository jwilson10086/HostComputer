using DbFramework; // 引入自定义数据库访问框架（假设 DbFramework 是你们的封装库）
using HostComputer.Common.Services.StartupModules;
using HostComputer.Models;
using System.Collections.Generic; // 提供 Dictionary 等泛型集合类
using System.Configuration; // 提供读取配置文件（App.config/Web.config）中的连接字符串的功能
using System.Threading.Tasks; // 提供异步编程所需的 Task 支持

namespace HostComputer.Common.Services
{
    /// <summary>
    /// 本地数据库访问类
    /// 主要用于执行与数据库相关的操作（例如登录验证）
    /// </summary>
    public class LocalDataAccess
    {
        // 定义数据库操作帮助对象，具体实现由 DbFramework 框架提供
        private readonly IDbHelper _db;

        /// <summary>
        /// 构造函数
        /// 初始化数据库连接（读取配置文件中的连接字符串，并创建对应数据库类型的帮助对象）
        /// </summary>
        public LocalDataAccess()
        {
           
            string connectionString = AppConfiguration.Current.Database.ConnectionString;

            // 通过 DbHelperFactory 工厂方法创建一个数据库帮助对象
            // 参数1: 数据库类型
            // 参数2: 连接字符串
            _db = DbHelperFactory.Create(DbType.SQLite, connectionString);
        }

        /// <summary>
        /// 异步检查用户名和密码是否存在于 Users 表中
        /// </summary>
        /// <param name="username">用户输入的用户名</param>
        /// <param name="password">用户输入的密码</param>
        /// <returns>如果用户名和密码匹配，返回 true；否则返回 false</returns>
        public async Task<bool> CheckLoginAsync(string username, string password)
        {
            // SQL 语句：统计满足用户名和密码匹配的记录数
            // 使用参数化查询，避免 SQL 注入攻击
            string sql = "SELECT COUNT(1) FROM Users WHERE UserName=@UserName AND Password=@Password";

            // 构造参数集合
            var parameters = new Dictionary<string, object>
            {
                { "UserName", username }, // 参数 @UserName 的值
                { "Password", password }  // 参数 @Password 的值
            };

            // 执行 SQL，返回结果（ExecuteScalarAsync 用于执行返回单一值的查询，例如 COUNT）
            var result = await _db.ExecuteScalarAsync(sql, parameters);

            // 将结果转换为整数，如果大于 0 说明有匹配的用户记录
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// 异步记录用户名和密码到 Users 表中
        /// </summary>
        /// <param name="username">用户输入的用户名</param>
        /// <param name="password">用户输入的密码</param>
        /// <returns>如果插入成功，返回 true；否则返回 false</returns>
        public async Task<bool> InsertUserAsync(string username, string password)
        {
            // SQL 语句：插入一条记录到 UserRemeber 表中
            string sql = "INSERT INTO UserRemeber (UserName, Password) VALUES (@UserName, @Password)";

            // 构造参数集合
            var parameters = new Dictionary<string, object>
            {
                { "UserName", username }, // 参数 @UserName 的值
                { "Password", password }  // 参数 @Password 的值
            };

            // 执行 SQL，返回受影响的行数（ExecuteNonQueryAsync 用于执行非查询语句，例如 INSERT）
            var result = await _db.ExecuteNonQueryAsync(sql, parameters);

            // 如果受影响的行数大于 0 说明插入成功
            return result > 0;
        }

        /// <summary>
        /// 异步删除UserRemeber表中的所有记录
        /// </summary>
        /// <returns>如果删除成功，返回 true；否则返回 false</returns>
        public async Task<bool> DeleteAllUsersAsync()
        {
            // SQL 语句：删除 UserRemeber 表中的所有记录
            string sql = "DELETE FROM UserRemeber";

            // 执行 SQL，返回受影响的行数
            var result = await _db.ExecuteNonQueryAsync(sql);

            // 如果受影响的行数大于 0 说明删除成功
            return result > 0;
        }

        /// <summary>
        /// 异步获取UserRemeber表中最后一条记录
        /// 如果表中没有记录，则返回默认的 admin 用户
        /// </summary>
        /// <returns>最后一条记录的 UserModel 对象</returns>
        public async Task<UserModel> GetLastUserAsync()
        {
            string sql = "SELECT UserName, Password FROM UserRemeber ORDER BY UserName DESC LIMIT 1";

            var user = await _db.QuerySingleAsync<UserModel>(sql);

            if (user == null)
            {
                return new UserModel
                {
                    UserName = "admin",
                    Password = ""
                };
            }

            return user;
        }


    }
}
