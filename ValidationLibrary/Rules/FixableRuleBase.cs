using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Helpers;
using ValidationLibrary.Utils;

namespace ValidationLibrary.Rules
{
    public abstract class FixableRuleBase<T> : IValidationRule where T : IValidationRule
    {
        public abstract string RuleName { get; }
        protected abstract string PullRequestBody { get; }
        protected const string MainBranch = "master";
        private readonly ILogger<FixableRuleBase<T>> _logger;
        private readonly GitUtils _gitUtils;
        private readonly string _pullRequestTitle;

        public FixableRuleBase(ILogger<FixableRuleBase<T>> logger, GitUtils gitUtils, string pullRequestTitle)
        {
            _logger = logger;
            _gitUtils = gitUtils;
            _pullRequestTitle = pullRequestTitle;
        }

        public abstract Task Init(IGitHubClient ghClient);
        public abstract Task<ValidationResult> IsValid(IGitHubClient client, Repository repository);
        protected abstract Task Fix(IGitHubClient client, Repository repository);

        protected async Task CreatePullRequestIfNeeded(IGitHubClient client, Repository repository, Reference latest)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (latest == null) throw new ArgumentNullException(nameof(latest));

            _logger.LogTrace("Rule {ruleClass} / {ruleName}: Trying to create or open a pull request for reference {ref} with name '{pullRequest}'",
                    typeof(T).Name, RuleName, latest.Ref, _pullRequestTitle);

            if (await _gitUtils.HasOpenPullRequest(client, repository, latest).ConfigureAwait(false))
            {
                _logger.LogInformation("Rule {ruleClass} / {ruleName}, Open pull request already exists for '{pullRequest}'. Skipping...", typeof(T).Name, RuleName, latest.Ref);
                return;
            }

            await CreateNewPullRequest(client, repository, latest).ConfigureAwait(false);
        }

        protected async Task<BlobReference> CreateBlob(IGitHubClient client, Repository repository, string fixedContent)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            var blob = new NewBlob()
            {
                Content = fixedContent,
                Encoding = EncodingType.Utf8
            };
            var blobReference = await client.Git.Blob.Create(repository.Owner.Login, repository.Name, blob).ConfigureAwait(false);
            _logger.LogTrace("Created blob SHA {sha}", blobReference.Sha);
            return blobReference;
        }

        protected async Task<Commit> GetCommitAsBase(string branchName, IGitHubClient client, Repository repository)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (repository == null) throw new ArgumentNullException(nameof(client));

            var branches = await client.Repository.Branch.GetAll(repository.Owner.Login, repository.Name).ConfigureAwait(false);
            var existingBranch = branches.FirstOrDefault(branch => branch.Name == branchName);
            if (existingBranch == null)
            {
                _logger.LogInformation("Rule {ruleClass} / {ruleName}, Branch {branchName} did not exists, creating branch.",
                     typeof(T).Name, RuleName, branchName);
                var branchReference = await client.Git.Reference.CreateBranch(repository.Owner.Login, repository.Name, branchName).ConfigureAwait(false);
                return await client.Git.Commit.Get(repository.Owner.Login, repository.Name, branchReference.Object.Sha).ConfigureAwait(false);
            }

            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Branch {branchName} already exists, using existing branch.",
                            typeof(T).Name, RuleName, branchName);
            return await client.Git.Commit.Get(repository.Owner.Login, repository.Name, existingBranch.Commit.Sha).ConfigureAwait(false);
        }

        private async Task CreateNewPullRequest(IGitHubClient client, Repository repository, Reference latest)
        {
            var master = await client.Git.Reference.Get(repository.Owner.Login, repository.Name, $"heads/{MainBranch}").ConfigureAwait(false);
            var pullRequest = new NewPullRequest(_pullRequestTitle, latest.Ref, master.Ref)
            {
                Body = PullRequestBody
            };
            await client.PullRequest.Create(repository.Owner.Login, repository.Name, pullRequest).ConfigureAwait(false);
        }

        protected async Task<IReadOnlyList<RepositoryContent>> GetContents(IGitHubClient client, Repository repository, string branch)
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
                 nameof(HasNewestPtcsJenkinsLibRule), RuleName, repository.Name, nameof(NotFoundException));
                return Array.Empty<RepositoryContent>();
            }
        }
    }
}
