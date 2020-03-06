using NUnit.Framework;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using ValidationLibrary.Rules;

namespace ValidationLibrary.AzureFunctions.Tests
{
    public class StartUpTests
    {
        [SetUp]
        public void Setup()
        {
            Environment.SetEnvironmentVariable("GitHub:Organization", "mock");
            Environment.SetEnvironmentVariable("GitHub:Token", "mock");
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable("GitHub:Organization", null);
            Environment.SetEnvironmentVariable("GitHub:Token", null);
        }

        [Test]
        public void Configure_CanBuildRepositoryValidator()
        {
            var host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var validator = host.Services.GetService(typeof(IRepositoryValidator));
            Assert.NotNull(validator);
        }

        [Test]
        public void Configure_ThrowsExceptionIfOrganizationIsMissing()
        {
            Environment.SetEnvironmentVariable("GitHub:Organization", null);

            var host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var ex = Assert.Throws<ArgumentNullException>(() => host.Services.GetService(typeof(IRepositoryValidator)));
            Assert.AreEqual("Organization", ex.ParamName);
        }

        [Test]
        public void Configure_ThrowsExceptionIfTokenIsMissing()
        {
            Environment.SetEnvironmentVariable("GitHub:Token", null);

            var host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var ex = Assert.Throws<ArgumentNullException>(() => host.Services.GetService(typeof(IRepositoryValidator)));
            Assert.AreEqual("Token", ex.ParamName);
        }

        [Test]
        public void Configure_CheckNormalRuleConfiguration()
        {
            // Get all rule classes.
            var expectedRules = typeof(HasLicenseRule).Assembly.GetExportedTypes().Where(t => t.GetInterface(nameof(IValidationRule)) != null && !t.IsAbstract);
            var expectedRuleNames = expectedRules.Select(r =>
            {
                var args = r.GetConstructors()[0].GetParameters().Select(p => (object)null).ToArray();
                return ((IValidationRule)Activator.CreateInstance(r, args)).RuleName;
            });

            var host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var validator = (IRepositoryValidator)host.Services.GetService(typeof(IRepositoryValidator));
            var actualRules = validator.Rules;

            Assert.AreEqual(expectedRules.Count(), actualRules.Length);
            foreach (var ruleName in expectedRuleNames)
            {
                Assert.IsTrue(actualRules.Any(r => r.RuleName.Equals(ruleName)));
            }
        }

        [Test]
        public void Configure_CheckExplicitRuleConfiguration()
        {
            // Environment variables for the configuration.
            Environment.SetEnvironmentVariable("Rules:HasLicenseRule", "disable");
            Environment.SetEnvironmentVariable("Rules:HasDescriptionRule", "enable"); // Decoy.

            // Get all rule classes.
            var ruleType = typeof(HasLicenseRule);
            var expectedRules = ruleType.Assembly.GetExportedTypes().Where(t => t.GetInterface(nameof(IValidationRule)) != null && !t.IsAbstract);

            var host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var validator = (IRepositoryValidator)host.Services.GetService(typeof(IRepositoryValidator));
            var actualRules = validator.Rules;

            Assert.AreEqual(expectedRules.Count() - 1, actualRules.Length);
            Assert.IsTrue(expectedRules.Any(r => r.Equals(ruleType)));
            Assert.IsFalse(actualRules.Any(r => r.RuleName.Equals("Missing License")));
            Assert.IsTrue(actualRules.Any(r => r.RuleName.Equals("Missing description")));

            // Tear down environment variables.
            Environment.SetEnvironmentVariable("Rules:HasLicenseRule", null);
            Environment.SetEnvironmentVariable("Rules:HasDescriptionRule", null);
        }
    }
}
