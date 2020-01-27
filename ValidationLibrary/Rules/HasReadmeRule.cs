using Octokit;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ValidationLibrary.Utils;
using System;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// This rule checks that repository has a proper Readme.md
    /// </summary>
    public class HasReadmeRule : FixableRuleBase<HasReadmeRule>, IValidationRule
    {
        public override string RuleName => "Missing Readme.md";
        protected override string PullRequestBody =>
                        "This Pull Request was created by [repository validator](https://github.com/protacon/repository-validator)." + Environment.NewLine +
                        Environment.NewLine +
                        "To prevent automatic validation, see documentation from [repository validator](https://github.com/protacon/repository-validator)." + Environment.NewLine +
                        Environment.NewLine +
                        "DO NOT change the name of this Pull Request. Names are used to identify the Pull Requests created by automation.";
        private const string ReadmeFileName = "README.md";
        private const string FileMode = "100644";
        private readonly string _branchName = "feature/readme-autofix-template";
        private readonly string _prTitle = "Create README.md template.";

        private readonly ILogger<HasReadmeRule> _logger;

        public HasReadmeRule(ILogger<HasReadmeRule> logger, GitUtils gitUtils) : base(logger, gitUtils)
        {
            _logger = logger;
        }

        public override Task Init(IGitHubClient ghClient)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Initialized", nameof(HasReadmeRule), RuleName);
            return Task.CompletedTask;
        }

        protected override Task<Commit> GetCommitAsBase(IGitHubClient client, Repository repository)
        {
            throw new System.NotImplementedException();
        }

        public override async Task<ValidationResult> IsValid(IGitHubClient client, Repository gitHubRepository)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (gitHubRepository is null) throw new ArgumentNullException(nameof(gitHubRepository));

            _logger.LogTrace("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}", nameof(HasReadmeRule), RuleName, gitHubRepository.FullName);
            try
            {
                var readme = await client.Repository.Content.GetReadme(gitHubRepository.Owner.Login, gitHubRepository.Name).ConfigureAwait(false);
                var isValid = !string.IsNullOrWhiteSpace(readme?.Content);
                var fix = isValid ? (Func<IGitHubClient, Repository, Task>)DoNothing : Fix;
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}. Readme has content: {readmeHasContent}", nameof(HasReadmeRule), RuleName, gitHubRepository.FullName, isValid);
                return new ValidationResult(RuleName, "Add README.md file to repository root with content describing this repository.", isValid, fix);
            }
            catch (NotFoundException)
            {
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, No Readme found, validation false.", nameof(HasReadmeRule), RuleName);
                return new ValidationResult(RuleName, "Add README.md file to repository root.", false, Fix);
            }
        }

        /// <summary>
        /// This fix creates a request with README.md template.
        /// </summary>
        /// <param name="client">Github client</param>
        /// <param name="repository">Repository to be fixed</param>
        protected override async Task Fix(IGitHubClient client, Repository repository)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            _logger.LogInformation("Rule {ruleClass} / {ruleName}, performing auto fix.", nameof(HasNewestPtcsJenkinsLibRule), RuleName);
            var latest = await GetCommitAsBase(client, repository).ConfigureAwait(false);
            _logger.LogTrace("Latest commit {sha} with message {message}", latest.Sha, latest.Message);

            var content = GetReadmeTemplateContent();
            var reference = await PushFix(client, repository, latest, content).ConfigureAwait(false);
            await CreateOrOpenPullRequest(_prTitle, client, repository, reference).ConfigureAwait(false);
        }

        private async Task<Reference> PushFix(IGitHubClient client, Repository repository, Commit latest, string jenkinsFile)
        {
            var oldTree = await client.Git.Tree.Get(repository.Owner.Login, repository.Name, latest.Sha).ConfigureAwait(false);
            var newTree = new NewTree
            {
                BaseTree = oldTree.Sha
            };

            BlobReference blobReference = await CreateBlob(client, repository, jenkinsFile).ConfigureAwait(false);
            var treeItem = new NewTreeItem()
            {
                Path = ReadmeFileName,
                Mode = FileMode,
                Type = TreeType.Blob,
                Sha = blobReference.Sha
            };
            newTree.Tree.Add(treeItem);

            var createdTree = await client.Git.Tree.Create(repository.Owner.Login, repository.Name, newTree).ConfigureAwait(false);
            var commit = new NewCommit($"Create README.md template file.", createdTree.Sha, new[] { latest.Sha });
            var commitResponse = await client.Git.Commit.Create(repository.Owner.Login, repository.Name, commit).ConfigureAwait(false);

            var refUpdate = new ReferenceUpdate(commitResponse.Sha);
            return await client.Git.Reference.Update(repository.Owner.Login, repository.Name, $"heads/{_branchName}", refUpdate).ConfigureAwait(false);
        }

        private Task DoNothing(IGitHubClient client, Repository repository)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, No fix.", nameof(HasReadmeRule), RuleName);
            return Task.CompletedTask;
        }

        private static string GetReadmeTemplateContent()
        {
            // TODO: Proper contents
            return "# Project name\nContent text here...";
        }
    }
}