using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace InstantRpc
{
    internal static class Parser
    {
        internal static TValue Parse<TValue>(string value)
        {
            return (TValue)Parse(typeof(TValue), value);
        }

        internal static object Parse(Type type, string value)
        {
            if (type == typeof(string)) { return value; }
            if (type.IsEnum) { return Enum.Parse(type, value); }
            if (TryParseValueTuple(type, value, out var valueTuple)) { return valueTuple; }

            var parseMethod = type.GetMethod("Parse", new Type[] { typeof(string) });
            if (parseMethod is null || !parseMethod.IsStatic) { throw new NotSupportedException($"static 'Parse(string)' is not implemented on type [{type}]."); }

            return parseMethod.Invoke(null, new[] { value });
        }

        internal static bool CanParse(Type type)
        {
            if (type == typeof(string)) { return true; }
            if (type.IsEnum) { return true; }
            if (type.FullName.StartsWith("System.ValueTuple")) { return true; }

            var parseMethod = type.GetMethod("Parse", new Type[] { typeof(string) });
            return parseMethod != null && parseMethod.IsStatic;
        }

        internal static bool TryParseValueTuple(Type tupleType, string value, out object valueTuple)
        {
            if (!tupleType.FullName.StartsWith("System.ValueTuple"))
            {
                valueTuple = null;
                return false;
            }

            // ex): "(1, 2)" → ["1", "2"]
            var match = Regex.Match(value, @"^\((.*)\)$");
            if (!match.Success) { throw new FormatException($"{value} is not ValueTuple string"); }

            var inner = match.Groups[1].Value;
            // nest is not supported
            var items = inner.Split(',').Select(s => s.Trim()).ToArray();

            var genericArgs = tupleType.GetGenericArguments();
            if (items.Length != genericArgs.Length) { throw new FormatException($"Nested ValueTuple is not supported: {value}"); }

            var parsedItems = new object[genericArgs.Length];
            for (int i = 0; i < genericArgs.Length; i++)
            {
                parsedItems[i] = Parse(genericArgs[i], items[i]);
            }

            valueTuple = Activator.CreateInstance(tupleType, parsedItems);
            return true;
        }
    }
}
