﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
namespace BusinessRules.Rules.Components.Tests
{
    [TestClass()]
    public class EmptyAssignmentTests
    {
        [TestMethod()]
        public void GetFormattedDescription_IfDescriptionIsRequestedForActivatedRule_DescriptionIsGivenWithoutError()
        {
            var testField = new BizField("test");
            var test = new EmptyAssignment();
            Assert.IsTrue(!string.IsNullOrWhiteSpace(test.GetFormattedDescription(testField, true)));
        }

        [TestMethod()]
        public void GetFormattedDescription_IfDescriptionIsRequestedForNonActivatedRule_DescriptionIsGivenWithoutError()
        {
            var testField = new BizField("test");
            var test = new EmptyAssignment();
            Assert.IsTrue(!string.IsNullOrWhiteSpace(test.GetFormattedDescription(testField, false)));
        }
    }
}