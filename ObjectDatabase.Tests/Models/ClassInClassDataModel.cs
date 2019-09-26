namespace ObjectDatabase.Tests.Models
{
    public class ClassInClassDataModel : DataModel
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public string TaskId { get; set; }

        [IgnoreProperty, RelationProperty("Task", nameof(TaskId), nameof(WorkTask.TaskId))]
        public WorkTask Task { get; set; }

        public class WorkTask
        {
            public string TaskId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }
    }
}