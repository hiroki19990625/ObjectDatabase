using System;

namespace ObjectDatabase
{
    /// <summary>
    /// データ定義でプロパティのシリアル化を無視する際に使用します。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnorePropertyAttribute : Attribute
    {
    }
}