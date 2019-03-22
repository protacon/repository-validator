using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;
using ValidationLibrary.Utils;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// THis rule validates that Jenkinsfile has newest jenkins-ptcs-library defined if it exists
    /// </summary>
    public class HasNewestPtcsJenkinsLibRule : IValidationRule
    {
        private const string JenkinsFileName = "Jenkinsfile";

        public string RuleName => "Old jenkins-ptcs-library";

        private readonly Regex _regex = new Regex(@"'jenkins-ptcs-library@(\d+.\d+.\d+.*)'", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ILogger _logger;
        private string _expectedVersion;
        
        public HasNewestPtcsJenkinsLibRule(ILogger logger)
        {
            _logger = logger;
        }

        public async Task Init(IGitHubClient ghClient)
        {
            var versionFetcher = new ReleaseVersionFetcher(ghClient, "protacon", "jenkins-ptcs-library");
            _expectedVersion = await versionFetcher.GetLatest();
            _logger.LogInformation("Newest version: {0}", _expectedVersion);
        }

        public async Task<ValidationResult> IsValid(IGitHubClient client, Repository repository)
        {
            _logger.LogTrace("Rule {0} / {1}, Validating repository {2}", nameof(HasNewestPtcsJenkinsLibRule), RuleName, repository.FullName);
            var rootContents = await client.Repository.Content.GetAllContents(repository.Owner.Login, repository.Name);

            var jenkinsFile = rootContents.FirstOrDefault(content => content.Name.Equals(JenkinsFileName, StringComparison.InvariantCultureIgnoreCase));
            if (jenkinsFile == null)
            {
                _logger.LogDebug("Rule {0} / {1}, No {2} found in root. Skipping.", nameof(HasNewestPtcsJenkinsLibRule), RuleName, JenkinsFileName);
                return OkResult();
            }
            var jenkinsContent = await client.Repository.Content.GetAllContents(repository.Owner.Login, repository.Name, jenkinsFile.Name);
            MatchCollection matches = _regex.Matches(jenkinsContent[0].Content);
            var match = matches.OfType<Match>().FirstOrDefault();
            if (match == null)
            {
                return OkResult();
            }

            var group = match.Groups.OfType<Group>().LastOrDefault();
            if (group == null)
            {
                return OkResult();
            }

            return new ValidationResult
            {
                RuleName = RuleName,
                HowToFix = "Update jenkins-ptcs-library to newest version.",
                IsValid = group.Value == _expectedVersion
            };
        }

        private ValidationResult OkResult()
        {
            return new ValidationResult
            {
                RuleName = RuleName,
                HowToFix = "Update jenkins-ptcs-library to newest version.",
                IsValid = true
            };
        }
    }
}