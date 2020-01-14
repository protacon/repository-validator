using NUnit.Framework;
using NSubstitute;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Octokit;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ValidationLibrary.GitHub.Tests
{
    [TestFixture]
    public class GitHubReporterTests
    {
        private const string Prefix = "[Mock testing prefix]";

        private readonly GitHubReportConfig _config = new GitHubReportConfig
        {
            Prefix = Prefix,
            GenericNotice = "Notice of most generic quality"
        };

        private GitHubReporter _reporter;
        private IGitHubClient _mockClient;
        private IIssuesClient _mockIssuesClient;
        private IUsersClient _mockUsersClient;
        private IRepositoriesClient _mockRepositoryClient;

        [SetUp]
        public void Setup()
        {
            var logger = Substitute.For<ILogger>();
            _mockClient = Substitute.For<IGitHubClient>();
            _mockIssuesClient = Substitute.For<IIssuesClient>();
            _mockClient.Issue.Returns(_mockIssuesClient);
            _mockUsersClient = Substitute.For<IUsersClient>();
            _mockClient.User.Returns(_mockUsersClient);
            _mockRepositoryClient = Substitute.For<IRepositoriesClient>();
            _mockClient.Repository.Returns(_mockRepositoryClient);

            var user = new User(null, null, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, 0, null, 0, 0, false, null, 0, 0, null, "LOGIN", null, null, 0, null, 0, 0, 0, null, new RepositoryPermissions(), false, null, null);
            _mockUsersClient.Current().Returns(Task.FromResult(user));

            _mockRepositoryClient.Get(Arg.Any<string>(), Arg.Any<string>()).Returns((args) => Task.FromResult(CreateRepository((string)args[0], (string)args[1], true)));

            _reporter = new GitHubReporter(logger, _mockClient, _config);
        }

        [Test]
        public async Task Report_WithEmptyArrayDoesntCallGitHub()
        {
            _mockClient = null;
            await _reporter.Report(new ValidationReport[0]);
        }

        [Test]
        public async Task Report_SkipsIfIssuesAreNotEnabled()
        {
            var report = new ValidationReport
            {
                Owner = "owner",
                RepositoryName = "repo",
                Results = new ValidationResult[]
                {
                    new ValidationResult("Rule", "how to fix", false, null)
                }
            };

            _mockRepositoryClient.Get(Arg.Any<string>(), Arg.Any<string>()).Returns((args) => Task.FromResult(CreateRepository((string)args[0], (string)args[1], false)));
            await _reporter.Report(new ValidationReport[] { report });
            await _mockIssuesClient.DidNotReceive().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NewIssue>());
        }

        [Test]
        public async Task Report_CreatesNewIssue()
        {
            var report = new ValidationReport
            {
                Owner = "owner",
                RepositoryName = "repo",
                Results = new ValidationResult[]
                {
                    new ValidationResult("Rule", "how to fix", false, null)
                }
            };

            await _reporter.Report(new ValidationReport[] { report });


            await _mockIssuesClient.Received().Create(report.Owner, report.RepositoryName, Arg.Is<NewIssue>(i => i.Title.Contains("Rule") && i.Body.EndsWith(_config.GenericNotice + Environment.NewLine)));
        }

        [Test]
        public async Task Report_SkipsCreationIfThereIsAlreadyOpenIssue()
        {
            var report = new ValidationReport
            {
                Owner = "owner",
                RepositoryName = "repo",
                Results = new ValidationResult[]
                {
                    new ValidationResult("Rule", "how to fix", false, null)
                }
            };

            var issue = CreateIssue(CreateIssueTitle("Rule"), ItemState.Open);
            _mockIssuesClient.GetAllForRepository(report.Owner, report.RepositoryName, Arg.Any<RepositoryIssueRequest>()).Returns(Task.FromResult((IReadOnlyList<Issue>)new List<Issue>() { issue }));

            await _reporter.Report(new ValidationReport[] { report });

            await _mockIssuesClient.DidNotReceive().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NewIssue>());
            await _mockIssuesClient.DidNotReceive().Update(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<IssueUpdate>());
        }

        [Test]
        public async Task Report_ReopensOldIssueIfItExists()
        {
            var report = new ValidationReport
            {
                Owner = "owner",
                RepositoryName = "repo",
                Results = new ValidationResult[]
                {
                    new ValidationResult("Rule", "how to fix", false, null)
                }
            };

            var issue = CreateIssue(CreateIssueTitle("Rule"), ItemState.Closed);
            _mockIssuesClient.GetAllForRepository(report.Owner, report.RepositoryName, Arg.Any<RepositoryIssueRequest>()).Returns(Task.FromResult((IReadOnlyList<Issue>)new List<Issue>() { issue }));

            await _reporter.Report(new ValidationReport[] { report });

            await _mockIssuesClient.DidNotReceive().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NewIssue>());
            await _mockIssuesClient.Received().Update(report.Owner, report.RepositoryName, issue.Number, Arg.Is<IssueUpdate>(update => update.State == ItemState.Open));
        }

        [Test]
        public async Task Report_ClosesOldIssueIfExists()
        {
            var report = new ValidationReport
            {
                Owner = "owner",
                RepositoryName = "repo",
                Results = new ValidationResult[]
                {
                    new ValidationResult("Rule", "how to fix", true, null)
                }
            };

            var issues = Enumerable.Range(0, 10).Select(i =>
            {
                return CreateIssue(CreateIssueTitle("Rule"), ItemState.Open);
            }).ToList();
            _mockIssuesClient.GetAllForRepository(report.Owner, report.RepositoryName, Arg.Any<RepositoryIssueRequest>()).Returns(Task.FromResult((IReadOnlyList<Issue>)issues));

            await _reporter.Report(new ValidationReport[] { report });

            foreach (var issue in issues)
            {
                await _mockIssuesClient.Received().Update(report.Owner, report.RepositoryName, issue.Number, Arg.Is<IssueUpdate>(update => update.State == ItemState.Closed));
            }
        }

        private static string CreateIssueTitle(string ruleName)
        {
            return $"{Prefix} {ruleName}";
        }

        private Issue CreateIssue(string title, ItemState state)
        {
            return new Issue(null, null, null, null, 1, state, title, null, null, null, null, null, null, null, 0, null, null, DateTime.UtcNow, null, 0, null, false, null, null);
        }

        private Repository CreateRepository(string owner, string name, bool hasIssues)
        {
            return new Repository(null, null, null, null, null, null, null, 0, null, null, name, $"{owner}/{name}", null, null, null, false, false, 0, 0, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, null, null, null, null, hasIssues, false, false, false, 0, 0, null, null, null, false);
        }
    }
}