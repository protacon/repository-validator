using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using ValidationLibrary.AzureFunctions;
using ValidationLibrary.Utils;
using System.Linq;
using ValidationLibrary.Rules;

[assembly: WebJobsStartup(typeof(Startup))]
namespace ValidationLibrary.AzureFunctions
{
    internal class CustomTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string _version;

        public CustomTelemetryInitializer()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            _version = fileVersionInfo.ProductVersion;
        }

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Component.Version = _version;
        }
    }

    public class Startup : IWebJobsStartup
    {
        private const string ProductHeader = "PTCS-Repository-Validator";

        public void Configure(IWebJobsBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            IConfiguration config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            builder
                .Services
                .AddLogging()
                .AddValidationRules(config)
                .AddTransient<IGitHubClient, GitHubClient>(services =>
                {
                    var githubConfig = new GitHubConfiguration();
                    config.GetSection("GitHub").Bind(githubConfig);

                    ValidateConfig(githubConfig);
                    return CreateClient(githubConfig);
                })
                .AddTransient<GitUtils>()
                .AddTransient<IValidationClient, ValidationClient>()
                .AddSingleton<IRepositoryValidator>(provider =>
                {
                    return new RepositoryValidator(
                        provider.GetService<ILogger<RepositoryValidator>>(),
                        provider.GetService<IGitHubClient>(),
                        provider.GetServices<IValidationRule>().ToArray());
                })
                .AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>()
                .AddTransient<RepositoryValidatorEndpoint>();
        }

        private static GitHubClient CreateClient(GitHubConfiguration gitHubConfiguration)
        {
            var client = new GitHubClient(new ProductHeaderValue(ProductHeader));
            var tokenAuth = new Credentials(gitHubConfiguration.Token);
            client.Credentials = tokenAuth;
            return client;
        }

        private static void ValidateConfig(GitHubConfiguration gitHubConfiguration)
        {
            if (gitHubConfiguration.Organization == null)
            {
                throw new ArgumentNullException(nameof(gitHubConfiguration.Organization), "Organization was missing.");
            }

            if (gitHubConfiguration.Token == null)
            {
                throw new ArgumentNullException(nameof(gitHubConfiguration.Token), "Token was missing.");
            }
        }
    }
}