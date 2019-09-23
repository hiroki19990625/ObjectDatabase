using System;

namespace ObjectDatabase
{
    public class SerializedData : ISerializedData
    {
        public string Name { get; }
        public TypeCode TypeCode { get; }
        public object Value { get; }

        public SerializedData(string name, TypeCode typeCode, object value)
        {
            Name = name;
            TypeCode = typeCode;
            Value = value;
        }
    }
}