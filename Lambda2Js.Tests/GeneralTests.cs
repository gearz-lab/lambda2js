using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lambda2Js.Tests
{
    [TestClass]
    public class GeneralTests
    {
        [TestMethod]
        public void FuncWithScopeArgsWithBody()
        {
            Expression<Func<MyClass, object>> expr = x => new { x.Age, x.Name, x.Phones };
            var js = expr.CompileToJavascript(new JavascriptCompilationOptions(JsCompilationFlags.ScopeParameter));
            Assert.AreEqual("function(Age,Name,Phones){return {Age:Age,Name:Name,Phones:Phones};}", js);
        }

        [TestMethod]
        public void FuncWithScopeArgsWoBody()
        {
            Expression<Func<MyClass, object>> expr = x => new { x.Age, x.Name, x.Phones };
            var js = expr.CompileToJavascript(new JavascriptCompilationOptions(JsCompilationFlags.ScopeParameter | JsCompilationFlags.BodyOnly));
            Assert.AreEqual("{Age:Age,Name:Name,Phones:Phones}", js);
        }

        [TestMethod]
        public void FuncWoScopeArgsWithBody()
        {
            Expression<Func<MyClass, object>> expr = x => new { x.Age, x.Name, x.Phones };
            var js = expr.CompileToJavascript(new JavascriptCompilationOptions(0));
            Assert.AreEqual("function(x){return {Age:x.Age,Name:x.Name,Phones:x.Phones};}", js);
        }

        [TestMethod]
        public void FuncWoScopeArgsWoBody()
        {
            Expression<Func<MyClass, object>> expr = x => new { x.Age, x.Name, x.Phones };
            var js = expr.CompileToJavascript(new JavascriptCompilationOptions(JsCompilationFlags.BodyOnly));
            Assert.AreEqual("{Age:x.Age,Name:x.Name,Phones:x.Phones}", js);
        }

        [TestMethod]
        public void LinqWhere()
        {
            Expression<Func<MyClass, object>> expr = x => x.Phones.Where(p => p.DDD == 21);
            var js = expr.CompileToJavascript();
            Assert.AreEqual("System.Linq.Enumerable.Where(Phones,function(p){return p.DDD===21;})", js);
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
            Assert.AreEqual("PhonesByName[\"Miguel\"].DDD===32||Phones.length!==1", js);
        }

        [TestMethod]
        public void OrOperator()
        {
            Expression<Func<MyClass, object>> expr = x => x.PhonesByName["Miguel"].DDD == 32 | x.Phones.Length != 1;
            var js = expr.CompileToJavascript();
            Assert.AreEqual("PhonesByName[\"Miguel\"].DDD===32|Phones.length!==1", js);
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

        [TestMethod]
        public void ArrowFunctionOneArg()
        {
            Expression<Func<int, int>> expr = x => 1024 + x;
            var js = expr.CompileToJavascript(new JavascriptCompilationOptions(0, ScriptVersion.Es60));
            Assert.AreEqual(@"x=>1024+x", js);
        }

        [TestMethod]
        public void ArrowFunctionManyArgs()
        {
            Expression<Func<int, int, int>> expr = (x, y) => y + x;
            var js = expr.CompileToJavascript(new JavascriptCompilationOptions(0, ScriptVersion.Es60));
            Assert.AreEqual(@"(x,y)=>y+x", js);
        }

        [TestMethod]
        public void ArrowFunctionNoArgs()
        {
            Expression<Func<int>> expr = () => 1024;
            var js = expr.CompileToJavascript(new JavascriptCompilationOptions(0, ScriptVersion.Es60));
            Assert.AreEqual(@"()=>1024", js);
        }

        [TestMethod]
        public void Regex1()
        {
            Expression<Func<Regex>> expr = () => new Regex(@"^\d{4}-\d\d-\d\d$", RegexOptions.IgnoreCase);
            var js = expr.Body.CompileToJavascript();
            Assert.AreEqual(@"/^\d{4}-\d\d-\d\d$/gi", js);
        }

        [TestMethod]
        public void Regex1b()
        {
            Expression<Func<Regex>> expr = () => new Regex(@"^\d{4}-\d\d-\d\d$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var js = expr.Body.CompileToJavascript();
            Assert.AreEqual(@"/^\d{4}-\d\d-\d\d$/gim", js);
        }

        [TestMethod]
        public void Regex2()
        {
            Expression<Func<Func<string, Regex>>> expr = () => (p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Multiline));
            var js = expr.Body.CompileToJavascript();
            Assert.AreEqual(@"function(p){return new RegExp(p,'gim');}", js);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void Regex3()
        {
            Expression<Func<Func<string, RegexOptions, Regex>>> expr = () => ((p, o) => new Regex(p, o | RegexOptions.Multiline));
            var js = expr.Body.CompileToJavascript();
            //Assert.AreEqual(@"function(p,o){return new RegExp(p,'g'+o+'m');}", js);
        }

        [TestMethod]
        public void StringCompare1()
        {
            Expression<Func<Func<string, string, int>>> expr = () => ((s, b) => string.Compare(s, b));
            var js = expr.Body.CompileToJavascript();
            Assert.AreEqual(@"function(s,b){return System.String.Compare(s,b);}", js);
        }

        [TestMethod]
        public void StringContains()
        {
            Expression<Func<MyClass, bool>> expr = o => o.Name.Contains("Miguel");
            var js = expr.CompileToJavascript();
            Assert.AreEqual("Name.indexOf(\"Miguel\")>=0", js);
        }

        [TestMethod]
        public void StringContains2()
        {
            Expression<Func<MyClass, bool>> expr = o => "Miguel Angelo Santos Bicudo".Contains(o.Name);
            var js = expr.CompileToJavascript();
            Assert.AreEqual("(\"Miguel Angelo Santos Bicudo\").indexOf(Name)>=0", js);
        }

        [TestMethod]
        public void StringIndexer1()
        {
            Expression<Func<string, char>> expr = s => s[0];
            var js = expr.CompileToJavascript();
            Assert.AreEqual("s[0]", js);
        }

        [TestMethod]
        public void StringIndexer2()
        {
            Expression<Func<string, char>> expr = s => "MASB"[0];
            var js = expr.CompileToJavascript();
            Assert.AreEqual("(\"MASB\")[0]", js);
        }

        [TestMethod]
        public void NumLiteralToString1()
        {
            Expression<Func<string>> expr = () => 1.ToString();
            var js = expr.Body.CompileToJavascript();
            Assert.AreEqual("(1).toString()", js);
        }

        [TestMethod]
        public void NumLiteralToStringD()
        {
            Expression<Func<string>> expr = () => 1.ToString("D");
            var js = expr.Body.CompileToJavascript();
            Assert.AreEqual("(1).toString()", js);
        }

        [TestMethod]
        public void NumLiteralToStringE()
        {
            Expression<Func<string>> expr = () => 1.ToString("E");
            var js = expr.Body.CompileToJavascript();
            Assert.AreEqual("(1).toExponential()", js);
        }

        [TestMethod]
        public void NumLiteralToStringE4()
        {
            Expression<Func<string>> expr = () => 1.ToString("E4");
            var js = expr.Body.CompileToJavascript();
            Assert.AreEqual("(1).toExponential(4)", js);
        }

        [TestMethod]
        public void NumLiteralToStringF()
        {
            Expression<Func<string>> expr = () => 1.ToString("F");
            var js = expr.Body.CompileToJavascript();
            Assert.AreEqual("(1).toFixed()", js);
        }

        [TestMethod]
        public void NumLiteralToStringF4()
        {
            Expression<Func<string>> expr = () => 1.ToString("F4");
            var js = expr.Body.CompileToJavascript();
            Assert.AreEqual("(1).toFixed(4)", js);
        }

        [TestMethod]
        public void NumLiteralToStringG()
        {
            Expression<Func<string>> expr = () => 1.ToString("G");
            var js = expr.Body.CompileToJavascript();
            Assert.AreEqual("(1).toFixed()", js);
        }

        [TestMethod]
        public void NumLiteralToStringG4()
        {
            Expression<Func<string>> expr = () => 1.ToString("G4");
            var js = expr.Body.CompileToJavascript();
            Assert.AreEqual("(1).toFixed(4)", js);
        }

        [TestMethod]
        public void NumLiteralToStringN()
        {
            Expression<Func<string>> expr = () => 1.ToString("N");
            var js = expr.Body.CompileToJavascript();
            Assert.AreEqual("(1).toLocaleString()", js);
        }

        [TestMethod]
        public void NumLiteralToStringX()
        {
            Expression<Func<string>> expr = () => 1.ToString("X");
            var js = expr.Body.CompileToJavascript();
            Assert.AreEqual("(1).toString(16)", js);
        }

        [TestMethod]
        public void StringAdd1()
        {
            Expression<Func<MyClass, string>> expr = o => o.Name + ":" + 10;
            var js = expr.CompileToJavascript();
            Assert.AreEqual(@"Name+"":""+10", js);
        }

        [TestMethod]
        public void StringAdd2()
        {
            Expression<Func<MyClass, string>> expr = o => o.Name + ":" + (o.Age + 10);
            var js = expr.CompileToJavascript();
            Assert.AreEqual(@"Name+"":""+(Age+10)", js);
        }

        [TestMethod]
        public void StringAdd3()
        {
            Expression<Func<MyClass, string>> expr = o => 1.5 + o.Name + ":" + (o.Age + 10);
            var js = expr.CompileToJavascript();
            Assert.AreEqual(@"1.5+Name+"":""+(Age+10)", js);
        }

        [TestMethod]
        public void StringAdd4()
        {
            Expression<Func<MyClass, string>> expr = o => 1.5 + o.Age + ":" + o.Name;
            var js = expr.CompileToJavascript();
            Assert.AreEqual(@"(1.5+Age)+"":""+Name", js);
        }

        [TestMethod]
        public void ConditionalCheck()
        {
          Expression<Func<MyClass, string>> expr = o => o.Phones.Length > 0 && o.Age < 50 ? o.Name + " " + o.Phones.Length + " has phones and is " + o.Age + "yo" : "ignore";
          var js = expr.CompileToJavascript();
          Assert.AreEqual(@"Phones.length>0&&Age<50?Name+"" ""+Phones.length+"" has phones and is ""+Age+""yo"":""ignore""",js);

        }
    }

    class MyClass
    {
        public Phone[] Phones { get; set; }
        public Dictionary<string, Phone> PhonesByName { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    class Phone
    {
        public int DDD { get; set; }
    }
}
