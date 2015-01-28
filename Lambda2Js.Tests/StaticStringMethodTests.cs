using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lambda2Js.Tests
{
    [TestClass]
    public class StaticStringMethodTests
    {
        [TestMethod]
        public void StringConcat0()
        {
            Expression<Func<MyClass, string>> expr = o => string.Concat("A", "B");

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new StaticStringMethods() }));

            Assert.AreEqual("''+\"A\"+\"B\"", js);
        }

        [TestMethod]
        public void StringEmpty()
        {
            Expression<Func<MyClass, string>> expr = o => string.Empty;
            var js = expr.CompileToJavascript();
            Assert.AreEqual("\"\"", js);
        }

        [TestMethod]
        public void StringJoin()
        {
            Expression<Func<MyClass, string>> expr = o => string.Join<Phone>(",", o.Phones);

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new StaticStringMethods() }));

            Assert.AreEqual("Phones.join(\",\")", js);
        }

        [TestMethod]
        public void StringJoin2()
        {
            Expression<Func<MyClass, string>> expr =
                o => string.Join(" ", new[] { "Miguel", "Angelo", "Santos", "Bicudo" });

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new StaticStringMethods() }));

            Assert.AreEqual("[\"Miguel\",\"Angelo\",\"Santos\",\"Bicudo\"].join(\" \")", js);
        }

        [TestMethod]
        public void StringConcat()
        {
            Expression<Func<MyClass, string>> expr = o => string.Concat(o.Name, ":", o.Age + 10);

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new StaticStringMethods() }));

            Assert.AreEqual("''+Name+\":\"+(Age+10)", js);
        }

        [TestMethod]
        public void StringConcatContains()
        {
            Expression<Func<MyClass, bool>> expr = o => string.Concat(o.Name, ":", o.Age + 10).Contains("30");

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new StaticStringMethods() }));

            Assert.AreEqual("(''+Name+\":\"+(Age+10)).indexOf(\"30\")>=0", js);
        }
    }
}