using System;
using System.Linq;
using System.Text;

namespace ValidationLibrary.MarkdownGenerator
{
    public class MarkdownableType
    {
        private readonly Type _type;
        private readonly ILookup<string, XmlDocumentComment> _commentLookup;

        public string Namespace => _type.Namespace;
        public string Name => _type.Name;

        public MarkdownableType(Type type, ILookup<string, XmlDocumentComment> commentLookup)
        {
            _type = type ?? throw new ArgumentNullException(nameof(type));
            _commentLookup = commentLookup ?? throw new ArgumentNullException(nameof(commentLookup));
        }

        public override string ToString()
        {
            var typeName = _type.Name;

            var mb = new MarkdownBuilder();

            mb.HeaderWithCode(1, typeName);
            mb.AppendLine();

            foreach (var summaryLine in _commentLookup[typeName].FirstOrDefault()?.Summary)
            {
                mb.AppendLine(summaryLine);
            }

            mb.AppendLine($"To ignore {typeName} validation, use following `repository-validator.json`");
            mb.AppendLine();

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("    \"Version\":\"1\",");
            sb.AppendLine("    \"IgnoredRules\": [");
            sb.AppendLine($"        \"{typeName}\"");
            sb.AppendLine("    ]");
            sb.Append("}");

            mb.Code("json", sb.ToString());

            return mb.ToString();
        }
    }
}
