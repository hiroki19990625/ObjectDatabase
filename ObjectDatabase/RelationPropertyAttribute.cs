using System;

namespace ObjectDatabase
{
    public class RelationPropertyAttribute : Attribute
    {
        public string TableName { get; set; }
        public string JoinLCondField { get; set; }
        public string JoinRCondField { get; set; }

        public RelationPropertyAttribute(string tableName, string joinLCondField, string joinRCondField)
        {
            TableName = tableName;
            JoinLCondField = joinLCondField;
            JoinRCondField = joinRCondField;
        }
    }
}