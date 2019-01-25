using System;
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
                    var existingIssue = issues.FirstOrDefault(issue => issue.Title == CreateIssueTitle(validationResult));
                    if (validationResult.IsValid)
                    {
                        await CloseIfNeeded(report, validationResult, existingIssue);
                    }
                    else {
                        await CreateOrOpenIssue(report, validationResult, existingIssue);
                    }
                }
            }
        }

        private async Task CloseIfNeeded(ValidationReport report, ValidationResult result, Issue existingIssue)
        {
            if (existingIssue != null && existingIssue.State == ItemState.Open)
            {
                await _client.Issue.Comment.Create(report.Owner, report.RepositoryName, existingIssue.Number, "Issue fixed. Closing issue.");
                var update = new IssueUpdate(){State = ItemState.Closed};
                await _client.Issue.Update(report.Owner, report.RepositoryName, existingIssue.Number, update);
            }
        }

        private async Task CreateOrOpenIssue(ValidationReport report, ValidationResult result, Issue existingIssue)
        {
            if (existingIssue == null)
            {
                await _client.Issue.Create(report.Owner, report.RepositoryName, CreateIssue(result));
            }
            else if (existingIssue.State == ItemState.Closed)
            {
                await _client.Issue.Comment.Create(report.Owner, report.RepositoryName, existingIssue.Number, "Issue resurfaced. Reopening issue.");
                var update = new IssueUpdate(){ State = ItemState.Open };
                await _client.Issue.Update(report.Owner, report.RepositoryName, existingIssue.Number, update);
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
