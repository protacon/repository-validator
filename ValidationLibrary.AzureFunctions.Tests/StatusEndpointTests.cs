using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Octokit;
using ValidationLibrary.Rules;

namespace ValidationLibrary.AzureFunctions.Tests
{
    [TestFixture]
    public class StatusEndpointTests
    {
        private StatusEndpoint _statusEndpoint;

        [Test]
        public void Run_NormalStatusEndpointCheck()
        {
            Environment.SetEnvironmentVariable("GitHub:Organization", "mock");
            Environment.SetEnvironmentVariable("GitHub:Token", "mock");
            
            var ruleType = typeof(HasLicenseRule);
            var expectedRulesTypes = ruleType.Assembly.GetExportedTypes().Where(t => t.GetInterface(nameof(IValidationRule)) != null && !t.IsAbstract);
            var expectedRules = expectedRulesTypes.Select(r =>
            {
                var args = r.GetConstructors()[0].GetParameters().Select(p => (object)null).ToArray();
                return ((IValidationRule)Activator.CreateInstance(r, args));
            }).ToArray();
            var expectedStatuses = expectedRules.Select(r => r.GetConfiguration());

            var validator = Substitute.For<ValidationLibrary.RepositoryValidator>(new object[] { Substitute.For<ILogger<ValidationLibrary.RepositoryValidator>>(), Substitute.For<IGitHubClient>(), expectedRules });
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

            Environment.SetEnvironmentVariable("GitHub:Organization", null);
            Environment.SetEnvironmentVariable("GitHub:Token", null);
        }
    }
}