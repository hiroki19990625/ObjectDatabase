using System;

namespace ObjectDatabase
{
    /// <summary>
    /// シリアル化されたデータを表します。
    /// </summary>
    public class SerializedData : ISerializedData
    {
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

        public SerializedData(string name, TypeCode typeCode, object value)
        {
            Name = name;
            TypeCode = typeCode;
            Value = value;
        }
    }
}