using NUnit.Framework;
using Microsoft.Extensions.Hosting;
using System;
using System.Reflection;
using System.Linq;

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
            IHost host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var validator = host.Services.GetService(typeof(RepositoryValidator));
            Assert.NotNull(validator);
        }

        [Test]
        public void Configure_ThrowsExceptionIfOrganizationIsMissing()
        {
            Environment.SetEnvironmentVariable("GitHub:Organization", null);

            IHost host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var ex = Assert.Throws<ArgumentNullException>(() => host.Services.GetService(typeof(RepositoryValidator)));
            Assert.AreEqual("Organization", ex.ParamName);
        }

        [Test]
        public void Configure_ThrowsExceptionIfTokenIsMissing()
        {
            Environment.SetEnvironmentVariable("GitHub:Token", null);

            IHost host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var ex = Assert.Throws<ArgumentNullException>(() => host.Services.GetService(typeof(RepositoryValidator)));
            Assert.AreEqual("Token", ex.ParamName);
        }

        [Test]
        public void Configure_CheckNormalRuleConfiguration()
        {
            // Get all rule classes.
            var assembly = Assembly.Load("ValidationLibrary.Rules, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            var expectedValidationRules = assembly.GetExportedTypes().Where(t => t.GetInterface(nameof(IValidationRule)) != null);
            var expectedValidationRuleNames = expectedValidationRules.Select(r =>
            {
                var args = r.GetConstructors()[0].GetParameters().Select(p => (object)null).ToArray();
                return ((IValidationRule)Activator.CreateInstance(r, args)).RuleName;
            });

            IHost host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var validator = (ValidationLibrary.RepositoryValidator)host.Services.GetService(typeof(ValidationLibrary.RepositoryValidator));
            var actualValidationRules = validator.GetRules();

            Assert.AreEqual(expectedValidationRules.Count(), actualValidationRules.Length);
            foreach (var ruleName in expectedValidationRuleNames)
            {
                Assert.AreEqual(true, actualValidationRules.Any(r => r.RuleName.Equals(ruleName)));
            }
        }

        [Test]
        public void Configure_CheckRuleConfiguration()
        {
            // Environment variables for the configuration.
            Environment.SetEnvironmentVariable("Rules:HasLicenseRule", "disable");
            Environment.SetEnvironmentVariable("Rules:HasDescriptionRule", "enable"); // Decoy.

            // Get all rule classes.
            var assembly = Assembly.Load("ValidationLibrary.Rules, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            var allValidationRules = assembly.GetExportedTypes().Where(t => t.GetInterface(nameof(IValidationRule)) != null);

            IHost host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var validator = (ValidationLibrary.RepositoryValidator)host.Services.GetService(typeof(ValidationLibrary.RepositoryValidator));
            var actualValidationRules = validator.GetRules();

            Assert.AreEqual(allValidationRules.Count() - 1, actualValidationRules.Length);
            Assert.AreEqual(false, actualValidationRules.Any(r => r.RuleName.Equals("Missing License")));

            // Tear down environment variables.
            Environment.SetEnvironmentVariable("Rules:HasLicenseRule", null);
            Environment.SetEnvironmentVariable("Rules:HasDescriptionRule", null);
        }
    }
}