using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Octokit;
using ValidationLibrary.Utils;

namespace ValidationLibrary.Tests.Utils
{
    [TestFixture]
    public class GitUtilsTests
    {
        private GitUtils _gitUtils;

        private IGitHubClient _mockClient;
        private IRepositoryBranchesClient _mockRepositoryBranchesClient;
        private IPullRequestsClient _mockPullRequestsClient;

        [SetUp]
        public void SetUp()
        {
            var mockLogger = Substitute.For<ILogger<GitUtils>>();
            _gitUtils = new GitUtils(mockLogger);

            _mockClient = Substitute.For<IGitHubClient>();
            _mockRepositoryBranchesClient = Substitute.For<IRepositoryBranchesClient>();
            _mockPullRequestsClient = Substitute.For<IPullRequestsClient>();
            _mockClient.Repository.Branch.Returns(_mockRepositoryBranchesClient);
            _mockClient.Repository.PullRequest.Returns(_mockPullRequestsClient);
        }

        [Test]
        public async Task HasOpenPullRequest_CallClientWithCorrectParameters()
        {
            var repository = CreateRepository("test");
            await _gitUtils.HasOpenPullRequest(_mockClient, repository, CreateReference("refs/heads/feature/jenkins-ptcs-library-update"));
            await _mockPullRequestsClient.Received()
                .GetAllForRepository(repository.Owner.Login, repository.Name, Arg.Is<PullRequestRequest>(r => r.State == ItemStateFilter.Open));
        }

        [Test]
        public async Task HasOpenPullRequest_ReturnsFalseIfThereAreNoPullRequests()
        {
            _mockPullRequestsClient.GetAllForRepository(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<PullRequestRequest>()).Returns(new List<PullRequest>());

            Assert.IsFalse(await _gitUtils.HasOpenPullRequest(_mockClient, CreateRepository("test"), CreateReference("ref")));
        }

        [Test]
        public async Task HasOpenPullRequest_ReturnTrueIfThereIsOpenPullRequestWithCorrectReference()
        {
            var pullRequest = CreatePullRequest("feature/jenkins-ptcs-library-update", "");
            _mockPullRequestsClient.GetAllForRepository(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<PullRequestRequest>()).Returns(new List<PullRequest>() { pullRequest });

            var reference = CreateReference("refs/heads/feature/jenkins-ptcs-library-update");
            Assert.IsTrue(await _gitUtils.HasOpenPullRequest(_mockClient, CreateRepository("test"), reference));
        }

        [Test]
        public async Task HasOpenPullRequest_ReturnFalseIfThereNoOpenPullRequestWithCorrectReference()
        {
            var totallyWrong = CreatePullRequest("feature/wrong_branch", "");
            var redHerring = CreatePullRequest("red_herring/feature/jenkins-ptcs-library-update", "");
            _mockPullRequestsClient.GetAllForRepository(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<PullRequestRequest>())
                .Returns(new List<PullRequest>() { totallyWrong, redHerring });

            var reference = CreateReference("refs/heads/feature/jenkins-ptcs-library-update");
            Assert.IsFalse(await _gitUtils.HasOpenPullRequest(_mockClient, CreateRepository("test"), reference));
        }

        private PullRequest CreatePullRequest(string reference, string sha)
        {
            var headReference = CreateGitReference(reference, sha);

            return new PullRequest(0, null, null, null, null, null, null, null, 666, ItemState.Closed, "title", null, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, headReference, null, null, null, null, false, null, null, null, null, 0, 0, 0, 0, 0, null, false, null, null, null);
        }

        private Reference CreateReference(string reference)
        {
            return new Reference(reference, "nodeID", "url", null);
        }

        private GitReference CreateGitReference(string reference, string sha)
        {
            return new GitReference(null, null, null, reference, sha, null, CreateRepository("test"));
        }

        private Repository CreateRepository(string name)
        {
            return new Repository(null, null, null, null, null, null, null, 0, null, CreateUser(), name, null, false, null, null, null, false, false, 0, 0, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, null, null, null, null, false, false, false, false, 0, 0, null, null, null, false);
        }

        private User CreateUser()
        {
            return new User(null, null, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, 0, null, 0, 0, false, null, 0, 0, null, "protacon", "protacon", null, 0, null, 0, 0, 0, null, new RepositoryPermissions(), false, null, null);
        }
    }
}