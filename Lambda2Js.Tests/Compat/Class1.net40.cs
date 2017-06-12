using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TestClassAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TestMethodAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ExpectedException : Attribute
    {
        Type exceptionType;

        public ExpectedException(Type exceptionType)
        {
            this.exceptionType = exceptionType;
        }

        public bool Accept(Exception ex) => ex.GetType() == this.exceptionType;
    }

    public class Assert
    {
        public static Asserter Default;

        public static void AreEqual<T>(T expected, T value)
        {
            Default.AreEqual(expected, value);
        }

        public static void IsInstanceOfType(object value, Type expectedType, string message = null)
        {
            Default.IsInstanceOfType(value, expectedType, message);
        }
    }

    public abstract class Asserter
    {
        public abstract void AreEqual<T>(T expected, T value);

        public abstract void IsInstanceOfType(object value, Type expectedType, string message);
    }
}

namespace System.Reflection
{
    public static class ReflectionExtensions
    {
        public static IEnumerable<T> GetCustomAttributes<T>(this MethodInfo mi)
        {
            return mi.GetCustomAttributes(typeof(T), true).OfType<T>();
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this Type t)
        {
            return t.GetCustomAttributes(typeof(T), true).OfType<T>();
        }
    }
}