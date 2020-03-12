using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit;

namespace ValidationLibrary
{
    public class RepositoryValidator : IRepositoryValidator
    {
        private const string ConfigFileName = "repository-validator.json";

        private readonly ILogger<RepositoryValidator> _logger;

        public IValidationRule[] Rules { get; }
        private readonly IGitHubClient _gitHubClient;

        public RepositoryValidator(ILogger<RepositoryValidator> logger, IGitHubClient gitHubClient, IValidationRule[] validationRules)
        {
            Rules = validationRules ?? throw new ArgumentNullException(nameof(validationRules));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));
            logger.LogInformation("Creating {className} with rules: {rules}", nameof(RepositoryValidator), string.Join(", ", Rules.Select(rule => rule.RuleName))); ;
        }

        /// <summary>
        /// Performs necessary initiation for all rules
        /// </summary>
        public async Task Init()
        {
            _logger.LogInformation("Initializing {className}", nameof(RepositoryValidator));
            foreach (var rule in Rules)
            {
                await rule.Init(_gitHubClient).ConfigureAwait(false);
            }
        }

        public async Task<ValidationReport> Validate(Repository gitHubRepository, bool overrideRuleIgnore)
        {
            if (gitHubRepository is null)
            {
                throw new ArgumentNullException(nameof(gitHubRepository));
            }

            _logger.LogTrace("Validating repository {repositoryName}", gitHubRepository.FullName);
            var config = await GetConfig(gitHubRepository).ConfigureAwait(false);

            var filteredRules = overrideRuleIgnore ? Rules : Rules.Where(rule =>
            {
                var name = rule.GetType().Name;
                var isIgnored = config.IgnoredRules.Contains(name);
                _logger.LogTrace("Rule {ruleClass} ignore status: {isIgnored}", name, isIgnored);
                return !isIgnored;
            });

            var validationResults = await Task.WhenAll(filteredRules.Select(async rule => await rule.IsValid(_gitHubClient, gitHubRepository).ConfigureAwait(false))).ConfigureAwait(false);
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
            try
            {
                _logger.LogTrace("Retrieving config for {repositoryName}", gitHubRepository.FullName);
                var contents = await _gitHubClient.Repository.Content.GetAllContents(gitHubRepository.Owner.Login, gitHubRepository.Name, ConfigFileName).ConfigureAwait(false);
                var jsonContent = contents[0].Content;
                var config = JsonConvert.DeserializeObject<ValidationConfiguration>(jsonContent);
                _logger.LogDebug("Configuration found for {repositoryName}. Ignored rules: {rules}", gitHubRepository.FullName, string.Join(",", config.IgnoredRules));
                return config;
            }
            catch (NotFoundException)
            {
                _logger.LogDebug("No {configFileName} found in {repositoryName}. Using default config.", ConfigFileName, gitHubRepository.FullName);
                return new ValidationConfiguration();
            }
        }
    }
}
