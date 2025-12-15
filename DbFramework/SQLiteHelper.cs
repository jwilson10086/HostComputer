using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DbFramework
{
    /// <summary>
    /// SQLite 数据库访问实现
    /// 特点：
    /// 1. 短连接（每次操作独立 Connection）
    /// 2. 写操作串行（SemaphoreSlim）
    /// 3. 事务仅存在于方法内部
    /// 4. 适合 WPF / async / 多线程环境
    /// </summary>
    public class SQLiteHelper : IDbHelper
    {
        private readonly string _connectionString;

        /// <summary>
        /// SQLite 写操作全局锁（防止并发写导致 database is locked）
        /// </summary>
        private static readonly SemaphoreSlim _writeLock = new(1, 1);

        public SQLiteHelper(string connectionString)
        {
            _connectionString = connectionString;
            DbHelperFactory.Logger?.Info("NEW SQLiteHelper instance created");
        }

        #region 连接控制（接口要求，SQLite 实际不使用长连接）

        /// <summary>
        /// SQLite 采用短连接模式，此方法仅为接口兼容
        /// </summary>
        public Task OpenConnectionAsync() => Task.CompletedTask;

        /// <summary>
        /// SQLite 采用短连接模式，此方法仅为接口兼容
        /// </summary>
        public void CloseConnection() { }

        #endregion

        #region 私有辅助方法

        private static void AddParameters(SqliteCommand cmd, Dictionary<string, object> parameters)
        {
            if (parameters == null) return;

            foreach (var kv in parameters)
            {
                cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
            }
        }

        private static void Log(string sql)
        {
            DbHelperFactory.Logger?.Info(sql);
        }

        private SqliteConnection CreateConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            return conn;
        }

        #endregion

        // ===========================================================
        #region 基础操作（ExecuteNonQuery / Scalar / Query）
        // ===========================================================

        /// <summary>
        /// 执行 INSERT / UPDATE / DELETE
        /// 写操作：串行执行
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(
            string sQL,
            Dictionary<string, object> parameters = null)
        {
            await _writeLock.WaitAsync();
            try
            {
                Log(sQL);

                using var conn = CreateConnection();
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = sQL;
                AddParameters(cmd, parameters);

                return await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                DbHelperFactory.Logger?.Error($"执行SQL失败: {sQL}", ex);
                throw;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        /// <summary>
        /// 执行返回单值的查询
        /// </summary>
        public async Task<object> ExecuteScalarAsync(
            string sQL,
            Dictionary<string, object> parameters = null)
        {
            Log(sQL);

            using var conn = CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sQL;
            AddParameters(cmd, parameters);

            return await cmd.ExecuteScalarAsync();
        }

        /// <summary>
        /// 执行查询，返回 DataTable
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(
            string sQL,
            Dictionary<string, object> parameters = null)
        {
            Log(sQL);

            using var conn = CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sQL;
            AddParameters(cmd, parameters);

            using var reader = await cmd.ExecuteReaderAsync();

            var dt = new DataTable();
            dt.Load(reader);
            return dt;
        }

        #endregion

        // ===========================================================
        #region 泛型查询（QuerySingle / QueryList）
        // ===========================================================

        public async Task<T> QuerySingleAsync<T>(
            string sQL,
            Dictionary<string, object> parameters = null) where T : new()
        {
            var dt = await ExecuteQueryAsync(sQL, parameters);
            if (dt.Rows.Count == 0) return default;

            return MapDataRow<T>(dt.Rows[0]);
        }

        public async Task<List<T>> QueryListAsync<T>(
            string sQL,
            Dictionary<string, object> parameters = null) where T : new()
        {
            var dt = await ExecuteQueryAsync(sQL, parameters);
            var list = new List<T>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(MapDataRow<T>(row));
            }

            return list;
        }

        private static T MapDataRow<T>(DataRow row) where T : new()
        {
            var obj = new T();
            var props = typeof(T).GetProperties();

            foreach (var prop in props)
            {
                if (!prop.CanWrite) continue;

                if (row.Table.Columns.Contains(prop.Name) &&
                    row[prop.Name] != DBNull.Value)
                {
                    prop.SetValue(obj,
                        Convert.ChangeType(row[prop.Name], prop.PropertyType));
                }
            }

            return obj;
        }

        #endregion

        // ===========================================================
        #region 插入 / 更新 / 删除
        // ===========================================================

        public async Task<int> InsertAsync(
            string tableName,
            Dictionary<string, object> data)
        {
            var keys = string.Join(",", data.Keys);
            var values = string.Join(",", data.Keys.Select(k => "@" + k));
            var sql = $"INSERT INTO {tableName} ({keys}) VALUES ({values})";

            return await ExecuteNonQueryAsync(sql, data);
        }

        public async Task<int> UpdateAsync(
            string tableName,
            Dictionary<string, object> data,
            string whereClause,
            Dictionary<string, object> whereParams = null)
        {
            var setSql = string.Join(",", data.Keys.Select(k => $"{k}=@{k}"));
            var sql = $"UPDATE {tableName} SET {setSql} WHERE {whereClause}";

            var parameters = new Dictionary<string, object>(data);
            if (whereParams != null)
            {
                foreach (var kv in whereParams)
                    parameters[kv.Key] = kv.Value;
            }

            return await ExecuteNonQueryAsync(sql, parameters);
        }

        public async Task<int> DeleteAsync(
            string tableName,
            string whereClause,
            Dictionary<string, object> whereParams = null)
        {
            var sql = $"DELETE FROM {tableName} WHERE {whereClause}";
            return await ExecuteNonQueryAsync(sql, whereParams);
        }

        #endregion

        // ===========================================================
        #region 批量操作
        // ===========================================================

        /// <summary>
        /// 批量插入（单事务 + 写锁）
        /// </summary>
        public async Task<int> BulkInsertAsync(
            string tableName,
            List<Dictionary<string, object>> dataList)
        {
            await _writeLock.WaitAsync();
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();

                using var tx = conn.BeginTransaction();

                int total = 0;
                foreach (var data in dataList)
                {
                    var keys = string.Join(",", data.Keys);
                    var values = string.Join(",", data.Keys.Select(k => "@" + k));
                    var sql = $"INSERT INTO {tableName} ({keys}) VALUES ({values})";

                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = sql;
                    AddParameters(cmd, data);

                    total += await cmd.ExecuteNonQueryAsync();
                }

                tx.Commit();
                return total;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        #endregion

        // ===========================================================
        #region 事务操作（接口兼容，不建议外部使用）
        // ===========================================================

        /// <summary>
        /// SQLiteHelper 不支持跨方法事务（保留接口兼容）
        /// </summary>
        public Task BeginTransactionAsync() => Task.CompletedTask;

        public Task CommitAsync() => Task.CompletedTask;

        public Task RollbackAsync() => Task.CompletedTask;

        #endregion

        public void Dispose()
        {
            // 短连接模式，无需释放资源
        }
    }
}
