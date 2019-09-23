namespace ObjectDatabase.Tests.Models
{
    public class TestDataModel : DataModel
    {
        public string Name { get; set; } = "Bob";
        public int Age { get; set; } = 12;

        [IgnoreProperty] public int Id { get; set; } = 123456;

        [SerializeProperty("location")]
        public string Location { get; set; } = "USA";
    }
}