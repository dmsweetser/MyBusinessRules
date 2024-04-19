using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Licensing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessRules.Licensing.Tests
{
    [TestClass()]
    public class LicenseManagerTests
    {
        [TestMethod()]
        public void LicenseManager_IfDateIsMinValue_AllowOfflineModeReturnsFalse()
        {
            var test = new LicenseManager();
            Assert.IsFalse(test.AllowOfflineMode.Invoke(DateTime.MinValue));
        }
    }
}