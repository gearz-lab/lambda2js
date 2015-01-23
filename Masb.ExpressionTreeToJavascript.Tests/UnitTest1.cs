using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Masb.ExpressionTreeToJavascript.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void LinqWhere()
        {
            Expression<Func<MyClass, object>> expr = x => x.Phones.Where(p => p.DDD == 21);
            var js = expr.CompileToJavascript();
            Assert.AreEqual("System.Linq.Enumerable.Where(Phones,function(p){return p.DDD==21;})", js);
        }

        [TestMethod]
        public void LinqCount()
        {
            Expression<Func<MyClass, object>> expr = x => x.Phones.Count();
            var js = expr.CompileToJavascript();
            Assert.AreEqual("System.Linq.Enumerable.Count(Phones)", js);
        }

        [TestMethod]
        public void LinqFirstOrDefault()
        {
            Expression<Func<MyClass, object>> expr = x => x.Phones.FirstOrDefault(p => p.DDD > 10);
            var js = expr.CompileToJavascript();
            Assert.AreEqual("System.Linq.Enumerable.FirstOrDefault(Phones,function(p){return p.DDD>10;})", js);
        }

        [TestMethod]
        public void ArrayLength()
        {
            Expression<Func<MyClass, object>> expr = x => x.Phones.Length;
            var js = expr.CompileToJavascript();
            Assert.AreEqual("Phones.length", js);
        }

        [TestMethod]
        public void ArrayIndex()
        {
            Expression<Func<MyClass, object>> expr = x => x.Phones[10];
            var js = expr.CompileToJavascript();
            Assert.AreEqual("Phones[10]", js);
        }

        [TestMethod]
        public void ListCount()
        {
            Expression<Func<MyClass, object>> expr = x => ((IList<Phone>)x.Phones).Count;
            var js = expr.CompileToJavascript();
            Assert.AreEqual("Phones.length", js);
        }

        [TestMethod]
        public void DictionaryItem()
        {
            Expression<Func<MyClass, object>> expr = x => x.PhonesByName["Miguel"];
            var js = expr.CompileToJavascript();
            Assert.AreEqual("PhonesByName[\"Miguel\"]", js);
        }

        [TestMethod]
        public void DictionaryContainsKey()
        {
            Expression<Func<MyClass, object>> expr = x => x.PhonesByName.ContainsKey("Miguel");
            var js = expr.CompileToJavascript();
            Assert.AreEqual("PhonesByName.hasOwnProperty(\"Miguel\")", js);
        }

        [TestMethod]
        public void OrElseOperator()
        {
            Expression<Func<MyClass, object>> expr = x => x.PhonesByName["Miguel"].DDD == 32 || x.Phones.Length != 1;
            var js = expr.CompileToJavascript();
            Assert.AreEqual("PhonesByName)[\"Miguel\"].DDD==32||Phones.length!=1", js);
        }

        [TestMethod]
        public void OrOperator()
        {
            Expression<Func<MyClass, object>> expr = x => x.PhonesByName["Miguel"].DDD == 32 | x.Phones.Length != 1;
            var js = expr.CompileToJavascript();
            Assert.AreEqual("PhonesByName[\"Miguel\"].DDD==32|Phones.length!=1", js);
        }
    }

    class MyClass
    {
        public Phone[] Phones { get; set; }
        public Dictionary<string, Phone> PhonesByName { get; set; }
    }

    class Phone
    {
        public int DDD { get; set; }
    }
}
