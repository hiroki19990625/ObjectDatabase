using System.Collections.Generic;
using System.Data.OleDb;

namespace ObjectDatabase
{
    public class ObjectDatabase
    {
        private readonly Dictionary<string, IDataTable> tables = new Dictionary<string, IDataTable>();

        private readonly OleDbConnection _connection;

        public ObjectDatabase(string file)
        {
            _connection = new OleDbConnection
            {
                ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0; Data Source=" + file
            };
            _connection.Open();
        }

        public void AddTable(IDataTable dataTable)
        {
            AddTable(dataTable.Name, dataTable);
        }

        public void AddTable(string name, IDataTable dataTable)
        {
            tables[dataTable.Name] = dataTable;
            dataTable.Fetch(_connection);
        }

        public IDataTable GetTable(string name)
        {
            return tables[name];
        }
    }
}