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
                var openIssues = new RepositoryIssueRequest
                {
                    State = ItemStateFilter.Open
                };

                var issues = await _client.Issue.GetAllForRepository(report.Owner, report.RepositoryName, openIssues);
                foreach(var validationResult in report.Results)
                {
                    var issueTitle = CreateIssueTitle(validationResult);
                    var existingIssue = issues.FirstOrDefault(issue => issue.Title == issueTitle);
                    if (existingIssue == null && !validationResult.IsValid)
                    {
                        await _client.Issue.Create(report.Owner, report.RepositoryName, CreateIssue(validationResult));
                    }
                    if (existingIssue != null && validationResult.IsValid)
                    {
                        var update = new IssueUpdate()
                        {
                            State = ItemState.Closed
                        };
                        await _client.Issue.Update(report.Owner, report.RepositoryName, existingIssue.Number, update);
                    }
                }
            }
            
        }

        private static NewIssue CreateIssue(ValidationResult validationResult)
        {
            return new NewIssue(CreateIssueTitle(validationResult));
        }

        private static string CreateIssueTitle(ValidationResult validationResult)
        {
            return $"Automatic repository Validation: {validationResult.RuleName}";
        }
    }
}
