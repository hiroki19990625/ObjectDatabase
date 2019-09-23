using System.Collections.Generic;

namespace ObjectDatabase
{
    public interface IDataModel
    {
        Dictionary<string, ISerializedData> Serialize();
        void Deserialize(Dictionary<string, ISerializedData> data);
    }
}