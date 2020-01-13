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
        private readonly string _branchName = $"feature/{LibraryName}-update";
        private readonly string _pullRequestTitle = $"[Automatic Validation] Update {LibraryName} to latest version.";
        private const string FileMode = "100644";
        private const string MainBranch = "master";

        public string RuleName => $"Old {LibraryName}";

        private readonly Regex _regex = new Regex($@"[""']{LibraryName}@(\d+.\d+.\d+.*)[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ILogger<HasNewestPtcsJenkinsLibRule> _logger;
        private readonly GitUtils _gitUtils;
        private string _expectedVersion;
        private string _latestReleaseUrl;

        public HasNewestPtcsJenkinsLibRule(ILogger<HasNewestPtcsJenkinsLibRule> logger, GitUtils gitUtils)
        {
            _logger = logger;
            _gitUtils = gitUtils;
        }

        public async Task Init(IGitHubClient ghClient)
        {
            var versionFetcher = new ReleaseVersionFetcher(ghClient, "protacon", LibraryName);
            var release = await versionFetcher.GetLatest();
            _expectedVersion = release.TagName;
            _latestReleaseUrl = release.HtmlUrl;
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Newest version: {expectedVersion}", nameof(HasNewestPtcsJenkinsLibRule), RuleName, _expectedVersion);
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
            var latest = await GetCommitAsBase(client, repository);
            _logger.LogTrace("Latest commit {sha} with message {message}", latest.Sha, latest.Message);

            string fixedContent = await GetFixedContent(client, repository, latest.Sha);
            if (fixedContent == null)
            {
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, Branch {branchName} already had latest version fix or it didn't have Jenkinsfile, skipping updating existing branch.",
                     nameof(HasNewestPtcsJenkinsLibRule), RuleName, _branchName);
                return;
            }

            var reference = await PushFix(client, repository, latest, fixedContent);
            var pullRequest = new PullRequestRequest
            {
                State = ItemStateFilter.All
            };
            var pullRequests = await client.PullRequest.GetAllForRepository(repository.Owner.Login, repository.Name, pullRequest);
            var openPullRequests = pullRequests.Where(pr => pr.Title == _pullRequestTitle && pr.State == ItemState.Open);
            if (openPullRequests.Any())
            {
                _logger.LogInformation("Rule {ruleClass} / {ruleName}, Open pull request already exists. Skipping.", nameof(HasNewestPtcsJenkinsLibRule), RuleName);
                return;
            }

            var closed = pullRequests.FirstOrDefault(pr => pr.Title == _pullRequestTitle && pr.State == ItemState.Closed && !pr.Merged);
            if (closed != null && await _gitUtils.PullRequestHasLiveBranch(client, closed))
            {
                _logger.LogInformation("Rule {ruleClass} / {ruleName}, Closed pull request with active branch found. Reopening pull request.", nameof(HasNewestPtcsJenkinsLibRule), RuleName);
                await OpenOldPullRequest(client, repository, closed);
                return;
            }

            await CreateNewPullRequest(client, repository, reference);
        }

        /// <summary>
        /// This takes either the latest commit from master or latest from updated branch if it exists.
        /// </summary>
        private async Task<Commit> GetCommitAsBase(IGitHubClient client, Repository repository)
        {
            var branches = await client.Repository.Branch.GetAll(repository.Owner.Login, repository.Name);
            var existingBranch = branches.FirstOrDefault(branch => branch.Name == _branchName);
            if (existingBranch == null)
            {
                _logger.LogInformation("Rule {ruleClass} / {ruleName}, Branch {branchName} did not exists, creating branch.",
                     nameof(HasNewestPtcsJenkinsLibRule), RuleName, _branchName);
                var branchReference = await client.Git.Reference.CreateBranch(repository.Owner.Login, repository.Name, _branchName);
                return await client.Git.Commit.Get(repository.Owner.Login, repository.Name, branchReference.Object.Sha);
            }

            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Branch {branchName} already exists, using existing branch.",
                    nameof(HasNewestPtcsJenkinsLibRule), RuleName, _branchName);
            return await client.Git.Commit.Get(repository.Owner.Login, repository.Name, existingBranch.Commit.Sha);
        }

        private async Task<Reference> PushFix(IGitHubClient client, Repository repository, Commit latest, string jenkinsFile)
        {
            var oldTree = await client.Git.Tree.Get(repository.Owner.Login, repository.Name, latest.Sha);
            var newTree = new NewTree
            {
                BaseTree = oldTree.Sha
            };

            BlobReference blobReference = await CreateBlob(client, repository, jenkinsFile);
            var treeItem = new NewTreeItem()
            {
                Path = JenkinsFileName,
                Mode = FileMode,
                Type = TreeType.Blob,
                Sha = blobReference.Sha
            };
            newTree.Tree.Add(treeItem);

            var createdTree = await client.Git.Tree.Create(repository.Owner.Login, repository.Name, newTree);
            var commit = new NewCommit($"Update {LibraryName} to latest version.", createdTree.Sha, new[] { latest.Sha });
            var commitResponse = await client.Git.Commit.Create(repository.Owner.Login, repository.Name, commit);

            var refUpdate = new ReferenceUpdate(commitResponse.Sha);
            return await client.Git.Reference.Update(repository.Owner.Login, repository.Name, $"heads/{_branchName}", refUpdate);
        }

        private async Task<string> GetFixedContent(IGitHubClient client, Repository repository, string branchName)
        {
            _logger.LogTrace("Rule {ruleClass} / {ruleName}: Retrieving fixed contents for JenkinsFile from branch {branch}", nameof(HasNewestPtcsJenkinsLibRule), RuleName, branchName);
            var jenkinsContent = await GetJenkinsFileContent(client, repository, branchName);
            if (jenkinsContent == null)
            {
                _logger.LogWarning("Rule {ruleClass} / {ruleName}, no {filename} found, unable to fix.",
                    nameof(HasNewestPtcsJenkinsLibRule), RuleName, JenkinsFileName);
                return null;
            }
            var fixedContent = _regex.Replace(jenkinsContent.Content, $"'{LibraryName}@{_expectedVersion}'");
            return string.Equals(fixedContent, jenkinsContent.Content, StringComparison.InvariantCulture) ? null : fixedContent;
        }

        private async Task OpenOldPullRequest(IGitHubClient client, Repository repository, PullRequest oldPullRequest)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}: Opening pull request #{number}", nameof(HasNewestPtcsJenkinsLibRule), RuleName, oldPullRequest.Number);
            var pullRequest = new PullRequestUpdate()
            {
                Title = _pullRequestTitle,
                State = ItemState.Open,
                Body = oldPullRequest.Body,
                Base = MainBranch
            };
            await client.PullRequest.Update(repository.Owner.Login, repository.Name, oldPullRequest.Number, pullRequest);
        }

        private async Task CreateNewPullRequest(IGitHubClient client, Repository repository, Reference latest)
        {
            var master = await client.Git.Reference.Get(repository.Owner.Login, repository.Name, $"heads/{MainBranch}");
            var pullRequest = new NewPullRequest(_pullRequestTitle, latest.Ref, master.Ref)
            {
                Body = "This Pull Request was created by [repository validator](https://github.com/protacon/repository-validator)." + Environment.NewLine +
                        Environment.NewLine +
                        "To prevent automatic validation, see documentation from [repository validator](https://github.com/protacon/repository-validator)." + Environment.NewLine +
                        Environment.NewLine +
                        "DO NOT change the name of this Pull Request. Names are used to identify the Pull Requests created by automation." + Environment.NewLine +
                        Environment.NewLine +
                        "The latest release can be found here: " + _latestReleaseUrl + Environment.NewLine
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


        private async Task<RepositoryContent> GetJenkinsFileContent(IGitHubClient client, Repository repository, string branch)
        {
            _logger.LogTrace("Retrieving JenkinsFile for {repositoryName} from branch {branch}", repository.FullName, branch);

            // NOTE: rootContents doesn't contain actual contents, content is only fetched when we fetch the single file later.
            var rootContents = await GetContents(client, repository, branch);

            var jenkinsFile = rootContents.FirstOrDefault(content => content.Name.Equals(JenkinsFileName, StringComparison.InvariantCultureIgnoreCase));
            if (jenkinsFile == null)
            {
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, No {jenkinsFileName} found in root.", nameof(HasNewestPtcsJenkinsLibRule), RuleName, JenkinsFileName);
                return null;
            }

            var matchingJenkinsFiles = await client.Repository.Content.GetAllContentsByRef(repository.Owner.Login, repository.Name, jenkinsFile.Name, branch);
            return matchingJenkinsFiles.FirstOrDefault();
        }

        public async Task<ValidationResult> IsValid(IGitHubClient client, Repository repository)
        {
            _logger.LogTrace("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}", nameof(HasNewestPtcsJenkinsLibRule), RuleName, repository.FullName);

            var jenkinsContent = await GetJenkinsFileContent(client, repository, MainBranch);
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

        private async Task<IReadOnlyList<RepositoryContent>> GetContents(IGitHubClient client, Repository repository, string branch)
        {
            try
            {
                return await client.Repository.Content.GetAllContentsByRef(repository.Owner.Login, repository.Name, branch);
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
