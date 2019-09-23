using System;

namespace ObjectDatabase
{
    public interface ISerializedData
    {
        TypeCode TypeCode { get; }
        object Value { get; }
    }
}