using System;
using System.Collections.Generic;
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

                    var summary = element.Elements("summary").FirstOrDefault()?.Value ?? "";
                    summary = string.Join("  ", summary.Split(new[] { "\r", "\n", "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(y => y.Trim()));

                    return new XmlDocumentComment
                    {
                        MemberName = match.Groups[3].Value,
                        Summary = summary.Trim()
                    };
                })
                .Where(x => x != null)
                .ToArray();
        }

        class Item1EqualityCompaerer<T1, T2> : EqualityComparer<Tuple<T1, T2>>
        {
            public override bool Equals(Tuple<T1, T2> x, Tuple<T1, T2> y)
            {
                return x.Item1.Equals(y.Item1);
            }

            public override int GetHashCode(Tuple<T1, T2> obj)
            {
                return obj.Item1.GetHashCode();
            }
        }
    }
}