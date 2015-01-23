using System;
using System.Collections;
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

        [TestMethod]
        public void InlineNewDictionary1()
        {
            Expression<Func<MyClass, object>> expr = x => new Dictionary<string, string>
                {
                    { "name", "Miguel" },
                    { "age", "30" },
                    { "birth-date", "1984-05-04" },
                };
            var js = expr.CompileToJavascript();
            Assert.AreEqual("{name:\"Miguel\",age:\"30\",\"birth-date\":\"1984-05-04\"}", js);
        }

        [TestMethod]
        public void InlineNewDictionary2()
        {
            Expression<Func<MyClass, object>> expr = x => new Hashtable
                {
                    { "name", "Miguel" },
                    { "age", 30 },
                    { "birth-date", "1984-05-04" },
                };
            var js = expr.CompileToJavascript();
            Assert.AreEqual("{name:\"Miguel\",age:30,\"birth-date\":\"1984-05-04\"}", js);
        }

        [TestMethod]
        public void InlineNewObject()
        {
            Expression<Func<MyClass, object>> expr = x => new
            {
                name = "Miguel",
                age = 30,
                birthDate = "1984-05-04",
            };
            var js = expr.CompileToJavascript();
            Assert.AreEqual("{name:\"Miguel\",age:30,birthDate:\"1984-05-04\"}", js);
        }

        [TestMethod]
        public void InlineNewArray1()
        {
            Expression<Func<MyClass, object>> expr = x => new[] { 1, 2, 3 };
            var js = expr.CompileToJavascript();
            Assert.AreEqual("[1,2,3]", js);
        }

        [TestMethod]
        public void InlineNewArray2()
        {
            Expression<Func<MyClass, object>> expr = x => new object[] { 1, 2, 3, "Miguel" };
            var js = expr.CompileToJavascript();
            Assert.AreEqual("[1,2,3,\"Miguel\"]", js);
        }

        [TestMethod]
        public void InlineNewArray3()
        {
            Expression<Func<MyClass, object>> expr = x => new object[] { 1, 2, 3, "Miguel", (Func<int>)(() => 20) };
            var js = expr.CompileToJavascript();
            Assert.AreEqual("[1,2,3,\"Miguel\",function(){return 20;}]", js);
        }

        [TestMethod]
        public void InlineNewArray4()
        {
            Expression<Func<MyClass, object>> expr = x => new[]
                {
                    new[] { 1, 2 },
                    new[] { 3, 4 },
                };
            var js = expr.CompileToJavascript();
            Assert.AreEqual("[[1,2],[3,4]]", js);
        }

        [TestMethod]
        public void InlineNewList1()
        {
            Expression<Func<MyClass, object>> expr = x => new List<int> { 1, 2, 3 };
            var js = expr.CompileToJavascript();
            Assert.AreEqual("[1,2,3]", js);
        }

        [TestMethod]
        public void InlineNewList2()
        {
            Expression<Func<MyClass, object>> expr = x => new ArrayList { 1, 2, 3 };
            var js = expr.CompileToJavascript();
            Assert.AreEqual("[1,2,3]", js);
        }

        [TestMethod]
        public void InlineNewMultipleThings()
        {
            Expression<Func<MyClass, object>> expr = x => new object[]
                {
                    new Dictionary<string, object>
                        {
                            { "name", "Miguel" },
                            { "age", 30 },
                            { "func", (Func<int, double>)(y => (y + 10) * 0.5) },
                            { "list", new List<string> { "a", "b", "c" } },
                        },
                    new
                        {
                            name = "André",
                            age = 30,
                            func = (Func<int, int>)(z => z + 5),
                            list = new List<int> { 10, 20, 30 },
                        }
                };
            var js = expr.CompileToJavascript();
            Assert.AreEqual(@"[{name:""Miguel"",age:30,func:function(y){return (y+10)*0.5;},list:[""a"",""b"",""c""]},{name:""André"",age:30,func:function(z){return z+5;},list:[10,20,30]}]", js);
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
