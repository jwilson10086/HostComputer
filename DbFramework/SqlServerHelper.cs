using Microsoft.Data.SqlClient;
using MyLogger;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DbFramework
{
    public class SqlServerHelper : IDbHelper
    {
        private SqlConnection _connection;
        private SqlTransaction _transaction;
        private readonly string _connectionString;
      

        public SqlServerHelper(string connectionString)
        {
            _connectionString = connectionString;
            _connection = new SqlConnection(_connectionString);
            try
            {
                _connection.Open();
                Log("数据库连接成功");
            }
            catch (Exception ex)
            {
                Log($"数据库连接失败: {ex.Message}");
                throw; // 如果需要继续抛出异常可以保留
            }
        }
        public async Task OpenConnectionAsync()
        {
            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();
        }

        public void CloseConnection() => _connection.Close();

        private void Log(string sql) => DbHelperFactory.Logger?.Info(sql);

        private void AddParameters(SqlCommand cmd, Dictionary<string, object> parameters)
        {
            if (parameters == null) return;
            foreach (var kv in parameters)
                cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
        }

        #region 基础操作
        public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters = null)
        {
            Log(sql);
            using var cmd = new SqlCommand(sql, _connection, _transaction);
            AddParameters(cmd, parameters);
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters = null)
        {
            Log(sql);
            using var cmd = new SqlCommand(sql, _connection, _transaction);
            AddParameters(cmd, parameters);
            return await cmd.ExecuteScalarAsync();
        }

        public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters = null)
        {
            Log(sql);
            using var cmd = new SqlCommand(sql, _connection, _transaction);
            AddParameters(cmd, parameters);
            var dt = new DataTable();
            using var adapter = new SqlDataAdapter(cmd);
            await Task.Run(() => adapter.Fill(dt));
            return dt;
        }
        #endregion

        #region 泛型查询
        public async Task<T> QuerySingleAsync<T>(string sql, Dictionary<string, object> parameters = null) where T : new()
        {
            var dt = await ExecuteQueryAsync(sql, parameters);
            if (dt.Rows.Count == 0) return default;
            return MapDataRow<T>(dt.Rows[0]);
        }

        public async Task<List<T>> QueryListAsync<T>(string sql, Dictionary<string, object> parameters = null) where T : new()
        {
            var dt = await ExecuteQueryAsync(sql, parameters);
            var list = new List<T>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapDataRow<T>(row));
            return list;
        }

        private T MapDataRow<T>(DataRow row) where T : new()
        {
            var obj = new T();
            foreach (var prop in typeof(T).GetProperties())
            {
                if (row.Table.Columns.Contains(prop.Name) && row[prop.Name] != DBNull.Value)
                    prop.SetValue(obj, Convert.ChangeType(row[prop.Name], prop.PropertyType));
            }
            return obj;
        }
        #endregion

        #region 插入/更新/删除
        public async Task<int> InsertAsync(string tableName, Dictionary<string, object> data)
        {
            var keys = string.Join(",", data.Keys);
            var values = string.Join(",", data.Keys.Select(k => "@" + k));
            string sql = $"INSERT INTO {tableName} ({keys}) VALUES ({values})";
            return await ExecuteNonQueryAsync(sql, data);
        }

        public async Task<int> UpdateAsync(string tableName, Dictionary<string, object> data, string whereClause, Dictionary<string, object> whereParams = null)
        {
            var setStr = string.Join(",", data.Keys.Select(k => $"{k}=@{k}"));
            string sql = $"UPDATE {tableName} SET {setStr} WHERE {whereClause}";
            var parameters = new Dictionary<string, object>(data);
            if (whereParams != null)
                foreach (var kv in whereParams) parameters[kv.Key] = kv.Value;
            return await ExecuteNonQueryAsync(sql, parameters);
        }

        public async Task<int> DeleteAsync(string tableName, string whereClause, Dictionary<string, object> whereParams = null)
        {
            string sql = $"DELETE FROM {tableName} WHERE {whereClause}";
            return await ExecuteNonQueryAsync(sql, whereParams);
        }
        #endregion

        #region 批量操作
        public async Task<int> BulkInsertAsync(string tableName, List<Dictionary<string, object>> dataList)
        {
            int total = 0;
            await BeginTransactionAsync();
            try
            {
                foreach (var data in dataList)
                    total += await InsertAsync(tableName, data);
                await CommitAsync();
            }
            catch
            {
                await RollbackAsync();
                throw;
            }
            return total;
        }
        #endregion

        #region 事务操作
        public Task BeginTransactionAsync()
        {
            _transaction = _connection.BeginTransaction();
            return Task.CompletedTask;
        }

        public Task CommitAsync()
        {
            _transaction?.Commit();
            _transaction = null;
            return Task.CompletedTask;
        }

        public Task RollbackAsync()
        {
            _transaction?.Rollback();
            _transaction = null;
            return Task.CompletedTask;
        }
        #endregion

        public void Dispose()
        {
            _transaction?.Dispose();
            _connection?.Dispose();
        }
    }
}
