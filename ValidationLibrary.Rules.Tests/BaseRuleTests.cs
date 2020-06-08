using System;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Octokit;

namespace ValidationLibrary.Tests.Rules
{
    [TestFixture]
    public abstract class BaseRuleTests<T> where T : IValidationRule
    {
        protected User Owner { get; set; }
        protected T _rule;

        protected IGitHubClient MockClient { get; set; }
        protected IRepositoriesClient MockRepositoryClient { get; set; }
        protected IRepositoryContentsClient MockRepositoryContentClient { get; set; }

        protected const string MainBranch = "master";

        [SetUp]
        public async Task Setup()
        {
            Owner = new User(null, null, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, 0, null, 0, 0, false, null, 0, 0, null, "by-pinja", "by-pinja", null, 0, null, 0, 0, 0, null, new RepositoryPermissions(), false, null, null);

            MockClient = Substitute.For<IGitHubClient>();
            MockRepositoryClient = Substitute.For<IRepositoriesClient>();
            MockClient.Repository.Returns(MockRepositoryClient);
            MockRepositoryContentClient = Substitute.For<IRepositoryContentsClient>();
            MockRepositoryClient.Content.Returns(MockRepositoryContentClient);
            OnSetup();
            await _rule.Init(MockClient);
        }

        protected abstract void OnSetup();

        [Test]
        public void RuleName_IsDefined()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(_rule.RuleName));
        }

        [Test]
        public void IsValid_ThrowsWhenRepositoryIsNull()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _rule.IsValid(MockClient, null));
        }

        [Test]
        public void IsValid_ThrowsWhenClientIsNull()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _rule.IsValid(null, CreateRepository("repo")));
        }

        [Test]
        public void GetConfiguration_HasClassName()
        {
            var config = _rule.GetConfiguration();
            Assert.AreEqual(typeof(T).Name, config["ClassName"]);
        }

        private Repository CreateRepository(string name)
        {
            return new Repository(null, null, null, null, null, null, null, 0, null, Owner, name, null, false, null, null, null, false, false, 0, 0, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, null, null, null, null, false, false, false, false, 0, 0, null, null, null, false, 0);
        }
    }
}
