using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ValidationLibrary.MarkdownGenerator
{
    public static class VisualStudioDocParser
    {
        public static XmlDocumentComment[] GetTypeSummaries(XDocument xDocument, string namespaceMatch)
        {
            return xDocument.Descendants("member")
                .Where(element => element.Attribute("name").Value.StartsWith($"T:{namespaceMatch}."))
                .Select(element =>
                {
                    var match = Regex.Match(element.Attribute("name").Value, @"(.):(.+)\.([^.()]+)?(\(.+\)|$)");
                    if (!match.Groups[1].Success) return null;

                    var summary = element.Elements("summary").FirstOrDefault()?.Value ?? string.Empty;

                    return new XmlDocumentComment
                    {
                        MemberName = match.Groups[3].Value,
                        Summary = summary.Split(new[] { "\r", "\n", "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(y => y.Trim()).ToArray()
                    };
                })
                .Where(x => x != null)
                .ToArray();
        }
    }
}
