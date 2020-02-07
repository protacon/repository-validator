using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ValidationLibrary.MarkdownGenerator
{
    public static class Beautifier
    {
        public static string BeautifyType(Type type)
        {
            if (type == null) return "";
            if (type == typeof(void)) return "void";
            if (!type.IsGenericType) return type.Name;

            var innerFormat = string.Join(", ", type.GetGenericArguments().Select(x => BeautifyType(x)));
            return Regex.Replace(type.GetGenericTypeDefinition().Name, @"`.+$", "") + "<" + innerFormat + ">";
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
