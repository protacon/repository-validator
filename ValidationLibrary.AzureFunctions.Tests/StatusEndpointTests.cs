using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace ValidationLibrary.AzureFunctions.Tests
{
    [TestFixture]
    public class StatusEndpointTests
    {
        private StatusEndpoint _statusEndpoint;
        private IRepositoryValidator _validator;

        [SetUp]
        public void Setup()
        {
            var rule = Substitute.For<IValidationRule>();
            rule.GetConfiguration().Returns(new Dictionary<string, string>
            {
                { "ClassName", "GoodTestClassName" },
                { "PullRequestTitle", "GoodTestTitle" },
                { "ReadMeTemplateFileLocation", "GoodTestLocation.md" },
                { "MainBranch", "GoodTestBranch" }
            });
            var rule2 = Substitute.For<IValidationRule>();
            rule2.GetConfiguration().Returns(new Dictionary<string, string>
            {
                { "ClassName", "BadTestClassName" },
                { "PullRequestTitle", "BadTestTitle" },
                { "ReadMeTemplateFileLocation", "BadTestLocation.md" },
                { "MainBranch", "BadTestBranch" }
            });

            var rules = new[] { rule, rule2 };
            _validator = Substitute.For<IRepositoryValidator>();
            _validator.Rules.Returns(rules);
            _statusEndpoint = new StatusEndpoint(Substitute.For<ILogger<StatusEndpoint>>(), _validator);
        }

        [Test]
        public void Run_NormalStatusEndpointCheck()
        {
            var expectedStatuses = _validator.Rules.Select(r => r.GetConfiguration());
            var request = new HttpRequestMessage();

            var result = _statusEndpoint.Run(request);

            var casted = result as JsonResult;
            Assert.NotNull(casted, "The returned result was not a JsonResult.");
            var actualStatuses = (Dictionary<string, string>[])casted.Value.GetType().GetProperty(nameof(_validator.Rules))?.GetValue(casted.Value, null);
            Assert.AreEqual(expectedStatuses.Count(), actualStatuses.Length);
            foreach (var expectedStatus in expectedStatuses)
            {
                Assert.IsTrue(actualStatuses.Any(s => !s.Except(expectedStatus).Any()));
            }
        }
    }
}