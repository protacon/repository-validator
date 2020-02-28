using Octokit;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// This rule checks that repository has CODEOWNERS defined in .github folder
    /// 
    /// Code owners should be defined for 2 reasons
    ///  1. Pull request reviewers are automatically added
    ///  1. Other people have have better visibility that who knows about the 
    ///  project.
    /// 
    /// See https://help.github.com/en/github/creating-cloning-and-archiving-repositories/about-code-owners
    /// for more information
    /// </summary>
    public class HasCodeownersRule : IValidationRule
    {
        public string RuleName => "Missing CODEOWNERS";

        private const string MainBranch = "master";

        private readonly ILogger<HasCodeownersRule> _logger;

        public HasCodeownersRule(ILogger<HasCodeownersRule> logger)
        {
            _logger = logger;
        }

        public Task Init(IGitHubClient gitHubClient)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Initialized", nameof(HasCodeownersRule), RuleName);
            return Task.CompletedTask;
        }

        public async Task<ValidationResult> IsValid(IGitHubClient client, Repository repo)
        {

            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (repo is null)
            {
                throw new ArgumentNullException(nameof(repo));
            }

            _logger.LogTrace("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}", nameof(HasCodeownersRule), RuleName, repo.FullName);
            var codeownersContent = await GetCodeownersContent(client, repo).ConfigureAwait(false);
            if (codeownersContent == null)
            {
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, No CODEOWNERS found, validation false.", nameof(HasReadmeRule), RuleName);
                return new ValidationResult(RuleName, "Add CODEOWNERS file.", false, DoNothing);
            }

            _logger.LogDebug("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}. CODEOWNERS exists: {codeownersExist}", nameof(HasCodeownersRule), RuleName, repo.FullName, !string.IsNullOrWhiteSpace(codeownersContent.Content));
            return new ValidationResult(RuleName, "Add CODEOWNERS file & add at least one owner.", !string.IsNullOrWhiteSpace(codeownersContent.Content), DoNothing);
        }

        public Dictionary<string, string> GetConfiguration()
        {
            return new Dictionary<string, string>
            {
                 { "PullRequestTitle", RuleName },
                 { "Main Branch", MainBranch }
            };
        }

        private Task DoNothing(IGitHubClient client, Repository repository)
        {
            return Task.CompletedTask;
        }

        private async Task<RepositoryContent> GetCodeownersContent(IGitHubClient client, Repository repository)
        {
            var contents = await GetContents(client, repository, MainBranch).ConfigureAwait(false);
            var codeownersFile = contents.FirstOrDefault(content => content.Name.Equals("CODEOWNERS", StringComparison.InvariantCultureIgnoreCase));
            var path = "CODEOWNERS";

            if (codeownersFile == null)
            {
                var directory = contents.FirstOrDefault(content => content.Name.Equals(".github", StringComparison.InvariantCultureIgnoreCase));
                if (directory != null)
                {
                    var directoryContents = await client.Repository.Content.GetAllContentsByRef(repository.Owner.Login, repository.Name, directory.Name, MainBranch).ConfigureAwait(false);
                    codeownersFile = directoryContents.FirstOrDefault(content => content.Name.Equals("CODEOWNERS", StringComparison.InvariantCultureIgnoreCase));
                    path = ".github/CODEOWNERS";
                }
            }

            if (codeownersFile == null)
            {
                var directory = contents.FirstOrDefault(content => content.Name.Equals("docs", StringComparison.InvariantCultureIgnoreCase));
                if (directory != null)
                {
                    var directoryContents = await client.Repository.Content.GetAllContentsByRef(repository.Owner.Login, repository.Name, directory.Name, MainBranch).ConfigureAwait(false);
                    codeownersFile = directoryContents.FirstOrDefault(content => content.Name.Equals("CODEOWNERS", StringComparison.InvariantCultureIgnoreCase));
                    path = "docs/CODEOWNERS";
                }
            }


            if (codeownersFile == null)
            {
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, No CODEOWNERS found.", nameof(HasCodeownersRule), RuleName);
                return null;
            }
            var matchingFile = await client.Repository.Content.GetAllContentsByRef(repository.Owner.Login, repository.Name, path, MainBranch).ConfigureAwait(false);
            return matchingFile[0];
        }

        private async Task<IReadOnlyList<RepositoryContent>> GetContents(IGitHubClient client, Repository repository, string branch)
        {
            try
            {
                return await client.Repository.Content.GetAllContentsByRef(repository.Owner.Login, repository.Name, branch).ConfigureAwait(false);
            }
            catch (NotFoundException exception)
            {
                _logger.LogWarning(exception, "Rule {ruleClass} / {ruleName}, Repository {repositoryName} caused {exceptionClass}. This may be a new repository, but if this persists, repository should be removed.",
                 nameof(HasCodeownersRule), RuleName, repository.Name, nameof(NotFoundException));
                return Array.Empty<RepositoryContent>();
            }
        }
    }
}