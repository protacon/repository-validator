using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ValidationLibrary.MarkdownGenerator;

namespace Runner
{
    public class DocumentationFileCreator
    {
        private readonly ILogger<DocumentationFileCreator> _logger;

        public DocumentationFileCreator(ILogger<DocumentationFileCreator> logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public void GenerateDocumentation(string outputFolder)
        {
            var types = GetRuleTypes();

            var homeBuilder = new MarkdownBuilder();
            homeBuilder.Header(1, "Rules");
            homeBuilder.AppendLine();
            homeBuilder.AppendLine("This page lists all rules the Repository Validator can validate.");

            MakeSureFolderStructureExists(outputFolder);

            foreach (var group in types.GroupBy(type => type.Namespace).OrderBy(group => group.Key))
            {
                homeBuilder.AppendLine();

                foreach (var item in group.OrderBy(type => type.Name))
                {
                    var name = item.Name;
                    var path = Path.Combine(outputFolder, $"{name}.md");
                    _logger.LogTrace("Creating file to path {path}", path);

                    homeBuilder.ListLink(MarkdownBuilder.MarkdownCodeQuote(item.Name), $"{name}");
                    File.WriteAllText(path, item.ToString());
                }
            }

            File.WriteAllText(Path.Combine(outputFolder, "Rules.md"), homeBuilder.ToString());
            _logger.LogInformation("Documentation rules generated");
        }

        private MarkdownableType[] GetRuleTypes()
        {
            const string RulesNamespace = "ValidationLibrary.Rules";
            _logger.LogInformation("Generating documentation files for rules in namespace {namespace}", RulesNamespace);
            var validationLibraryAssembly = Assembly.Load(RulesNamespace);
            var types = TypeExtractor.Load(validationLibraryAssembly, RulesNamespace);
            return types;
        }

        private static void MakeSureFolderStructureExists(string folder)
        {
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        }
    }
}
