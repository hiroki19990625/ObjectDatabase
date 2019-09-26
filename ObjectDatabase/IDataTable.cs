using System.Data.OleDb;

namespace ObjectDatabase
{
    public interface IDataTable
    {
        string Name { get; }
        string FetchQuery { get; set; }

        void Fetch(OleDbConnection connection);
        void Sync();
    }
}