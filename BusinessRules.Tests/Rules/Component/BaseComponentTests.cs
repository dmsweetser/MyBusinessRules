using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Rules.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using BusinessRules.Domain.Fields;

namespace BusinessRules.Domain.Rules.Component.Tests
{
    [TestClass()]
    public class BaseComponentTests
    {
        [TestMethod()]
        public void GetFormattedDescriptionTest()
        {
            var test = new BaseComponent();
            Assert.IsTrue(string.IsNullOrWhiteSpace(test.GetFormattedDescription(new BizField("test"), true)));
        }
    }
}