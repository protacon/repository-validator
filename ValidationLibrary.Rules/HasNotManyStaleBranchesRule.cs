using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// This rule checks that repository does not have too many stale branches.
    /// This rule returns invalid if there are 10 or more branches with latest commit over
    /// 90 days ago.
    /// 
    /// Stale branches should be removed to make it easier for new developers to see which
    /// branches are actually related to current development etc.
    /// 
    /// To make branch management easier, use GitHub repository settings to protect relevant branches and
    /// automatically delete merged branches.
    /// 
    /// There is no automatic fix for this rule.
    /// 
    /// When to ignore
    ///  * Repository doesn't utilize feature branches
    ///  * Repository is migrated from SVN and has branches instead of tags
    /// </summary>
    public class HasNotManyStaleBranchesRule : IValidationRule
    {
        public string RuleName => "Stale branches";
        private const int StaleCountLimit = 10;

        private DateTimeOffset _staleThreshold = DateTimeOffset.UtcNow - TimeSpan.FromDays(90);
        private readonly ILogger<HasNotManyStaleBranchesRule> _logger;

        public HasNotManyStaleBranchesRule(ILogger<HasNotManyStaleBranchesRule> logger)
        {
            _logger = logger;
        }

        public Task Init(IGitHubClient ghClient)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Initialized", nameof(HasNotManyStaleBranchesRule), RuleName);
            return Task.CompletedTask;
        }

        public async Task<ValidationResult> IsValid(IGitHubClient client, Repository gitHubRepository)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (gitHubRepository is null)
            {
                throw new ArgumentNullException(nameof(gitHubRepository));
            }

            _logger.LogTrace("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}", nameof(HasNotManyStaleBranchesRule), RuleName, gitHubRepository.FullName);

            var branches = await client.Repository.Branch.GetAll(gitHubRepository.Owner.Login, gitHubRepository.Name).ConfigureAwait(false);

            var staleCommitsMap = new Dictionary<string, bool>();
            var staleCount = 0;

            foreach (var branch in branches)
            {
                if (!staleCommitsMap.ContainsKey(branch.Commit.Sha))
                {
                    var commit = await client.Repository.Commit.Get(gitHubRepository.Id, branch.Commit.Sha).ConfigureAwait(false);
                    staleCommitsMap[branch.Commit.Sha] = commit.Commit.Author.Date < _staleThreshold;
                }

                if (staleCommitsMap[branch.Commit.Sha]) staleCount++;
                if (staleCount >= StaleCountLimit) break;
            }

            _logger.LogDebug("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}. Not too many stale branches: {isValid}", nameof(HasNotManyStaleBranchesRule), RuleName, gitHubRepository.FullName, staleCount < StaleCountLimit);
            return new ValidationResult(RuleName, "Remove branches, that have not been updated in 90 days or more.", staleCount < StaleCountLimit, DoNothing);
        }

        public Dictionary<string, string> GetConfiguration()
        {
            return new Dictionary<string, string>
            {
                { "ClassName", nameof(HasNotManyStaleBranchesRule) },
                { "RuleName", RuleName },
                { "StaleCountLimit", $"{StaleCountLimit}" },
                { "StaleBranchTimeThreshold", $"{_staleThreshold.ToString("r", CultureInfo.InvariantCulture)}" }
            };
        }

        private Task DoNothing(IGitHubClient client, Repository repository)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, No fix.", nameof(HasNotManyStaleBranchesRule), RuleName);
            return Task.CompletedTask;
        }
    }
}
