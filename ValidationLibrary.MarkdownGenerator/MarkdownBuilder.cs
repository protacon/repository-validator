using System.Collections.Generic;
using System.Text;

namespace ValidationLibrary.MarkdownGenerator
{
    public class MarkdownBuilder
    {
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        public static string MarkdownCodeQuote(string code)
        {
            return "`" + code + "`";
        }

        public void Append(string text)
        {
            _stringBuilder.Append(text);
        }

        public void AppendLine()
        {
            _stringBuilder.AppendLine();
        }

        public void AppendLine(string text)
        {
            _stringBuilder.AppendLine(text);
        }

        public void Header(int level, string text)
        {
            for (int i = 0; i < level; i++)
            {
                _stringBuilder.Append("#");
            }
            _stringBuilder.Append(" ");
            _stringBuilder.AppendLine(text);
        }

        public void HeaderWithCode(int level, string code)
        {
            for (int i = 0; i < level; i++)
            {
                _stringBuilder.Append("#");
            }
            _stringBuilder.Append(" ");
            CodeQuote(code);
            _stringBuilder.AppendLine();
        }

        public void HeaderWithLink(int level, string text, string url)
        {
            for (int i = 0; i < level; i++)
            {
                _stringBuilder.Append("#");
            }
            _stringBuilder.Append(" ");
            Link(text, url);
            _stringBuilder.AppendLine();
        }

        public void Link(string text, string url)
        {
            _stringBuilder.Append("[");
            _stringBuilder.Append(text);
            _stringBuilder.Append("]");
            _stringBuilder.Append("(");
            _stringBuilder.Append(url);
            _stringBuilder.Append(")");
        }

        public void Image(string altText, string imageUrl)
        {
            _stringBuilder.Append("!");
            Link(altText, imageUrl);
        }

        public void Code(string language, string code)
        {
            _stringBuilder.Append("```");
            _stringBuilder.AppendLine(language);
            _stringBuilder.AppendLine(code);
            _stringBuilder.AppendLine("```");
        }

        public void CodeQuote(string code)
        {
            _stringBuilder.Append("`");
            _stringBuilder.Append(code);
            _stringBuilder.Append("`");
        }

        public void Table(string[] headers, IEnumerable<string[]> items)
        {
            _stringBuilder.Append("| ");
            foreach (var item in headers)
            {
                _stringBuilder.Append(item);
                _stringBuilder.Append(" | ");
            }
            _stringBuilder.AppendLine();

            _stringBuilder.Append("| ");
            foreach (var item in headers)
            {
                _stringBuilder.Append("---");
                _stringBuilder.Append(" | ");
            }
            _stringBuilder.AppendLine();


            foreach (var item in items)
            {
                _stringBuilder.Append("| ");
                foreach (var item2 in item)
                {
                    _stringBuilder.Append(item2);
                    _stringBuilder.Append(" | ");
                }
                _stringBuilder.AppendLine();
            }
            _stringBuilder.AppendLine();
        }

        public void List(string text) // nest zero
        {
            _stringBuilder.Append("- ");
            _stringBuilder.AppendLine(text);
        }

        public void ListLink(string text, string url) // nest zero
        {
            _stringBuilder.Append("- ");
            Link(text, url);
            _stringBuilder.AppendLine();
        }

        public override string ToString()
        {
            return _stringBuilder.ToString();
        }
    }
}