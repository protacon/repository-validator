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
    public class HasNewestPtcsJenkinsLibRule : IValidationRule
    {
        private const string JenkinsFileName = "Jenkinsfile";

        private const string LibraryName = "jenkins-ptcs-library";
        private const string FileMode = "100644";

        public string RuleName => $"Old {LibraryName}";

        private readonly Regex _regex = new Regex($@"'{LibraryName}@(\d+.\d+.\d+.*)'", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ILogger _logger;
        private string _expectedVersion;

        public HasNewestPtcsJenkinsLibRule(ILogger logger)
        {
            _logger = logger;
        }

        public async Task Init(IGitHubClient ghClient)
        {
            var versionFetcher = new ReleaseVersionFetcher(ghClient, "protacon", LibraryName);
            _expectedVersion = await versionFetcher.GetLatest();
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Newest version: {expectedVersion}", nameof(HasNewestPtcsJenkinsLibRule), RuleName, _expectedVersion);
        }

        /// <summary>
        /// This fix creates a pull request with updated Jenkinsfile
        /// </summary>
        /// <param name="client">Github client</param>
        /// <param name="repository">Repository to be fixed</param>
        /// <returns></returns>
        private async Task Fix(IGitHubClient client, Repository repository)
        {
            // This method should be refactored when we have a better general idea how we want to fix things
            var branchName = $"feature/{LibraryName}-update";
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, performing auto fix.", nameof(HasNewestPtcsJenkinsLibRule), RuleName);

            var jenkinsContent = await GetJenkinsFileContent(client, repository);
            if (jenkinsContent == null)
            {
                _logger.LogWarning("Rule {ruleClass} / {ruleName}, no {filename} found, unable to fix.",
                    nameof(HasNewestPtcsJenkinsLibRule), RuleName, JenkinsFileName);
                return;
            }
            var fixedContent = _regex.Replace(jenkinsContent.Content, $"'{LibraryName}@{_expectedVersion}'");

            var branches = await client.Repository.Branch.GetAll(repository.Owner.Login, repository.Name);
            if (branches.Any(branch => branch.Name == branchName))
            {
                _logger.LogInformation("Rule {ruleClass} / {ruleName}, Branch {branchName} already exists, skipping fix.",
                     nameof(HasNewestPtcsJenkinsLibRule), RuleName, branchName);
                return;
            }
            var branchReference = await client.Git.Reference.CreateBranch(repository.Owner.Login, repository.Name, branchName);
            var master = await client.Git.Reference.Get(repository.Owner.Login, repository.Name, "heads/master");

            var latest = await client.Git.Commit.Get(repository.Owner.Login, repository.Name, branchReference.Object.Sha);
            _logger.LogTrace("Latest commit with message {a}", latest.Message);

            var oldTree = await client.Git.Tree.Get(repository.Owner.Login, repository.Name, latest.Sha);
            var newTree = new NewTree
            {
                BaseTree = oldTree.Sha
            };

            BlobReference blobReference = await CreateBlob(client, repository, fixedContent);

            var treeItem = new NewTreeItem()
            {
                Path = JenkinsFileName,
                Mode = FileMode,
                Type = TreeType.Blob,
                Sha = blobReference.Sha
            };
            newTree.Tree.Add(treeItem);

            var createdTree = await client.Git.Tree.Create(repository.Owner.Login, repository.Name, newTree);
            var commit = new NewCommit($"Update {LibraryName} to latest versios.", createdTree.Sha, new[] { latest.Sha });
            var commitResponse = await client.Git.Commit.Create(repository.Owner.Login, repository.Name, commit);

            var refUpdate = new ReferenceUpdate(commitResponse.Sha);
            await client.Git.Reference.Update(repository.Owner.Login, repository.Name, $"heads/{branchName}", refUpdate);

            var pullRequest = new NewPullRequest($"[Automatic Validation] Update {LibraryName} to latest version.", branchReference.Ref, master.Ref)
            {
                Body = "This Pull Request was created by [repository validator](https://github.com/protacon/repository-validator)." + Environment.NewLine +
                        Environment.NewLine +
                        "To prevent automatic validation, see documentation from [repository validator](https://github.com/protacon/repository-validator)." + Environment.NewLine +
                        Environment.NewLine +
                        "DO NOT change the name of this Pull Request. Names are used to identify the Pull Requests created by automation." + Environment.NewLine
            };
            await client.PullRequest.Create(repository.Owner.Login, repository.Name, pullRequest);
        }

        private async Task<BlobReference> CreateBlob(IGitHubClient client, Repository repository, string fixedContent)
        {
            var blob = new NewBlob()
            {
                Content = fixedContent,
                Encoding = EncodingType.Utf8
            };
            var blobReference = await client.Git.Blob.Create(repository.Owner.Login, repository.Name, blob);
            _logger.LogTrace("Created blob SHA {sha}", blobReference.Sha);
            return blobReference;
        }


        private async Task<RepositoryContent> GetJenkinsFileContent(IGitHubClient client, Repository repository)
        {
            _logger.LogTrace("Retrieving JenkinsFile for {repositoryName}", repository.FullName);

            // NOTE: rootContents doesn't contain actual contents, content is only fetched when we fetch the single file later.
            var rootContents = await GetContents(client, repository);

            var jenkinsFile = rootContents.FirstOrDefault(content => content.Name.Equals(JenkinsFileName, StringComparison.InvariantCultureIgnoreCase));
            if (jenkinsFile == null)
            {
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, No {jenkinsFileName} found in root.", nameof(HasNewestPtcsJenkinsLibRule), RuleName, JenkinsFileName);
                return null;
            }

            var matchingJenkinsFiles = await client.Repository.Content.GetAllContents(repository.Owner.Login, repository.Name, jenkinsFile.Name);
            return matchingJenkinsFiles.FirstOrDefault();
        }

        public async Task<ValidationResult> IsValid(IGitHubClient client, Repository repository)
        {
            _logger.LogTrace("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}", nameof(HasNewestPtcsJenkinsLibRule), RuleName, repository.FullName);

            var jenkinsContent = await GetJenkinsFileContent(client, repository);
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
                return OkResult();
            }

            var group = match.Groups.OfType<Group>().LastOrDefault();
            if (group == null)
            {
                return OkResult();
            }

            return new ValidationResult(RuleName, $"Update {LibraryName} to newest version.", group.Value == _expectedVersion, Fix);
        }

        private async Task<IReadOnlyList<RepositoryContent>> GetContents(IGitHubClient client, Repository repository)
        {
            try
            {
                return await client.Repository.Content.GetAllContents(repository.Owner.Login, repository.Name);
            }
            catch (Octokit.NotFoundException exception)
            {
                /* 
                 * NOTE: Repository that was just created (empty repository) doesn't have content this causes
                 * Octokit.NotFoundException. This same thing would probably be throw if the whole repository
                 * was missing, but we don't care for that case (no point to validate if repository doesn't exist.)
                 */
                _logger.LogWarning(exception, "Rule {ruleClass} / {ruleName}, Repository {repositoryName} caused {exceptionClass}. This may be a new repository, but if this persists, repository should be removed.",
                 nameof(HasNewestPtcsJenkinsLibRule), RuleName, repository.Name, nameof(Octokit.NotFoundException));
                return new RepositoryContent[0];
            }
        }

        private ValidationResult OkResult()
        {
            return new ValidationResult(RuleName, $"Update {LibraryName} to newest version. Newest version can be found in https://github.com/protacon/{LibraryName}/releases", true, DoNothing);
        }

        private Task DoNothing(IGitHubClient client, Repository repository)
        {
            return Task.FromResult(0);
        }
    }
}