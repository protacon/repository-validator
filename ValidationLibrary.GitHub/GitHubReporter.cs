using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace ValidationLibrary.GitHub
{
    public class GitHubReporter
    {
        private ILogger _logger;
        private readonly IGitHubClient _client;

        public GitHubReporter(ILogger logger, IGitHubClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task Report(params ValidationReport[] reports)
        {
            _logger.LogTrace("Reporting {0} reports to github", reports.Count());
            foreach(var report in reports)
            {
                _logger.LogTrace("Reporting for {0}/{1}, url: {2}", report.Owner, report.RepositoryName, report.RepositoryUrl);
                var allIssues = new RepositoryIssueRequest
                {
                    State = ItemStateFilter.All
                };
                var issues = await _client.Issue.GetAllForRepository(report.Owner, report.RepositoryName, allIssues);
                _logger.LogTrace("Found {0} total issues.", issues.Count);
                foreach(var validationResult in report.Results)
                {
                    _logger.LogTrace("Reporting rule {0}, IsValid: {1}", validationResult.RuleName, validationResult.IsValid);
                    var title = CreateIssueTitle(validationResult);
                    var existingIssues = issues.Where(issue => issue.Title == title);
                    _logger.LogTrace("Found {0} existing issues with title {1}", existingIssues.Count(), title);
                    if (validationResult.IsValid)
                    {
                        await CloseIfNeeded(report, validationResult, existingIssues);
                    }
                    else {
                        await CreateOrOpenIssue(report, validationResult, existingIssues);
                    }
                }
            }
        }

        private async Task CloseIfNeeded(ValidationReport report, ValidationResult result, IEnumerable<Issue> existingIssues)
        {
            _logger.LogTrace("Closing issues if needed for rule {0}", result.RuleName);
            foreach(var existingIssue in existingIssues)
            {
                _logger.LogTrace("Checking issue: #{0}, state: {1}", existingIssue.Number, existingIssue.State);
                if (existingIssue.State == ItemState.Open)
                {
                    _logger.LogTrace("Found open issue #{0}", existingIssue.Number);
                    await _client.Issue.Comment.Create(report.Owner, report.RepositoryName, existingIssue.Number, "Issue fixed. Closing issue.");
                    var update = new IssueUpdate(){State = ItemState.Closed};
                    await _client.Issue.Update(report.Owner, report.RepositoryName, existingIssue.Number, update);
                    _logger.LogInformation("Closed issue #{0} for {1}/{2}", existingIssue.Number, report.Owner, report.RepositoryName);
                }
            }
        }

        private async Task CreateOrOpenIssue(ValidationReport report, ValidationResult result, IEnumerable<Issue> existingIssues)
        {
            if (!existingIssues.Any())
            {
                _logger.LogInformation("No issues found, creating new issue.");
                await _client.Issue.Create(report.Owner, report.RepositoryName, CreateIssue(result));
            }
            else
            {
                _logger.LogTrace("Found {0} existing issues.", existingIssues.Count());
                var openIssue = existingIssues.FirstOrDefault(issue => issue.State == ItemState.Open);
                if (openIssue != null)
                {
                    _logger.LogDebug("Found already open issue. No need to reopen issue.");
                    // There is already one open issue for this thing, not opening another.
                    return;
                }
                var closedIssue = existingIssues.FirstOrDefault(issue => issue.State == ItemState.Closed);
                await _client.Issue.Comment.Create(report.Owner, report.RepositoryName, closedIssue.Number, "Issue resurfaced. Reopening issue.");
                
                var update = new IssueUpdate(){ State = ItemState.Open };
                await _client.Issue.Update(report.Owner, report.RepositoryName, closedIssue.Number, update);
                _logger.LogInformation("Reopened issue #{0} for {1}/{2}", closedIssue.Number, report.Owner, report.RepositoryName);
            }
        }

        private static NewIssue CreateIssue(ValidationResult validationResult)
        {
            return new NewIssue(CreateIssueTitle(validationResult))
            {
                Body = validationResult.HowToFix
            };
        }

        private static string CreateIssueTitle(ValidationResult validationResult)
        {
            return $"Automatic repository Validation: {validationResult.RuleName}";
        }
    }
}
