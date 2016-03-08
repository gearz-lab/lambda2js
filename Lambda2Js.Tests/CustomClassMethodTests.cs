using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lambda2Js.Tests
{
    [TestClass]
    public class CustomClassMethodTests
    {
        public class MyCustomClass
        {
            public static int GetValue()
            {
                return 1;
            }

            public static int GetValue(int x)
            {
                return x + 1;
            }
        }

        public class MyCustomClassMethods : JavascriptConversionExtension
        {
            public override void ConvertToJavascript(JavascriptConversionContext context)
            {
                var methodCall = context.Node as MethodCallExpression;
                if (methodCall != null)
                    if (methodCall.Method.DeclaringType == typeof(MyCustomClass))
                    {
                        switch (methodCall.Method.Name)
                        {
                            case "GetValue":
                            {
                                using (context.Operation(JavascriptOperationTypes.Call))
                                {
                                    using (context.Operation(JavascriptOperationTypes.IndexerProperty))
                                        context.Write("Xpto.GetValue");

                                    context.WriteManyIsolated('(', ')', ',', methodCall.Arguments);
                                }

                                return;
                            }
                        }
                    }
            }
        }

        [TestMethod]
        public void CombiningMultipleExtensions()
        {
            Expression<Func<string>> expr = () => string.Concat(MyCustomClass.GetValue(1) * 2, "XYZ");

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly,
                    new MyCustomClassMethods(),
                    new StaticStringMethods()));

            Assert.AreEqual("''+Xpto.GetValue(1)*2+\"XYZ\"", js);
        }

        [TestMethod]
        public void CombiningMultipleExtensions2()
        {
            Expression<Func<string>> expr = () => string.Concat(MyCustomClass.GetValue());

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly,
                    new MyCustomClassMethods(),
                    new StaticStringMethods()));

            Assert.AreEqual("''+Xpto.GetValue()", js);
        }
    }
}