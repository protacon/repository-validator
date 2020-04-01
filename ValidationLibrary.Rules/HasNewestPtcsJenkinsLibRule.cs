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
    /// 
    /// Newest version should be used for better support and possible bug fixes. With major releases the pipeline might break
    /// if older version is used.
    /// 
    /// Automatic fix for this rule creates a Pull Request which updates jenkins-ptcs-library to latest version.
    /// 
    /// When to ignore
    /// 
    ///  * Repository is testing some specific version of jenkins-ptcs-library
    /// </summary>
    public class HasNewestPtcsJenkinsLibRule : FixableRuleBase<HasNewestPtcsJenkinsLibRule>
    {
        protected override string PullRequestBody =>
                        "This Pull Request was created by [repository validator](https://github.com/by-pinja/repository-validator)." + Environment.NewLine +
                        Environment.NewLine +
                        "To prevent automatic validation, see documentation from [repository validator](https://github.com/by-pinja/repository-validator)." + Environment.NewLine +
                        Environment.NewLine +
                        "DO NOT change the name of this Pull Request. Names are used to identify the Pull Requests created by automation." + Environment.NewLine +
                        Environment.NewLine +
                        "The latest release can be found here: " + _latestReleaseUrl + Environment.NewLine;
        private const string LibraryName = "jenkins-ptcs-library";
        private const string JenkinsFileName = "Jenkinsfile";
        private const string FileMode = "100644";
        private readonly string _branchName = $"feature/{LibraryName}-update";
        private readonly Regex _regex = new Regex($@"^(library)[\s][""']{LibraryName}@(\d+.\d+.\d+.*)[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private readonly ILogger<HasNewestPtcsJenkinsLibRule> _logger;
        private readonly GitUtils _gitUtils;
        private string _expectedVersion;
        private string _latestReleaseUrl;

        public HasNewestPtcsJenkinsLibRule(ILogger<HasNewestPtcsJenkinsLibRule> logger, GitUtils gitUtils) : base(logger, gitUtils, $"Old {LibraryName}", $"[Automatic Validation] Update {LibraryName} to latest version")
        {
            _logger = logger;
            _gitUtils = gitUtils;
        }

        public override async Task Init(IGitHubClient ghClient)
        {
            var versionFetcher = new ReleaseVersionFetcher(ghClient, "by-pinja", LibraryName);
            var release = await versionFetcher.GetLatest().ConfigureAwait(false);
            _expectedVersion = release.TagName;
            _latestReleaseUrl = release.HtmlUrl;
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Newest version: {expectedVersion}", nameof(HasNewestPtcsJenkinsLibRule), RuleName, _expectedVersion);
        }

        public override async Task<ValidationResult> IsValid(IGitHubClient client, Repository repository)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (repository is null) throw new ArgumentNullException(nameof(repository));

            _logger.LogTrace("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}", nameof(HasNewestPtcsJenkinsLibRule), RuleName, repository.FullName);

            var isValid = IsValid(await GetJenkinsFileContent(client, repository, MainBranch).ConfigureAwait(false));

            return new ValidationResult(RuleName, $"Update {LibraryName} to newest version. Newest version can be found in https://github.com/by-pinja/{LibraryName}/releases",
                isValid, Fix);
        }

        public override Dictionary<string, string> GetConfiguration()
        {
            return new Dictionary<string, string>
            {
                { "ClassName", nameof(HasNewestPtcsJenkinsLibRule) },
                { "PullRequestTitle", RuleName },
                { "LatestJenkinsLibraryVersion", _expectedVersion },
                { "MainBranch", MainBranch }
            };
        }

        private bool IsValid(RepositoryContent jenkinsContent)
        {
            if (jenkinsContent == null)
            {
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, no {jenkinsFileName} found. Skipping.", nameof(HasNewestPtcsJenkinsLibRule), RuleName, JenkinsFileName);
                return true;
            }
            var content = jenkinsContent.Content;

            var matches = _regex.Matches(content);
            var match = matches.OfType<Match>().FirstOrDefault();
            if (match == null)
            {
                _logger.LogTrace("Rule {ruleClass} / {ruleName}, no jenkins-ptcs-library matches found. Skipping.", nameof(HasNewestPtcsJenkinsLibRule), RuleName, JenkinsFileName);
                return true;
            }

            var group = match.Groups.OfType<Group>().LastOrDefault();
            if (group == null)
            {
                _logger.LogTrace("Rule {ruleClass} / {ruleName}, no jenkins-ptcs-library groups found. Skipping.", nameof(HasNewestPtcsJenkinsLibRule), RuleName, JenkinsFileName);
                return true;
            }

            return group.Value == _expectedVersion;
        }

        /// <summary>
        /// This fix creates a pull request with updated Jenkinsfile
        /// </summary>
        /// <param name="client">Github client</param>
        /// <param name="repository">Repository to be fixed</param>
        private async Task Fix(IGitHubClient client, Repository repository)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            // This method should be refactored when we have a better general idea how we want to fix things
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, performing auto fix.", nameof(HasNewestPtcsJenkinsLibRule), RuleName);
            var latest = await GetCommitAsBase(_branchName, client, repository).ConfigureAwait(false);
            _logger.LogTrace("Latest commit {sha} with message {message}", latest.Sha, latest.Message);
            var jenkinsContent = await GetJenkinsFileContent(client, repository, latest.Sha).ConfigureAwait(false);
            if (IsValid(jenkinsContent))
            {
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, Branch {branchName} already had latest version fix or it didn't have Jenkinsfile, skipping updating existing branch.",
                    nameof(HasNewestPtcsJenkinsLibRule), RuleName, _branchName);
                return;
            }
            else
            {
                var fixedContent = _regex.Replace(jenkinsContent.Content, $"library '{LibraryName}@{_expectedVersion}'");
                var reference = await PushFix(client, repository, latest, fixedContent).ConfigureAwait(false);

                await CreatePullRequestIfNeeded(client, repository, reference).ConfigureAwait(false);
            }
        }

        private async Task<Reference> PushFix(IGitHubClient client, Repository repository, Commit latest, string jenkinsFile)
        {
            var oldTree = await client.Git.Tree.Get(repository.Owner.Login, repository.Name, latest.Sha).ConfigureAwait(false);
            var newTree = new NewTree
            {
                BaseTree = oldTree.Sha
            };

            var blobReference = await CreateBlob(client, repository, jenkinsFile).ConfigureAwait(false);
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
    }
}
