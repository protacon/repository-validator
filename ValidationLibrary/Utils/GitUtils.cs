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

        /// <summary>
        /// Checks if this pull reqeest has a branch that is alive.
        /// 
        /// If branch is not alive, PR can't be opened.
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="pullRequest">Pull request</param>
        /// <returns>True if branch is alive, false if not.</returns>
        public async Task<bool> PullRequestHasLiveBranch(IGitHubClient client, PullRequest pullRequest)
        {
            if (client is null) throw new System.ArgumentNullException(nameof(client));
            if (pullRequest is null) throw new System.ArgumentNullException(nameof(pullRequest));

            _logger.LogTrace("Pull request head: {ref} {sha} from repostitory {owner}/{name}", pullRequest.Head.Ref, pullRequest.Head.Sha, pullRequest.Head.Repository.Owner.Login, pullRequest.Head.Repository.Name);
            var owner = pullRequest.Head.Repository.Owner.Login;
            var repositoryName = pullRequest.Head.Repository.Name;

            var branch = await client.Repository.Branch.Get(owner, repositoryName, pullRequest.Head.Ref).ConfigureAwait(false);
            _logger.LogTrace("Refence SHA {sha}", branch.Commit.Sha, branch.Commit.Ref);

            return string.Equals(branch.Commit.Sha, pullRequest.Head.Sha, System.StringComparison.InvariantCulture);
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