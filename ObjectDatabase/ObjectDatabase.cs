using System;
using System.Collections.Generic;
using System.Data.OleDb;
using LogAdapter;

namespace ObjectDatabase
{
    /// <summary>
    /// データベースへのアクセス、テーブルの管理をします。
    /// </summary>
    public class ObjectDatabase : IDisposable
    {
        internal static ILogger _logger = new LogAdapter.LogAdapter().Create();

        private readonly Dictionary<string, IDataTable> _tables = new Dictionary<string, IDataTable>();

        private readonly OleDbConnection _connection;

        private readonly string _fetchQuery;

        /// <summary>
        /// Accessファイルを指定してデータベースを開始します。
        /// </summary>
        /// <param name="file">Accessファイル</param>
        /// <param name="version">プロバイダーのバージョン</param>
        public ObjectDatabase(string file, string version = "12.0", string fetchQuery = "select * from {0}",
            Action<ILogMessage> logCallback = null)
        {
            if (logCallback != null)
                AddLogEvent(logCallback);

            _fetchQuery = fetchQuery;

            _logger.Info($"Hello ObjectDatabase Version: {typeof(ObjectDatabase).Assembly.GetName().Version}");

            _connection = new OleDbConnection
            {
                ConnectionString = $"Provider=Microsoft.ACE.OLEDB.{version}; Data Source={file}"
            };
            _connection.Open();

            _logger.Info($"Connected OleDB!");
            _logger.Debug($"Used Provider > Microsoft.ACE.OLEDB.{version}; Data Source={file}");
        }

        /// <summary>
        /// テーブルの管理を開始します。
        /// </summary>
        /// <param name="dataTable">データテーブル</param>
        public void AddTable(IDataTable dataTable)
        {
            AddTable(dataTable.Name, dataTable);
        }

        /// <summary>
        /// テーブルの管理を開始します。
        /// </summary>
        /// <param name="name">テーブル名</param>
        /// <param name="dataTable">データテーブル</param>
        public void AddTable(string name, IDataTable dataTable)
        {
            _logger.Info($"Subscribe {name} Table");

            _tables[dataTable.Name] = dataTable;
            dataTable.FetchQuery = _fetchQuery;
            dataTable.Fetch(_connection);
        }

        /// <summary>
        /// データテーブルを取得します。
        /// </summary>
        /// <param name="name">テーブル名</param>
        /// <returns></returns>
        public IDataTable GetTable(string name)
        {
            return _tables[name];
        }

        public void AddLogEvent(Action<ILogMessage> callback)
        {
            _logger.AddCallback(callback);
        }

        public void RemoveLogEvent(Action<ILogMessage> callback)
        {
            _logger.RemoveCallback(callback);
        }

        public void Dispose()
        {
            _logger.Info("Goodbye ObjectDatabase!");
            _logger.Debug("Close Tables");

            foreach (KeyValuePair<string, IDataTable> dataTable in _tables)
            {
                dataTable.Value.Sync();
            }

            _connection.Close();
        }
    }

    internal static class ObjectDatabaseExtensions
    {
        internal static void QueryLog(this ILogger logger, string query)
        {
            logger.Log("Query", query);
        }

        internal static void OperationLog(this ILogger logger, string operation)
        {
            logger.Log("Operation", operation);
        }
    }
}