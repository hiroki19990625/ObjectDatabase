using System.Collections.Generic;
using System.Data.OleDb;

namespace ObjectDatabase
{
    public class DataTable<T> : IDataTable where T : IDataModel
    {
        public string Name { get; }

        public List<T> Data = new List<T>();

        public DataTable(string name)
        {
            Name = name;
        }

        public void Fetch(OleDbConnection connection)
        {
        }

        public void Sync()
        {
        }
    }
}