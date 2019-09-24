using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;

namespace ObjectDatabase
{
    public class DataTable<T> : IDataTable where T : IDataModel, new()
    {
        private OleDbConnection _connection;
        private readonly List<T> _data = new List<T>();

        public string Name { get; }
        public int Count => _data.Count;

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
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    serializedData.Add(reader.GetName(i),
                        new SerializedData(reader.GetName(i), Type.GetTypeCode(reader.GetFieldType(i)),
                            reader.GetValue(i)));
                }

                T dataModel = new T();
                dataModel.Deserialize(serializedData);
                _data.Add(dataModel);
            }

            reader.Close();
            command.Dispose();
        }

        public void Insert(params T[] models)
        {
            OleDbCommand[] commands = CreateInsertCommands(models);
            int idx = 0;
            foreach (OleDbCommand oleDbCommand in commands)
            {
                int c = oleDbCommand.ExecuteNonQuery();
                if (c == 1)
                    _data.Add(models[idx++]);
            }
        }

        public int Delete(Func<T, bool> where)
        {
            T[] models = _data.Where(where).ToArray();
            int count = 0;
            int idx = 0;
            OleDbCommand[] cmds = CreateDeleteCommands(models);
            foreach (T model in models)
            {
                cmds[idx++].ExecuteNonQuery();
                _data.Remove(model);
                count++;
            }

            return count;
        }

        public void Sync()
        {
            foreach (T dataModel in _data)
            {
                Dictionary<string, ISerializedData> serializedData = dataModel.Serialize();
                foreach (KeyValuePair<string, ISerializedData> data in serializedData)
                {
                }
            }
        }

        private OleDbCommand[] CreateInsertCommands(T[] models)
        {
            List<OleDbCommand> cmds = new List<OleDbCommand>();
            foreach (T model in models)
            {
                var fields = model.Serialize();
                string cmd = $"insert into {Name} (";
                foreach (KeyValuePair<string, ISerializedData> field in fields)
                {
                    cmd += field.Key + ", ";
                }

                var remove = cmd.Remove(cmd.Length - 2, 2);
                cmd = remove + ") values(";
                foreach (KeyValuePair<string, ISerializedData> field in fields)
                {
                    cmd += "?, ";
                }

                remove = cmd.Remove(cmd.Length - 2, 2);
                cmd = remove + ")";

                OleDbCommand command = new OleDbCommand(cmd, _connection);
                foreach (KeyValuePair<string, ISerializedData> serializedData in fields)
                {
                    command.Parameters.Add(new OleDbParameter(serializedData.Key, serializedData.Value.Value));
                }

                cmds.Add(command);
            }

            return cmds.ToArray();
        }

        private OleDbCommand[] CreateDeleteCommands(IEnumerable<T> it)
        {
            List<OleDbCommand> cmds = new List<OleDbCommand>();
            foreach (T model in it)
            {
                var fields = model.Serialize();
                string cmd = $"delete from {Name} where ";
                foreach (KeyValuePair<string, ISerializedData> serializedData in fields)
                {
                    if (serializedData.Value.TypeCode == TypeCode.String)
                        cmd += $"{serializedData.Key} = '{serializedData.Value.Value}' AND ";
                    else
                        cmd += $"{serializedData.Key} = {serializedData.Value.Value} AND ";
                }

                cmd = cmd.Remove(cmd.Length - 4, 4);
                OleDbCommand command = new OleDbCommand(cmd, _connection);
                cmds.Add(command);
            }

            return cmds.ToArray();
        }
    }
}