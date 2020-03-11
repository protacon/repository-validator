using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace ValidationLibrary.GitHub
{
    public class GitHubReporter : IGitHubReporter
    {
        private readonly ILogger _logger;
        private readonly IGitHubClient _client;
        private readonly GitHubReportConfig _config;

        public GitHubReporter(ILogger logger, IGitHubClient client, GitHubReportConfig config)
        {
            _logger = logger;
            _client = client;
            _config = config;
        }

        public async Task Report(IEnumerable<ValidationReport> reports)
        {
            if (reports is null) throw new ArgumentNullException(nameof(reports));

            _logger.LogTrace("Reporting {count} reports to GitHub.", reports.Count());
            var current = await _client.User.Current().ConfigureAwait(false);
            foreach (var report in reports)
            {
                var repository = await _client.Repository.Get(report.Owner, report.RepositoryName).ConfigureAwait(false);
                if (repository.Archived)
                {
                    _logger.LogWarning("Repository {owner}/{repositoryName} is archived. Skipping reporting.", report.Owner, report.RepositoryName);
                    continue;
                }

                if (!repository.HasIssues)
                {
                    _logger.LogInformation("Repository {owner}/{repositoryName} has issues disabled. Skipping reporting.", report.Owner, report.RepositoryName);
                    continue;
                }

                using (_logger.BeginScope(ScopeParameters(report)))
                {
                    _logger.LogTrace("Reporting for {owner}/{repositoryName}, url: {repositoryUrl}", report.Owner, report.RepositoryName, report.RepositoryUrl);
                    var allIssues = new RepositoryIssueRequest
                    {
                        State = ItemStateFilter.All,
                        Creator = current.Login,
                    };

                    var issues = await _client.Issue.GetAllForRepository(report.Owner, report.RepositoryName, allIssues).ConfigureAwait(false);
                    _logger.LogTrace("Found {count} total issues.", issues.Count);
                    foreach (var validationResult in report.Results)
                    {
                        _logger.LogTrace("Reporting rule {ruleName}, IsValid: {isValid}", validationResult.RuleName, validationResult.IsValid);
                        var title = CreateIssueTitle(validationResult);
                        var existingIssues = issues.Where(issue => issue.Title == title);
                        _logger.LogTrace("Found {count} existing issues with title {title}", existingIssues.Count(), title);
                        if (validationResult.IsValid)
                        {
                            await CloseIfNeeded(report, validationResult, existingIssues).ConfigureAwait(false);
                        }
                        else
                        {
                            await CreateOrOpenIssue(report, validationResult, existingIssues).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private static Dictionary<string, object> ScopeParameters(ValidationReport report)
        {
            return new Dictionary<string, object>
            {
                { nameof(report.Owner), report.Owner },
                { nameof(report.RepositoryName), report.RepositoryName },
                { nameof(report.RepositoryUrl), report.RepositoryUrl }
            };
        }

        private async Task CloseIfNeeded(ValidationReport report, ValidationResult result, IEnumerable<Issue> existingIssues)
        {
            _logger.LogTrace("Closing issues if needed for rule {0}", result.RuleName);
            foreach (var existingIssue in existingIssues)
            {
                _logger.LogTrace("Checking issue: #{0}, state: {1}", existingIssue.Number, existingIssue.State);
                if (existingIssue.State == ItemState.Open)
                {
                    _logger.LogTrace("Found open issue #{0}", existingIssue.Number);
                    await _client.Issue.Comment.Create(report.Owner, report.RepositoryName, existingIssue.Number, $"{_config.Prefix}: Issue fixed. Closing issue.").ConfigureAwait(false);
                    var update = new IssueUpdate() { State = ItemState.Closed };
                    await _client.Issue.Update(report.Owner, report.RepositoryName, existingIssue.Number, update).ConfigureAwait(false);
                    _logger.LogInformation("Closed issue #{0} for {1}/{2}", existingIssue.Number, report.Owner, report.RepositoryName);
                }
            }
        }

        private async Task CreateOrOpenIssue(ValidationReport report, ValidationResult result, IEnumerable<Issue> existingIssues)
        {
            if (!existingIssues.Any())
            {
                _logger.LogInformation("No issues found, creating new issue for {0}/{1}.", report.Owner, report.RepositoryName);
                await _client.Issue.Create(report.Owner, report.RepositoryName, CreateIssue(result)).ConfigureAwait(false);
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
                await _client.Issue.Comment.Create(report.Owner, report.RepositoryName, closedIssue.Number, $"{_config.Prefix}: Issue resurfaced. Reopening issue.").ConfigureAwait(false);

                var update = new IssueUpdate() { State = ItemState.Open };
                await _client.Issue.Update(report.Owner, report.RepositoryName, closedIssue.Number, update).ConfigureAwait(false);
                _logger.LogInformation("Reopened issue #{0} for {1}/{2}", closedIssue.Number, report.Owner, report.RepositoryName);
            }
        }

        private NewIssue CreateIssue(ValidationResult validationResult)
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(validationResult.HowToFix))
            {
                builder.AppendLine("# How to fix");
                builder.AppendLine(validationResult.HowToFix);
            }
            if (!string.IsNullOrWhiteSpace(_config.GenericNotice))
            {
                builder.AppendLine("# About validation");
                builder.AppendLine(_config.GenericNotice);
            }

            return new NewIssue(CreateIssueTitle(validationResult))
            {
                Body = builder.ToString()
            };
        }

        private string CreateIssueTitle(ValidationResult validationResult)
        {
            return $"{_config.Prefix} {validationResult.RuleName}";
        }
    }
}
