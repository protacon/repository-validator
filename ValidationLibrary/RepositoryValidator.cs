using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit;
using ValidationLibrary.Rules;

namespace ValidationLibrary
{
    public class RepositoryValidator
    {
        private const string ConfigFileName = "repository-validator.json";

        private readonly ILogger _logger;

        private readonly IValidationRule[] _rules;
        private readonly GitHubClient _gitHubClient;

        public RepositoryValidator(ILogger logger, GitHubClient gitHubClient)
        {
            _rules = new IValidationRule[]
            {
                new HasDescriptionRule(logger),
                new HasReadmeRule(logger),
                new HasNewestPtcsJenkinsLibRule(logger),
                new HasLicenseRule(logger)
            };

            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _gitHubClient = gitHubClient ?? throw new System.ArgumentNullException(nameof(gitHubClient));
            logger.LogInformation("Creating {className} with rules: {rules}", nameof(RepositoryValidator), string.Join(", ", _rules.Select(rule => rule.RuleName)));;
        }

        /// <summary>
        /// Performs necessary initiation for all rules
        /// </summary>
        public async Task Init()
        {
            _logger.LogInformation("Initializing {className}", nameof(RepositoryValidator));
            foreach (var rule in _rules)
            {
                await rule.Init(_gitHubClient);
            }
        }

        public async Task<ValidationReport> Validate(Repository gitHubRepository)
        {
            _logger.LogTrace("Validating repository {repositoryName}", gitHubRepository.FullName);
            var config = await GetConfig(gitHubRepository);

            var filteredRules = _rules.Where(rule => 
            {
                var name = rule.GetType().Name;
                var isIgnored = config.IgnoredRules.Contains(name);
                _logger.LogTrace("Rule {ruleClass} ignore status: {isIgnored}", name, isIgnored);
                return !isIgnored;
            });

            var validationResults = await Task.WhenAll(filteredRules.Select(async rule => await rule.IsValid(_gitHubClient, gitHubRepository)));
            return new ValidationReport
            {
                Owner = gitHubRepository.Owner.Login,
                Repository = gitHubRepository,
                RepositoryName = gitHubRepository.Name,
                RepositoryUrl = gitHubRepository.HtmlUrl,
                Results = validationResults.ToArray()
            };
        }

        private async Task<ValidationConfiguration> GetConfig(Repository gitHubRepository)
        {
            try {
                _logger.LogTrace("Retrieving config for {repositoryName}", gitHubRepository.FullName);
                var contents = await _gitHubClient.Repository.Content.GetAllContents(gitHubRepository.Owner.Login, gitHubRepository.Name, ConfigFileName);
                var jsonContent = contents.FirstOrDefault().Content;
                var config = JsonConvert.DeserializeObject<ValidationConfiguration>(jsonContent);
                _logger.LogDebug("Configuration found for {repositoryName}. Ignored rules: {rules}", gitHubRepository.FullName, string.Join(",", config.IgnoredRules));
                return config;
            } catch (Octokit.NotFoundException) {
                _logger.LogDebug("No {configFileName} found in {repositoryName}. Using default config.", ConfigFileName, gitHubRepository.FullName);
                return new ValidationConfiguration();
            }
        }
    }
}