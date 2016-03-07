using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lambda2Js.Tests
{
    [TestClass]
    public class LinqMethodsTests
    {
        [TestMethod]
        public void LinqWhere1()
        {
            Expression<Func<string[], IEnumerable<string>>> expr = array => array.Where(x => x == "Miguel");

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new LinqMethods(), }));

            Assert.AreEqual("array.filter(function(x){return x==\"Miguel\";})", js);
        }

        [TestMethod]
        public void LinqSelect1()
        {
            Expression<Func<string[], IEnumerable<char>>> expr = array => array.Select(x => x[0]);

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new LinqMethods(), }));

            Assert.AreEqual("array.map(function(x){return x[0];})", js);
        }

        [TestMethod]
        public void LinqToArray1()
        {
            Expression<Func<string[], IEnumerable<string>>> expr = array => array.ToArray();

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new LinqMethods(), }));

            Assert.AreEqual("array.slice()", js);
        }
    }
}