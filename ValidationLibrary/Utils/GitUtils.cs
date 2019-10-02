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
            _logger.LogTrace("Pull request head: {ref} {sha} from repostitory {owner}/{name}", pullRequest.Head.Ref, pullRequest.Head.Sha, pullRequest.Head.Repository.Owner.Login, pullRequest.Head.Repository.Name);
            var owner = pullRequest.Head.Repository.Owner.Login;
            var repositoryName = pullRequest.Head.Repository.Name;

            var branch = await client.Repository.Branch.Get(owner, repositoryName, pullRequest.Head.Ref);
            _logger.LogTrace("Refence SHA {sha}", branch.Commit.Sha, branch.Commit.Ref);

            return string.Equals(branch.Commit.Sha, pullRequest.Head.Sha, System.StringComparison.InvariantCulture);
        }
    }
}