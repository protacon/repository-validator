using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ValidationLibrary.MarkdownGenerator;

namespace Runner
{
    public static class DocumentationUtils
    {
        public static void GenerateDocumentation(ILogger<Program> logger)
        {
            string rulesNamespace = "ValidationLibrary.Rules";
            logger.LogInformation("Generating documentation files for rules in namespace {namespace}", rulesNamespace);
            var validationLibraryAssembly = Assembly.Load(rulesNamespace);
            var types = TypeExtractor.Load(validationLibraryAssembly, rulesNamespace);

            var documentationFolder = "Documentation";

            var homeBuilder = new MarkdownBuilder();
            homeBuilder.Header(1, "References");
            homeBuilder.AppendLine();

            foreach (var g in types.GroupBy(x => x.Namespace).OrderBy(x => x.Key))
            {
                if (!Directory.Exists(documentationFolder)) Directory.CreateDirectory(documentationFolder);
                if (!Directory.Exists(documentationFolder + "\\Rules")) Directory.CreateDirectory(documentationFolder + "\\Rules");

                homeBuilder.Header(2, g.Key);
                homeBuilder.AppendLine();

                foreach (var item in g.OrderBy(x => x.Name))
                {
                    var name = item.Name.Replace("<", "").Replace(">", "").Replace(",", "").Replace(" ", "-").ToLower();
                    homeBuilder.ListLink(MarkdownBuilder.MarkdownCodeQuote(item.Name), $"\\Rules\\{name}");
                    File.WriteAllText(Path.Combine(documentationFolder + "\\Rules", $"{name}.md"), item.ToString());
                }

                homeBuilder.AppendLine();
            }

            File.WriteAllText(Path.Combine(documentationFolder, "rules.md"), homeBuilder.ToString());
            logger.LogInformation("Documentation rules generated");
        }
    }
}