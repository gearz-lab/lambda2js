using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lambda2Js.Tests
{
    [TestClass]
    public class UsingCustomMethods
    {
        [TestMethod]
        public void LinqWhere1()
        {
            Expression<Func<JSArray, object>> expr = array => array.RemoveAt(2);

            var extension = new CustomMethods();
            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new LinqMethods(), extension));

            Assert.AreEqual("array.splice(arg_0, 1)", js);
            Assert.AreEqual(2, ((ConstantExpression)extension.Parameters["arg_0"]).Value);
        }

        public class JavascriptMethodNameAttribute : Attribute
        {
            public string Name { get; }

            public object[] PositionalArguments { get; set; }

            public JavascriptMethodNameAttribute(string name)
            {
                Name = name;
            }
        }

        public class CustomMethods : JavascriptConversionExtension
        {
            public Dictionary<string, object> Parameters = new Dictionary<string, object>();
            public override void ConvertToJavascript(JavascriptConversionContext context)
            {
                var methodCallExpression = context.Node as MethodCallExpression;

                var nameAttribute = methodCallExpression?
                    .Method
                    .GetCustomAttributes(typeof(JavascriptMethodNameAttribute),false)
                    .OfType<JavascriptMethodNameAttribute>()
                    .FirstOrDefault();

                if (nameAttribute == null)
                    return;

                context.PreventDefault();

                context.Visitor.Visit(methodCallExpression.Object);
                var javascriptWriter = context.GetWriter();
                javascriptWriter.Write(".");
                javascriptWriter.Write(nameAttribute.Name);
                javascriptWriter.Write("(");

                for (int i = 0; i < methodCallExpression.Arguments.Count; i++)
                {
                    var name = "arg_" + Parameters.Count;
                    if (i != 0)
                        javascriptWriter.Write(", ");
                    javascriptWriter.Write(name);
                    Parameters[name] = methodCallExpression.Arguments[i];
                }
                if (nameAttribute.PositionalArguments != null)
                {
                    for (int i = methodCallExpression.Arguments.Count;
                        i < nameAttribute.PositionalArguments.Length;
                        i++)
                    {
                        if (i != 0)
                            javascriptWriter.Write(", ");
                        context.Visitor.Visit(Expression.Constant(nameAttribute.PositionalArguments[i]));
                    }
                }

                javascriptWriter.Write(")");
            }
        }

        public class JSArray
        {
            [JavascriptMethodName("splice", PositionalArguments = new object[] {0,1})]
            public JSArray RemoveAt(int index)
            {
                throw new NotSupportedException("Never called");
            }
        }
    }
}