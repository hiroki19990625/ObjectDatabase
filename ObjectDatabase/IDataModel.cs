using System.Collections.Generic;

namespace ObjectDatabase
{
    /// <summary>
    /// シリアル化出来るデータモデルを実装します。
    /// </summary>
    public interface IDataModel
    {
        Dictionary<string, ISerializedData> Serialize();
        void Deserialize(Dictionary<string, ISerializedData> data);
    }
}