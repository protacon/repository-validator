using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace ValidationLibrary.GitHub
{
    public class GitHubReporter
    {
        private readonly GitHubClient _client;

        public GitHubReporter(GitHubClient client)
        {
            _client = client;
        }

        public async Task Report(params ValidationReport[] reports)
        {
            foreach(var report in reports)
            {
                var allIssues = new RepositoryIssueRequest
                {
                    State = ItemStateFilter.All
                };
                var issues = await _client.Issue.GetAllForRepository(report.Owner, report.RepositoryName, allIssues);
                foreach(var validationResult in report.Results)
                {
                    var existingIssues = issues.Where(issue => issue.Title == CreateIssueTitle(validationResult));
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
            foreach(var existingIssue in existingIssues)
            {
                if (existingIssue.State == ItemState.Open)
                {
                    await _client.Issue.Comment.Create(report.Owner, report.RepositoryName, existingIssue.Number, "Issue fixed. Closing issue.");
                    var update = new IssueUpdate(){State = ItemState.Closed};
                    await _client.Issue.Update(report.Owner, report.RepositoryName, existingIssue.Number, update);
                }
            }
        }

        private async Task CreateOrOpenIssue(ValidationReport report, ValidationResult result, IEnumerable<Issue> existingIssues)
        {
            if (!existingIssues.Any())
            {
                await _client.Issue.Create(report.Owner, report.RepositoryName, CreateIssue(result));
            } else
            {
                var openIssue = existingIssues.FirstOrDefault(issue => issue.State == ItemState.Open);
                if (openIssue == null)
                {
                    // There is already one open issue for this thing, not opening another.
                    return;
                }
                var closedIssue = existingIssues.FirstOrDefault(issue => issue.State == ItemState.Closed);
                await _client.Issue.Comment.Create(report.Owner, report.RepositoryName, closedIssue.Number, "Issue resurfaced. Reopening issue.");
                var update = new IssueUpdate(){ State = ItemState.Open };
                await _client.Issue.Update(report.Owner, report.RepositoryName, closedIssue.Number, update);
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
