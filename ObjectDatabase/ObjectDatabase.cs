using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using LogAdapter;

namespace ObjectDatabase
{
    /// <summary>
    /// データベースへのアクセス、テーブルの管理をします。
    /// </summary>
    public class ObjectDatabase : IDisposable
    {
        internal static ILogger _logger = new LogAdapter.LogAdapter().Create();

        private readonly Dictionary<string, IDataTable> _tables = new Dictionary<string, IDataTable>();

        private readonly OleDbConnection _connection;

        /// <summary>
        /// Accessファイルを指定してデータベースを開始します。
        /// </summary>
        /// <param name="file">Accessファイル</param>
        /// <param name="version">プロバイダーのバージョン</param>
        /// <param name="logCallback">ログのコールバック</param>
        public ObjectDatabase(string file, string version = "12.0", Action<ILogMessage> logCallback = null)
        {
            if (logCallback != null)
                AddLogEvent(logCallback);

            _logger.Info($"Hello ObjectDatabase Version: {typeof(ObjectDatabase).Assembly.GetName().Version}");

            _connection = new OleDbConnection
            {
                ConnectionString = $"Provider=Microsoft.ACE.OLEDB.{version}; Data Source={file}"
            };
            _connection.Open();

            _logger.Info($"Connected OleDB!");
            _logger.Debug($"Used Provider > Microsoft.ACE.OLEDB.{version}; Data Source={file}");
        }

        /// <summary>
        /// テーブルの管理を開始します。
        /// </summary>
        /// <param name="dataTable">データテーブル</param>
        public void AddTable(IDataTable dataTable, string fetchQuery = "select * from {0}")
        {
            AddTable(dataTable.Name, dataTable, fetchQuery);
        }

        /// <summary>
        /// テーブルの管理を開始します。
        /// </summary>
        /// <param name="name">テーブル名</param>
        /// <param name="dataTable">データテーブル</param>
        public void AddTable(string name, IDataTable dataTable, string fetchQuery = "select * from {0}")
        {
            _logger.Info($"Subscribe {name} Table");

            _tables[dataTable.Name] = dataTable;
            dataTable.FetchQuery = fetchQuery;
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

        /// <summary>
        /// ログのコールバックを追加します。
        /// </summary>
        /// <param name="callback">ログのコールバック</param>
        public void AddLogEvent(Action<ILogMessage> callback)
        {
            _logger.AddCallback(callback);
        }

        /// <summary>
        /// ログのコールバックを削除します。
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveLogEvent(Action<ILogMessage> callback)
        {
            _logger.RemoveCallback(callback);
        }

        /// <summary>
        /// データベースからテーブルを自動生成します。
        /// </summary>
        public void GenerateCode(string projectName)
        {
            CodeCompileUnit compileUnit = new CodeCompileUnit();
            CodeNamespace importCodeNamespace = new CodeNamespace();
            importCodeNamespace.Imports.Add(new CodeNamespaceImport("ObjectDatabase"));
            compileUnit.Namespaces.Add(importCodeNamespace);
            CodeNamespace codeNamespace = new CodeNamespace($"{projectName}.DataModels");
            compileUnit.Namespaces.Add(codeNamespace);

            var tables = _connection.GetSchema("Tables");
            foreach (DataRow row in tables.Rows)
            {
                var name = row["TABLE_NAME"].ToString();
                CodeTypeDeclaration typeDeclaration = new CodeTypeDeclaration(name);
                typeDeclaration.BaseTypes.Add(new CodeTypeReference(typeof(DataModel)));
                var columns = _connection.GetSchema("Columns").Select($"TABLE_NAME='{name}'");
                foreach (var column in columns)
                {
                    string colName = column["COLUMN_NAME"].ToString();
                    OleDbType type = (OleDbType) column["DATA_TYPE"];
                    var rows = _connection.GetSchema("Indexes")
                        .Select($"TABLE_NAME='{name}' AND COLUMN_NAME='{colName}'");
                    bool isKey = rows.Length > 0 && bool.Parse(rows[0]["UNIQUE"].ToString());
                    CodeMemberField field = new CodeMemberField(type.CreateType(), colName.ToLower());
                    CodeMemberProperty property = new CodeMemberProperty
                    {
                        Type = new CodeTypeReference(type.CreateType()),
                        Name = colName,
                        HasGet = true,
                        HasSet = true,
                        Attributes = MemberAttributes.Public | MemberAttributes.Final
                    };
                    property.CustomAttributes.Add(new CodeAttributeDeclaration(
                        new CodeTypeReference(typeof(SerializePropertyAttribute)),
                        new CodeAttributeArgument(new CodePrimitiveExpression(colName)),
                        new CodeAttributeArgument("IsKey", new CodePrimitiveExpression(isKey)),
                        new CodeAttributeArgument("RelationKey", new CodePrimitiveExpression(isKey))));
                    property.GetStatements.Add(
                        new CodeMethodReturnStatement(new CodeFieldReferenceExpression(null, colName.ToLower())));
                    property.SetStatements.Add(
                        new CodeAssignStatement(new CodeFieldReferenceExpression(null, colName.ToLower()),
                            new CodePropertySetValueReferenceExpression()));
                    typeDeclaration.Members.Add(field);
                    typeDeclaration.Members.Add(property);
                }

                codeNamespace.Types.Add(typeDeclaration);
            }

            CodeDomProvider provider = CodeDomProvider.CreateProvider("CS");
            StringWriter writer = new StringWriter();
            provider.GenerateCodeFromCompileUnit(compileUnit, writer, new CodeGeneratorOptions
            {
                IndentString = "    "
            });

            Directory.CreateDirectory("GeneratedCode");
            File.WriteAllText("GeneratedCode/DatabaseGenerationCode.cs", writer.ToString());
        }

        public void Dispose()
        {
            _logger.Info("Goodbye ObjectDatabase!");
            _logger.Debug("Close Tables");

            foreach (KeyValuePair<string, IDataTable> dataTable in _tables)
            {
                dataTable.Value.Sync();
            }

            _connection.Close();
        }
    }

    internal static class ObjectDatabaseExtensions
    {
        internal static void QueryLog(this ILogger logger, string query)
        {
            logger.Log("Query", query);
        }

        internal static void OperationLog(this ILogger logger, string operation)
        {
            logger.Log("Operation", operation);
        }

        internal static Type CreateType(this OleDbType type)
        {
            switch (type)
            {
                case OleDbType.Empty:
                    return typeof(void);
                case OleDbType.SmallInt:
                    return typeof(short);
                case OleDbType.Integer:
                    return typeof(int);
                case OleDbType.Single:
                    return typeof(float);
                case OleDbType.Double:
                    return typeof(double);
                case OleDbType.Currency:
                    return typeof(decimal);
                case OleDbType.Date:
                    return typeof(DateTime);
                case OleDbType.BSTR:
                    return typeof(string);
                case OleDbType.IDispatch:
                    return typeof(object);
                case OleDbType.Error:
                    return typeof(Exception);
                case OleDbType.Boolean:
                    return typeof(bool);
                case OleDbType.Variant:
                    return typeof(object);
                case OleDbType.IUnknown:
                    return typeof(object);
                case OleDbType.Decimal:
                    return typeof(decimal);
                case OleDbType.TinyInt:
                    return typeof(sbyte);
                case OleDbType.UnsignedTinyInt:
                    return typeof(byte);
                case OleDbType.UnsignedSmallInt:
                    return typeof(ushort);
                case OleDbType.UnsignedInt:
                    return typeof(uint);
                case OleDbType.BigInt:
                    return typeof(long);
                case OleDbType.UnsignedBigInt:
                    return typeof(ulong);
                case OleDbType.Filetime:
                    return typeof(DateTime);
                case OleDbType.Guid:
                    return typeof(Guid);
                case OleDbType.Binary:
                    return typeof(byte[]);
                case OleDbType.Char:
                    return typeof(string);
                case OleDbType.WChar:
                    return typeof(string);
                case OleDbType.Numeric:
                    return typeof(decimal);
                case OleDbType.DBDate:
                    return typeof(DateTime);
                case OleDbType.DBTime:
                    return typeof(TimeSpan);
                case OleDbType.DBTimeStamp:
                    return typeof(TimeSpan);
                case OleDbType.PropVariant:
                    return typeof(object);
                case OleDbType.VarNumeric:
                    return typeof(decimal);
                case OleDbType.VarChar:
                    return typeof(string);
                case OleDbType.LongVarChar:
                    return typeof(string);
                case OleDbType.VarWChar:
                    return typeof(string);
                case OleDbType.LongVarWChar:
                    return typeof(string);
                case OleDbType.VarBinary:
                    return typeof(byte[]);
                case OleDbType.LongVarBinary:
                    return typeof(byte[]);
                default:
                    return typeof(void);
            }
        }
    }
}