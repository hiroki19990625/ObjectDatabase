using System;

namespace ObjectDatabase
{
    /// <summary>
    /// シリアル化されたデータを表します。
    /// </summary>
    public class SerializedData : ISerializedData
    {
        /// <summary>
        /// 主キーが設定されているフィールド
        /// </summary>
        public bool IsKey { get; }

        /// <summary>
        /// リレーションが設定されている主キー
        /// </summary>
        public bool RelationKey { get; }

        /// <summary>
        /// プロパティ名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// プロパティの型
        /// </summary>
        public TypeCode TypeCode { get; }

        /// <summary>
        ///プロパティの値 
        /// </summary>
        public object Value { get; }

        public SerializedData(string name, TypeCode typeCode, object value, bool isKey = false,
            bool relationKey = false)
        {
            Name = name;
            TypeCode = typeCode;
            Value = value;
            IsKey = isKey;
            RelationKey = relationKey;
        }
    }
}