using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Octokit;
using ValidationLibrary.Rules;

namespace ValidationLibrary.Tests.Rules
{
    public class HasLicenseRuleTests
    {
        private HasLicenseRule _rule;

        private IGitHubClient _mockClient;


        [SetUp]
        public void Setup()
        {
            _mockClient = Substitute.For<IGitHubClient>();
            _rule = new HasLicenseRule(Substitute.For<ILogger<HasLicenseRule>>());
        }

        [Test]
        public void RuleName_IsDefined()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(_rule.RuleName));
        }

        [Test]
        public async Task IsValid_ReturnsOkForPrivateRepository()
        {
            var repository = CreateRepository("repomen", true, null);

            var result = await _rule.IsValid(_mockClient, repository);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsOkForPublicRepositoryWithLicense()
        {
            LicenseMetadata license = new LicenseMetadata("key", "node", "name", "spdxID", "url", false);

            var repository = CreateRepository("repomen", false, license);

            var result = await _rule.IsValid(_mockClient, repository);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsInvalidForPublicRepositoryWithLicense()
        {
            var repository = CreateRepository("repomen", false, null);

            var result = await _rule.IsValid(_mockClient, repository);
            Assert.IsFalse(result.IsValid);
        }

        private Repository CreateRepository(string name, bool isPrivate, LicenseMetadata license)
        {
            return new Repository(null, null, null, null, null, null, null, 0, null, null, name, null, false, null, null, null, isPrivate, false, 0, 0, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, null, null, null, license, false, false, false, false, 0, 0, null, null, null, false);
        }
    }
}