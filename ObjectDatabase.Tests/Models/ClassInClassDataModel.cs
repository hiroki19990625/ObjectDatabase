namespace ObjectDatabase.Tests.Models
{
    public class ClassInClassDataModel : DataModel
    {
        public string Name { get; set; }
        public string Description { get; set; }

        [IgnoreProperty] public WorkTask Task { get; set; }

        public class WorkTask
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }
    }
}