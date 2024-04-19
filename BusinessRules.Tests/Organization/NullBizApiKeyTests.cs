using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusinessRules.Domain.Organization.Tests
{
    [TestClass()]
	public class NullBizApiKeyTests
	{
		[TestMethod()]
		public void NullBizApiKey_IfNullBizApiKeyIsCreated_IdIsEmptyGuid()
		{
			var test = new NullBizApiKey();

			Assert.IsTrue(test.CompanyId == Guid.Empty);
		}
	}
}