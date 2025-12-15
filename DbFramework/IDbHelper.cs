using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DbFramework
{
    /// <summary>
    /// 数据库访问通用接口，提供异步的增删改查、事务和批量操作能力。
    /// 支持 SQL Server / SQLite / MySQL 等多种数据库的统一访问。
    /// </summary>
    public interface IDbHelper : IDisposable
    {
        /// <summary>
        /// 打开数据库连接（异步）
        /// </summary>
        Task OpenConnectionAsync();

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        void CloseConnection();

        // ===========================================================
        #region 基础操作（ExecuteNonQuery / Scalar / Query）
        // ===========================================================

        /// <summary>
        /// 执行 INSERT / UPDATE / DELETE 等带参数的非查询 SQL
        /// </summary>
        /// <param name="sQL">SQL 语句</param>
        /// <param name="parameters">可选参数字典（键：参数名，值：参数值）</param>
        /// <returns>受影响的行数</returns>
        Task<int> ExecuteNonQueryAsync(string sQL, Dictionary<string, object> parameters = null);

        /// <summary>
        /// 执行查询单个值的 SQL，例如 SELECT COUNT(*) 或 SELECT MAX(ID)
        /// </summary>
        /// <param name="sQL">SQL 语句</param>
        /// <param name="parameters">可选参数字典</param>
        /// <returns>查询的结果对象（单值）</returns>
        Task<object> ExecuteScalarAsync(string sQL, Dictionary<string, object> parameters = null);

        /// <summary>
        /// 执行查询 SQL，返回 DataTable
        /// </summary>
        /// <param name="sQL">SQL 语句</param>
        /// <param name="parameters">可选参数字典</param>
        /// <returns>查询结果的 DataTable</returns>
        Task<DataTable> ExecuteQueryAsync(string sQL, Dictionary<string, object> parameters = null);

        #endregion

        // ===========================================================
        #region 泛型查询（QuerySingle / QueryList）
        // ===========================================================

        /// <summary>
        /// 查询一个对象（1 行数据），并自动映射到泛型实体 T 的属性
        /// </summary>
        /// <typeparam name="T">目标实体类（必须有无参构造）</typeparam>
        /// <param name="sQL">SQL 语句</param>
        /// <param name="parameters">参数字典</param>
        /// <returns>映射后的实体对象，如果无数据返回默认值 null</returns>
        Task<T> QuerySingleAsync<T>(string sQL, Dictionary<string, object> parameters = null) where T : new();

        /// <summary>
        /// 查询多个对象（多行数据），自动映射到泛型实体 T 的列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="sQL">SQL 语句</param>
        /// <param name="parameters">参数字典</param>
        /// <returns>实体列表，可为空列表</returns>
        Task<List<T>> QueryListAsync<T>(string sQL, Dictionary<string, object> parameters = null) where T : new();

        #endregion

        // ===========================================================
        #region 插入 / 更新 / 删除
        // ===========================================================

        /// <summary>
        /// 动态插入数据（自动构建 INSERT 语句）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="data">键值对（键：列名，值：列值）</param>
        /// <returns>受影响的行数</returns>
        Task<int> InsertAsync(string tableName, Dictionary<string, object> data);

        /// <summary>
        /// 动态更新数据（自动构建 UPDATE 语句）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="data">更新数据（列名 → 值）</param>
        /// <param name="whereClause">WHERE 条件（不带 WHERE 关键字）</param>
        /// <param name="whereParams">条件参数</param>
        /// <returns>受影响的行数</returns>
        Task<int> UpdateAsync(string tableName, Dictionary<string, object> data, string whereClause, Dictionary<string, object> whereParams = null);

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="whereClause">条件（不带 WHERE）</param>
        /// <param name="whereParams">参数</param>
        /// <returns>受影响的行数</returns>
        Task<int> DeleteAsync(string tableName, string whereClause, Dictionary<string, object> whereParams = null);

        #endregion

        // ===========================================================
        #region 批量操作
        // ===========================================================

        /// <summary>
        /// 批量插入数据（适用于大量数据导入）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="dataList">多行数据列表，每条记录是 Dictionary&lt;列名, 值&gt;</param>
        /// <returns>总影响行数</returns>
        Task<int> BulkInsertAsync(string tableName, List<Dictionary<string, object>> dataList);

        #endregion

        // ===========================================================
        #region 事务操作
        // ===========================================================

        /// <summary>
        /// 开始事务
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// 提交事务
        /// </summary>
        Task CommitAsync();

        /// <summary>
        /// 回滚事务
        /// </summary>
        Task RollbackAsync();

        #endregion
    }
}
