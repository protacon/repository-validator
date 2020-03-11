using System;
using System.Text;

namespace ValidationLibrary.MarkdownGenerator
{
    public class MarkdownBuilder
    {
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        public static string MarkdownCodeQuote(string code)
        {
            if (code is null) throw new ArgumentNullException(nameof(code));

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
            if (text is null) throw new ArgumentNullException(nameof(text));

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
            if (code is null) throw new ArgumentNullException(nameof(code));

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
            if (text is null) throw new ArgumentNullException(nameof(text));
            if (url is null) throw new ArgumentNullException(nameof(url));

            _stringBuilder.Append($"[{text}]({url})");
        }

        public void Code(string language, string code)
        {
            if (language is null) throw new ArgumentNullException(nameof(language));
            if (code is null) throw new ArgumentNullException(nameof(code));

            _stringBuilder.Append("```");
            _stringBuilder.AppendLine(language);
            _stringBuilder.AppendLine(code);
            _stringBuilder.AppendLine("```");
        }

        public void CodeQuote(string code)
        {
            if (code is null) throw new ArgumentNullException(nameof(code));

            _stringBuilder.Append("`");
            _stringBuilder.Append(code);
            _stringBuilder.Append("`");
        }

        public void ListLink(string text, string url)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));
            if (url is null) throw new ArgumentNullException(nameof(url));

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
