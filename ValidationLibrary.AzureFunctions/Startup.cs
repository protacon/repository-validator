using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ValidationLibrary.AzureFunctions;

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
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
        }
    }
}