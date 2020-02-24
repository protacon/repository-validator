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

            // Get all rule classes.
            var assembly = Assembly.Load("ValidationLibrary.Rules, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            var allValidationRules = assembly.GetExportedTypes().Where(t => t.GetInterface(nameof(IValidationRule)) != null && !t.IsAbstract);
            var disabledRules = new System.Collections.Generic.List<string>();

            // Select those rules defined by the configuration and the environment variables which should be disabled.
            var selectedValidationRules = allValidationRules.Where(r =>
            {
                if (string.Equals(config.GetValue<string>($"Rules:{r.Name}"), "disable", StringComparison.InvariantCultureIgnoreCase))
                {
                    disabledRules.Add(r.Name);
                    return false;
                }
                return true;
            }).ToArray();

            // Add each rule as available for the dependancy injection.
            foreach (var rule in selectedValidationRules)
            {
                builder.Services.AddTransient(rule);
            }

            builder
                .Services
                .AddLogging()
                .AddTransient<IGitHubClient, GitHubClient>(services =>
                {
                    var githubConfig = new GitHubConfiguration();
                    config.GetSection("GitHub").Bind(githubConfig);

                    ValidateConfig(githubConfig);
                    return CreateClient(githubConfig);
                })
                .AddTransient<GitUtils>()
                .AddTransient<IValidationClient, ValidationClient>()
                .AddSingleton(provider =>
                {
                    var logger = provider.GetService<ILogger<Startup>>();
                    var rules = selectedValidationRules.Select(r => (IValidationRule)provider.GetService(r)).ToArray();
                    if (disabledRules.Count != 0)
                    {
                        logger.LogInformation($"Ignoring rules: {disabledRules}");
                    }
                    return new ValidationLibrary.RepositoryValidator(
                        provider.GetService<ILogger<ValidationLibrary.RepositoryValidator>>(),
                        provider.GetService<IGitHubClient>(),
                        rules);
                })
                .AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>()
                .AddTransient<RepositoryValidator>();
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