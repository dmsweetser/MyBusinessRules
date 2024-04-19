using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Diagnostics;

namespace BusinessRules.Domain.Fields.Tests
{
    [TestClass()]
    public class BizField_ExtensionsTests
    {
        [TestMethod()]
        public void FlattenFieldsWithDescription_IfFieldIsProvided_FieldIsFlattenedCleanly()
        {
            var test = new BizField("test");
            test.AddChildField(new BizField("child1"));
            test.AddChildField(new BizField("child2"));

            var flattenedField = test.FlattenFieldsWithDescription();
            Console.WriteLine(JsonConvert.SerializeObject(flattenedField, Formatting.Indented));
            Assert.IsTrue(flattenedField.Count == 2 
                && flattenedField.Values.Any(x => x == "test.child1")
                && flattenedField.Values.Any(x => x == "test.child2"));
        }

        [TestMethod()]
        public void FlattenFieldsWithDescription_IfFieldIsLarge_PerformanceIsAcceptable()
        {
            var test = new BizField("test");
            for (int i = 0; i < 100; i++)
            {
                var newChild = new BizField("child" + i);
                for (int j = 0; j < 1000; j++)
                {
                    var subChild = new BizField("subChild" + j);
                    newChild.AddChildField(subChild);
                }
                test.AddChildField(newChild);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var flattenedField = test.FlattenFieldsWithDescription();
            stopwatch.Stop();

            Console.WriteLine("Total time: " + stopwatch.ElapsedMilliseconds);

            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500 
                && !flattenedField.Values.Any(x => x == "testtest"));
        }
    }
}