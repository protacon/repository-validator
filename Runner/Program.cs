using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Octokit;
using ValidationLibrary;
using ValidationLibrary.Rules;

namespace Runner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.Development.json", optional: true)
                .Build();

            Run(config["Token"], config["Organization"]).Wait();
        }

        public static async Task Run(string token, string organization)
        {
            var validator = new RepositoryValidator();

            var client = new GitHubClient(new ProductHeaderValue("Protacon-Repository-Validator-DEV"));
            var tokenAuth = new Credentials(token);
            client.Credentials = tokenAuth;
            var allRepos = await client.Repository.GetAllForOrg(organization);
            var problemRepositories = allRepos.Select(repo => validator.Validate(repo))
                                            .Where(report => report.Results.Any(result => !result.IsValid));

            foreach (var repo in problemRepositories)
            {
                Console.WriteLine("Repo: {0}", repo.RepositoryName);
            }
        }
    }
}
