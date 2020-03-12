using System;
using System.Text;

namespace ValidationLibrary.MarkdownGenerator
{
    public class MarkdownBuilder
    {
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        public static string MarkdownCodeQuote(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException(nameof(code));

            return $"`{code}`";
        }

        public void AppendLine()
        {
            _stringBuilder.AppendLine();
        }

        public void AppendLine(string text)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));

            _stringBuilder.AppendLine(text);
        }

        public void Header(int level, string text)
        {
            if (level < 1) throw new ArgumentException("Level must be above 0", nameof(level));
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException(nameof(text));

            for (var i = 0; i < level; i++)
            {
                _stringBuilder.Append("#");
            }
            _stringBuilder.Append(" ");
            _stringBuilder.AppendLine(text);
        }

        public void HeaderWithCode(int level, string code)
        {
            if (level < 1) throw new ArgumentException("Level must be above 0", nameof(level));
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException(nameof(code));

            for (var i = 0; i < level; i++)
            {
                _stringBuilder.Append("#");
            }
            _stringBuilder.Append(" ");
            CodeQuote(code);
            _stringBuilder.AppendLine();
        }

        public void Link(string text, string url)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException(nameof(text));
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException(nameof(url));

            _stringBuilder.Append($"[{text}]({url})");
        }

        public void Code(string language, string code)
        {
            if (string.IsNullOrWhiteSpace(language)) throw new ArgumentException(nameof(language));
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException(nameof(code));

            _stringBuilder.Append("```");
            _stringBuilder.AppendLine(language);
            _stringBuilder.AppendLine(code);
            _stringBuilder.AppendLine("```");
        }

        public void CodeQuote(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException(nameof(code));

            _stringBuilder.Append($"`{code}`");
        }

        public void ListLink(string text, string url)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException(nameof(text));
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException(nameof(url));

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
