namespace ObjectDatabase.Tests.Models
{
    public class JobDataModel : DataModel
    {
        [SerializeProperty("JobId", IsKey = true, RelationKey = true)]
        public string JobId { get; set; }

        public string Name { get; set; }
    }
}