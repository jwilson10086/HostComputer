using System.Data;
using Microsoft.Data.Sqlite;
namespace DbFramework
{
    public class SQLiteHelper : IDbHelper
    {
        private SqliteConnection _connection;
        private SqliteTransaction _transaction;
        private readonly string _connectionString;
       

        public SQLiteHelper(string connectionString)
        {
            _connectionString = connectionString;
            _connection = new SqliteConnection(_connectionString);
           
        }

        public async Task OpenConnectionAsync()
        {
            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();
        }

        public void CloseConnection() => _connection.Close();

        private void Log(string sQL) => DbHelperFactory.Logger?.Info(sQL);

        private void AddParameters(SqliteCommand cmd, Dictionary<string, object> parameters)
        {
            if (parameters == null) return;
            foreach (var kv in parameters)
            {
                var param = cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
                // 可以根据 value 的类型，显式设置 SqliteType，例如：
                // if (kv.Value is int) param.SqliteType = SqliteType.Integer;
                // else if (kv.Value is string) param.SqliteType = SqliteType.Text;
                // 这对于批量插入等场景有优化作用。
            }
        }

        #region 基础操作
        public async Task<int> ExecuteNonQueryAsync(string sQL, Dictionary<string, object> parameters = null)
        {
            // 确保连接已打开
            await OpenConnectionAsync();
            Log(sQL);
            using var cmd = new SqliteCommand(sQL, _connection, _transaction);
            AddParameters(cmd, parameters);
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<object> ExecuteScalarAsync(string sQL, Dictionary<string, object> parameters = null)
        {
            // 确保连接已打开
            await OpenConnectionAsync();
            Log(sQL);
            using var cmd = new SqliteCommand(sQL, _connection, _transaction);
            AddParameters(cmd, parameters);
            return await cmd.ExecuteScalarAsync();
        }

        public async Task<DataTable> ExecuteQueryAsync(string sQL, Dictionary<string, object> parameters = null)
        {
           
            // 详细记录SQL和参数
            Log($"准备执行SQL: {sQL}");
            if (parameters != null)
            {
                Log($"参数: {string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"))}");
            }
            await OpenConnectionAsync();
            using var cmd = new SqliteCommand(sQL, _connection, _transaction);
            AddParameters(cmd, parameters);
            var dt = new DataTable();
            // 使用 SqliteDataReader 读取数据并填充 DataTable
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                // 创建 DataTable 的列
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var columnType = reader.GetFieldType(i) ?? typeof(string);
                    dt.Columns.Add(columnName, columnType);
                }
                // 逐行读取数据
                while (await reader.ReadAsync())
                {
                    var row = dt.NewRow();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                    }
                    dt.Rows.Add(row);
                }
            }
            return dt;
        }
        #endregion

        #region 泛型查询
        public async Task<T> QuerySingleAsync<T>(string sQL, Dictionary<string, object> parameters = null) where T : new()
        {
            var dt = await ExecuteQueryAsync(sQL, parameters);
            if (dt.Rows.Count == 0) return default;
            return MapDataRow<T>(dt.Rows[0]);
        }

        public async Task<List<T>> QueryListAsync<T>(string sQL, Dictionary<string, object> parameters = null) where T : new()
        {
            Log(sQL);
            var list = new List<T>();
            using var cmd = new SqliteCommand(sQL, _connection, _transaction);
            AddParameters(cmd, parameters);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    T obj = new T();
                    // 使用反射或更高效的方式（如序列化库）将 reader 的当前行映射到 obj
                    // ... (映射逻辑，与之前 MapDataRow 类似，但直接从 reader 读取)
                    list.Add(obj);
                }
            }
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
            string sQL = $"INSERT INTO {tableName} ({keys}) VALUES ({values})";
            return await ExecuteNonQueryAsync(sQL, data);
        }

        public async Task<int> UpdateAsync(string tableName, Dictionary<string, object> data, string whereClause, Dictionary<string, object> whereParams = null)
        {
            var setStr = string.Join(",", data.Keys.Select(k => $"{k}=@{k}"));
            string sQL = $"UPDATE {tableName} SET {setStr} WHERE {whereClause}";
            var parameters = new Dictionary<string, object>(data);
            if (whereParams != null)
                foreach (var kv in whereParams) parameters[kv.Key] = kv.Value;
            return await ExecuteNonQueryAsync(sQL, parameters);
        }

        public async Task<int> DeleteAsync(string tableName, string whereClause, Dictionary<string, object> whereParams = null)
        {
            string sQL = $"DELETE FROM {tableName} WHERE {whereClause}";
            return await ExecuteNonQueryAsync(sQL, whereParams);
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
