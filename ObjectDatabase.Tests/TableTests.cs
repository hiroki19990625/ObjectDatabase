using System;
using System.Diagnostics;
using System.Linq;
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
            ObjectDatabase database = new ObjectDatabase("ObjectDatabase.accdb");
            DataTable<TestDataModel> table = new DataTable<TestDataModel>("Test");

            Stopwatch stopwatch = Stopwatch.StartNew();
            database.AddTable(table);
            stopwatch.Stop();
            Console.WriteLine($"Fetch {stopwatch.ElapsedMilliseconds}ms");

            stopwatch = Stopwatch.StartNew();
            table.Delete(model => model.Name == "Alice");
            stopwatch.Stop();
            Console.WriteLine($"Delete {stopwatch.ElapsedMilliseconds}ms");

            stopwatch = Stopwatch.StartNew();
            table.Insert(new TestDataModel
            {
                Name = "Alice",
                Age = 20,
                Location = "US"
            });
            stopwatch.Stop();
            Console.WriteLine($"Insert {stopwatch.ElapsedMilliseconds}ms");

            stopwatch = Stopwatch.StartNew();
            table.ToArray()[0].Age = 21;
            table.Sync();
            stopwatch.Stop();
            Console.WriteLine($"Sync {stopwatch.ElapsedMilliseconds}ms");

            Assert.True(table.Where(model => true).First().Name == "Alice");
            Assert.True(table.Select(model => model.Age).First() == 21);

            database.Dispose();
        }

        [Test]
        public void ClassInClassTest()
        {
            ObjectDatabase database = new ObjectDatabase("ObjectDatabase.accdb");
            DataTable<ClassInClassDataModel> table = new DataTable<ClassInClassDataModel>("ClassInClassTest");
            database.AddTable(table);

            table.Delete(model => true);

            table.Insert(new ClassInClassDataModel
            {
                Name = "HogeProject",
                Description = "HogeHoge",
                TaskId = "011",
                Task = new ClassInClassDataModel.WorkTask
                {
                    Name = "HugaTask",
                    Description = "HugaHuga"
                }
            });

            database.Dispose();
        }
    }
}