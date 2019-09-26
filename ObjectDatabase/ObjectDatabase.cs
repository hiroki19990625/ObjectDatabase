using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace ObjectDatabase
{
    /// <summary>
    /// データベースへのアクセス、テーブルの管理をします。
    /// </summary>
    public class ObjectDatabase : IDisposable
    {
        private readonly Dictionary<string, IDataTable> _tables = new Dictionary<string, IDataTable>();

        private readonly OleDbConnection _connection;

        /// <summary>
        /// Accessファイルを指定してデータベースを開始します。
        /// </summary>
        /// <param name="file">Accessファイル</param>
        /// <param name="version">プロバイダーのバージョン</param>
        public ObjectDatabase(string file, string version = "12.0")
        {
            _connection = new OleDbConnection
            {
                ConnectionString = $"Provider=Microsoft.ACE.OLEDB.{version}; Data Source={file}"
            };
            _connection.Open();
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
            _tables[dataTable.Name] = dataTable;
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

        public void Dispose()
        {
            foreach (KeyValuePair<string, IDataTable> dataTable in _tables)
            {
                dataTable.Value.Sync();
            }

            _connection.Close();
        }
    }
}