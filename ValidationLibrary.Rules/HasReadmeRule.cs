using Octokit;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ValidationLibrary.Utils;
using System;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// This rule checks that repository has a README.md defined
    /// 
    /// Readme file is used to improve the maintanability of our repositories. Readme should at least
    /// describe what project does and how project can be built/developed
    /// 
    /// See https://help.github.com/en/github/creating-cloning-and-archiving-repositories/about-readmes for
    /// more information about GitHub readmes
    /// </summary>
    public class HasReadmeRule : IValidationRule
    {
        public string RuleName => "Missing Readme.md";
        private const string ReadmeFileName = "README.md";
        private const string ReadmeFilePrefix = "README";
        private readonly Uri _templateFileUrl;
        private readonly ILogger<HasReadmeRule> _logger;

        public HasReadmeRule(ILogger<HasReadmeRule> logger, Uri templateFileUrl = null)
        {
            _logger = logger;
            _templateFileUrl = templateFileUrl ?? new Uri("https://raw.githubusercontent.com/by-pinja/repository-validator/master/README_TEMPLATE.md");
        }

        public Task Init(IGitHubClient ghClient)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Initialized", nameof(HasReadmeRule), RuleName);
            return Task.CompletedTask;
        }

        public async Task<ValidationResult> IsValid(IGitHubClient client, Repository gitHubRepository)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (gitHubRepository is null) throw new ArgumentNullException(nameof(gitHubRepository));

            _logger.LogTrace("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}",
                nameof(HasReadmeRule), RuleName, gitHubRepository.FullName);
            var hasReadmeWithContent = await HasReadmeWithContent(client, gitHubRepository, gitHubRepository.DefaultBranch).ConfigureAwait(false);

            _logger.LogDebug("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}. Readme has content: {readmeHasContent}",
                nameof(HasReadmeRule), RuleName, gitHubRepository.FullName, hasReadmeWithContent);
            return new ValidationResult(RuleName, "Add README.md file to repository root with content describing this repository.", hasReadmeWithContent, DoNothing);
        }

        public Dictionary<string, string> GetConfiguration()
        {
            return new Dictionary<string, string>
            {
                { "ClassName", nameof(HasReadmeRule) }
            };
        }

        private async Task<bool> HasReadmeWithContent(IGitHubClient client, Repository repository, string branchName)
        {
            _logger.LogTrace("Rule {ruleClass} / {ruleName}: Retrieving fixed contents for JenkinsFile from branch {branch}", nameof(HasReadmeRule), RuleName, branchName);
            var readme = await GetReadmeFromBranch(client, repository, branchName).ConfigureAwait(false);
            return !string.IsNullOrWhiteSpace(readme?.Content);
        }

        private async Task<RepositoryContent> GetReadmeFromBranch(IGitHubClient client, Repository repository, string branch)
        {
            _logger.LogTrace("Retrieving JenkinsFile for {repositoryName} from branch {branch}", repository.FullName, branch);

            // NOTE: rootContents doesn't contain actual contents, content is only fetched when we fetch the single file later.
            var rootContents = await GetContents(client, repository, branch).ConfigureAwait(false);

            var readmeFile = rootContents.FirstOrDefault(content => Regex.IsMatch(content.Name, $@"^{ReadmeFilePrefix}(\....?)?$", RegexOptions.IgnoreCase));
            //  See HasReadMeRuleTests for examples of valid and invalid readme file names.
            if (readmeFile == null)
            {
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, No {readmeFileName} found in root.", nameof(HasReadmeRule), RuleName, ReadmeFileName);
                return null;
            }

            var matchingFiles = await client.Repository.Content.GetAllContentsByRef(repository.Owner.Login, repository.Name, readmeFile.Name, branch).ConfigureAwait(false);
            return matchingFiles[0];
        }

        private async Task<IReadOnlyList<RepositoryContent>> GetContents(IGitHubClient client, Repository repository, string branch)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (repository is null) throw new ArgumentNullException(nameof(repository));
            if (string.IsNullOrEmpty(branch)) throw new ArgumentException("branch is missing", nameof(branch));

            try
            {
                return await client.Repository.Content.GetAllContentsByRef(repository.Owner.Login, repository.Name, branch).ConfigureAwait(false);
            }
            catch (NotFoundException exception)
            {
                /*
                 * NOTE: Repository that was just created (empty repository) doesn't have content this causes
                 * Octokit.NotFoundException. This same thing would probably be throw if the whole repository
                 * was missing, but we don't care for that case (no point to validate if repository doesn't exist.)
                 */
                _logger.LogWarning(exception, "Rule {ruleClass} / {ruleName}, Repository {repositoryName} caused {exceptionClass}. This may be a new repository, but if this persists, repository should be removed.",
                typeof(HasReadmeRule).Name, RuleName, repository.Name, nameof(NotFoundException));
                return Array.Empty<RepositoryContent>();
            }
        }

        private async Task<string> GetReadmeTemplateContent()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(_templateFileUrl).ConfigureAwait(false);
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Error fetching README template.");
                }
            }

            return string.Empty;
        }

        private Task DoNothing(IGitHubClient client, Repository repository)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, No fix.", nameof(HasNotManyStaleBranchesRule), RuleName);
            return Task.CompletedTask;
        }
    }
}
