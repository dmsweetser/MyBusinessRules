using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Data.Implementation.JsonStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;

namespace BusinessRules.Data.Implementation.JsonStorage.Tests
{
    [TestClass()]
    public class JsonStorageRepositoryTests
    {
        [TestMethod()]
        public async Task JsonStorageRepository_CRUD_ApiKey()
        {
            var repository = new JsonStorageRepository(Path.GetTempPath());
            var companyId = Guid.NewGuid();
            var topLevelFieldId = Guid.NewGuid();

            var apiKey = new BizApiKey(companyId, topLevelFieldId);

            await repository.SaveApiKey(apiKey);
            
            var foundApiKey = await repository.GetApiKey(apiKey.Id.ToString());

            await repository.DeleteApiKey(apiKey.Id.ToString());

            var nullApiKey = await repository.GetApiKey(apiKey.Id.ToString());

            Assert.IsTrue(foundApiKey is not NullBizApiKey && nullApiKey is NullBizApiKey);
        }

        [TestMethod()]
        public async Task JsonStorageRepository_CRUD_Company()
        {
            var repository = new JsonStorageRepository(Path.GetTempPath());

            var company = new BizCompany(Guid.NewGuid().ToString("N"));

            await repository.SaveCompany(company);

            var foundCompany = await repository.GetCompany(company.Id.ToString());

            await repository.DeleteCompany(company.Id.ToString());

            var nullCompany = await repository.GetCompany(company.Id.ToString());

            Assert.IsTrue(foundCompany is not NullBizCompany && nullCompany is NullBizCompany);
        }

        [TestMethod()]
        public async Task JsonStorageRepository_CRUD_BizField()
        {
            var repository = new JsonStorageRepository(Path.GetTempPath());
            var companyId = Guid.NewGuid().ToString();

            var bizField = new BizField(Guid.NewGuid().ToString("N"));

            await repository.SaveTopLevelField(companyId, bizField);

            var foundBizField = await repository.GetTopLevelField(companyId, bizField.Id.ToString());

            await repository.DeleteTopLevelField(companyId, bizField.Id.ToString());

            var nullBizField = await repository.GetTopLevelField(companyId, bizField.Id.ToString());

            Assert.IsTrue(foundBizField is not NullBizField && nullBizField is NullBizField);
        }

        [TestMethod()]
        public async Task JsonStorageRepository_CRUD_Rule()
        {
            var repository = new JsonStorageRepository(Path.GetTempPath());
            var newField = new BizField("Test");

            var bizRule = new BizRule("test", newField);

            await repository.SaveRule(newField.Id.ToString(), bizRule);

            var foundBizRule = await repository.GetRule(newField.Id.ToString(), bizRule.Id.ToString());

            await repository.DeleteRule(newField.Id.ToString(), bizRule.Id.ToString());

            var nullBizRule = await repository.GetRule(newField.Id.ToString(), bizRule.Id.ToString());

            Assert.IsTrue(foundBizRule is not NullBizRule && nullBizRule is NullBizRule);
        }

        [TestMethod()]
        public async Task JsonStorageRepository_CRUD_UserToCompany()
        {
            var repository = new JsonStorageRepository(Path.GetTempPath());
            var companyId = Guid.NewGuid();

            var userId = Guid.NewGuid().ToString("N");

            var userToCompany = new BizUserToCompany(userId, companyId);

            await repository.SaveUserToCompanyReference(userToCompany);

            var foundReference = await repository.GetUserToCompanyReference(userId);

            await repository.DeleteUserToCompanyReference(userId);

            var nullReference = await repository.GetUserToCompanyReference(userId);

            Assert.IsTrue(foundReference is not NullBizUserToCompany && nullReference is NullBizUserToCompany);
        }

        [TestMethod()]
        public async Task JsonStorageRepository_CRUD_User()
        {
            var repository = new JsonStorageRepository(Path.GetTempPath());
            
            var company = new BizCompany(Guid.NewGuid().ToString("N"));
            var newUser = new BizUser(Guid.NewGuid().ToString("N"), UserRole.Administrator);
            company.Users.Add(newUser);

            await repository.SaveCompany(company);

            var foundUser = repository.GetUser(company.Id.ToString(), newUser.EmailAddress.ToString());

            Assert.IsNotNull(foundUser);
        }

        [TestMethod()]
        public async Task JsonStorageRepository_CRUD_CreditCode()
        {
            var repository = new JsonStorageRepository(Path.GetTempPath());

            var creditCode = new BizCreditCode(Guid.NewGuid(), 10, Guid.NewGuid().ToString("N"));

            await repository.SaveCreditCode(creditCode);

            var foundCode = await repository.GetCreditCode(creditCode.Id.ToString());

            Assert.IsTrue(foundCode is not null);
        }

        [TestMethod()]
        public void JsonStorageRepository_CRUD_IfPathDoesNotExist_ItIsCreated()
        {
            var additionalPath = Guid.NewGuid().ToString("N");
            var repository = new JsonStorageRepository(Path.Combine(Path.GetTempPath(), additionalPath));
            Assert.IsTrue(Directory.Exists(Path.Combine(Path.GetTempPath(), additionalPath)));
        }
    }
}