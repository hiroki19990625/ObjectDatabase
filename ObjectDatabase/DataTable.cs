using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

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

            Stopwatch sw = Stopwatch.StartNew();
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

            sw.Stop();
            ObjectDatabase._logger.QueryLog($"Fetch Exec Query {command.CommandText}");
            ObjectDatabase._logger.OperationLog($"Fetch {sw.ElapsedMilliseconds}ms");

            reader.Close();
            command.Dispose();
        }

        /// <summary>
        /// データベースにデータを挿入します。
        /// </summary>
        /// <param name="models">挿入するデータ</param>
        public void Insert(params T[] models)
        {
            Stopwatch sw = Stopwatch.StartNew();

            bool s = false;
            OleDbCommand[] commands = CreateInsertCommands(models);
            int idx = 0;
            foreach (OleDbCommand oleDbCommand in commands)
            {
                ObjectDatabase._logger.QueryLog($"Insert Exec Query {oleDbCommand.CommandText}");

                OleDbTransaction transaction = _connection.BeginTransaction();
                try
                {
                    oleDbCommand.Transaction = transaction;
                    int c = oleDbCommand.ExecuteNonQuery();
                    if (c == 1)
                        _data.Add(models[idx++]);

                    transaction.Commit();
                    oleDbCommand.Dispose();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    ObjectDatabase._logger.Error($"Insert Rollback! {e.Message}");
                }

                transaction.Dispose();
            }

            sw.Stop();
            ObjectDatabase._logger.OperationLog($"Insert {sw.ElapsedMilliseconds}ms - count: {idx}");
        }

        /// <summary>
        /// データベースのデータを削除します。
        /// </summary>
        /// <param name="where">条件</param>
        /// <returns>データベースの変更数</returns>
        public int Delete(Func<T, bool> where)
        {
            Stopwatch sw = Stopwatch.StartNew();

            T[] models = _data.Where(where).ToArray();
            int count = 0;
            int idx = 0;
            OleDbCommand[] cmds = CreateDeleteCommands(models);
            foreach (T model in models)
            {
                ObjectDatabase._logger.QueryLog($"Delete Exec Query {cmds[idx].CommandText}");

                OleDbTransaction transaction = _connection.BeginTransaction();
                try
                {
                    OleDbCommand command = cmds[idx++];
                    command.Transaction = transaction;
                    command.ExecuteNonQuery();
                    _data.Remove(model);
                    count++;

                    transaction.Commit();
                    command.Dispose();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    ObjectDatabase._logger.Error($"Delete Rollback! {e.Message}");
                }

                transaction.Dispose();
            }

            sw.Stop();
            ObjectDatabase._logger.OperationLog($"Delete {sw.ElapsedMilliseconds}ms - count: {idx}");

            return count;
        }

        public void Union<TUnionTarget>(DataTable<TUnionTarget> table, string additionalWhere = null)
            where TUnionTarget : IDataModel, new()
        {
            Stopwatch sw = Stopwatch.StartNew();

            Type t = typeof(TUnionTarget);
            Type myT = typeof(T);
            string unionField = null;
            PropertyInfo p = myT.GetProperties()
                .FirstOrDefault(prop =>
                {
                    UnionTargetAttribute att = prop.GetCustomAttribute<UnionTargetAttribute>();
                    if (att != null && att.FieldName == additionalWhere)
                    {
                        unionField = att.FieldName;
                        return true;
                    }

                    if (att != null)
                    {
                        unionField = att.FieldName;
                        return true;
                    }

                    return false;
                });
            if (p != null && !string.IsNullOrWhiteSpace(unionField))
            {
                if (p.PropertyType == t)
                {
                    PropertyInfo up = t.GetProperty(unionField);
                    PropertyInfo mp = myT.GetProperty(unionField);

                    if (up != null && mp != null)
                    {
                        foreach (T dataModel in ToArray())
                        {
                            foreach (TUnionTarget unionTarget in table.ToArray())
                            {
                                object objA = up.GetValue(unionTarget);
                                object objB = mp.GetValue(dataModel);
                                if (objA.Equals(objB))
                                {
                                    p.SetValue(dataModel, unionTarget);
                                }
                            }
                        }

                        sw.Stop();
                        ObjectDatabase._logger.OperationLog($"Success Union {sw.ElapsedMilliseconds}ms");
                        return;
                    }
                }
            }

            sw.Stop();
            ObjectDatabase._logger.OperationLog($"Failed Union {sw.ElapsedMilliseconds}ms");
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
            Stopwatch sw = Stopwatch.StartNew();

            OleDbCommand[] cmds = CreateUpdateCommands();
            int c = 0;
            foreach (OleDbCommand cmd in cmds)
            {
                ObjectDatabase._logger.QueryLog($"Sync Exec Query {cmd.CommandText}");

                OleDbTransaction transaction = _connection.BeginTransaction();
                try
                {
                    cmd.ExecuteNonQuery();
                    c++;
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    ObjectDatabase._logger.Error($"Delete Rollback! {e.Message}");
                }
            }

            sw.Stop();
            ObjectDatabase._logger.OperationLog($"Sync {sw.ElapsedMilliseconds}ms - count: {c}");
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
                bool foundRelationKey = false;
                KeyValuePair<string, ISerializedData> relationKey = default;
                foreach (KeyValuePair<string, ISerializedData> data in serializedData)
                {
                    if (data.Value.RelationKey)
                    {
                        foundRelationKey = true;
                        relationKey = data;
                        continue;
                    }

                    if (data.Value.TypeCode == TypeCode.String)
                        cmd += $" {data.Key}='{data.Value.Value}',";
                    else
                        cmd += $" {data.Key}={data.Value.Value},";
                }

                OleDbCommand command = new OleDbCommand(cmd.Remove(cmd.Length - 1, 1), _connection);
                if (foundRelationKey)
                    if (relationKey.Value.TypeCode == TypeCode.String)
                        command.CommandText += $" where {relationKey.Key} = '{relationKey.Value.Value}'";
                    else
                        command.CommandText += $" where {relationKey.Key} = {relationKey.Value.Value}";

                commands.Add(command);
            }

            return commands.ToArray();
        }
    }
}