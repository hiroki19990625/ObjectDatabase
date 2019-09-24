using System;

namespace ObjectDatabase
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SerializePropertyAttribute : Attribute
    {
        public string Name { get; }
        public bool IsKey { get; set; }

        public SerializePropertyAttribute(string name)
        {
            Name = name;
        }
    }
}