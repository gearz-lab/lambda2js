using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Lambda2Js;

namespace Test.NuGet.Net_v4._0
{
    class Program
    {
        static void Main(string[] args)
        {
            Expression<Func<int, int>> expr = x => 1024 + x;
            var js = expr.CompileToJavascript(new JavascriptCompilationOptions(0, ScriptVersion.Es60));
            //Assert.AreEqual(@"x=>1024+x", js);
        }
    }
}
