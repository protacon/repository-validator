using Octokit;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ValidationLibrary.Utils;
using System;
using System.Net.Http;
using System.Linq;
using Octokit.Helpers;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// This rule checks that repository has a proper Readme.md
    /// </summary>
    public class HasReadmeRule : FixableRuleBase<HasReadmeRule>, IValidationRule
    {
        public override string RuleName => "Missing Readme.md";
        protected override string PullRequestBody =>
                        "This Pull Request provides only a template README.md file for guidance. You should edit the file according to your project needs." +
                        Environment.NewLine + Environment.NewLine +
                        "This Pull Request was created by [repository validator](https://github.com/protacon/repository-validator)." + Environment.NewLine +
                        Environment.NewLine +
                        "To prevent automatic validation, see documentation from [repository validator](https://github.com/protacon/repository-validator)." + Environment.NewLine +
                        Environment.NewLine +
                        "DO NOT change the name of this Pull Request. Names are used to identify the Pull Requests created by automation.";
        private const string ReadmeFileName = "README.md";
        private const string FileMode = "100644";
        private readonly string _branchName = "feature/readme-autofix-template";
        private readonly string _prTitle = "Create README.md template.";
        private readonly Uri _templateFileUrl;
        private readonly ILogger<HasReadmeRule> _logger;
        private string _content;

        public HasReadmeRule(ILogger<HasReadmeRule> logger, GitUtils gitUtils, Uri templateFileUrl = null) : base(logger, gitUtils)
        {
            _logger = logger;
            _templateFileUrl = templateFileUrl ?? new Uri("https://raw.githubusercontent.com/protacon/repository-validator/master/README_TEMPLATE.md");
        }

        public override async Task Init(IGitHubClient ghClient)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Initialized", nameof(HasReadmeRule), RuleName);
            _content = await GetReadmeTemplateContent().ConfigureAwait(false);
        }

        public override async Task<ValidationResult> IsValid(IGitHubClient client, Repository gitHubRepository)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (gitHubRepository is null) throw new ArgumentNullException(nameof(gitHubRepository));

            _logger.LogTrace("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}",
                nameof(HasReadmeRule), RuleName, gitHubRepository.FullName);
            bool HasReadmeWithContent = await this.HasReadmeWithContent(client, gitHubRepository, MainBranch).ConfigureAwait(false);

            _logger.LogDebug("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}. Readme has content: {readmeHasContent}",
                nameof(HasReadmeRule), RuleName, gitHubRepository.FullName, HasReadmeWithContent);
            return new ValidationResult(RuleName, "Add README.md file to repository root with content describing this repository.", HasReadmeWithContent, Fix);
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

            _logger.LogInformation("Rule {ruleClass} / {ruleName}, performing auto fix.", nameof(HasReadmeRule), RuleName);
            var latest = await GetCommitAsBase(_branchName, client, repository).ConfigureAwait(false);
            _logger.LogTrace("Latest commit {sha} with message {message}", latest.Sha, latest.Message);

            bool hasReadmeWithContent = await HasReadmeWithContent(client, repository, latest.Sha).ConfigureAwait(false);
            if (hasReadmeWithContent)
            {
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, Branch {branchName} already had readme, skipping updating existing branch.",
                     nameof(HasReadmeRule), RuleName, _branchName);
                return;
            }

            var reference = await PushFix(client, repository, latest, _content).ConfigureAwait(false);
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

        private async Task<bool> HasReadmeWithContent(IGitHubClient client, Repository repository, string branchName)
        {
            _logger.LogTrace("Rule {ruleClass} / {ruleName}: Retrieving fixed contents for JenkinsFile from branch {branch}", nameof(HasReadmeRule), RuleName, branchName);
            var readme = await GetReadmeFromBranch(client, repository, branchName).ConfigureAwait(false);
            return !string.IsNullOrWhiteSpace(readme?.Content);
        }

        private async Task<RepositoryContent> GetReadmeFromBranch(IGitHubClient client, Repository repository, string branch)
        {
            _logger.LogTrace("Retrieving JenkinsFile for {repositoryName} from branch {branch}", repository.FullName, branch);

            // NOTE: rootContents doesn't contain actual contents, content is only fetched when we fetch the single file later.
            var rootContents = await GetContents(client, repository, branch).ConfigureAwait(false);

            var readmeFile = rootContents.FirstOrDefault(content => content.Name.Equals(ReadmeFileName, StringComparison.InvariantCultureIgnoreCase));
            if (readmeFile == null)
            {
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, No {readmeFileName} found in root.", nameof(HasReadmeRule), RuleName, ReadmeFileName);
                return null;
            }

            var matchingFiles = await client.Repository.Content.GetAllContentsByRef(repository.Owner.Login, repository.Name, readmeFile.Name, branch).ConfigureAwait(false);
            return matchingFiles[0];
        }

        private async Task<string> GetReadmeTemplateContent()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(_templateFileUrl).ConfigureAwait(false);
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Error fetching README template.");
                }
            }

            return "";
        }
    }
}