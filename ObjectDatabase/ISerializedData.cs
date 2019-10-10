using System;

namespace ObjectDatabase
{
    /// <summary>
    /// シリアル化されたデータを実装します。
    /// </summary>
    public interface ISerializedData
    {
        bool IsKey { get; }
        bool RelationKey { get; }
        string Name { get; }
        TypeCode TypeCode { get; }
        object Value { get; }
    }
}