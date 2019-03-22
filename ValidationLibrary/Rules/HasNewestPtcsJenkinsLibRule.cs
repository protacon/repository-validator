using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;
using ValidationLibrary.Utils;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// Rule validates that Jenkinsfile has newest jenkins-ptcs-library is used if jenkins-ptcs-library is used at all.
    /// jenkins-ptcs-library is an internal company library that offers utilities for CI pipelines.
    /// </summary>
    public class HasNewestPtcsJenkinsLibRule : IValidationRule
    {
        private const string JenkinsFileName = "JENKINSFILE";

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

            // NOTE: rootContents doesn't contain actual contents, content is only fetched when we fetch the single file later.
            var rootContents = await GetContents(client, repository);
            
            var jenkinsFile = rootContents.FirstOrDefault(content => content.Name.Equals(JenkinsFileName, StringComparison.InvariantCultureIgnoreCase));
            if (jenkinsFile == null)
            {
                _logger.LogDebug("Rule {0} / {1}, No {2} found in root. Skipping.", nameof(HasNewestPtcsJenkinsLibRule), RuleName, JenkinsFileName);
                return OkResult();
            }

            var matchingJenkinsFiles = await client.Repository.Content.GetAllContents(repository.Owner.Login, repository.Name, jenkinsFile.Name);
            var jenkinsContent = matchingJenkinsFiles.FirstOrDefault();
            if (jenkinsContent == null)
            {
                // THhis is unlikely to happen.
                _logger.LogDebug("Rule {0} / {1}, {2} was removed after checking from repository root. Skipping.", nameof(HasNewestPtcsJenkinsLibRule), RuleName, JenkinsFileName);
                return OkResult();
            }

            MatchCollection matches = _regex.Matches(jenkinsContent.Content);
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

        private async Task<IReadOnlyList<RepositoryContent>> GetContents(IGitHubClient client, Repository repository)
        {
            try {
                return await client.Repository.Content.GetAllContents(repository.Owner.Login, repository.Name);
            } 
            catch (Octokit.NotFoundException exception)
            {
                /* 
                 * NOTE: Repository that was just created (empty repository) doesn't have content this causes
                 * Octokit.NotFoundException. This same thing would probably be throw if the whole repository
                 * was missing, but we don't care for that case (no point to validate if repository doesn't exist.)
                 */
                _logger.LogWarning(exception, "Rule {0} / {1}, Repository {2} caused {3}. This may be a new repository, but if this persists, repository should be removed.",
                 nameof(HasNewestPtcsJenkinsLibRule), RuleName, repository.Name, nameof(Octokit.NotFoundException));
                return new RepositoryContent[0];
            }
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