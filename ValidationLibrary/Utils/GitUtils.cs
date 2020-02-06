using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace ValidationLibrary.Utils
{
    /// <summary>
    /// Miscellaneous Git utils
    /// </summary>
    public class GitUtils
    {
        private readonly ILogger<GitUtils> _logger;

        public GitUtils(ILogger<GitUtils> logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public async Task<bool> HasOpenPullRequest(IGitHubClient client, Repository repository, Reference reference)
        {
            if (client is null) throw new System.ArgumentNullException(nameof(client));
            if (repository is null) throw new System.ArgumentNullException(nameof(repository));
            if (reference is null) throw new System.ArgumentNullException(nameof(reference));

            _logger.LogTrace("Checking if there is existing open pull request in repository {repositoryName} for reference '{pullRequest}'.",
                repository.FullName, reference.Ref);

            var openRequests = new PullRequestRequest
            {
                State = ItemStateFilter.Open
            };
            var pullRequests = await client.Repository.PullRequest.GetAllForRepository(repository.Owner.Login, repository.Name, openRequests).ConfigureAwait(false);

            return pullRequests.Any(pr => $"refs/heads/{pr.Head.Ref}" == reference.Ref);
        }

        public async Task<PullRequest> GetClosedNonMergedPullRequestOrNull(IGitHubClient client, Repository repository, string pullRequestTitle)
        {
            if (client is null) throw new System.ArgumentNullException(nameof(client));
            if (repository is null) throw new System.ArgumentNullException(nameof(repository));
            if (string.IsNullOrWhiteSpace(pullRequestTitle)) throw new System.ArgumentException("Pull request title must be defined", nameof(pullRequestTitle));

            _logger.LogTrace("Checking if there is existing closed pull request in repository {repositoryName} for pull request '{pullRequest}'.",
                repository.FullName, pullRequestTitle);

            var closedRequests = new PullRequestRequest
            {
                State = ItemStateFilter.Closed
            };
            var pullRequests = await client.Repository.PullRequest.GetAllForRepository(repository.Owner.Login, repository.Name, closedRequests).ConfigureAwait(false);

            return pullRequests.FirstOrDefault(pr => !pr.Merged && pr.Title == pullRequestTitle);
        }
    }
}