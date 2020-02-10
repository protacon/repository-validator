using System;
using System.Linq;
using System.Text;

namespace ValidationLibrary.MarkdownGenerator
{
    public class MarkdownableType
    {
        readonly Type type;
        readonly ILookup<string, XmlDocumentComment> commentLookup;

        public string Namespace => type.Namespace;
        public string Name => type.Name;

        public MarkdownableType(Type type, ILookup<string, XmlDocumentComment> commentLookup)
        {
            this.type = type;
            this.commentLookup = commentLookup;
        }

        public override string ToString()
        {
            var typeName = type.Name;

            var mb = new MarkdownBuilder();

            mb.HeaderWithCode(2, typeName);
            mb.AppendLine();

            var desc = commentLookup[type.Name].FirstOrDefault()?.Summary ?? "";
            if (desc != "")
            {
                mb.AppendLine(desc);
            }

            mb.AppendLine();
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

            mb.AppendLine();
            return mb.ToString();
        }
    }
}