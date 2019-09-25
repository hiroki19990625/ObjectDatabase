using System;

namespace ObjectDatabase
{
    /// <summary>
    /// シリアル化されたデータを実装します。
    /// </summary>
    public interface ISerializedData
    {
        string Name { get; }
        TypeCode TypeCode { get; }
        object Value { get; }
    }
}