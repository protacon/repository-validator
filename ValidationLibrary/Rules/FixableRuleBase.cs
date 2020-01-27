using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;
using ValidationLibrary.Utils;

namespace ValidationLibrary.Rules
{
    public abstract class FixableRuleBase<T> : IValidationRule where T : IValidationRule
    {
        public abstract string RuleName { get; }
        protected const string MainBranch = "master";
        protected static string FormatPrTitle(string message) => $"[Automatic Validation] {message}";
        private readonly ILogger<FixableRuleBase<T>> _logger;
        private readonly GitUtils _gitUtils;
        private string _prTitle;

        public FixableRuleBase(ILogger<FixableRuleBase<T>> logger, GitUtils gitUtils)
        {
            _logger = logger;
            _gitUtils = gitUtils;
        }

        public abstract Task Init(IGitHubClient ghClient);
        public abstract Task<ValidationResult> IsValid(IGitHubClient client, Repository repository);
        protected abstract Task<Commit> GetCommitAsBase(IGitHubClient client, Repository repository);

        protected async Task CreateOrOpenPullRequest(string prTitle, IGitHubClient client, Repository repository, Reference latest)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (latest == null) throw new ArgumentNullException(nameof(latest));

            _prTitle = prTitle;

            var pullRequest = new PullRequestRequest
            {
                State = ItemStateFilter.All
            };

            var pullRequests = await client.PullRequest.GetAllForRepository(repository.Owner.Login, repository.Name, pullRequest).ConfigureAwait(false);
            var openPullRequests = pullRequests.Where(pr => pr.Title == FormatPrTitle(_prTitle) && pr.State == ItemState.Open);
            if (openPullRequests.Any())
            {
                _logger.LogInformation("Rule {ruleClass} / {ruleName}, Open pull request already exists. Skipping.", nameof(T), RuleName);
                return;
            }

            var closed = pullRequests.FirstOrDefault(pr => pr.Title == FormatPrTitle(_prTitle) && pr.State == ItemState.Closed && !pr.Merged);
            if (closed != null && await _gitUtils.PullRequestHasLiveBranch(client, closed).ConfigureAwait(false))
            {
                _logger.LogInformation("Rule {ruleClass} / {ruleName}, Closed pull request with active branch found. Reopening pull request.");
                await OpenOldPullRequest(client, repository, closed).ConfigureAwait(false);
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

        private async Task OpenOldPullRequest(IGitHubClient client, Repository repository, PullRequest oldPullRequest)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}: Opening pull request #{number}", nameof(T), RuleName, oldPullRequest.Number);
            var pullRequest = new PullRequestUpdate()
            {
                Title = FormatPrTitle(_prTitle),
                State = ItemState.Open,
                Body = oldPullRequest.Body,
                Base = MainBranch
            };
            await client.PullRequest.Update(repository.Owner.Login, repository.Name, oldPullRequest.Number, pullRequest).ConfigureAwait(false);
        }

        private async Task CreateNewPullRequest(IGitHubClient client, Repository repository, Reference latest)
        {
            var master = await client.Git.Reference.Get(repository.Owner.Login, repository.Name, $"heads/{MainBranch}").ConfigureAwait(false);
            var pullRequest = new NewPullRequest(FormatPrTitle(_prTitle), latest.Ref, master.Ref)
            {
                Body = "This Pull Request was created by [repository validator](https://github.com/protacon/repository-validator)." + Environment.NewLine +
                        Environment.NewLine +
                        "To prevent automatic validation, see documentation from [repository validator](https://github.com/protacon/repository-validator)." + Environment.NewLine +
                        Environment.NewLine +
                        "DO NOT change the name of this Pull Request. Names are used to identify the Pull Requests created by automation."
            };
            await client.PullRequest.Create(repository.Owner.Login, repository.Name, pullRequest).ConfigureAwait(false);
        }
    }
}