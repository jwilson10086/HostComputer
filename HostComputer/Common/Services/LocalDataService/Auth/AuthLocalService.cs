using System.Collections.Generic;
using System.Threading.Tasks;
using HostComputer.Common.Services.LocalDataService.Base;
using HostComputer.Models;
using static HostComputer.ViewModels.LoginViewModel;

namespace HostComputer.Common.Services.Local.Auth
{
    public class AuthLocalService : LocalDbServiceBase
    {
        /// <summary>
        /// 登录校验（成功则返回完整 UserModel）
        /// </summary>
        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            string sql =
                @"SELECT UserName,Password,Level,[Group] FROM Users WHERE UserName = @UserName AND Password = @Password LIMIT 1";

            var parameters = new Dictionary<string, object>
            {
                ["UserName"] = username,
                ["Password"] = password
            };

            var user = await Db.QuerySingleAsync<UserModel>(sql, parameters);

            if (user == null)
            {
                return new LoginResult { Success = false, Message = "用户名或密码错误" };
            }

            return new LoginResult
            {
                Success = true,
                Message = "登录成功",
                User = user
            };
        }

        /// <summary>
        /// 注册新用户
        /// </summary>
        public async Task<bool> RegisterAsync(UserModel user)
        {
            string sql =
                @"
                INSERT INTO Users (UserName, Password, Level, [Group])
                VALUES (@UserName, @Password, @Level, @Group)
            ";
            var parameters = new Dictionary<string, object>
            {
                ["UserName"] = user.UserName,
                ["Password"] = user.Password,
                ["Level"] = user.Level,
                ["Group"] = user.Group
            };
            var result = await Db.ExecuteNonQueryAsync(sql, parameters);
            return result > 0;
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<bool> UpdateUserAsync(UserModel user)
        {
            string sql =
                @"
                UPDATE Users
                SET Password = @Password,
                    Level = @Level,
                    [Group] = @Group
                WHERE UserName = @UserName
            ";
            var parameters = new Dictionary<string, object>
            {
                ["UserName"] = user.UserName,
                ["Password"] = user.Password,
                ["Level"] = user.Level,
                ["Group"] = user.Group
            };
            var result = await Db.ExecuteNonQueryAsync(sql, parameters);
            return result > 0;
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public async Task<bool> DeleteUserAsync(string username)
        {
            string sql =
                @"
                DELETE FROM Users
                WHERE UserName = @UserName
            ";
            var parameters = new Dictionary<string, object> { ["UserName"] = username };
            var result = await Db.ExecuteNonQueryAsync(sql, parameters);
            return result > 0;
        }

        /// <summary>
        /// 获取所有用户
        /// </summary>
        /// <returns></returns>
        public async Task<List<UserModel>> GetAllUsersAsync()
        {
            string sql =
                @"
                SELECT 
                    UserName,
                    Password,
                    Level,
                    [Group]
                FROM Users
            ";
            var users = await Db.QueryListAsync<UserModel>(sql);
            return users;
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
            var result = await Db.ExecuteNonQueryAsync(sql);

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
            string sql =
                "SELECT UserName, Password FROM UserRemeber ORDER BY UserName DESC LIMIT 1";

            var user = await Db.QuerySingleAsync<UserModel>(sql);

            if (user == null)
            {
                return new UserModel
                {
                    UserName = "admin",
                    Password = "",
                    Level = 0,
                    Group = ""
                };
            }

            return user;
        }

        /// <summary>
        /// 保存“唯一一条”记住的用户（先清空再插入）
        /// </summary>
        public async Task SaveRememberUserAsync(UserModel user)
        {
            await Db.BeginTransactionAsync();
            try
            {
                await Db.ExecuteNonQueryAsync("DELETE FROM UserRemeber");

                string insertSql = """
                        INSERT INTO UserRemeber (UserName, Password, Level, `Group`)
                        VALUES (@UserName, @Password, @Level, @Group)
                    """;

                var parameters = new Dictionary<string, object>
                {
                    ["UserName"] = user.UserName,
                    ["Password"] = user.Password,
                    ["Level"] = user.Level,
                    ["Group"] = user.Group
                };

                await Db.ExecuteNonQueryAsync(insertSql, parameters);
                await Db.CommitAsync();
            }
            catch
            {
                await Db.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// 异步记录用户名和密码到 Users 表中
        /// </summary>
        /// <param name="username">用户输入的用户名</param>
        /// <param name="password">用户输入的密码</param>
        /// <returns>如果插入成功，返回 true；否则返回 false</returns>
        public async Task<bool> InsertUserAsync(
            string username,
            string password,
            string level = null,
            string group = null
        )
        {
            // SQL 语句：插入一条记录到 UserRemeber 表中
            string sql =
                "INSERT INTO UserRemeber (UserName, Password) VALUES (@UserName, @Password, @Level, @Group)";

            // 构造参数集合
            var parameters = new Dictionary<string, object>
            {
                { "UserName", username }, // 参数 @UserName 的值
                { "Password", password } // 参数 @Password 的值
            };

            // 执行 SQL，返回受影响的行数（ExecuteNonQueryAsync 用于执行非查询语句，例如 INSERT）
            var result = await Db.ExecuteNonQueryAsync(sql, parameters);

            // 如果受影响的行数大于 0 说明插入成功
            return result > 0;
        }
    }
}
