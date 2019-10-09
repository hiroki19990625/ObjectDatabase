using System;

namespace ObjectDatabase
{
    [AttributeUsage(AttributeTargets.Property)]
    public class UnionTargetAttribute : Attribute
    {
        public string FieldName { get; }

        public UnionTargetAttribute(string fieldName)
        {
            FieldName = fieldName;
        }
    }
}