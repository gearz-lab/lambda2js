using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Lambda2Js.Tests
{
    [TestClass]
    public class CustomClassMethodTests
    {
        public class MyCustomClass
        {
            public MyCustomClass(string name)
            {
                this.Name = name;
            }

            public MyCustomClass()
            {
            }

            public static int GetValue()
            {
                return 1;
            }

            public static int GetValue(int x)
            {
                return x + 1;
            }

            public string Name { get; set; }

            [JavascriptMember(MemberName = "otherName")]
            public string Custom { get; set; }

            [JsonProperty(PropertyName = "otherName2")]
            public string Custom2 { get; set; }
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
                                        context.Write("Xpto").WriteAccessor("GetValue");

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

        [TestMethod]
        public void NewCustomClassAsJson()
        {
            Expression<Func<MyCustomClass>> expr = () => new MyCustomClass { Name = "Miguel" };

            var js = expr.Body.CompileToJavascript(
                new JavascriptCompilationOptions(
                    new MemberInitAsJson(typeof(MyCustomClass))));

            Assert.AreEqual("{Name:\"Miguel\"}", js);
        }

        [TestMethod]
        public void NewCustomClassAsNewOfType()
        {
            Expression<Func<MyCustomClass>> expr = () => new MyCustomClass("Miguel");

            var js = expr.Body.CompileToJavascript();

            Assert.AreEqual("new Lambda2Js.Tests.CustomClassMethodTests.MyCustomClass(\"Miguel\")", js);
        }

        [TestMethod]
        public void NewCustomClassAsNewOfTypeWithMemberInit()
        {
            Expression<Func<MyCustomClass>> expr = () => new MyCustomClass { Name = "Miguel" };

            Exception exception = null;
            try
            {
                expr.Body.CompileToJavascript();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsInstanceOfType(exception, typeof(NotSupportedException), "Exception not thrown.");
        }

        [TestMethod]
        public void CustomMetadata1()
        {
            Expression<Func<MyCustomClass, string>> expr = o => o.Custom;
            var js = expr.CompileToJavascript();
            Assert.AreEqual(@"otherName", js);
        }

        [TestMethod]
        public void CustomMetadata2()
        {
            Expression<Func<MyCustomClass, string>> expr = o => o.Custom2;
            var js = expr.CompileToJavascript();
            Assert.AreEqual(@"otherName2", js);
        }
    }
}