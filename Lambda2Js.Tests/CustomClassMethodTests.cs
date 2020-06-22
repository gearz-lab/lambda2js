using System;
using System.Collections.Generic;
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

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Custom3 { get; set; }

            public Dictionary<string, object> Dictionary { get; set; }

            public List<string> List { get; set; }
            
            public NestedThing Nested { get; set; }
        }

        public class NestedThing
        {
            public string Name { get; set; }
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
                                    context.PreventDefault();
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
        public void NewClassAsJson()
        {
            Expression<Func<MyCustomClass>> expr = () => new MyCustomClass { Name = "Miguel" };

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    (JsCompilationFlags)0,
                    MemberInitAsJson.ForAllTypes));

            Assert.AreEqual("function(){return {Name:\"Miguel\"};}", js);
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
        public void NewCustomClassWithNestedThingInit()
        {
            Expression<Func<MyCustomClass>> expr = () => new MyCustomClass { Name = "Miguel", Nested = { Name = "Nested" } };

            var js = expr.Body.CompileToJavascript(
                new JavascriptCompilationOptions(
                    new MemberInitAsJson(typeof(MyCustomClass))));

            Assert.AreEqual("{Name:\"Miguel\",Nested:{Name:\"Nested\"}}", js);
        }

        [TestMethod]
        public void NewCustomClassWithNestedThingConstructorFailsWithDisallowedTypeInit()
        {
            Expression<Func<MyCustomClass>> expr = () => new MyCustomClass { Name = "Miguel", Nested = new NestedThing { Name = "Nested" } };

            Exception exception = null;
            try
            {
                expr.Body.CompileToJavascript(
                    new JavascriptCompilationOptions(
                        new MemberInitAsJson(typeof(MyCustomClass))));
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsInstanceOfType(exception, typeof(NotSupportedException), "Exception not thrown.");
        }

        [TestMethod]
        public void NewCustomClassWithNestedThingConstructorInit()
        {
            Expression<Func<MyCustomClass>> expr = () => new MyCustomClass { Name = "Miguel", Nested = new NestedThing { Name = "Nested" } };

            var js = expr.Body.CompileToJavascript(
                new JavascriptCompilationOptions(
                    new MemberInitAsJson(typeof(MyCustomClass), typeof(NestedThing))));

            Assert.AreEqual("{Name:\"Miguel\",Nested:{Name:\"Nested\"}}", js);
        }
        
        [TestMethod]
        public void NewCustomClassWithListInit()
        {
            Expression<Func<MyCustomClass>> expr = () => new MyCustomClass { Name = "Miguel", List = { "One", "Two" } };

            var js = expr.Body.CompileToJavascript(
                new JavascriptCompilationOptions(
                    new MemberInitAsJson(typeof(MyCustomClass))));

            Assert.AreEqual("{Name:\"Miguel\",List:[\"One\",\"Two\"]}", js);
        }

        //This Dictionary constructor is only present for netstandard 2.0 or later (netcoreapp2.0 tests, not netcoreapp1.0)
#if !NETCOREAPP1_1
        [TestMethod]
        public void NewCustomClassWithDictionaryFromListConstructorInit()
        {
            Expression<Func<MyCustomClass>> expr = () => new MyCustomClass
                    { Name = "Miguel", Dictionary = new Dictionary<string, object>(new[] { new KeyValuePair<string, object>("One", 1), new KeyValuePair<string, object>("Two", 2) }) };

            var js = expr.Body.CompileToJavascript(
                new JavascriptCompilationOptions(MemberInitAsJson.ForAllTypes));

            Assert.AreEqual("{Name:\"Miguel\",Dictionary:{\"One\":1,\"Two\":2}}", js);
        }
#endif

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

        [TestMethod]
        public void CustomMetadata3()
        {
            Expression<Func<MyCustomClass, string>> expr = o => o.Custom3;
            var js = expr.CompileToJavascript();
            Assert.AreEqual(@"Custom3", js);
        }

        public class MyClassBase
        {
            [JsonProperty(PropertyName = "type")]
            public string Type { get; set; }
        }

        public class MyClass : MyClassBase
        {
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }
        }

        public delegate object MyFuncDef(string name);

        [TestMethod]
        public void CustomMetadataInMemberInit()
        {
            Expression<MyFuncDef> expr = name => new MyClass
            {
                Type = "xpto",
                Name = name,
            };

            var js = expr.CompileToJavascript(
                    new JavascriptCompilationOptions((JsCompilationFlags)0, MemberInitAsJson.ForAllTypes));

            Assert.AreEqual("function(name){return {type:\"xpto\",name:name};}", js);
        }
    }
}