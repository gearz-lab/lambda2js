using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lambda2Js.Tests
{
    [TestClass]
    public class StaticMathMethodTests
    {
        [TestMethod]
        public void MathPow()
        {
            Expression<Func<MyClass, double>> expr = o => Math.Pow(o.Age, 2.0);

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new StaticMathMethods() }));

            Assert.AreEqual("Math.pow(Age,2)", js);
        }

        [TestMethod]
        public void MathLog()
        {
            Expression<Func<MyClass, double>> expr = o => Math.Log(o.Age) + 1;

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new StaticMathMethods(), }));

            Assert.AreEqual("Math.log(Age)+1", js);
        }

        [TestMethod]
        public void MathLog2Args()
        {
            Expression<Func<MyClass, double>> expr = o => Math.Log(o.Age, 2.0) + 1;

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new StaticMathMethods(), }));

            Assert.AreEqual("Math.log(Age)/Math.log(2)+1", js);
        }

        [TestMethod]
        public void MathRound()
        {
            Expression<Func<MyClass, double>> expr = o => Math.Round(o.Age / 0.7);

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new StaticMathMethods(true) }));

            Assert.AreEqual("Math.round(Age/0.7)", js);
        }

        [TestMethod]
        public void MathRound2Args()
        {
            Expression<Func<MyClass, double>> expr = o => Math.Round(o.Age / 0.7, 2);

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new StaticMathMethods(true) }));

            Assert.AreEqual("(function(a,b){return Math.round(a*b)/b;})(Age/0.7,Math.pow(10,2))", js);
        }

        [TestMethod]
        public void MathE()
        {
            Expression<Func<MyClass, double>> expr = o => Math.E;

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new StaticMathMethods() }));

            Assert.AreEqual("Math.E", js);
        }

        [TestMethod]
        public void MathPI()
        {
            Expression<Func<MyClass, double>> expr = o => Math.PI;

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new StaticMathMethods() }));

            Assert.AreEqual("Math.PI", js);
        }
    }
}