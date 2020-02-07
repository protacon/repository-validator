using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ValidationLibrary.MarkdownGenerator
{
    public static class Beautifier
    {
        public static string BeautifyType(Type t)
        {
            if (t == null) return "";
            if (t == typeof(void)) return "void";
            if (!t.IsGenericType) return t.Name;

            var innerFormat = string.Join(", ", t.GetGenericArguments().Select(x => BeautifyType(x)));
            return Regex.Replace(t.GetGenericTypeDefinition().Name, @"`.+$", "") + "<" + innerFormat + ">";
        }

        public static string ToMarkdownMethodInfo(MethodInfo methodInfo)
        {
            var isExtension = methodInfo.GetCustomAttributes<System.Runtime.CompilerServices.ExtensionAttribute>(false).Any();

            var seq = methodInfo.GetParameters().Select(x =>
            {
                var suffix = x.HasDefaultValue ? (" = " + (x.DefaultValue ?? $"null")) : "";
                return "`" + BeautifyType(x.ParameterType) + "` " + x.Name + suffix;
            });

            return methodInfo.Name + "(" + (isExtension ? "this " : "") + string.Join(", ", seq) + ")";
        }
    }
}
