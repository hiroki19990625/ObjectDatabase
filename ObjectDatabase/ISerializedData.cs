using System;

namespace ObjectDatabase
{
    public interface ISerializedData
    {
        string Name { get; }
        TypeCode TypeCode { get; }
        object Value { get; }
    }
}