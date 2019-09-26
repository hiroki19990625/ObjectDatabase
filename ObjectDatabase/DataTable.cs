using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;

namespace ObjectDatabase
{
    /// <summary>
    /// データベースのテーブルを表します。
    /// </summary>
    /// <typeparam name="T">データ定義の型</typeparam>
    public class DataTable<T> : IDataTable where T : IDataModel, new()
    {
        private OleDbConnection _connection;
        private readonly List<T> _data = new List<T>();

        /// <summary>
        /// テーブルの名前
        /// </summary>
        public string Name { get; }

        public string FetchQuery { get; set; }

        /// <summary>
        /// テーブルに格納されているデータ数
        /// </summary>
        public int Count => _data.Count;

        /// <summary>
        /// テーブル名からインスタンスを作成します。
        /// </summary>
        /// <param name="name">テーブル名</param>
        public DataTable(string name)
        {
            Name = name;
        }

        /// <summary>
        /// データベースのデータをフェッチします。
        /// </summary>
        /// <param name="connection">データベースのコネクション</param>
        public void Fetch(OleDbConnection connection)
        {
            _connection = connection;

            OleDbCommand command = new OleDbCommand(string.Format(FetchQuery, Name), connection);
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

        /// <summary>
        /// データベースにデータを挿入します。
        /// </summary>
        /// <param name="models">挿入するデータ</param>
        public void Insert(params T[] models)
        {
            OleDbCommand[] commands = CreateInsertCommands(models);
            int idx = 0;

            OleDbTransaction transaction = _connection.BeginTransaction();
            try
            {
                foreach (OleDbCommand oleDbCommand in commands)
                {
                    oleDbCommand.Transaction = transaction;
                    int c = oleDbCommand.ExecuteNonQuery();
                    if (c == 1)
                        _data.Add(models[idx++]);

                    transaction.Commit();
                    oleDbCommand.Dispose();
                }
            }
            catch
            {
                transaction.Rollback();
                Console.WriteLine("Rollback!");
            }

            transaction.Dispose();
        }

        /// <summary>
        /// データベースのデータを削除します。
        /// </summary>
        /// <param name="where">条件</param>
        /// <returns>データベースの変更数</returns>
        public int Delete(Func<T, bool> where)
        {
            T[] models = _data.Where(where).ToArray();
            int count = 0;
            int idx = 0;
            OleDbTransaction transaction = _connection.BeginTransaction();
            OleDbCommand[] cmds = CreateDeleteCommands(models);
            try
            {
                foreach (T model in models)
                {
                    cmds[idx].Transaction = transaction;
                    cmds[idx++].ExecuteNonQuery();
                    _data.Remove(model);
                    count++;

                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                Console.WriteLine("Rollback!");
            }

            transaction.Dispose();

            return count;
        }

        public IEnumerable<TResult> Select<TResult>(Func<T, TResult> select)
        {
            return _data.Select(select);
        }

        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            return _data.Where(predicate);
        }

        public T[] ToArray()
        {
            return _data.ToArray();
        }

        /// <summary>
        /// データベースとデータを同期します。
        /// </summary>
        public void Sync()
        {
            OleDbCommand[] cmds = CreateUpdateCommands();
            foreach (OleDbCommand cmd in cmds)
            {
                cmd.ExecuteNonQuery();
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

        private OleDbCommand[] CreateUpdateCommands()
        {
            List<OleDbCommand> commands = new List<OleDbCommand>();
            foreach (T dataModel in _data)
            {
                Dictionary<string, ISerializedData> serializedData = dataModel.Serialize();
                string cmd = $"update {Name} set";
                foreach (KeyValuePair<string, ISerializedData> data in serializedData)
                {
                    if (data.Value.TypeCode == TypeCode.String)
                        cmd += $" {data.Key}='{data.Value.Value}',";
                    else
                        cmd += $" {data.Key}={data.Value.Value},";
                }

                OleDbCommand command = new OleDbCommand(cmd.Remove(cmd.Length - 1, 1), _connection);
                commands.Add(command);
            }

            return commands.ToArray();
        }
    }
}