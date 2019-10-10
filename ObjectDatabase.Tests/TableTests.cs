using System;
using System.Linq;
using LogAdapter;
using NUnit.Framework;
using ObjectDatabase.Tests.Models;

namespace ObjectDatabase.Tests
{
    [TestFixture]
    public class TableTests
    {
        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }

        [Test]
        public void AllTest()
        {
            ObjectDatabase database = new ObjectDatabase("ObjectDatabase.accdb", logCallback: Callback);
            DataTable<TestDataModel> table = new DataTable<TestDataModel>("Test");

            database.AddTable(table);

            table.Delete(model => model.Name == "Alice");

            table.Insert(new TestDataModel
            {
                Name = "Alice",
                Age = 20,
                Location = "US",
                JobId = "B200"
            });

            table.ToArray()[0].Age = 21;
            table.Sync();

            DataTable<JobDataModel> table2 = new DataTable<JobDataModel>("Job");
            database.AddTable(table2);
            table.Union(table2);
            table2.Where(model => model.Name == "Earth").First().Name = "#Earth";
            table.ToArray()[0].Job.Name = "Earth";

            Assert.True(table.Where(model => true).First().Name == "Alice");
            Assert.True(table.Select(model => model.Age).First() == 21);
            Assert.True(table.Select(model => model.Job).First().Name == "Earth");
            Assert.True(table2.Select(model => model.Name).ToArray()[1] == "Earth");

            database.Dispose();
        }

        private void Callback(ILogMessage obj)
        {
            Console.WriteLine($"[{obj.LogLevel}] {string.Format(obj.Data.ToString(), obj.Args)}");
        }
    }
}