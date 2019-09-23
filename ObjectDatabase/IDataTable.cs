using System.Data.OleDb;

namespace ObjectDatabase
{
    public interface IDataTable
    {
        string Name { get; }

        void Fetch(OleDbConnection connection);
        void Sync();
    }
}