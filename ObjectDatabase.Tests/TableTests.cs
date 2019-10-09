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
                Location = "US",
                JobId = "B200"
            });
            stopwatch.Stop();
            Console.WriteLine($"Insert {stopwatch.ElapsedMilliseconds}ms");

            try
            {
                stopwatch = Stopwatch.StartNew();
                table.ToArray()[0].Age = 21;
                table.Sync();
                stopwatch.Stop();
                Console.WriteLine($"Sync {stopwatch.ElapsedMilliseconds}ms");
            }
            catch
            {
                // ignored
            }

            stopwatch = Stopwatch.StartNew();
            DataTable<JobDataModel> table2 = new DataTable<JobDataModel>("Job");
            database.AddTable(table2);
            table.Union(table2);
            table2.ToArray()[1].Name = "#Earth";
            table.ToArray()[0].Job.Name = "Earth";
            Console.WriteLine($"Union {stopwatch.ElapsedMilliseconds}ms");

            Assert.True(table.Where(model => true).First().Name == "Alice");
            Assert.True(table.Select(model => model.Age).First() == 21);
            Assert.True(table.Select(model => model.Job).First().Name == "Earth");
            Assert.True(table2.Select(model => model.Name).ToArray()[1] == "Earth");

            try
            {
                database.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }
}