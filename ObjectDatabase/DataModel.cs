using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ObjectDatabase
{
    public abstract class DataModel : IDataModel
    {
        public Dictionary<string, ISerializedData> Serialize()
        {
            Type type = GetType();
            Dictionary<string, ISerializedData> properties = new Dictionary<string, ISerializedData>();
            foreach (PropertyInfo property in type.GetProperties())
            {
                if (property.GetCustomAttribute<IgnorePropertyAttribute>() != null)
                    continue;

                try
                {
                    SerializePropertyAttribute propData = property.GetCustomAttribute<SerializePropertyAttribute>();
                    properties.Add(propData != null ? propData.Name : property.Name,
                        new SerializedData(property.Name, Type.GetTypeCode(property.PropertyType),
                            property.GetValue(this)));
                }
                catch
                {
                    // ignored
                }
            }

            return properties;
        }

        public void Deserialize(Dictionary<string, ISerializedData> data)
        {
            Type type = GetType();
            foreach (KeyValuePair<string, ISerializedData> serializedData in data)
            {
                try
                {
                    PropertyInfo propertyInfo = type.GetProperty(serializedData.Key);
                    if (propertyInfo != null)
                        propertyInfo.SetValue(this, serializedData.Value.Value);
                    else
                    {
                        propertyInfo = type.GetProperties().Where(prop =>
                        {
                            var att =
                                prop.GetCustomAttribute<SerializePropertyAttribute>();
                            return att != null && serializedData.Key == att?.Name;
                        }).FirstOrDefault();
                        if (propertyInfo != null)
                            propertyInfo.SetValue(this, serializedData.Value.Value);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}