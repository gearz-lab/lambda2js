using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TestRunner
{
    internal class MyAssert : Asserter
    {
        public override void AreEqual<T>(T expected, T value)
        {
            var prev = Console.ForegroundColor;
            var eql = EqualityComparer<T>.Default.Equals(expected, value);
            Console.ForegroundColor = eql ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write(eql ? "  OK  " : " FAIL ");
            Console.ForegroundColor = prev;
            Console.WriteLine($"AreEqual({ToLiteral(expected)}, {ToLiteral(value)})");
        }

        private static string ToLiteral<T>(T value)
        {
            if (value == null)
                return "null";

            if (value is string)
            {
                var val = Regex.Replace(
                    value.ToString(),
                    @"[\r\n\t\0]",
                    m => new[] { "\\r", "\\n", "\\t", "\\0" }["\r\n\t\0".IndexOf(m.Value)]);

                return $"\"{val}\"";
            }

            return value.ToString();
        }

        public override void IsInstanceOfType(object value, Type expectedType, string message)
        {
            var prev = Console.ForegroundColor;
            var ok = value != null && expectedType.IsAssignableFrom(value.GetType());
            Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write(ok ? "  OK  " : " FAIL ");
            Console.ForegroundColor = prev;
            Console.WriteLine($"IsInstanceOfType({ToLiteral(value)}, typeof({expectedType.ToString()}), {ToLiteral(message)})");
        }
    }
}