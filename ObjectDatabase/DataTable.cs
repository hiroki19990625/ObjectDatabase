using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

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

        /// <summary>
        /// データベースからデータをフェッチする際のクエリコマンドを設定します。
        /// </summary>
        public string FetchQuery { get; set; }

        /// <summary>
        /// true にすると、データベースへの同期を自動で行います。
        /// </summary>
        public bool AutoSync { get; set; } = true;

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

            bool s = true;
            string[] commands = CreateInsertCommands(models);
            int idx = 0;
            OleDbCommand cmd = new OleDbCommand
            {
                Connection = _connection
            };

            OleDbTransaction transaction = _connection.BeginTransaction();
            foreach (string command in commands)
            {
                ObjectDatabase._logger.QueryLog($"Insert Exec Query {command}");

                try
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = command;
                    int c = cmd.ExecuteNonQuery();
                    if (c == 1)
                        _data.Add(models[idx++]);
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    ObjectDatabase._logger.Error($"Insert Rollback! {e.Message}");

                    s = false;
                    break;
                }
            }

            if (s)
                transaction.Commit();
            transaction.Dispose();

            cmd.Dispose();

            sw.Stop();
            ObjectDatabase._logger.OperationLog($"Insert {sw.ElapsedMilliseconds}ms - count: {idx}");

            if (AutoSync)
                Sync();
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
            string[] cmds = CreateDeleteCommands(models);
            OleDbCommand command = new OleDbCommand
            {
                Connection = _connection
            };
            bool s = true;

            OleDbTransaction transaction = _connection.BeginTransaction();
            foreach (T model in models)
            {
                ObjectDatabase._logger.QueryLog($"Delete Exec Query {cmds[idx]}");

                try
                {
                    command.Transaction = transaction;
                    command.CommandText = cmds[idx++];
                    command.ExecuteNonQuery();
                    _data.Remove(model);
                    count++;
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    ObjectDatabase._logger.Error($"Delete Rollback! {e.Message}");

                    s = false;
                    break;
                }
            }

            if (s)
                transaction.Commit();
            transaction.Dispose();

            command.Dispose();

            sw.Stop();
            ObjectDatabase._logger.OperationLog($"Delete {sw.ElapsedMilliseconds}ms - count: {idx}");

            if (AutoSync)
                Sync();

            return count;
        }

        /// <summary>
        /// 他のテーブルのデータと結合します。
        /// </summary>
        /// <param name="table">結合するテーブル</param>
        /// <param name="additionalWhere">追加の条件を絞り込み</param>
        /// <typeparam name="TUnionTarget">結合するテーブルの型</typeparam>
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
                    if (att != null && !string.IsNullOrWhiteSpace(additionalWhere) && att.FieldName == additionalWhere)
                    {
                        unionField = att.FieldName;
                        return true;
                    }

                    if (att != null && additionalWhere == null)
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

        /// <summary>
        /// linqを用いた select(列選択) を行います。
        /// </summary>
        /// <param name="select"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns>選択した結果</returns>
        public IEnumerable<TResult> Select<TResult>(Func<T, TResult> select)
        {
            return _data.Select(select);
        }

        /// <summary>
        /// linqを用いた　where(条件絞り込み) を行います。
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns>条件絞り込みの結果</returns>
        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            return _data.Where(predicate);
        }

        /// <summary>
        /// テーブルのデータを配列にして返します。
        /// </summary>
        /// <returns>テーブルの全てのデータ</returns>
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

            bool s = true;
            string[] cmds = CreateUpdateCommands();
            int c = 0;
            OleDbCommand cmd = new OleDbCommand
            {
                Connection = _connection
            };

            OleDbTransaction transaction = _connection.BeginTransaction();
            foreach (string command in cmds)
            {
                ObjectDatabase._logger.QueryLog($"Sync Exec Query {command}");

                try
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = command;
                    cmd.ExecuteNonQuery();
                    c++;
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    ObjectDatabase._logger.Error($"Delete Rollback! {e.Message}");
                    s = false;
                    break;
                }
            }

            if (s)
                transaction.Commit();
            transaction.Dispose();

            cmd.Dispose();

            sw.Stop();
            ObjectDatabase._logger.OperationLog($"Sync {sw.ElapsedMilliseconds}ms - count: {c}");
        }

        private string[] CreateInsertCommands(T[] models)
        {
            List<string> cmds = new List<string>();
            StringBuilder builder = new StringBuilder();
            foreach (T model in models)
            {
                var fields = model.Serialize();
                builder.Append($"insert into {Name} (");
                foreach (KeyValuePair<string, ISerializedData> field in fields)
                {
                    builder.Append($"{field.Key}, ");
                }

                builder.Remove(builder.Length - 2, 2);
                builder.Append($") values(");
                foreach (KeyValuePair<string, ISerializedData> field in fields)
                {
                    if (field.Value.TypeCode == TypeCode.String || field.Value.TypeCode == TypeCode.DateTime)
                        builder.Append($"'{field.Value.Value}', ");
                    else
                        builder.Append($"{field.Value.Value}, ");
                }

                builder.Remove(builder.Length - 2, 2);
                builder.Append(")");

                cmds.Add(builder.ToString());
                builder.Clear();
            }

            return cmds.ToArray();
        }

        private string[] CreateDeleteCommands(IEnumerable<T> it)
        {
            List<string> cmds = new List<string>();
            StringBuilder builder = new StringBuilder();
            foreach (T model in it)
            {
                var fields = model.Serialize();
                builder.Append($"delete from {Name} where ");
                foreach (KeyValuePair<string, ISerializedData> serializedData in fields)
                {
                    if (serializedData.Value.TypeCode == TypeCode.String ||
                        serializedData.Value.TypeCode == TypeCode.DateTime)
                        builder.Append($"{serializedData.Key} = '{serializedData.Value.Value}' AND ");
                    else
                        builder.Append($"{serializedData.Key} = {serializedData.Value.Value} AND ");
                }

                builder.Remove(builder.Length - 4, 4);
                cmds.Add(builder.ToString());
                builder.Clear();
            }

            return cmds.ToArray();
        }

        private string[] CreateUpdateCommands()
        {
            List<string> commands = new List<string>();
            StringBuilder builder = new StringBuilder();
            foreach (T dataModel in _data)
            {
                Dictionary<string, ISerializedData> serializedData = dataModel.Serialize();
                builder.Append($"update {Name} set");
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

                    if (data.Value.TypeCode == TypeCode.String ||
                        data.Value.TypeCode == TypeCode.DateTime)
                        builder.Append($" {data.Key}='{data.Value.Value}',");
                    else
                        builder.Append($" {data.Key}={data.Value.Value},");
                }

                builder.Remove(builder.Length - 1, 1);

                if (foundRelationKey)
                    if (relationKey.Value.TypeCode == TypeCode.String ||
                        relationKey.Value.TypeCode == TypeCode.DateTime)
                        builder.Append($" where {relationKey.Key} = '{relationKey.Value.Value}'");
                    else
                        builder.Append($" where {relationKey.Key} = {relationKey.Value.Value}");

                commands.Add(builder.ToString());
                builder.Clear();
            }

            return commands.ToArray();
        }
    }
}