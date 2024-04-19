using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusinessRules.Domain.DTO.Tests
{
    [TestClass()]
    public class EndUserDTOTests
    {
        [TestMethod()]
        public void EndUserDTO_IfEndUserDTOIsInstantiated_ItExistsLol()
        {
            var test = new EndUserDTO();
            Assert.IsTrue(test is not null);
        }
    }
}