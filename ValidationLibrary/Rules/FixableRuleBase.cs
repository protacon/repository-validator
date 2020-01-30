using System;
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
        protected static string FormatPrTitle(string message) => $"[Automatic Validation] {message}";
        private readonly ILogger<FixableRuleBase<T>> _logger;
        private readonly GitUtils _gitUtils;

        public FixableRuleBase(ILogger<FixableRuleBase<T>> logger, GitUtils gitUtils)
        {
            _logger = logger;
            _gitUtils = gitUtils;
        }

        public abstract Task Init(IGitHubClient ghClient);
        public abstract Task<ValidationResult> IsValid(IGitHubClient client, Repository repository);
        protected abstract Task Fix(IGitHubClient client, Repository repository);

        protected async Task CreateOrOpenPullRequest(string prTitle, IGitHubClient client, Repository repository, Reference latest)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (latest == null) throw new ArgumentNullException(nameof(latest));

            var pullRequest = new PullRequestRequest
            {
                State = ItemStateFilter.All
            };

            var pullRequests = await client.PullRequest.GetAllForRepository(repository.Owner.Login, repository.Name, pullRequest).ConfigureAwait(false);
            var openPullRequests = pullRequests.Where(pr => pr.Title == FormatPrTitle(prTitle) && pr.State == ItemState.Open);
            if (openPullRequests.Any())
            {
                _logger.LogInformation("Rule {ruleClass} / {ruleName}, Open pull request already exists. Skipping.", typeof(T).Name, RuleName);
                return;
            }

            var closed = pullRequests.FirstOrDefault(pr => pr.Title == FormatPrTitle(prTitle) && pr.State == ItemState.Closed && !pr.Merged);
            if (closed != null && await _gitUtils.PullRequestHasLiveBranch(client, closed).ConfigureAwait(false))
            {
                _logger.LogInformation("Rule {ruleClass} / {ruleName}, Closed pull request with active branch found. Reopening pull request.");
                await OpenOldPullRequest(prTitle, client, repository, closed).ConfigureAwait(false);
                return;
            }

            await CreateNewPullRequest(prTitle, client, repository, latest).ConfigureAwait(false);
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

        private async Task OpenOldPullRequest(string prTitle, IGitHubClient client, Repository repository, PullRequest oldPullRequest)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}: Opening pull request #{number}", typeof(T).Name, RuleName, oldPullRequest.Number);
            var pullRequest = new PullRequestUpdate()
            {
                Title = FormatPrTitle(prTitle),
                State = ItemState.Open,
                Body = oldPullRequest.Body,
                Base = MainBranch
            };
            await client.PullRequest.Update(repository.Owner.Login, repository.Name, oldPullRequest.Number, pullRequest).ConfigureAwait(false);
        }

        private async Task CreateNewPullRequest(string prTitle, IGitHubClient client, Repository repository, Reference latest)
        {
            var master = await client.Git.Reference.Get(repository.Owner.Login, repository.Name, $"heads/{MainBranch}").ConfigureAwait(false);
            var pullRequest = new NewPullRequest(FormatPrTitle(prTitle), latest.Ref, master.Ref)
            {
                Body = PullRequestBody
            };
            await client.PullRequest.Create(repository.Owner.Login, repository.Name, pullRequest).ConfigureAwait(false);
        }
    }
}