using System;

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

            var parseMethod = type.GetMethod("Parse", new Type[] { typeof(string) });
            if (parseMethod is null) { throw new NotSupportedException($"'Parse(string)' is not implemented on type [{type}]."); }

            return parseMethod.Invoke(null, new[] { value });
        }

        internal static bool CanParse(Type type)
        {
            if (type == typeof(string)) { return true; }
            if (type.IsEnum) { return true; }

            var parseMethod = type.GetMethod("Parse", new Type[] { typeof(string) });
            return parseMethod != null;
        }
    }
}
