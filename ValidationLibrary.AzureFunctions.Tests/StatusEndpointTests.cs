using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Octokit;

namespace ValidationLibrary.AzureFunctions.Tests
{
    [TestFixture]
    public class StatusEndpointTests
    {
        private StatusEndpoint _statusEndpoint;

        [Test]
        public void Run_NormalStatusEndpointCheck()
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
            var expectedStatuses = rules.Select(r => r.GetConfiguration());

            var validator = Substitute.For<ValidationLibrary.RepositoryValidator>(new object[] { Substitute.For<ILogger<ValidationLibrary.RepositoryValidator>>(), Substitute.For<IGitHubClient>(), rules });
            _statusEndpoint = new StatusEndpoint(Substitute.For<ILogger<StatusEndpoint>>(), validator);
            var request = new HttpRequestMessage();

            var result = _statusEndpoint.Run(request);

            var casted = result as JsonResult;
            Assert.NotNull(casted, "The returned result was not a JsonResult.");
            var actualStatuses = (Dictionary<string, string>[])casted.Value.GetType().GetProperty("Rules")?.GetValue(casted.Value, null);
            Assert.AreEqual(expectedStatuses.Count(), actualStatuses.Length);
            foreach (var expectedStatus in expectedStatuses)
            {
                Assert.IsTrue(actualStatuses.Any(s => !s.Except(expectedStatus).Any()));
            }
        }
    }
}