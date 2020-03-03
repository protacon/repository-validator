using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
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

            IHost host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var validator = host.Services.GetService(typeof(ValidationLibrary.RepositoryValidator));
            _statusEndpoint = new StatusEndpoint(Substitute.For<ILogger<StatusEndpoint>>(), (ValidationLibrary.RepositoryValidator)validator);
            var request = new HttpRequestMessage();
            var ruleType = typeof(HasLicenseRule);
            var expectedRules = ruleType.Assembly.GetExportedTypes().Where(t => t.GetInterface(nameof(IValidationRule)) != null && !t.IsAbstract);
            var expectedStatuses = expectedRules.Select(r =>
            {
                var args = r.GetConstructors()[0].GetParameters().Select(p => (object)null).ToArray();
                return ((IValidationRule)Activator.CreateInstance(r, args)).GetConfiguration();
            });

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