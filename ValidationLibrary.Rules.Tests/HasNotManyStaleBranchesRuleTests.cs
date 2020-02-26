using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Octokit;
using ValidationLibrary.Rules;

namespace ValidationLibrary.Tests.Rules
{
    public class HasNotManyStaleBranchesRuleTests
    {
        private HasNotManyStaleBranchesRule _rule;
        private IGitHubClient _mockClient;

        [SetUp]
        public void Setup()
        {
            _mockClient = Substitute.For<IGitHubClient>();
            _rule = new HasNotManyStaleBranchesRule(Substitute.For<ILogger<HasNotManyStaleBranchesRule>>());
        }

        [Test]
        public void RuleName_IsDefined()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(_rule.RuleName));
        }

        [Test]
        public async Task IsValid_ReturnsOkIfNotTooManyStaleBranches()
        {
            var repository = CreateRepository("repomen", "owner");
            _mockClient.Repository.Branch.GetAll("owner", "repomen").Returns(CreateBranchList(10, repository));
            _mockClient.Repository.Commit.Get(repository.Id, "mockshalol").Returns(CreateCommit(2, repository));

            var result = await _rule.IsValid(_mockClient, repository);

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsInvalidIfTooManyStaleBranches()
        {
            var repository = CreateRepository("repomen", "owner");
            _mockClient.Repository.Branch.GetAll("owner", "repomen").Returns(CreateBranchList(10, repository));
            _mockClient.Repository.Commit.Get(repository.Id, "mockshalol").Returns(CreateCommit(92, repository));

            var result = await _rule.IsValid(_mockClient, repository);

            Assert.IsFalse(result.IsValid);
        }

        private Repository CreateRepository(string name, string ownerName)
        {
            var owner = new User(null, null, null, 0, null, DateTimeOffset.FromUnixTimeSeconds(0), DateTimeOffset.FromUnixTimeSeconds(0), 0, null, 0, 0, null, null, 0, 0, null, ownerName, null, null, 0, null, 0, 0, 1, null, null, true, null, null);
            return new Repository(
                null, null, null, null, null, null,
                null, 0, null, owner, name, $"{owner}/{name}",
                false, null, null, null, false, false, 0,
                0, null, 0, null, DateTime.UtcNow, DateTime.UtcNow,
                null, null, null, null, false, false,
                false, false, 0, 0, null, null,
                null, false);
        }

        private IReadOnlyList<Branch> CreateBranchList(int count, Repository repository)
        {
            var list = new List<Branch>();

            for (var i = 0; i < count; i++)
            {
                list.Add(new Branch("branch_" + i.ToString(), new GitReference("", "", "", "", "mockshalol", null, repository), false));
            }

            return list.AsReadOnly();
        }

        private GitHubCommit CreateCommit(int offsetDays, Repository repository)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var diff = (DateTime.UtcNow - TimeSpan.FromDays(offsetDays)) - origin;
            var author = new Committer("", "", DateTimeOffset.FromUnixTimeSeconds((long)Math.Floor(diff.TotalSeconds)));
            var commit = new Commit("", "", "", "", "mockshalol", null, repository, "", author, null, null, new List<GitReference>().AsReadOnly(), 0, null);
            return new GitHubCommit("", "", "", "", "mockshalol", null, repository, null, "", commit, null, "", null, new List<GitReference>().AsReadOnly(), null);
        }
    }
}