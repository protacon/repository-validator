using NUnit.Framework;
using Microsoft.Extensions.Hosting;
using System;

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
    }
}