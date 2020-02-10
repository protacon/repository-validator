using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ValidationLibrary.MarkdownGenerator;

namespace Runner
{
    public static class DocumentationUtils
    {
        private const string DocumentationFolder = "Documentation";
        private const string RulesFolder = "Rules";

        public static void GenerateDocumentation(ILogger<Program> logger)
        {
            var types = GetRuleTypes(logger);

            var homeBuilder = new MarkdownBuilder();
            homeBuilder.Header(1, "References");
            homeBuilder.AppendLine();

            MakeSureFolderStructureExists();

            foreach (var g in types.GroupBy(x => x.Namespace).OrderBy(x => x.Key))
            {


                homeBuilder.Header(2, g.Key);
                homeBuilder.AppendLine();

                foreach (var item in g.OrderBy(x => x.Name))
                {
                    var name = item.Name.Replace("<", "").Replace(">", "").Replace(",", "").Replace(" ", "-").ToLower();
                    homeBuilder.ListLink(MarkdownBuilder.MarkdownCodeQuote(item.Name), $"\\{RulesFolder}\\{name}");
                    File.WriteAllText(Path.Combine(DocumentationFolder + "\\" + RulesFolder, $"{name}.md"), item.ToString());
                }

                homeBuilder.AppendLine();
            }

            File.WriteAllText(Path.Combine(DocumentationFolder, "rules.md"), homeBuilder.ToString());
            logger.LogInformation("Documentation rules generated");
        }

        private static MarkdownableType[] GetRuleTypes(ILogger<Program> logger)
        {
            string rulesNamespace = "ValidationLibrary.Rules";
            logger.LogInformation("Generating documentation files for rules in namespace {namespace}", rulesNamespace);
            var validationLibraryAssembly = Assembly.Load(rulesNamespace);
            var types = TypeExtractor.Load(validationLibraryAssembly, rulesNamespace);
            return types;
        }

        private static void MakeSureFolderStructureExists()
        {
            if (!Directory.Exists(DocumentationFolder)) Directory.CreateDirectory(DocumentationFolder);
            if (!Directory.Exists($"{DocumentationFolder}\\{RulesFolder}")) Directory.CreateDirectory($"{DocumentationFolder}\\{RulesFolder}");
        }
    }
}