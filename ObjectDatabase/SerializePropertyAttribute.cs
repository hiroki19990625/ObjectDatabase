using System;

namespace ObjectDatabase
{
    /// <summary>
    /// データ定義でプロパティのシリアル化の名前を変更したり主キーの指定に使用します。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SerializePropertyAttribute : Attribute
    {
        /// <summary>
        /// シリアル化の名前
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 主キーにするかどうか
        /// </summary>
        public bool IsKey { get; set; }

        public SerializePropertyAttribute(string name)
        {
            Name = name;
        }
    }
}