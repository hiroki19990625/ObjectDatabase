using System;
using System.Collections.Generic;
using NUnit.Framework;
using ObjectDatabase.Tests.Models;

namespace ObjectDatabase.Tests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void Test1()
        {
            TestDataModel model = new TestDataModel();
            model.Age = 100;
            foreach (KeyValuePair<string, ISerializedData> data in model.Serialize())
            {
                Console.WriteLine($"{data.Key}:{data.Value.TypeCode}:{data.Value.Value}");
            }

            TestDataModel model2 = new TestDataModel();
            model2.Deserialize(model.Serialize());

            Assert.True(model.Age == model2.Age);
        }
    }
}