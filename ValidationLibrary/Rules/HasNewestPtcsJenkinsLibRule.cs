using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Helpers;
using ValidationLibrary.Utils;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// Rule validates that Jenkinsfile has newest jenkins-ptcs-library is used if jenkins-ptcs-library is used at all.
    /// jenkins-ptcs-library is an internal company library that offers utilities for CI pipelines.
    /// </summary>
    public class HasNewestPtcsJenkinsLibRule : FixableRuleBase<HasNewestPtcsJenkinsLibRule>, IValidationRule
    {
        public override string RuleName => $"Old {LibraryName}";
        private const string LibraryName = "jenkins-ptcs-library";
        private const string JenkinsFileName = "Jenkinsfile";
        private const string FileMode = "100644";

        private readonly string _branchName = $"feature/{LibraryName}-update";
        private readonly string _prTitle = $"Update {LibraryName} to latest version.";
        private readonly Regex _regex = new Regex($@"[""']{LibraryName}@(\d+.\d+.\d+.*)[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ILogger<HasNewestPtcsJenkinsLibRule> _logger;
        private readonly GitUtils _gitUtils;
        private string _expectedVersion;
        private string _latestReleaseUrl;

        public HasNewestPtcsJenkinsLibRule(ILogger<HasNewestPtcsJenkinsLibRule> logger, GitUtils gitUtils) : base(logger, gitUtils)
        {
            _logger = logger;
            _gitUtils = gitUtils;
        }

        public override async Task Init(IGitHubClient ghClient)
        {
            var versionFetcher = new ReleaseVersionFetcher(ghClient, "protacon", LibraryName);
            var release = await versionFetcher.GetLatest().ConfigureAwait(false);
            _expectedVersion = release.TagName;
            _latestReleaseUrl = release.HtmlUrl;
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Newest version: {expectedVersion}", nameof(HasNewestPtcsJenkinsLibRule), RuleName, _expectedVersion);
        }

        public override async Task<ValidationResult> IsValid(IGitHubClient client, Repository repository)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (repository is null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            _logger.LogTrace("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}", nameof(HasNewestPtcsJenkinsLibRule), RuleName, repository.FullName);

            var jenkinsContent = await GetJenkinsFileContent(client, repository, MainBranch).ConfigureAwait(false);
            if (jenkinsContent == null)
            {
                // This is unlikely to happen.
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, no {jenkinsFileName} found. Skipping.", nameof(HasNewestPtcsJenkinsLibRule), RuleName, JenkinsFileName);
                return OkResult();
            }

            MatchCollection matches = _regex.Matches(jenkinsContent.Content);
            var match = matches.OfType<Match>().FirstOrDefault();
            if (match == null)
            {
                _logger.LogTrace("Rule {ruleClass} / {ruleName}, no jenkins-ptcs-library matches found. Skipping.", nameof(HasNewestPtcsJenkinsLibRule), RuleName, JenkinsFileName);
                return OkResult();
            }

            var group = match.Groups.OfType<Group>().LastOrDefault();
            if (group == null)
            {
                _logger.LogTrace("Rule {ruleClass} / {ruleName}, no jenkins-ptcs-library groups found. Skipping.", nameof(HasNewestPtcsJenkinsLibRule), RuleName, JenkinsFileName);
                return OkResult();
            }

            return new ValidationResult(RuleName, $"Update {LibraryName} to newest version.", group.Value == _expectedVersion, Fix);
        }

        /// <summary>
        /// This takes either the latest commit from master or latest from updated branch if it exists.
        /// </summary>
        protected override async Task<Commit> GetCommitAsBase(IGitHubClient client, Repository repository)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (repository == null) throw new ArgumentNullException(nameof(client));

            var branches = await client.Repository.Branch.GetAll(repository.Owner.Login, repository.Name).ConfigureAwait(false);
            var existingBranch = branches.FirstOrDefault(branch => branch.Name == _branchName);
            if (existingBranch == null)
            {
                _logger.LogInformation("Rule {ruleClass} / {ruleName}, Branch {branchName} did not exists, creating branch.",
                     nameof(HasNewestPtcsJenkinsLibRule), RuleName, _branchName);
                var branchReference = await client.Git.Reference.CreateBranch(repository.Owner.Login, repository.Name, _branchName).ConfigureAwait(false);
                return await client.Git.Commit.Get(repository.Owner.Login, repository.Name, branchReference.Object.Sha).ConfigureAwait(false);
            }

            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Branch {branchName} already exists, using existing branch.",
                            nameof(HasNewestPtcsJenkinsLibRule), RuleName, _branchName);
            return await client.Git.Commit.Get(repository.Owner.Login, repository.Name, existingBranch.Commit.Sha).ConfigureAwait(false);
        }

        /// <summary>
        /// This fix creates a pull request with updated Jenkinsfile
        /// </summary>
        /// <param name="client">Github client</param>
        /// <param name="repository">Repository to be fixed</param>
        private async Task Fix(IGitHubClient client, Repository repository)
        {
            // This method should be refactored when we have a better general idea how we want to fix things
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, performing auto fix.", nameof(HasNewestPtcsJenkinsLibRule), RuleName);
            var latest = await GetCommitAsBase(client, repository).ConfigureAwait(false);
            _logger.LogTrace("Latest commit {sha} with message {message}", latest.Sha, latest.Message);

            string fixedContent = await GetFixedContent(client, repository, latest.Sha).ConfigureAwait(false);
            if (fixedContent == null)
            {
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, Branch {branchName} already had latest version fix or it didn't have Jenkinsfile, skipping updating existing branch.",
                     nameof(HasNewestPtcsJenkinsLibRule), RuleName, _branchName);
                return;
            }

            var reference = await PushFix(client, repository, latest, fixedContent).ConfigureAwait(false);
            await CreateOrOpenPullRequest(_prTitle, client, repository, reference).ConfigureAwait(false);
        }

        private async Task<Reference> PushFix(IGitHubClient client, Repository repository, Commit latest, string jenkinsFile)
        {
            var oldTree = await client.Git.Tree.Get(repository.Owner.Login, repository.Name, latest.Sha).ConfigureAwait(false);
            var newTree = new NewTree
            {
                BaseTree = oldTree.Sha
            };

            BlobReference blobReference = await CreateBlob(client, repository, jenkinsFile).ConfigureAwait(false);
            var treeItem = new NewTreeItem()
            {
                Path = JenkinsFileName,
                Mode = FileMode,
                Type = TreeType.Blob,
                Sha = blobReference.Sha
            };
            newTree.Tree.Add(treeItem);

            var createdTree = await client.Git.Tree.Create(repository.Owner.Login, repository.Name, newTree).ConfigureAwait(false);
            var commit = new NewCommit($"Update {LibraryName} to latest version.", createdTree.Sha, new[] { latest.Sha });
            var commitResponse = await client.Git.Commit.Create(repository.Owner.Login, repository.Name, commit).ConfigureAwait(false);

            var refUpdate = new ReferenceUpdate(commitResponse.Sha);
            return await client.Git.Reference.Update(repository.Owner.Login, repository.Name, $"heads/{_branchName}", refUpdate).ConfigureAwait(false);
        }

        private async Task<string> GetFixedContent(IGitHubClient client, Repository repository, string branchName)
        {
            _logger.LogTrace("Rule {ruleClass} / {ruleName}: Retrieving fixed contents for JenkinsFile from branch {branch}", nameof(HasNewestPtcsJenkinsLibRule), RuleName, branchName);
            var jenkinsContent = await GetJenkinsFileContent(client, repository, branchName).ConfigureAwait(false);
            if (jenkinsContent == null)
            {
                _logger.LogWarning("Rule {ruleClass} / {ruleName}, no {filename} found, unable to fix.",
                    nameof(HasNewestPtcsJenkinsLibRule), RuleName, JenkinsFileName);
                return null;
            }
            var fixedContent = _regex.Replace(jenkinsContent.Content, $"'{LibraryName}@{_expectedVersion}'");
            return string.Equals(fixedContent, jenkinsContent.Content, StringComparison.InvariantCulture) ? null : fixedContent;
        }

        private async Task<RepositoryContent> GetJenkinsFileContent(IGitHubClient client, Repository repository, string branch)
        {
            _logger.LogTrace("Retrieving JenkinsFile for {repositoryName} from branch {branch}", repository.FullName, branch);

            // NOTE: rootContents doesn't contain actual contents, content is only fetched when we fetch the single file later.
            var rootContents = await GetContents(client, repository, branch).ConfigureAwait(false);

            var jenkinsFile = rootContents.FirstOrDefault(content => content.Name.Equals(JenkinsFileName, StringComparison.InvariantCultureIgnoreCase));
            if (jenkinsFile == null)
            {
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, No {jenkinsFileName} found in root.", nameof(HasNewestPtcsJenkinsLibRule), RuleName, JenkinsFileName);
                return null;
            }

            var matchingJenkinsFiles = await client.Repository.Content.GetAllContentsByRef(repository.Owner.Login, repository.Name, jenkinsFile.Name, branch).ConfigureAwait(false);
            return matchingJenkinsFiles[0];
        }

        private async Task<IReadOnlyList<RepositoryContent>> GetContents(IGitHubClient client, Repository repository, string branch)
        {
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
                 nameof(HasNewestPtcsJenkinsLibRule), RuleName, repository.Name, nameof(NotFoundException));
                return Array.Empty<RepositoryContent>();
            }
        }

        private ValidationResult OkResult()
        {
            return new ValidationResult(RuleName, $"Update {LibraryName} to newest version. Newest version can be found in https://github.com/protacon/{LibraryName}/releases", true, DoNothing);
        }

        private Task DoNothing(IGitHubClient client, Repository repository)
        {
            return Task.CompletedTask;
        }
    }
}
