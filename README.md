# Lambda2Js

This is an ExpressionTree (lambda) to Javascript converter.

It is portable, so that you can use it in most environments.

It's purpose is to convert a C# expression tree (from Linq namespace) to a syntatically correct javascript code.

It can be extended to customize the mapping of expressions:

 - support custom static methods, instead of emiting code that would otherwise depend on external javascript
 - support custom types, converting method calls and properties accordingly

It is well tesded, and won't break. **More than 70 tests passing**.

This project uses Semantic versioning.

Installing [NuGet package](https://www.nuget.org/packages/Lambda2Js):

    PM> Install-Package Lambda2Js

Samples
-------

Converting lambda with boolean and numeric operations:

    Expression<Func<MyClass, object>> expr = x => x.PhonesByName["Miguel"].DDD == 32 || x.Phones.Length != 1;
    var js = expr.CompileToJavascript();
    // js = PhonesByName["Miguel"].DDD==32||Phones.length!=1

Converting lambda with LINQ expression, containing a inner lambda:

    Expression<Func<MyClass, object>> expr = x => x.Phones.FirstOrDefault(p => p.DDD > 10);
    var js = expr.CompileToJavascript();
    // js = System.Linq.Enumerable.FirstOrDefault(Phones,function(p){return p.DDD>10;})

Converting lambda with Linq `Select` method:

	Expression<Func<string[], IEnumerable<char>>> expr = array => array.Select(x => x[0]);
    var js = expr.CompileToJavascript(
        new JavascriptCompilationOptions(
            JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
            new[] { new LinqMethods(), }));
    // js = array.map(function(x){return x[0];})

Clone using `ToArray` and targeting ES6:

    Expression<Func<string[], IEnumerable<string>>> expr = array => array.ToArray();
    var js = expr.Body.CompileToJavascript(
        ScriptVersion.Es60,
        new JavascriptCompilationOptions(new LinqMethods()));
    // js = [...array]