using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace ObjectDatabase
{
    public class DataTable<T> : IDataTable where T : IDataModel, new()
    {
        private OleDbConnection _connection;

        public string Name { get; }

        public List<T> Data { get; } = new List<T>();

        public DataTable(string name)
        {
            Name = name;
        }

        public void Fetch(OleDbConnection connection)
        {
            _connection = connection;

            OleDbCommand command = new OleDbCommand("select * from " + Name, connection);
            OleDbDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Dictionary<string, ISerializedData> serializedData = new Dictionary<string, ISerializedData>();
                for (int i = 0; i > reader.FieldCount; i++)
                {
                    serializedData.Add(reader.GetName(i),
                        new SerializedData(reader.GetName(i), Type.GetTypeCode(reader.GetFieldType(i)),
                            reader.GetValue(i)));
                }

                T dataModel = new T();
                dataModel.Deserialize(serializedData);
                Data.Add(dataModel);
            }
        }

        public void Sync()
        {
            foreach (T dataModel in Data)
            {
                Dictionary<string, ISerializedData> serializedData = dataModel.Serialize();
                foreach (KeyValuePair<string, ISerializedData> data in serializedData)
                {
                }
            }
        }
    }
}