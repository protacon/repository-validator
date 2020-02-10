using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ValidationLibrary.MarkdownGenerator
{
    public class TypeExtractor
    {
        public static MarkdownableType[] Load(Assembly assembly, string namespaceMatch)
        {
            if (string.IsNullOrEmpty(namespaceMatch))
            {
                throw new ArgumentException("Namespace must be defined for assembly loading", nameof(namespaceMatch));
            }
            var namespaceRegex = new Regex(namespaceMatch);

            var xmlPath = Path.Combine(Directory.GetParent(assembly.Location).FullName, Path.GetFileNameWithoutExtension(assembly.Location) + ".xml");

            XmlDocumentComment[] comments = GetXmlDocumentComments(xmlPath, namespaceMatch);
            var commentsLookup = comments.ToLookup(x => x.MemberName);

            var markdownableTypes = assembly.GetTypes()
                .Where(type =>
                    type.IsPublic
                    && !type.IsAbstract
                    && !type.IsInterface
                    && IsRequiredNamespace(type, namespaceRegex))
                .Select(type => new MarkdownableType(type, commentsLookup))
                .ToArray();

            return markdownableTypes;
        }

        private static XmlDocumentComment[] GetXmlDocumentComments(string xmlFileLocation, string namespaceMatch)
        {
            var xmlComments = File.ReadAllText(xmlFileLocation);
            var xmlDocument = XDocument.Parse(xmlComments);
            return VisualStudioDocParser.GetTypeSummaries(xmlDocument, namespaceMatch);
        }

        private static bool IsRequiredNamespace(Type type, Regex regex)
        {
            return regex.IsMatch(type.Namespace ?? string.Empty);
        }
    }
}