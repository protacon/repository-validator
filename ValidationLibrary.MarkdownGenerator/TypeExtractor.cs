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
            var xmlPath = Path.Combine(Directory.GetParent(assembly.Location).FullName, Path.GetFileNameWithoutExtension(assembly.Location) + ".xml");

            XmlDocumentComment[] comments = new XmlDocumentComment[0];
            if (File.Exists(xmlPath))
            {
                comments = VisualStudioDocParser.ParseXmlComment(XDocument.Parse(File.ReadAllText(xmlPath)), namespaceMatch);
            }
            var commentsLookup = comments.ToLookup(x => x.ClassName);

            var namespaceRegex =
                !string.IsNullOrEmpty(namespaceMatch) ? new Regex(namespaceMatch) : null;

            var markdownableTypes = new[] { assembly }
                .SelectMany(x =>
                {
                    try
                    {
                        return x.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        return ex.Types.Where(t => t != null);
                    }
                    catch
                    {
                        return Type.EmptyTypes;
                    }
                })
                .Where(x => x != null)
                .Where(x =>
                    x.IsPublic
                    && !typeof(Delegate).IsAssignableFrom(x) && !x.GetCustomAttributes<ObsoleteAttribute>().Any()
                    && !x.IsAbstract
                    && !x.IsInterface)
                .Where(x => IsRequiredNamespace(x, namespaceRegex))
                .Select(x => new MarkdownableType(x, commentsLookup))
                .ToArray();


            return markdownableTypes;
        }

        private static bool IsRequiredNamespace(Type type, Regex regex)
        {
            if (regex == null)
            {
                return true;
            }
            return regex.IsMatch(type.Namespace ?? string.Empty);
        }
    }
}