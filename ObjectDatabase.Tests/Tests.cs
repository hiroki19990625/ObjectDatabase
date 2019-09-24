using System;
using System.Collections.Generic;
using NUnit.Framework;
using ObjectDatabase.Tests.Models;

namespace ObjectDatabase.Tests
{
    [TestFixture]
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }

        [Test]
        public void Test1()
        {
            TestDataModel model = new TestDataModel();
            model.Age = 100;
            model.Location = "JP";
            foreach (KeyValuePair<string, ISerializedData> data in model.Serialize())
            {
                Console.WriteLine($"{data.Key}:{data.Value.TypeCode}:{data.Value.Value}");
            }

            TestDataModel model2 = new TestDataModel();
            model2.Deserialize(model.Serialize());

            Assert.True(model.Age == model2.Age);
            Assert.True(model.Location == model2.Location);
        }

        [Test]
        public void Test2()
        {
            ObjectDatabase database = new ObjectDatabase("ObjectDatabase.accdb");
            DataTable<TestDataModel> table = new DataTable<TestDataModel>("Test");
            database.AddTable(table);

            table.Delete(model => { return model.Name == "Alice"; });

            table.Insert(new TestDataModel
            {
                Name = "Alice",
                Age = 20,
                Location = "US"
            });
        }
    }
}